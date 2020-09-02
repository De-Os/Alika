﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Alika.Libs.VK.Longpoll
{
    public class LongPoll
    {
        private VK vk;
        private bool stop;

        private RestClient http;
        private RestRequest request;
        private int ts;

        public delegate void LPHandler(JToken lpevent);
        public event LPHandler Event;

        public LongPoll(VK vk)
        {
            this.vk = vk;
            this.stop = true;
            this.Generate();
            _ = this.StartListening();
        }

        public void Generate()
        {
            LPResponse lp = this.vk.Call<LPResponse>("messages.getLongPollServer", new Dictionary<string, dynamic> {
                {"lp_version", 3},
                {"need_pts", 0}
            });
            this.http = new RestClient("https://" + lp.server);
            this.request = new RestRequest("");
            request.AddParameter("act", "a_check");
            request.AddParameter("key", lp.key);
            request.AddParameter("ts", lp.ts);
            request.AddParameter("wait", 25);
            request.AddParameter("mode", 2);
            request.AddParameter("version", 3);
            this.ts = lp.ts;
            this.stop = false;
        }

        internal async Task StartListening()
        {
            await Task.Factory.StartNew(() =>
            {
                while (!this.stop)
                {
                    try
                    {
                        this.request.AddParameter("ts", this.ts);
                        string data = this.http.Get(this.request).Content;
                        if (data == null || data.Length == 0)
                        {
                            this.stop = true;
                            this.Generate();
                        }
                        JObject parsedData = JObject.Parse(data);
                        if (parsedData.ContainsKey("failed"))
                        {
                            if ((int)parsedData["failed"] != 1)
                            {
                                this.stop = true;
                                this.Generate();
                            }
                            else this.ts = (int)parsedData["ts"];
                        }
                        else
                        {
                            this.ts = (int)parsedData["ts"];
                            JToken updates = parsedData["updates"];
                            if (updates.HasValues) Event?.Invoke(updates);
                        }
                    }
                    catch { }
                }
            });
        }
    }

    public class LPResponse
    {
        [JsonProperty("key")]
        public string key { get; set; }
        [JsonProperty("server")]
        public string server { get; set; }
        [JsonProperty("ts")]
        public int ts { get; set; }
    }
}