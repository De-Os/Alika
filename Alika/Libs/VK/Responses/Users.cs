using Newtonsoft.Json;

namespace Alika.Libs.VK.Responses
{
    public class User
    {
        [JsonProperty("id")]
        public int UserId;
        [JsonProperty("first_name")]
        public string FirstName;
        [JsonProperty("last_name")]
        public string LastName;
        [JsonProperty("verified")]
        public int Verified;
        [JsonProperty("photo_50")]
        public string Photo50;
        [JsonProperty("photo_100")]
        public string Photo100;
        [JsonProperty("photo_200")]
        public string Photo200;
        [JsonProperty("screen_name")]
        public string ScreenName;
        [JsonProperty("online")]
        public int Online;
        [JsonProperty("online_mobile")]
        public int OnlineMobile;
        [JsonProperty("online_info")]
        public UserOnlineInfo OnlineInfo;

        public class UserOnlineInfo
        {
            [JsonProperty("visible")]
            public bool Visible;
            [JsonProperty("last_seen")]
            public int LastSeen;
            [JsonProperty("is_online")]
            public bool IsOnline;
            [JsonProperty("is_mobile")]
            public bool IsMobile;
        }
    }
}
