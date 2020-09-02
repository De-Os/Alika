﻿using Alika.Libs.VK.Longpoll;
using Alika.Libs.VK.Responses;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;

namespace Alika.Libs.VK
{
    public partial class VK
    {
        private string token;
        public string version;
        public int user_id;

        public VK.Messages messages;
        public VK.Users users;
        public VK.Groups groups;

        public CaptchaSettings captchaSettings;

        public VK(string token, string version, CaptchaSettings captcha = null)
        {
            this.token = token;
            this.version = version;
            this.captchaSettings = captcha;

            this.messages = new VK.Messages(this);
            this.users = new VK.Users(this);
            this.groups = new VK.Groups(this);

            this.user_id = this.users.Get(new List<int>(), "photo_200")[0].user_id; // Getting current user's user_id & adding it's photo to cache
        }

        public LongPoll GetLP()
        {
            return new LongPoll(this);
        }

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
            if (job.error != null)
            {
                // TODO: Captcha handling
                /*if (job.error.code == 14 && this.captchaSettings != null)
                {
                    System.Diagnostics.Debug.WriteLine(ObjectDumper.Dump(job.error));
                    CaptchaDialog captcha = new CaptchaDialog(job.error.captcha_img, this.captchaSettings);
                    CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await captcha.ShowAsync();
                    });
                    fields.Add("captcha_sid", job.error.captcha_sid);
                    fields.Add("captcha_key", captcha.text.Text);
                    return this.Call<Type>(method, fields);
                }
                else */
                throw new Exception(job.error.message);
            };
            return job.response;
        }

        /// <summary>
        /// Use it only if you need non-deserialized output
        /// </summary>
        /// <param name="method">Method name</param>
        /// <param name="fields">Parameters</param>
        /// <returns>JSON string</returns>
        public string CallMethod(string method, Dictionary<string, dynamic> fields = null)
        {
            var http = new RestClient("https://api.vk.com/method");
            var request = new RestRequest(method);
            request.AddParameter("access_token", this.token);
            request.AddParameter("v", this.version);

            if (fields != null && fields.Count > 0)
            {
                foreach (KeyValuePair<string, dynamic> field in fields) request.AddParameter(field.Key, field.Value);
            }

            return http.Post(request).Content;
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
    }
}