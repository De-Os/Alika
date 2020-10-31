using Newtonsoft.Json;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    public class GetStickersResponse
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("items")]
        public List<StickerPackInfo> items { get; set; }

        public class StickerPackInfo
        {
            [JsonProperty("product")]
            public Product product { get; set; }
            [JsonProperty("description")]
            public string description { get; set; }
            [JsonProperty("author")]
            public string author { get; set; }
            [JsonProperty("can_purchase")]
            public int can_purchase { get; set; }
            [JsonProperty("payment_type")]
            public string payment_type { get; set; }
            [JsonProperty("price")]
            public int price { get; set; }
            [JsonProperty("price_buy")]
            public int price_buy { get; set; }
            [JsonProperty("new")]
            public int is_new { get; set; }
            [JsonProperty("background")]
            public string background { get; set; }

            public class Product
            {
                [JsonProperty("id")]
                public int id { get; set; }
                [JsonProperty("base_id")]
                public int base_id { get; set; }
                [JsonProperty("type")]
                public string type { get; set; }
                [JsonProperty("purchased")]
                public int purchased { get; set; }
                [JsonProperty("active")]
                public int active { get; set; }
                [JsonProperty("style_ids")]
                public List<int> style_ids { get; set; }
                [JsonProperty("title")]
                public string title { get; set; }
                [JsonProperty("icon")]
                public Icons icons { get; set; }
                [JsonProperty("previews")]
                public List<Attachment.Photo.Size> previews { get; set; }
                [JsonProperty("url")]
                public string url { get; set; }
                [JsonProperty("stickers")]
                public List<Attachment.Sticker> stickers { get; set; }

                public struct Icons
                {
                    [JsonProperty("base_url")]
                    public string base_url;
                }
            }
        }
    }

    public class GetStickersKeywordsResponse
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("dictionary")]
        public List<Dictionary> dictionary { get; set; }

        public class Dictionary
        {
            [JsonProperty("words")]
            public List<string> words { get; set; }
            [JsonProperty("user_stickers")]
            public List<Attachment.Sticker> user_stickers { get; set; }
            [JsonProperty("promoted_stickers")]
            public List<Attachment.Sticker> promoted_stickers { get; set; }
        }
    }

    public class GetRecentStickersResponse
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("items")]
        public List<Attachment.Sticker> stickers { get; set; }
    }
}
