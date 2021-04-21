using Newtonsoft.Json;
using System.Collections.Generic;

namespace Alika.Libs.VK.Responses
{
    public class ConversationResponse
    {
        [JsonProperty("conversation")]
        public ConversationInfo Conversation;

        [JsonProperty("last_message")]
        public Message LastMessage;
    }

    public class ConversationInfo
    {
        [JsonProperty("peer")]
        public PeerInfo Peer;

        [JsonProperty("last_message_id")]
        public int LastMessageId;

        [JsonProperty("in_read")]
        public int InRead;

        [JsonProperty("out_read")]
        public int OutRead;

        [JsonIgnore]
        public int LastRead
        {
            get
            {
                return InRead > OutRead ? InRead : OutRead;
            }
        }

        [JsonProperty("unread_count")]
        public int UnreadCount;

        [JsonProperty("push_settings")]
        public PeerPushSettings PushSettings;

        [JsonProperty("can_write")]
        public PeerWriteSettings WriteSettings;

        [JsonProperty("chat_settings")]
        public PeerSettings Settings;

        [JsonProperty("sort_id")]
        public PeerSortId SortId;

        public class PeerInfo
        {
            [JsonProperty("id")]
            public int Id;

            [JsonProperty("type")]
            public string Type;

            [JsonProperty("local_id")]
            public int LocalId;
        }

        public class PeerPushSettings
        {
            [JsonProperty("disabled_until")]
            public int DisabledUntil;

            [JsonProperty("disabled_forever")]
            public bool DisabledForever;

            [JsonProperty("no_sound")]
            public bool NoSound;
        }

        public class PeerWriteSettings
        {
            [JsonProperty("allowed")]
            public bool Allowed;

            [JsonProperty("reason")]
            public int Reason;

            /*public string ReasonText()
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
                return reasons[this.Reason];
            }*/
        }

        public class PeerSettings
        {
            [JsonProperty("owner_id")]
            public int OwnerId;

            [JsonProperty("members_count")]
            public int MembersCount;

            [JsonProperty("title")]
            public string Title;

            [JsonProperty("pinned_message")]
            public Message PinnedMessage;

            [JsonProperty("state")]
            public string State;

            [JsonProperty("acl")]
            public AccessSettings Access;

            [JsonProperty("permissions")]
            public PeerPermissions Permissions;

            [JsonProperty("photo")]
            public PeerPhotos Photos;

            [JsonProperty("active_ids")]
            public List<int> ActiveIds;

            [JsonProperty("is_group_channel")]
            public bool IsChannel;

            public class PeerPermissions
            {
                [JsonProperty("invite")]
                public string Invite;

                [JsonProperty("change_info")]
                public string ChangeInfo;

                [JsonProperty("change_pin")]
                public string ChangePin;

                [JsonProperty("use_mass_mentions")]
                public string UseMassMentions;

                [JsonProperty("see_invite_link")]
                public string SeeInviteLink;

                [JsonProperty("call")]
                public string Call;

                [JsonProperty("change_admins")]
                public string ChangeAdmins;
            }

            public class AccessSettings
            {
                [JsonProperty("can_change_info")]
                public bool CanChangeInfo;

                [JsonProperty("can_change_invite_link")]
                public bool CanChangeInviteLink;

                [JsonProperty("can_change_pin")]
                public bool CanChangePin;

                [JsonProperty("can_invite")]
                public bool CanInvite;

                [JsonProperty("can_promote_users")]
                public bool CanPromoteUsers;

                [JsonProperty("can_see_invite_link")]
                public bool CanSeeInviteLink;

                [JsonProperty("can_moderate")]
                public bool CanModerate;

                [JsonProperty("can_copy_chat")]
                public bool CanCopyChat;

                [JsonProperty("can_call")]
                public bool CanCall;

                [JsonProperty("can_use_mass_mentions")]
                public bool CanUseMassMentions;

                [JsonProperty("can_change_service_type")]
                public bool CanChangeServiceType;
            }

            public class PeerPhotos
            {
                [JsonProperty("photo_50")]
                public string Photo50;

                [JsonProperty("photo_100")]
                public string Photo100;

                [JsonProperty("photo_200")]
                public string Photo200;
            }
        }

        public class PeerSortId
        {
            [JsonProperty("major_id")]
            public int MajorId;

            [JsonProperty("minor_id")]
            public int MinorId;
        }
    }

    public class ConversationMember
    {
        [JsonProperty("member_id")]
        public int MemberId;

        [JsonProperty("invited_by")]
        public int InvitedBy;

        [JsonProperty("join_date")]
        public int JoinDate;

        [JsonProperty("is_admin")]
        public bool IsAdmin;

        [JsonProperty("can_kick")]
        public bool CanKick;
    }

    public class ChangeChatPhotoResponse
    {
        [JsonProperty("message_id")]
        public int MessageId;

        [JsonProperty("chat")]
        public MultiDialog Chat;
    }

    public class MultiDialog
    {
        [JsonProperty("type")]
        public string Type;

        [JsonProperty("title")]
        public string Title;

        [JsonProperty("admin_id")]
        public int AdminId;

        [JsonProperty("members_count")]
        public int MembersCount;

        [JsonProperty("id")]
        public int Id;

        [JsonProperty("users")]
        public List<int> Users;

        [JsonProperty("photo_50")]
        public string Photo50;

        [JsonProperty("photo_100")]
        public string Photo100;

        [JsonProperty("photo_200")]
        public string Photo200;

        [JsonProperty("is_default_photo")]
        public bool IsDeaultPhoto;
    }

    public struct GetInviteLinkResponse
    {
        [JsonProperty("link")]
        public string Link;
    }
}