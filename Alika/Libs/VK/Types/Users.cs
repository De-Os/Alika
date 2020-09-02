using Alika.Libs.VK.Responses;
using System;
using System.Collections.Generic;

namespace Alika.Libs.VK
{
    public partial class VK
    {
        public class Users
        {
            VK vk;
            public Users(VK vk)
            {
                this.vk = vk;
            }

            /// <summary>
            /// users.get
            /// </summary>
            public List<User> Get(List<int> user_ids, string fields = "", string name_case = "Nom")
            {
                Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
                if (user_ids.Count > 0) request.Add("user_ids", String.Join(",", user_ids));
                request.Add("name_case", name_case);
                if (fields.Length > 0) request.Add("fields", fields);
                List<User> users = this.vk.Call<List<User>>("users.get", request);
                App.cache.Update(users);
                return users;
            }
        }
    }
}
