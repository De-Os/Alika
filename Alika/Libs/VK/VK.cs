using Alika.Libs.VK.Longpoll;
using Alika.Libs.VK.Methods;
using Alika.Libs.VK.Responses;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;

namespace Alika.Libs.VK
{
    public partial class VK
    {

        public int user_id;
        public string domain
        {
            get
            {
                return this._http.BaseUrl.AbsoluteUri;
            }
            set
            {
                this._http = new RestClient(value);
            }
        }
        public WebProxy proxy
        {
            get
            {
                return this._http.Proxy as WebProxy;
            }
            set
            {
                this._http.Proxy = value;
            }
        }
        public string api_ver;

        private readonly string token;
        private RestClient _http = new RestClient();

        public VK(Settings settings)
        {
            this.token = settings.Token;
            this.api_ver = settings.ApiVer;
            this.domain = settings.ApiDomain;

            this.user_id = this.Users.Get(new List<int>(), "photo_200, online_info")[0].user_id; // Getting current user's user_id & adding it's photo to cache
        }

        public LongPoll GetLP() => new LongPoll(this);

        /// <summary>
        /// Main method to call & deserialize api methods
        /// </summary>
        /// <typeparam name="Type">Deserializing type</typeparam>
        /// <param name="method">Method name</param>
        /// <param name="fields">Parameters</param>
        /// <returns>Deserialized object</returns>
        public Type Call<Type>(string method, Dictionary<string, dynamic> fields = null)
        {
            var result = this.CallMethod(method, fields);
            BasicResponse<Type> job = JsonConvert.DeserializeObject<BasicResponse<Type>>(result);
            if (job?.error != null)
            {
                throw new Exception(method + ": " + job.error.message);
            }
            else return job.response;
        }

        /// <summary>
        /// Use it only if you need non-deserialized output
        /// </summary>
        /// <param name="method">Method name</param>
        /// <param name="fields">Parameters</param>
        /// <returns>JSON string</returns>
        public string CallMethod(string method, Dictionary<string, dynamic> fields = null)
        {
            var request = new RestRequest(method);
            request.AddOrUpdateParameter("access_token", this.token);
            request.AddOrUpdateParameter("v", this.api_ver);

            if (fields != null && fields.Count > 0)
            {
                foreach (KeyValuePair<string, dynamic> field in fields) request.AddOrUpdateParameter(field.Key, field.Value);
            }

            return this._http.Post(request).Content;
        }

        /// <summary>
        /// store.getStockItems with type=stickers
        /// </summary>
        public GetStickersResponse GetStickers()
        {
            return this.Call<GetStickersResponse>("store.getStockItems", new Dictionary<string, dynamic> { { "type", "stickers" } });
        }

        /// <summary>
        /// store.getStickersKeywords
        /// </summary>
        public GetStickersKeywordsResponse GetStickersKeywords()
        {
            return this.Call<GetStickersKeywordsResponse>("store.getStickersKeywords", new Dictionary<string, dynamic> { });
        }

        public Groups Groups => new Groups(this);
        public Users Users => new Users(this);
        public Messages Messages => new Messages(this);

        public class Settings
        {
            public string ApiDomain { get; set; } = "https://api.vk.com/method";
            public string ApiVer { get; set; } = "5.129";
            public string Token { get; set; }
        }
    }
}
