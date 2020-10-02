using Alika.Libs.VK.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Alika.Libs.VK.Longpoll
{
    public class LongPoll
    {
        private readonly VK vk;
        private bool stop;

        public WebProxy proxy
        {
            get
            {
                return this._http.Proxy as WebProxy;
            }
            set
            {
                this._proxy = value;
                this._http.Proxy = value;
            }
        }

        private WebProxy _proxy;
        private RestClient _http;
        private RestRequest request;
        private int ts;

        public delegate void LPHandler(JToken lpevent);
        public event LPHandler Event;
        public delegate void NewMessage(Message message);
        public delegate void ReadMessage(LPEvents.ReadState readState);
        public event NewMessage OnNewMessage;
        public event NewMessage OnMessageEdition;
        public event ReadMessage OnReadMessage;

        public LongPoll(VK vk)
        {
            this.vk = vk;
            this.stop = true;
            this.Generate();
            this.Event += this.CustomEventProcessing;
            _ = this.StartListening();
        }

        public void Generate()
        {
            LPResponse lp = this.vk.Call<LPResponse>("messages.getLongPollServer", new Dictionary<string, dynamic> {
                {"lp_version", 3},
                {"need_pts", 0}
            });
            this._http = new RestClient("https://" + lp.server) { Proxy = this._proxy };
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
                        this.request.AddOrUpdateParameter("ts", this.ts);
                        string data = this._http.Get(this.request).Content;
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

        private void CustomEventProcessing(JToken updates)
        {
            if (updates.Count() == 0) return;

            Task.Factory.StartNew(() =>
            {
                List<int> msg_ids = new List<int>();
                foreach (JToken update in updates)
                {
                    if ((int)update[0] == 4) msg_ids.Add((int)update[1]);
                }
                if (msg_ids.Count > 0) foreach (Message msg in this.vk.Messages.GetById(msg_ids).messages) this.OnNewMessage?.Invoke(msg);
            });

            Task.Factory.StartNew(() =>
            {
                var readStates = new List<LPEvents.ReadState>();
                foreach (JToken update in updates)
                {
                    if ((int)update[0] == 6 || (int)update[0] == 7) readStates.Add(new LPEvents.ReadState
                    {
                        peer_id = (int)update[1],
                        msg_id = (int)update[2]
                    });
                }
                if (readStates.Count > 0) foreach (var rs in readStates) this.OnReadMessage?.Invoke(rs);
            });

            Task.Factory.StartNew(() =>
            {
                var msg_ids = new List<int>();
                foreach (JToken update in updates)
                {
                    if ((int)update[0] == 5) msg_ids.Add((int)update[1]);
                }
                if (msg_ids.Count > 0) foreach (Message msg in this.vk.Messages.GetById(msg_ids).messages) this.OnMessageEdition?.Invoke(msg);
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

    public class LPEvents
    {
        public class ReadState
        {
            public int peer_id { get; set; }
            public int msg_id { get; set; }
        }
    }
}
