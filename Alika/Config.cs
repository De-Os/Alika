using Alika.Libs;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;

namespace Alika
{
    public class Config
    {
        public delegate void SettingUpdated(string name);

        public event SettingUpdated OnSettingUpdated;

        [JsonProperty("vk")]
        public VK vk { get; set; }

        [JsonProperty("proxy")]
        public Proxy proxy { get; set; }

        public class VK
        {
            [JsonProperty("domain")]
            public string domain { get; set; } // Domain url

            [JsonProperty("ping_url")]
            public string ping_url { get; set; }

            [JsonProperty("login")]
            public Login login { get; set; } // Login options

            public class Login
            {
                [JsonProperty("client_id")]
                public int client_id { get; set; } // Client id -- change only if you have issues with android client

                [JsonProperty("client_secret")]
                public string client_secret { get; set; } // Client secret -- change only if you have issues with android client

                [JsonProperty("scope")]
                public List<string> scope { get; set; } // List of scopes -- messages, offline by default

                [JsonProperty("domain")]
                public string domain { get; set; } // OAuth domain
            }
        }

        public class Proxy
        {
            [JsonProperty("enabled")]
            public bool enabled { get; set; }

            [JsonProperty("url")]
            public string url { get; set; }

            [JsonProperty("port")]
            public int port { get; set; }

            [JsonProperty("username")]
            public string username { get; set; }

            [JsonProperty("password")]
            public string password { get; set; }

            public WebProxy ToWebProxy()
            {
                var proxy = new WebProxy(url, port);
                if (username != null || password != null)
                {
                    var credentials = new NetworkCredential();
                    if (username.Length > 0) credentials.UserName = username;
                    if (password.Length > 0) credentials.Password = password;
                    proxy.Credentials = credentials;
                }
                return proxy;
            }
        }

        public void CallUpdateEvent(string setting_name) => this.OnSettingUpdated?.Invoke(setting_name.Replace("+", "."));

        public static async Task EnsureCreated()
        {
            var folder = ApplicationData.Current.LocalFolder;
            if (!await folder.FileExistsAsync("settings.json"))
            {
                await folder.CreateFileAsync("settings.json");
                await folder.WriteTextToFileAsync(File.ReadAllText(Utils.AppPath("settings.json")), "settings.json");
            }
            App.Settings = JsonConvert.DeserializeObject<Config>(await folder.ReadTextFromFileAsync("settings.json"));
        }
    }
}