using Alika.Libs.VK.Longpoll;
using Alika.Libs.VK.Methods;
using Alika.Libs.VK.Responses;
using MessagePack;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;

namespace Alika.Libs.VK
{
    public partial class VK
    {
        public const string API_VER = "5.140";

        public int UserId;

        public string Domain
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

        public WebProxy Proxy
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

        private readonly string Token;
        private RestClient _http = new RestClient();

        public VK(Settings settings)
        {
            this.Token = settings.Token;
            this.Domain = settings.ApiDomain;
            this.UserId = this.Users.Get(new List<int>(), "photo_200, online_info")[0].UserId; // Getting current user's user_id & adding it's photo to cache
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
            if (job == null || job?.Error != null)
            {
                throw new Exception(method + ": " + (job == null ? result : job.Error.Message));
            }
            else
            {
                if (job.Response is IItemsResponse items)
                {
                    App.Cache.Update(items.Groups);
                    App.Cache.Update(items.Profiles);

                    if (job.Response is ItemsResponse<Group> groups) App.Cache.Update(groups.Items);
                    if (job.Response is ItemsResponse<User> users) App.Cache.Update(users.Items);
                    if (job.Response is ItemsResponse<ConversationResponse> convs) App.Cache.Update(convs.Items);
                    if (job.Response is ItemsResponse<ConversationInfo> convinfos) App.Cache.Update(convinfos.Items);
                }
                else if (job.Response is GetImportantMessagesResponse response)
                {
                    App.Cache.Update(response.Groups);
                    App.Cache.Update(response.Conversations);
                    App.Cache.Update(response.Profiles);
                }
                else if (job.Response is List<User> users)
                {
                    App.Cache.Update(users);
                }
                else if (job.Response is List<Group> groups)
                {
                    App.Cache.Update(groups);
                }

                return job.Response;
            }
        }

        /// <summary>
        /// Use it only if you need non-deserialized output
        /// </summary>
        /// <param name="method">Method name</param>
        /// <param name="fields">Parameters</param>
        /// <returns>JSON string</returns>
        public string CallMethod(string method, Dictionary<string, dynamic> fields = null)
        {
            var request = new RestRequest(method + ".msgpack");
            request.AddOrUpdateParameter("access_token", this.Token);
            request.AddOrUpdateParameter("v", API_VER);

            if (fields != null && fields.Count > 0)
            {
                foreach (KeyValuePair<string, dynamic> field in fields) request.AddOrUpdateParameter(field.Key, field.Value);
            }

            return MessagePackSerializer.ConvertToJson(this._http.Post(request).RawBytes);
        }

        /// <summary>
        /// store.getStockItems with type=stickers
        /// </summary>
        public ItemsResponse<StickerPackInfo> GetStickers() => this.Call<ItemsResponse<StickerPackInfo>>("store.getStockItems", new Dictionary<string, dynamic> { { "type", "stickers" } });

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
        public Friends Friends => new Friends(this);

        public class Settings
        {
            public string ApiDomain { get; set; } = "https://api.vk.com/method";
            public string Token { get; set; }
        }
    }
}