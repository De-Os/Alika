using Newtonsoft.Json;

namespace Alika.Libs.VK.Responses
{
    public class UploadServers
    {
        public class PhotoMessages : UploadServerBase
        {
            [JsonProperty("album_id")]
            public int AlbumId;

            [JsonProperty("user_id")]
            public int UserId;

            [JsonProperty("group_id")]
            public int GroupId;

            public class UploadResult
            {
                [JsonProperty("server")]
                public int Server;

                [JsonProperty("photo")]
                public string Photo;

                [JsonProperty("hash")]
                public string Hash;
            }
        }

        public class DocumentMessages : UploadServerBase
        {
            public class UploadResult
            {
                [JsonProperty("file")]
                public string File;
            }

            public class SaveResult
            {
                [JsonProperty("type")]
                public string Type;

                [JsonProperty("doc")]
                public Attachment.DocumentAtt Document;

                [JsonProperty("graffiti")]
                public Attachment.GraffitiAtt Graffiti;

                [JsonProperty("audio_message")]
                public Attachment.AudioMessageAtt AudioMessage;
            }
        }

        public class UploadServerBase
        {
            [JsonProperty("upload_url")]
            public string UploadUrl;
        }
    }
}