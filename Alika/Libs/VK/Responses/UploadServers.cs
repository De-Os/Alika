using Newtonsoft.Json;

namespace Alika.Libs.VK.Responses
{
    public class UploadServers
    {
        public class PhotoMessages
        {
            [JsonProperty("album_id")]
            public int album_id { get; set; }
            [JsonProperty("upload_url")]
            public string upload_url { get; set; }
            [JsonProperty("user_id")]
            public int user_id { get; set; }
            [JsonProperty("group_id")]
            public int group_id { get; set; }

            public class UploadResult
            {
                [JsonProperty("server")]
                public int server { get; set; }
                [JsonProperty("photo")]
                public string photo { get; set; }
                [JsonProperty("hash")]
                public string hash { get; set; }
            }
        }
        public class DocumentMessages
        {
            [JsonProperty("upload_url")]
            public string upload_url { get; set; }
            public class UploadResult
            {
                [JsonProperty("file")]
                public string file { get; set; }
            }

            public class SaveResult
            {
                [JsonProperty("type")]
                public string type { get; set; }
                [JsonProperty("doc")]
                public Attachment.Document document { get; set; }
                [JsonProperty("graffiti")]
                public Attachment.Graffiti graffiti { get; set; }
                [JsonProperty("audio_message")]
                public Attachment.AudioMessage audio_message { get; set; }

            }
        }

        public class ChatPhoto
        {
            [JsonProperty("upload_url")]
            public string upload_url { get; set; }
        }
    }
}
