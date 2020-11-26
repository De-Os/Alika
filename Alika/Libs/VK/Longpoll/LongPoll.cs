using Alika.Libs.VK.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Alika.Libs.VK.Longpoll
{
    public class LongPoll
    {
        private enum Updates
        {
            SET_FLAGS = 2,
            RESET_FLAGS = 3,
            NEW_MESSAGE = 4,
            EDIT_MESSAGE = 5,
            READ_IN_MESSAGES = 6,
            READ_OUT_MESSAGES = 7,
            FRIEND_ONLINE = 8,
            FRIEND_OFFLINE = 9,
            RESET_CHAT_FLAGS = 10,
            SET_CHAT_FLAGS = 12,
            DELETE_ALL_MESSAGES = 13,
            CHANGE_MESSAGE = 18,
            RESET_CACHE_MESSAGE = 19,
            EDIT_CHAT = 52,
            TYPING = 63,
            VOICING = 64,
            UNREAD_COUNT_UPDATE = 80,
            CALLBACK_BUTTON_RESPONSE = 119
        }

        private readonly VK vk;
        private bool stop;

        public WebProxy Proxy
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

        public delegate void CallbackEvent(LPEvents.CallbackAction callbackAction);

        public event LPHandler Event;

        public event NewMessage OnNewMessage;

        public event NewMessage OnMessageEdition;

        public event ReadMessage OnReadMessage;

        public event OnlineEvent UserOnline;

        public event OnlineEvent UserOffline;

        public event TypeEvent Typing;

        public event CallbackEvent Callback;

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
            var lp = this.vk.Call<LPResponse>("messages.getLongPollServer", new Dictionary<string, dynamic> {
                {"lp_version", 10},
                {"need_pts", 0}
            });
            this._http = new RestClient("https://" + lp.Server) { Proxy = this._proxy };
            this.request = new RestRequest("");
            this.request.AddParameter("act", "a_check");
            this.request.AddParameter("key", lp.Key);
            this.request.AddParameter("ts", lp.Ts);
            this.request.AddParameter("wait", 25);
            this.request.AddParameter("mode", 2 | 8 | 32 | 64 | 128);
            this.request.AddParameter("version", 10);
            this.ts = lp.Ts;
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
                        var data = this._http.Get(this.request).Content.Replace("<br>", "\n");
                        if (data == null || data.Length == 0)
                        {
                            this.stop = true;
                            this.Generate();
                        }
                        var parsedData = JObject.Parse(data);
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
                            var updates = parsedData["updates"];
                            if (updates.HasValues) Event?.Invoke(updates);
                        }
                    }
                    catch (Exception exc) {
                        System.Diagnostics.Debug.WriteLine(exc);
                    }
                }
            });
        }

        private void CustomEventProcessing(JToken updates)
        {
            if (updates.Count() == 0) return;

            Task.Factory.StartNew(() =>
            {
                var msgs = updates.Where(i => (int)i[0] == (int)Updates.NEW_MESSAGE || (int)i[0] == (int)Updates.EDIT_MESSAGE);
                var basic = msgs.Where(i => i[6]?["source_act"] == null & !i[7].HasValues && (i[2].ToObject<Message.Flags>() & Message.Flags.REPLY_MSG) == Message.Flags.NONE);
                foreach (var msg in basic)
                {
                    var message = new Message(msg);
                    if ((int)msg[0] == (int)Updates.NEW_MESSAGE) this.OnNewMessage?.Invoke(message); else this.OnMessageEdition?.Invoke(message);
                }
                var advanced = msgs.Where(i => !basic.Contains(i)).ToList();
                if (advanced.Count > 0)
                {
                    var messages = this.vk.Messages.GetById(advanced.Select(i => (int)i[1]).ToList()).Items;
                    foreach (var msg in advanced)
                    {
                        var message = messages.Find(i => i.Id == (int)msg[1]);
                        if ((int)msg[0] == (int)Updates.NEW_MESSAGE) this.OnNewMessage?.Invoke(message); else this.OnMessageEdition?.Invoke(message);
                    }
                }
            });

            Task.Factory.StartNew(() =>
            {
                var readStates = updates.Where(i => (int)i[0] == (int)Updates.READ_IN_MESSAGES || (int)i[0] == (int)Updates.READ_OUT_MESSAGES).Select(i => new LPEvents.ReadState
                {
                    PeerId = (int)i[1],
                    MsgId = (int)i[2],
                    Unread = (int)i[3]
                }).ToList();
                if (readStates.Count > 0) foreach (var rs in readStates) this.OnReadMessage?.Invoke(rs);
            });

            Task.Factory.StartNew(() =>
            {
                var onlines = updates.Where(i => (int)i[0] == (int)Updates.FRIEND_ONLINE).Select(i => new LPEvents.OnlineState
                {
                    UserId = -(int)i[1],
                    Timestamp = (int)i[3]
                }).ToList();
                if (onlines.Count > 0) foreach (var online in onlines) this.UserOnline?.Invoke(online);
            });

            Task.Factory.StartNew(() =>
            {
                var offlines = updates.Where(i => (int)i[0] == (int)Updates.FRIEND_OFFLINE).Select(i => new LPEvents.OnlineState
                {
                    UserId = -(int)i[1],
                    Timestamp = (int)i[3]
                }).ToList();
                if (offlines.Count > 0) foreach (var offline in offlines) this.UserOffline?.Invoke(offline);
            });

            Task.Factory.StartNew(() =>
            {
                var typings = updates.Where(i => (int)i[0] == (int)Updates.TYPING).Select(i => new LPEvents.TypeState
                {
                    Peerid = (int)i[1],
                    UserIds = i[2].ToObject<List<int>>()
                }).ToList();
                if (typings.Count > 0) foreach (var type in typings) this.Typing?.Invoke(type);
            });

            Task.Factory.StartNew(() =>
            {
                var callbacks = updates.Where(i => (int)i[0] == (int)Updates.CALLBACK_BUTTON_RESPONSE).Select(i => i[1].ToObject<LPEvents.CallbackAction>()).ToList();
                if (callbacks.Count > 0) foreach (var cb in callbacks) this.Callback?.Invoke(cb);
            });
        }
    }

    public class LPResponse
    {
        [JsonProperty("key")]
        public string Key;

        [JsonProperty("server")]
        public string Server;

        [JsonProperty("ts")]
        public int Ts;
    }

    public class LPEvents
    {
        public struct ReadState
        {
            public int PeerId;
            public int MsgId;
            public int Unread;
        }

        public struct OnlineState
        {
            public int UserId;
            public int Timestamp;
        }

        public struct TypeState
        {
            public List<int> UserIds;
            public int Peerid;
        }

        public struct CallbackAction
        {
            [JsonProperty("owner_id")]
            public int OwnerId;

            [JsonProperty("peer_id")]
            public int PeerId;

            [JsonProperty("event_id")]
            public string EventId;

            [JsonProperty("action")]
            public CBAction Action;

            public struct CBAction
            {
                [JsonProperty("type")]
                public string Type;

                [JsonProperty("text")]
                public string Text;

                [JsonProperty("link")]
                public string Link;

                [JsonProperty("app_id")]
                public int? AppId;

                [JsonProperty("owner_id")]
                public int? OwnerId;

                [JsonProperty("hash")]
                public string Hash;
            }
        }
    }
}