using Alika.Libs.VK.Responses;
using System.Collections.Generic;

namespace Alika.Libs.VK.Methods
{
    public class Friends
    {
        private readonly VK _vk;

        internal Friends(VK vk) => this._vk = vk;

        public ItemsResponse<User> Get(string order = "hints", int count = 100, int offset = 0, string fields = "photo_200")
        {
            Dictionary<string, dynamic> request = new Dictionary<string, dynamic> {
                {"order", order},
                {"count", count}
            };
            if (offset != 0) request.Add("offset", offset);
            if (!fields.Contains("online_info")) fields += ",online_info";
            request.Add("fields", fields);
            var response = this._vk.Call<ItemsResponse<User>>("friends.get", request);
            App.Cache.Update(response.Items);
            return response;
        }
    }
}