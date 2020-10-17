using Newtonsoft.Json;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    class BasicResponse<Type>
    {
        [JsonProperty("response")]
        public Type response { get; set; }
        [JsonProperty("error")]
        public Error error { get; set; }
    }

    class Error
    {
        [JsonProperty("error_code")]
        public int code { get; set; }
        [JsonProperty("error_msg")]
        public string message { get; set; }
        [JsonProperty("captcha_sid")]
        public string captcha_sid { get; set; }
        [JsonProperty("captcha_img")]
        public string captcha_img { get; set; }
        [JsonProperty("request_params")]
        public List<Dictionary<string, string>> request_params { get; set; }
    }
}
