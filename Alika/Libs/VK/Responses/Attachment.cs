using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace Alika.Libs.VK.Responses
{
    public struct Attachment
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("photo")]
        public PhotoAtt Photo;

        [JsonProperty("video")]
        public VideoAtt Video;

        [JsonProperty("audio")]
        public AudioAtt Audio;

        [JsonProperty("doc")]
        public DocumentAtt Document;

        [JsonProperty("graffiti")]
        public GraffitiAtt Graffiti;

        [JsonProperty("audio_message")]
        public AudioMessageAtt AudioMessage;

        [JsonProperty("link")]
        public LinkAtt Link;

        [JsonProperty("wall")]
        public WallAtt Wall;

        [JsonProperty("wall_reply")]
        public WallReplyAtt WallReply;

        [JsonProperty("sticker")]
        public StickerAtt Sticker;

        [JsonProperty("gift")]
        public GiftAtt Gift;

        public class PhotoAtt : AttachBase
        {
            [JsonProperty("album_id")]
            public int AlbumId;

            [JsonProperty("user_id")]
            public int UserId;

            [JsonProperty("text")]
            public string Text;

            [JsonProperty("date")]
            public int Date;

            [JsonProperty("sizes")]
            public List<Size> Sizes;

            public Size GetBestQuality() => this.Sizes.OrderByDescending(i => i.Width).ThenByDescending(i => i.Height).First();

            public override string ToAttachFormat() => "photo" + base.ToAttachFormat();

            public class Size
            {
                [JsonProperty("type")]
                public string Type;

                [JsonProperty("url")]
                public string Url;

                [JsonProperty("width")]
                public int Width;

                [JsonProperty("height")]
                public int Height;
            }
        }

        public class VideoAtt : AttachBase
        {
            [JsonProperty("title")]
            public string Title;

            [JsonProperty("description")]
            public string Description;

            [JsonProperty("duration")]
            public int Duration;

            [JsonProperty("photo_130")]
            public string Photo130;

            [JsonProperty("photo_320")]
            public string Photo320;

            [JsonProperty("photo_640")]
            public string Photo640;

            [JsonProperty("photo_800")]
            public string Photo800;

            [JsonProperty("photo_1280")]
            public string Photo1280;

            [JsonProperty("first_frame_130")]
            public string FirstFrame130;

            [JsonProperty("first_frame_320")]
            public string FirstFrame320;

            [JsonProperty("first_frame_640")]
            public string FirstFrame640;

            [JsonProperty("first_frame_800")]
            public string FirstFrame800;

            [JsonProperty("first_frame_1280")]
            public string FirstFrame1280;

            [JsonProperty("date")]
            public int Date;

            [JsonProperty("adding_date")]
            public int AddingDate;

            [JsonProperty("views")]
            public int Views;

            [JsonProperty("comments")]
            public int Comments;

            [JsonProperty("player")]
            public string Player;

            [JsonProperty("platform")]
            public string Platform;

            [JsonProperty("can_edit")]
            public int CanEdit;

            [JsonProperty("can_add")]
            public int CanAdd;

            [JsonProperty("is_private")]
            public int IsPrivate;

            [JsonProperty("processing")]
            public int Processing;

            [JsonProperty("live")]
            public int Live;

            [JsonProperty("upcoming")]
            public int Upcoming;

            [JsonProperty("is_favorite")]
            public bool IsFavorite;

            public override string ToAttachFormat() => "video" + base.ToAttachFormat();
        }

        public class AudioAtt : AttachBase
        {
            [JsonProperty("artist")]
            public string Artist;

            [JsonProperty("title")]
            public string Title;

            [JsonProperty("duration")]
            public int Duration;

            [JsonProperty("url")]
            public string Url;

            [JsonProperty("lyrics_id")]
            public int LyricsId;

            [JsonProperty("album_id")]
            public int AlbumId;

            [JsonProperty("genre_id")]
            public int GenreId;

            [JsonProperty("date")]
            public int Date;

            [JsonProperty("no_search")]
            public int NoSearch;

            [JsonProperty("is_hq")]
            public int IsHQ;

            public override string ToAttachFormat() => "audio" + base.ToAttachFormat();
        }

        public class DocumentAtt : AttachBase
        {
            [JsonProperty("title")]
            public string Title;

            [JsonProperty("size")]
            public int Size;

            [JsonProperty("ext")]
            public string Extension;

            [JsonProperty("url")]
            public string Url;

            [JsonProperty("date")]
            public string Date;

            [JsonProperty("type")]
            public string Type;

            [JsonProperty("preview")]
            public DocPreview Preview;

            public override string ToAttachFormat() => "doc" + base.ToAttachFormat();

            public class DocPreview
            {
                [JsonProperty("photo")]
                public PhotoAtt Photo;

                [JsonProperty("graffiti")]
                public GraffitiAtt Graffiti;

                [JsonProperty("audio_message")]
                public AudioMessageAtt AudioMessage;
            }
        }

        public class GraffitiAtt : AttachBase
        {
            [JsonProperty("url")]
            public string Url;

            [JsonProperty("width")]
            public int Width;

            [JsonProperty("height")]
            public int Height;

            public override string ToAttachFormat() => "doc" + base.ToAttachFormat();
        }

        public class AudioMessageAtt : AttachBase
        {
            [JsonProperty("duration")]
            public int Duration;

            [JsonProperty("waveform")]
            public List<int> Waveform;

            [JsonProperty("link_ogg")]
            public string LinkOGG;

            [JsonProperty("link_mp3")]
            public string LinkMP3;

            [JsonProperty("transcript")]
            public string Transcript;

            [JsonProperty("transcript_state")]
            public string TranscriptState;

            public override string ToAttachFormat() => "doc" + base.ToAttachFormat();
        }

        public class LinkAtt
        {
            [JsonProperty("url")]
            public string Url;

            [JsonProperty("title")]
            public string Title;

            [JsonProperty("caption ")]
            public string Caption;

            [JsonProperty("description")]
            public string Description;

            [JsonProperty("photo")]
            public PhotoAtt Photo;
        }

        public class WallAtt
        {
            [JsonProperty("id")]
            public int Id;

            [JsonProperty("owner_id")]
            public int OwnerId;

            [JsonProperty("to_id")]
            public int ToId;

            [JsonProperty("from_id")]
            public int FromId;

            [JsonProperty("date")]
            public int Date;

            [JsonProperty("text")]
            public string Text;

            [JsonProperty("reply_owner_id")]
            public int ReplyOwnerId;

            [JsonProperty("reply_post_id")]
            public int ReplyPostId;

            [JsonProperty("friends_only")]
            public int FriendsOnly;
        }

        public class WallReplyAtt
        {
            [JsonProperty("id")]
            public int Id;

            [JsonProperty("from_id")]
            public int FromId;

            [JsonProperty("post_id")]
            public int PostId;

            [JsonProperty("owner_id")]
            public int OwnerId;

            [JsonProperty("date")]
            public int Date;

            [JsonProperty("text")]
            public string Text;

            [JsonProperty("reply_to_user")]
            public int ReplyToUser;

            [JsonProperty("reply_to_comment")]
            public int ReplyToComment;
        }

        public class StickerAtt
        {
            [JsonProperty("product_id")]
            public int ProductId;

            [JsonProperty("sticker_id")]
            public int StickerId;

            [JsonProperty("images")]
            public List<Image> Images;

            [JsonProperty("images_with_background")]
            public List<Image> ImagesWithBackground;

            [JsonProperty("animation_url")]
            public string AnimationUrl;

            [JsonProperty("is_allowed")]
            public bool IsAllowed;

            public string GetBestQuality(bool background = false) => (background ? this.ImagesWithBackground : this.Images).OrderByDescending(i => i.Width).ThenByDescending(i => i.Height).First().Url;

            public class Image
            {
                [JsonProperty("url")]
                public string Url;

                [JsonProperty("width")]
                public int Width;

                [JsonProperty("height")]
                public int Height;
            }
        }

        public class GiftAtt
        {
            [JsonProperty("id")]
            public int Id;

            [JsonProperty("thumb_256")]
            public string Thumb256;

            [JsonProperty("thumb_96")]
            public string Thumb96;

            [JsonProperty("thumb_48")]
            public string Thumb48;
        }

        public abstract class AttachBase
        {
            [JsonProperty("id")]
            public int Id;

            [JsonProperty("owner_id")]
            public int OwnerId;

            [JsonProperty("access_key")]
            public string AccessKey;

            public virtual string ToAttachFormat() => this.OwnerId.ToString() + "_" + this.Id.ToString() + (this.AccessKey?.Length > 0 ? "_" + this.AccessKey : "");
        }
    }

    public class GetHistoryAttachmentsResponse : ItemsResponse<GetHistoryAttachmentsResponse.AttachmentElement>
    {
        [JsonProperty("next_from")]
        public string NextFrom;

        public class AttachmentElement
        {
            [JsonProperty("message_id")]
            public int MessageId;

            [JsonProperty("from_id")]
            public int FromId;

            [JsonProperty("attachment")]
            public Attachment Attachment;
        }
    }
}