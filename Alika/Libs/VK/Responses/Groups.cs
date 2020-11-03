using Newtonsoft.Json;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    public class Group
    {
        private int _id;

        [JsonProperty("id")]
        public int Id
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
        public string Name;

        [JsonProperty("screen_name")]
        public string ScreenName;

        [JsonProperty("verified")]
        public int Verified;

        [JsonProperty("photo_50")]
        public string Photo50;

        [JsonProperty("photo_100")]
        public string Photo100;

        [JsonProperty("photo_200")]
        public string Photo200;

        [JsonProperty("is_closed")]
        public int IsClosed;
    }

    public class GroupsResponse
    {
        [JsonProperty("groups")]
        public List<Group> Groups;
    }
}