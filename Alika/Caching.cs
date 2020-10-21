using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Responses;
using Alika.UI;
using System.Collections.Generic;
using static Alika.Libs.VK.Responses.GetConversationsResponse;

namespace Alika
{
    public class Caching
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Group> Groups { get; set; } = new List<Group>();
        public List<ConversationResponse.ConversationInfo> Conversations { get; set; } = new List<ConversationResponse.ConversationInfo>();
        public List<GetStickersResponse.StickerPackInfo> StickerPacks { get; set; }
        public StickersSelector StickersSelector;
        public Dictionary<string, List<Attachment.Sticker>> StickerDictionary { get; set; }
        public Caching() { }

        public void Update(List<User> users)
        {
            if (users != null && users.Count > 0) users.ForEach((User user) => this.Update(user));
        }
        public void Update(User user)
        {
            try
            {
                User us = null;
                if (this.Users.Exists(u => u.user_id == user.user_id))
                {
                    us = this.Users.Find(u => u.user_id == user.user_id);
                    this.Users.RemoveAll(u => u.user_id == user.user_id);
                    this.Users.Add(this.Update(user, us));
                }
                else us = user;
                this.Users.Add(us);
            }
            catch { }
        }
        public void Update(List<Group> groups)
        {
            if (groups != null && groups.Count > 0) groups.ForEach((Group group) => this.Update(group));
        }
        public void Update(Group group)
        {
            try
            {
                Group gr = null;
                if (this.Groups.Exists(g => g.id == group.id))
                {
                    gr = this.Groups.Find(g => g.id == group.id);
                    this.Groups.RemoveAll(g => g.id == group.id);
                    this.Groups.Add(this.Update(group, gr));
                }
                else gr = group;
                this.Groups.Add(gr);
            }
            catch { }
        }
        public void Update(List<ConversationResponse> conversations)
        {
            if (conversations != null && conversations.Count > 0) conversations.ForEach((ConversationResponse conversation) =>
            {
                this.Update(conversation.conversation);
            });
        }
        public void Update(List<ConversationResponse.ConversationInfo> conversations)
        {
            if (conversations != null && conversations.Count > 0) conversations.ForEach((ConversationResponse.ConversationInfo conversation) =>
            {
                this.Update(conversation);
            });
        }
        public void Update(ConversationResponse.ConversationInfo conversation)
        {
            try
            {
                ConversationResponse.ConversationInfo conv = null;
                if (this.Conversations.Exists(c => c.peer.id == conversation.peer.id))
                {
                    conv = this.Conversations.Find(c => c.peer.id == conversation.peer.id);
                    this.Conversations.RemoveAll(c => c.peer.id == conversation.peer.id);
                    this.Conversations.Add(this.Update(conversation, conv));
                }
                else conv = conversation;
                this.Conversations.Add(conv);
            }
            catch { }
        }
        public void Update(List<GetStickersResponse.StickerPackInfo> stickers)
        {
            this.StickerPacks = stickers;
            this.StickersSelector = new StickersSelector(this.StickerPacks);
        }
        public void Update(List<GetStickersKeywordsResponse.Dictionary> dictionaries)
        {
            this.StickerDictionary = new Dictionary<string, List<Attachment.Sticker>>();
            dictionaries.ForEach((dict) =>
            {
                dict.words.ForEach((word) =>
                {
                    if (!this.StickerDictionary.ContainsKey(word)) this.StickerDictionary.Add(word, new List<Attachment.Sticker>());
                    dict.user_stickers.ForEach((sticker) => this.StickerDictionary[word].Add(sticker));
                    string wordEng = Utils.RuToEng(word);
                    if (wordEng != null && wordEng != word)
                    {
                        if (!this.StickerDictionary.ContainsKey(wordEng)) this.StickerDictionary.Add(wordEng, new List<Attachment.Sticker>());
                        dict.user_stickers.ForEach((sticker) => this.StickerDictionary[wordEng].Add(sticker));
                    }
                });
            });
        }
        private Type Update<Type>(Type from, Type to)
        {
            var type = from.GetType();
            foreach (var field in type.GetFields())
            {
                var val = field.GetValue(from);
                if (val != null) field.SetValue(to, val);
            }

            foreach (var prop in type.GetProperties())
            {
                var val = prop.GetValue(from);
                if (val != null) prop.SetValue(to, val);
            }

            return to;
        }

        public string GetAvatar(int peer_id)
        {
            if (peer_id > Limits.Messages.PEERSTART)
            {
                var avatar = this.GetConversation(peer_id).settings.photos?.photo_200;
                return avatar ?? "https://vk.com/images/camera_200.png?ava=1";
            }
            else
            {
                if (peer_id < 0)
                {
                    return this.GetGroup(peer_id).photo_200;
                }
                else return this.GetUser(peer_id).photo_200;
            }
        }

        public string GetName(int id)
        {
            if (id > Limits.Messages.PEERSTART)
            {
                return this.GetConversation(id).settings.title;
            }
            else
            {
                if (id < 0)
                {
                    return this.GetGroup(id).name;
                }
                else
                {
                    var user = this.GetUser(id);
                    return user.first_name + " " + user.last_name;
                }
            }
        }

        public User GetUser(int user_id)
        {
            if (!this.Users.Exists(u => u.user_id == user_id)) this.Update(user_id);
            return this.Users.Find(u => u.user_id == user_id);
        }

        public Group GetGroup(int group_id)
        {
            if (!this.Groups.Exists(g => g.id == group_id)) this.Update(group_id);
            return this.Groups.Find(g => g.id == group_id);
        }

        public ConversationResponse.ConversationInfo GetConversation(int peer_id)
        {
            if (!this.Conversations.Exists(c => c.peer.id == peer_id)) App.vk.Messages.GetConversationsById(new List<int> { peer_id }, "photo_200,online_info");
            return this.Conversations.Find(c => c.peer.id == peer_id);
        }

        public void Update(int id)
        {
            if (id > 0)
            {
                if (id > Limits.Messages.PEERSTART)
                {
                    App.vk.Messages.GetConversationsById(new List<int> { id }, "photo_200,online_info");
                }
                else App.vk.Users.Get(new List<int> { id }, "photo_200,online_info");
            }
            else App.vk.Groups.GetById(new List<int> { id }, "photo_200");
        }
    }
}
