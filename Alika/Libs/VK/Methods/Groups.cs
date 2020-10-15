using Alika.Libs.VK.Responses;
using System;
using System.Collections.Generic;

namespace Alika.Libs.VK.Methods
{
    public class Groups
    {
        private readonly VK _vk;

        internal Groups(VK vk) => this._vk = vk;

        /// <summary>
        /// groups.getById
        /// </summary>
        public List<Group> GetById(List<int> group_ids, string fields = "")
        {
            Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
            if (group_ids.Count > 0)
            {
                foreach (var id in group_ids) if (id < 0)
                    {
                        group_ids.Remove(id);
                        group_ids.Add(-id);
                    }
                request.Add("group_ids", String.Join(",", group_ids));
            }
            if (fields.Length > 0) request.Add("fields", fields);
            List<Group> groups = this._vk.Call<List<Group>>("groups.getById", request);
            App.cache.Update(groups);
            return groups;
        }
    }
}
