using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    public class Message
    {
        [JsonProperty("id")]
        public int id { get; set; }
        [JsonProperty("peer_id")]
        public int peer_id { get; set; }
        [JsonProperty("from_id")]
        public int from_id { get; set; }
        [JsonProperty("date")]
        public int date { get; set; }
        [JsonProperty("read_state")]
        public int read_state { get; set; }
        [JsonProperty("text")]
        public string text { get; set; }
        [JsonProperty("fwd_messages", NullValueHandling = NullValueHandling.Ignore)]
        public List<Message> fwd_messages { get; set; }
        [JsonProperty("reply_message", NullValueHandling = NullValueHandling.Ignore)]
        public Message reply_message { get; set; }
        [JsonProperty("out", NullValueHandling = NullValueHandling.Ignore)]
        public int isOut { get; set; }
        [JsonProperty("attachments", NullValueHandling = NullValueHandling.Ignore)]
        public List<Attachment> attachments { get; set; }
        [JsonProperty("important", NullValueHandling = NullValueHandling.Ignore)]
        public bool important { get; set; }
        [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
        public string payload { get; set; }
        [JsonProperty("keyboard", NullValueHandling = NullValueHandling.Ignore)]
        public Keyboard keyboard { get; set; }
        [JsonProperty("action", NullValueHandling = NullValueHandling.Ignore)]
        public Action action { get; set; }

        public Message() { }
        public Message(JToken message)
        {
            if (message != null)
            {
                this.id = (int)message[1];
                this.peer_id = (int)message[3];
                this.date = (int)message[4];
                this.text = ((string)message[5]).Replace("<br>", "\n");
                try { this.from_id = (int)message[6]["from"]; } catch { }
            }
        }

        public DateTime GetFormattedDate()
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(this.date).ToLocalTime();
        }

        public class Keyboard
        {
            [JsonProperty("one_time", NullValueHandling = NullValueHandling.Ignore)]
            public bool one_time { get; set; }
            [JsonProperty("inline", NullValueHandling = NullValueHandling.Ignore)]
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
                    [JsonProperty("label", NullValueHandling = NullValueHandling.Ignore)]
                    public string label { get; set; }
                    [JsonProperty("payload", NullValueHandling = NullValueHandling.Ignore)]
                    public string payload { get; set; }

                    [JsonProperty("url", NullValueHandling = NullValueHandling.Ignore)]
                    public string url { get; set; }
                    [JsonProperty("hash", NullValueHandling = NullValueHandling.Ignore)]
                    public string hash { get; set; }
                    [JsonProperty("app_id", NullValueHandling = NullValueHandling.Ignore)]
                    public int app_id { get; set; }
                    [JsonProperty("owner_id", NullValueHandling = NullValueHandling.Ignore)]
                    public int owner_id { get; set; }
                }
            }
        }
        public class Action
        {
            [JsonProperty("type")]
            public string type { get; set; }
            [JsonProperty("member_id", NullValueHandling = NullValueHandling.Ignore)]
            public int member_id { get; set; }
            [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
            public string text { get; set; }
            [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
            public string email { get; set; }
            [JsonProperty("photo", NullValueHandling = NullValueHandling.Ignore)]
            public GetConversationsResponse.ConversationResponse.ConversationInfo.PeerSettings.PeerPhotos photo { get; set; }
        }
    }
}
