using Newtonsoft.Json;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    public class StickerPackInfo
    {
        [JsonProperty("product")]
        public ProductInfo Product;
        [JsonProperty("description")]
        public string Description;
        [JsonProperty("author")]
        public string Author;
        [JsonProperty("can_purchase")]
        public int CanPurchase;
        [JsonProperty("payment_type")]
        public string PaymentType;
        [JsonProperty("price")]
        public int Price;
        [JsonProperty("price_buy")]
        public int PriceBuy;
        [JsonProperty("new")]
        public int IsNew;
        [JsonProperty("background")]
        public string Background;

        public class ProductInfo
        {
            [JsonProperty("id")]
            public int Id;
            [JsonProperty("base_id")]
            public int BaseId;
            [JsonProperty("type")]
            public string Type;
            [JsonProperty("purchased")]
            public int Purchased;
            [JsonProperty("active")]
            public int Active;
            [JsonProperty("style_ids")]
            public List<int> StyleIds;
            [JsonProperty("title")]
            public string Title;
            [JsonProperty("icon")]
            public IconsInfo Icons;
            [JsonProperty("previews")]
            public List<Attachment.PhotoAtt.Size> Previes;
            [JsonProperty("url")]
            public string Url { get; set; }
            [JsonProperty("stickers")]
            public List<Attachment.StickerAtt> Stickers;

            public struct IconsInfo
            {
                [JsonProperty("base_url")]
                public string BaseUrl;
            }
        }
    }

    public class GetStickersKeywordsResponse
    {
        [JsonProperty("count")]
        public int Count;
        [JsonProperty("dictionary")]
        public List<DictionaryInfo> Dictionary;

        public class DictionaryInfo
        {
            [JsonProperty("words")]
            public List<string> Words;
            [JsonProperty("user_stickers")]
            public List<Attachment.StickerAtt> UserStickers;
            [JsonProperty("promoted_stickers")]
            public List<Attachment.StickerAtt> promoted_stickers;
        }
    }
}
