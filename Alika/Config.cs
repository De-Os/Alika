using Newtonsoft.Json;
using System.Collections.Generic;

namespace Alika
{
    public class Config
    {
        [JsonProperty("vk")]
        public VK vk { get; set; }

        public class VK
        {
            [JsonProperty("api")]
            public string api { get; set; } // Api version
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
            }
        }
    }
}
