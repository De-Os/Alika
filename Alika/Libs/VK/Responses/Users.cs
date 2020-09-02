using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Alika.Libs.VK.Responses
{
    public class User
    {
        [JsonProperty("id"), Key]
        public int user_id { get; set; }
        [JsonProperty("first_name")]
        public string first_name { get; set; }
        [JsonProperty("last_name")]
        public string last_name { get; set; }
        [JsonProperty("verified", NullValueHandling = NullValueHandling.Ignore)]
        public int verified { get; set; }
        [JsonProperty("photo_50", NullValueHandling = NullValueHandling.Ignore)]
        public string photo_50 { get; set; }
        [JsonProperty("photo_100", NullValueHandling = NullValueHandling.Ignore)]
        public string photo_100 { get; set; }
        [JsonProperty("photo_200", NullValueHandling = NullValueHandling.Ignore)]
        public string photo_200 { get; set; }
        [JsonProperty("screen_name", NullValueHandling = NullValueHandling.Ignore)]
        public string screen_name { get; set; }
        [JsonProperty("online", NullValueHandling = NullValueHandling.Ignore)]
        public int online { get; set; }
        [JsonProperty("online_mobile", NullValueHandling = NullValueHandling.Ignore)]
        public int online_mobile { get; set; }
        [JsonProperty("online_info", NullValueHandling = NullValueHandling.Ignore)]
        public OnlineInfo online_info { get; set; }

        public class OnlineInfo
        {
            [JsonProperty("visible")]
            public bool visible { get; set; }
            [JsonProperty("last_seen")]
            public int last_seen { get; set; }
            [JsonProperty("is_online")]
            public bool is_online { get; set; }
            [JsonProperty("is_mobile")]
            public bool is_mobile { get; set; }

        }
    }
}
