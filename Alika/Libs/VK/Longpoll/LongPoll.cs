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
        public delegate void NewMessage(Message message);
        public delegate void ReadMessage(LPEvents.ReadState readState);
        public delegate void OnlineEvent(LPEvents.OnlineState onlineState);
        public delegate void TypeEvent(LPEvents.TypeState typeState);
        public event LPHandler Event;
        public event NewMessage OnNewMessage;
        public event NewMessage OnMessageEdition;
        public event ReadMessage OnReadMessage;
        public event OnlineEvent UserOnline;
        public event OnlineEvent UserOffline;
        public event TypeEvent Typing;

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
            request.AddParameter("wait", 50);
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
                var msgs = updates.Where(i => (int)i[0] == 4 || (int)i[0] == 5);
                foreach (var msg in msgs.Where(i => !i[7].HasValues))
                {
                    if ((int)msg[0] == 4) this.OnNewMessage?.Invoke(new Message(msg)); else this.OnMessageEdition?.Invoke(new Message(msg));
                }
                var msg_ids = msgs.Where(i => i[7].HasValues).Select(i => (int)i[1]).ToList();
                if (msg_ids.Count > 0)
                {
                    var messages = this.vk.Messages.GetById(msg_ids).messages;
                    foreach (var msg in messages)
                    {
                        if (msgs.Any(i => (int)i[1] == msg.id && (int)i[0] == 4))
                        {
                            this.OnNewMessage?.Invoke(msg);
                        }
                        else this.OnMessageEdition?.Invoke(msg);
                    }
                }
            });

            Task.Factory.StartNew(() =>
            {
                var readStates = updates.Where(i => (int)i[0] == 6 || (int)i[1] == 7).Select(i => new LPEvents.ReadState
                {
                    peer_id = (int)i[1],
                    msg_id = (int)i[2]
                }).ToList();
                if (readStates.Count > 0) foreach (var rs in readStates) this.OnReadMessage?.Invoke(rs);
            });

            Task.Factory.StartNew(() =>
            {
                var onlines = updates.Where(i => (int)i[0] == 8).Select(i => new LPEvents.OnlineState
                {
                    user_id = -(int)i[1],
                    timestamp = (int)i[3]
                }).ToList();
                if (onlines.Count > 0) foreach (var online in onlines) this.UserOnline?.Invoke(online);
            });

            Task.Factory.StartNew(() =>
            {
                var offlines = updates.Where(i => (int)i[0] == 9).Select(i => new LPEvents.OnlineState
                {
                    user_id = -(int)i[1],
                    timestamp = (int)i[3]
                }).ToList();
                if (offlines.Count > 0) foreach (var offline in offlines) this.UserOffline?.Invoke(offline);
            });

            Task.Factory.StartNew(() =>
            {
                var typings = updates.Where(i => (int)i[0] == 63).Select(i => new LPEvents.TypeState
                {
                    user_ids = i[1].ToObject<List<int>>(),
                    peer_id = (int)i[2]
                }).ToList();
                if (typings.Count > 0) foreach (var type in typings) this.Typing?.Invoke(type);
            });

            Task.Factory.StartNew(() =>
            {
                var typings = updates.Where(i => (int)i[0] == 62).Select(i => new LPEvents.TypeState
                {
                    user_ids = new List<int> { (int)i[1] },
                    peer_id = (int)i[2] + Limits.Messages.PEERSTART
                }).ToList();
                if (typings.Count > 0) foreach (var type in typings) this.Typing?.Invoke(type);
            });

            Task.Factory.StartNew(() =>
            {
                var typings = updates.Where(i => (int)i[0] == 61).Select(i => new LPEvents.TypeState
                {
                    user_ids = new List<int> { (int)i[1] },
                    peer_id = (int)i[1]
                }).ToList();
                if (typings.Count > 0) foreach (var type in typings) this.Typing?.Invoke(type);
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
        public struct ReadState
        {
            public int peer_id;
            public int msg_id;
        }

        public struct OnlineState
        {
            public int user_id;
            public int timestamp;
        }

        public struct TypeState
        {
            public List<int> user_ids;
            public int peer_id;
        }
    }
}
