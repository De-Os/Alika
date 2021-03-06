﻿using Alika.Libs.VK;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Alika.UI.Misc
{
    [Windows.UI.Xaml.Data.Bindable]
    public class Avatar : PersonPicture
    {
        public bool NoDefaultPhoto { get; set; } = true; // Set to false if you need default camera avatar
        public bool OpenInfoOnClick { get; set; } = true;

        public Avatar(int id)
        {
            this.DisplayName = App.Cache.GetName(id);
            this.LoadAvatar(App.Cache.GetAvatar(id));

            this.PointerPressed += (a, b) =>
            {
                if (this.OpenInfoOnClick) new ChatInformation(id);
            };
        }

        private async void LoadAvatar(string url)
        {
            if (this.NoDefaultPhoto && url.StartsWith(Limits.DefaultAvatar))
            {
                this.ProfilePicture = null;
            }
            else this.ProfilePicture = await ImageCache.Instance.GetFromCacheAsync(new Uri(url));
        }
    }
}