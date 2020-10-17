using Newtonsoft.Json;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    public class GetConversationsResponse
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("items")]
        public List<ConversationResponse> conversations { get; set; }
        [JsonProperty("profiles")]
        public List<User> profiles { get; set; }
        [JsonProperty("groups")]
        public List<Group> groups { get; set; }

        public class ConversationResponse
        {
            [JsonProperty("conversation")]
            public ConversationInfo conversation { get; set; }
            [JsonProperty("last_message")]
            public Message last_message { get; set; }

            public class ConversationInfo
            {
                [JsonProperty("peer")]
                public PeerInfo peer { get; set; }
                [JsonProperty("last_message_id")]
                public int last_message_id { get; set; }
                [JsonProperty("in_read")]
                public int in_read { get; set; }
                [JsonProperty("out_read")]
                public int out_read { get; set; }
                [JsonProperty("unread_count")]
                public int unread_count { get; set; }
                [JsonProperty("push_settings")]
                public PeerPushSettings push_settings { get; set; }
                [JsonProperty("can_write")]
                public PeerWriteSettings write_settings { get; set; }
                [JsonProperty("chat_settings")]
                public PeerSettings settings { get; set; }

                public class PeerInfo
                {
                    [JsonProperty("id")]
                    public int id { get; set; }
                    [JsonProperty("type")]
                    public string type { get; set; }
                    [JsonProperty("local_id")]
                    public int local_id { get; set; }
                }

                public class PeerPushSettings
                {
                    [JsonProperty("disabled_until")]
                    public int disabled_until { get; set; }
                    [JsonProperty("disabled_forever")]
                    public bool disabled_forever { get; set; }
                    [JsonProperty("no_sound ")]
                    public bool no_sound { get; set; }
                }

                public class PeerWriteSettings
                {
                    [JsonProperty("allowed")]
                    public bool allowed { get; set; }
                    [JsonProperty("reason")]
                    public int reason { get; set; }

                    public string ReasonText()
                    {
                        Dictionary<int, string> reasons = new Dictionary<int, string> {
                {18, "пользователь заблокирован или удален"},
                {203, "нет доступа к сообществу"},
                {900, "нельзя отправить сообщение пользователю, который в чёрном списке"},
                {901, "пользователь запретил сообщения от сообщества"},
                {902, "пользователь запретил присылать ему сообщения с помощью настроек приватности"},
                {915, "в сообществе отключены сообщения"},
                {916, "в сообществе заблокированы сообщения"},
                {917, "нет доступа к чату"},
                {918, "нет доступа к e-mail"}
            };
                        return reasons[this.reason];
                    }
                }

                public class PeerSettings
                {
                    [JsonProperty("owner_id")]
                    public int owner_id { get; set; }
                    [JsonProperty("members_count")]
                    public int members_count { get; set; }
                    [JsonProperty("title")]
                    public string title { get; set; }

                    [JsonProperty("pinned_message")]
                    public Message pinned_message { get; set; }
                    [JsonProperty("state")]
                    public string state { get; set; }
                    [JsonProperty("acl")]
                    public AccessSettings access { get; set; }
                    [JsonProperty("permissions")]
                    public Permissions permissions { get; set; }
                    [JsonProperty("photo")]
                    public PeerPhotos photos { get; set; }
                    [JsonProperty("active_ids")]
                    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
                    public List<int> active_ids { get; set; }
                    [JsonProperty("is_group_channel")]
                    public bool is_channel { get; set; }

                    public class Permissions
                    {
                        [JsonProperty("invite")]
                        public string invite { get; set; }
                        [JsonProperty("change_info")]
                        public string change_info { get; set; }
                        [JsonProperty("change_pin")]
                        public string change_pin { get; set; }
                        [JsonProperty("use_mass_mentions")]
                        public string use_mass_mentions { get; set; }
                        [JsonProperty("see_invite_link")]
                        public string see_invite_link { get; set; }
                        [JsonProperty("call")]
                        public string call { get; set; }
                        [JsonProperty("change_admins")]
                        public string change_admins { get; set; }
                    }

                    public class AccessSettings
                    {
                        [JsonProperty("can_change_info")]
                        public bool can_change_info { get; set; }
                        [JsonProperty("can_change_invite_link")]
                        public bool can_change_invite_link { get; set; }
                        [JsonProperty("can_change_pin")]
                        public bool can_change_pin { get; set; }
                        [JsonProperty("can_invite")]
                        public bool can_invite { get; set; }
                        [JsonProperty("can_promote_users")]
                        public bool can_promote_users { get; set; }
                        [JsonProperty("can_see_invite_link")]
                        public bool can_see_invite_link { get; set; }
                        [JsonProperty("can_moderate")]
                        public bool can_moderate { get; set; }
                        [JsonProperty("can_copy_chat")]
                        public bool can_copy_chat { get; set; }
                        [JsonProperty("can_call")]
                        public bool can_call { get; set; }
                        [JsonProperty("can_use_mass_mentions")]
                        public bool can_use_mass_mentions { get; set; }
                        [JsonProperty("can_change_service_type")]
                        public bool can_change_service_type { get; set; }
                    }

                    public class PeerPhotos
                    {
                        [JsonProperty("photo_50")]
                        public string photo_50 { get; set; }
                        [JsonProperty("photo_100")]
                        public string photo_100 { get; set; }
                        [JsonProperty("photo_200")]
                        public string photo_200 { get; set; }

                    }
                }
            }
        }
    }

    public class GetHistoryResponse
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("items")]
        public List<Message> messages { get; set; }

        [JsonProperty("profiles")]
        public List<User> profiles { get; set; }
        [JsonProperty("groups")]
        public List<Group> groups { get; set; }
    }

    public class GetConversationsByIdResponse
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("items")]
        public List<GetConversationsResponse.ConversationResponse.ConversationInfo> conversations { get; set; }
        [JsonProperty("profiles")]
        public List<User> profiles { get; set; }
        [JsonProperty("groups")]
        public List<Group> groups { get; set; }
    }

    public class GetConversationMembersResponse
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("items")]
        public List<Member> members { get; set; }
        [JsonProperty("profiles")]
        public List<User> profiles { get; set; }
        [JsonProperty("groups")]
        public List<Group> groups { get; set; }

        public class Member
        {
            [JsonProperty("member_id")]
            public int member_id { get; set; }
            [JsonProperty("invited_by")]
            public int invited_by { get; set; }
            [JsonProperty("join_date")]
            public int join_date { get; set; }
            [JsonProperty("is_admin")]
            public bool is_admin { get; set; }
            [JsonProperty("can_kick")]
            public bool can_kick { get; set; }
        }
    }

    public class ChangeChatPhotoResponse
    {
        [JsonProperty("message_id")]
        public int message_id { get; set; }
        [JsonProperty("chat")]
        public MultiDialog chat { get; set; }
        public class MultiDialog
        {
            [JsonProperty("type")]
            public string type { get; set; }
            [JsonProperty("title")]
            public string title { get; set; }
            [JsonProperty("admin_id")]
            public int admin_id { get; set; }
            [JsonProperty("members_count")]
            public int members_count { get; set; }
            [JsonProperty("id")]
            public int id { get; set; }
            [JsonProperty("users")]
            public List<int> users { get; set; }
            [JsonProperty("photo_50")]
            public string photo_50 { get; set; }
            [JsonProperty("photo_100")]
            public string photo_100 { get; set; }
            [JsonProperty("photo_200")]
            public string photo_200 { get; set; }
            [JsonProperty("is_default_photo")]
            public bool is_default_photo { get; set; }
        }
    }
}
