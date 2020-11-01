using Alika.Libs.VK.Responses;
using System;
using System.Collections.Generic;

namespace Alika.Libs.VK.Methods
{
    public class Users
    {
        private readonly VK _vk;

        internal Users(VK vk) => this._vk = vk;

        /// <summary>
        /// users.get
        /// </summary>
        public List<User> Get(List<int> user_ids, string fields = "", string name_case = "Nom")
        {
            Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
            if (user_ids.Count > 0) request.Add("user_ids", String.Join(",", user_ids));
            request.Add("name_case", name_case);
            if (fields.Length > 0)
            {
                if (!fields.Contains("online_info")) fields += ",online_info";
                request.Add("fields", fields);
            }
            List<User> users = this._vk.Call<List<User>>("users.get", request);
            App.Cache.Update(users);
            return users;
        }
    }
}
