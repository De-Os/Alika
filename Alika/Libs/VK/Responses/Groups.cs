using Newtonsoft.Json;

namespace Alika.Libs.VK.Responses
{
    public class Group
    {
        private int _id;
        [JsonProperty("id")]
        public int id
        {
            get
            {
                return this._id;
            }
            set
            {
                this._id = value < 0 ? value : -value;
            }
        }
        [JsonProperty("name")]
        public string name { get; set; }
        [JsonProperty("screen_name")]
        public string screen_name { get; set; }
        [JsonProperty("verified", NullValueHandling = NullValueHandling.Ignore)]
        public int verified { get; set; }
        [JsonProperty("photo_50", NullValueHandling = NullValueHandling.Ignore)]
        public string photo_50 { get; set; }
        [JsonProperty("photo_100", NullValueHandling = NullValueHandling.Ignore)]
        public string photo_100 { get; set; }
        [JsonProperty("photo_200", NullValueHandling = NullValueHandling.Ignore)]
        public string photo_200 { get; set; }
        [JsonProperty("is_closed")]
        public int is_closed { get; set; }

    }
}
