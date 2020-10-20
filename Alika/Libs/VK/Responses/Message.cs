using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    public class Message
    {
        public enum Flags
        {
            NONE = 0,
            UNREAD = 1,
            OUTBOX = 2,
            IMPORTANT = 8,
            CHAT = 16,
            FRIENDS = 32,
            SPAM = 64,
            DELETED = 128,
            AUDIO_LISTENED = 4096,
            CHAT2 = 8192,
            CANCEL_SPAM = 32768,
            HIDDEN = 65536,
            DELETED_ALL = 131072,
            CHAT_IN = 524288,
            SILENT = 1048576,
            REPLY_MSG = 2097152
        }

        [JsonProperty("id")]
        public int id { get; set; }
        [JsonProperty("peer_id")]
        public int peer_id { get; set; }
        [JsonProperty("from_id")]
        public int from_id { get; set; }
        [JsonProperty("date")]
        public int date { get; set; }
        [JsonProperty("read_state")]
        public int read_state { get; set; } = 0;
        [JsonProperty("text")]
        public string text { get; set; }
        [JsonProperty("fwd_messages")]
        public List<Message> fwd_messages { get; set; }
        [JsonProperty("reply_message")]
        public Message reply_message { get; set; }
        [JsonProperty("out")]
        public int isOut { get; set; } = 0;
        [JsonProperty("attachments")]
        public List<Attachment> attachments { get; set; }
        [JsonProperty("important")]
        public bool important { get; set; }
        [JsonProperty("payload")]
        public string payload { get; set; }
        [JsonProperty("keyboard")]
        public Keyboard keyboard { get; set; }
        [JsonProperty("action")]
        public Action action { get; set; }
        [JsonProperty("update_time")]
        public int update_time { get; set; }

        public Message() { }
        public Message(JToken message)
        {
            if (message != null)
            {
                this.id = (int)message[1];
                this.peer_id = (int)message[3];
                this.from_id = this.peer_id;
                this.date = (int)message[4];
                this.text = ((string)message[5]).Replace("<br>", "\n");
                if (message[6] != null && message[6].HasValues)
                {
                    var additions = message[6];
                    if (additions["keyboard"] != null) this.keyboard = additions["keyboard"].ToObject<Keyboard>();
                    if (additions["payload"] != null) this.payload = (string)additions["payload"];
                    if (additions["from"] != null) this.from_id = int.Parse((string)additions["from"]);
                }

                if ((message[2].ToObject<Flags>() & Flags.OUTBOX) != Flags.NONE) this.from_id = App.vk.user_id;
            }
        }

        public DateTime GetFormattedDate() => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(this.date).ToLocalTime();

        public class Keyboard
        {
            [JsonProperty("one_time")]
            public bool one_time { get; set; }
            [JsonProperty("inline")]
            public bool inline { get; set; }
            [JsonProperty("buttons")]
            public List<List<Button>> buttons { get; set; }
            public class Button
            {
                [JsonProperty("color")]
                public string color { get; set; }
                [JsonProperty("action")]
                public Action action { get; set; }

                public class Action
                {
                    [JsonProperty("type")]
                    public string type { get; set; }
                    [JsonProperty("label")]
                    public string label { get; set; }
                    [JsonProperty("payload")]
                    public string payload { get; set; }

                    [JsonProperty("url")]
                    public string url { get; set; }
                    [JsonProperty("hash")]
                    public string hash { get; set; }
                    [JsonProperty("app_id")]
                    public int app_id { get; set; }
                    [JsonProperty("owner_id")]
                    public int owner_id { get; set; }
                }
            }
        }
        public class Action
        {
            [JsonProperty("type")]
            public string type { get; set; }
            [JsonProperty("member_id")]
            public int member_id { get; set; }
            [JsonProperty("text")]
            public string text { get; set; }
            [JsonProperty("email")]
            public string email { get; set; }
            [JsonProperty("photo")]
            public GetConversationsResponse.ConversationResponse.ConversationInfo.PeerSettings.PeerPhotos photo { get; set; }
        }
    }
}
