using Newtonsoft.Json;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    public class BasicResponse<Type>
    {
        [JsonProperty("response")]
        public Type Response;

        [JsonProperty("error")]
        public Error Error;
    }

    public class ItemsResponse<Type> : IItemsResponse
    {
        [JsonProperty("count")]
        public int Count;

        [JsonProperty("items")]
        public List<Type> Items;

        [JsonProperty("profiles")]
        public List<User> Profiles { get; set; }

        [JsonProperty("groups")]
        public List<Group> Groups { get; set; }
    }

    public interface IItemsResponse
    {
        public List<User> Profiles { get; set; }
        public List<Group> Groups { get; set; }
    }

    public class Error
    {
        [JsonProperty("error_code")]
        public int Code;

        [JsonProperty("error_msg")]
        public string Message;

        [JsonProperty("captcha_sid")]
        public string CaptchaSid;

        [JsonProperty("captcha_img")]
        public string CaptchaImg;

        [JsonProperty("request_params")]
        public List<Dictionary<string, string>> RequestParams;
    }
}