﻿using Alika.Libs.VK.Responses;
using System;
using System.Collections.Generic;

namespace Alika.Libs.VK
{
    public partial class VK
    {
        public class Groups
        {
            VK vk;
            public Groups(VK vk)
            {
                this.vk = vk;
            }

            public List<Group> GetById(List<int> group_ids, string fields = "")
            {
                Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
                if (group_ids.Count > 0)
                {
                    for (int x = 0; x < group_ids.Count; x++)
                    {
                        if (group_ids[x] < 0) group_ids[x] = -group_ids[x];
                    }
                    request.Add("group_ids", String.Join(",", group_ids));
                }
                if (fields.Length > 0) request.Add("fields", fields);
                List<Group> groups = this.vk.Call<List<Group>>("groups.getById", request);
                App.cache.Update(groups);
                return groups;
            }
        }
    }
}
