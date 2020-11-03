namespace Alika.Libs.VK
{
    /// <summary>
    /// Some limits for future?
    /// </summary>
    public static class Limits
    {
        public static class Messages
        {
            public static int MAX_ATTACHMENTS = 10;
            public static int MAX_LENGTH = 4096;
            public static int PEERSTART = 2000000000;
            public static string[] PHOTO_TYPES = new string[] { ".jpg", ".png", ".gif" };
            public static string[] VOICE_TYPES = new string[] { ".mp3", ".ogg" };
            public static int MAX_RECENT_STICKERS_COUNT = 32;
        }

        public static string DefaultAvatar = "https://vk.com/images/camera";
    }
}