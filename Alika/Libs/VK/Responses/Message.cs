using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Web;

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
        public int Id;

        [JsonProperty("peer_id")]
        public int PeerId;

        [JsonProperty("from_id")]
        public int FromId;

        [JsonProperty("date")]
        public int Date;

        [JsonProperty("read_state")]
        public int ReadState = 0;

        [JsonProperty("text")]
        public string Text;

        [JsonProperty("fwd_messages")]
        public List<Message> FwdMessages;

        [JsonProperty("reply_message")]
        public Message ReplyMessage;

        [JsonProperty("out")]
        public int IsOut = 0;

        [JsonProperty("attachments")]
        public List<Attachment> Attachments;

        [JsonProperty("important")]
        public bool Important;

        [JsonProperty("payload")]
        public string Payload;

        [JsonProperty("keyboard")]
        public MsgKeyboard Keyboard;

        [JsonProperty("action")]
        public MsgAction Action;

        [JsonProperty("update_time")]
        public int UpdateTime;

        public Message()
        {
        }

        public Message(JToken message)
        {
            if (message != null)
            {
                this.Id = (int)message[1];
                this.PeerId = (int)message[3];
                this.FromId = this.PeerId;
                this.Date = (int)message[4];
                this.Text = HttpUtility.HtmlDecode((string)message[5]);
                if (message[6] != null && message[6].HasValues)
                {
                    var additions = message[6];
                    if (additions["keyboard"] != null) this.Keyboard = additions["keyboard"].ToObject<MsgKeyboard>();
                    if (additions["payload"] != null) this.Payload = (string)additions["payload"];
                    if (additions["from"] != null) this.FromId = int.Parse((string)additions["from"]);
                }

                if ((message[2].ToObject<Flags>() & Flags.OUTBOX) != Flags.NONE) this.FromId = App.VK.UserId;
            }
        }

        public class MsgKeyboard
        {
            [JsonProperty("one_time")]
            public bool OneTime;

            [JsonProperty("inline")]
            public bool Inline;

            [JsonProperty("buttons")]
            public List<List<Button>> Buttons;

            public class Button
            {
                [JsonProperty("color")]
                public string Color;

                [JsonProperty("action")]
                public BtnAction Action;

                public class BtnAction
                {
                    [JsonProperty("type")]
                    public string Type;

                    [JsonProperty("label")]
                    public string Label;

                    [JsonProperty("payload")]
                    public string Payload;

                    [JsonProperty("url")]
                    public string Url;

                    [JsonProperty("hash")]
                    public string Hash;

                    [JsonProperty("app_id")]
                    public int AppId;

                    [JsonProperty("owner_id")]
                    public int OwnerId;
                }
            }
        }

        public class MsgAction
        {
            [JsonProperty("type")]
            public string Type;

            [JsonProperty("member_id")]
            public int MemberId;

            [JsonProperty("text")]
            public string Text;

            [JsonProperty("email")]
            public string EMail;

            [JsonProperty("photo")]
            public ConversationInfo.PeerSettings.PeerPhotos Photo;
        }
    }

    public class GetImportantMessagesResponse
    {
        [JsonProperty("messages")]
        public ItemsResponse<Message> Messages;

        [JsonProperty("profiles")]
        public List<User> Profiles;

        [JsonProperty("groups")]
        public List<Group> Groups;

        [JsonProperty("conversations")]
        public List<ConversationInfo> Conversations;
    }
}