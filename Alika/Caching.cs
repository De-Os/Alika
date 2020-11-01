using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Responses;
using Alika.UI;
using System.Collections.Generic;

namespace Alika
{
    public class Caching
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Group> Groups { get; set; } = new List<Group>();
        public List<ConversationInfo> Conversations { get; set; } = new List<ConversationInfo>();
        public List<StickerPackInfo> StickerPacks { get; set; }
        public List<Attachment.StickerAtt> RecentStickers { get; set; }
        public StickersSelector StickersSelector;
        public Dictionary<string, List<Attachment.StickerAtt>> StickerDictionary { get; set; }
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
                if (this.Users.Exists(u => u.UserId == user.UserId))
                {
                    us = this.Users.Find(u => u.UserId == user.UserId);
                    this.Users.RemoveAll(u => u.UserId == user.UserId);
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
                if (this.Groups.Exists(g => g.Id == group.Id))
                {
                    gr = this.Groups.Find(g => g.Id == group.Id);
                    this.Groups.RemoveAll(g => g.Id == group.Id);
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
                this.Update(conversation.Conversation);
            });
        }
        public void Update(List<ConversationInfo> conversations)
        {
            if (conversations != null && conversations.Count > 0) conversations.ForEach((ConversationInfo conversation) =>
            {
                this.Update(conversation);
            });
        }
        public void Update(ConversationInfo conversation)
        {
            try
            {
                ConversationInfo conv = null;
                if (this.Conversations.Exists(c => c.Peer.Id == conversation.Peer.Id))
                {
                    conv = this.Conversations.Find(c => c.Peer.Id == conversation.Peer.Id);
                    this.Conversations.RemoveAll(c => c.Peer.Id == conversation.Peer.Id);
                    this.Conversations.Add(this.Update(conversation, conv));
                }
                else conv = conversation;
                this.Conversations.Add(conv);
            }
            catch { }
        }
        public void Update(List<StickerPackInfo> stickers, List<Attachment.StickerAtt> recent)
        {
            this.StickerPacks = stickers;
            this.RecentStickers = recent;
            this.StickersSelector = new StickersSelector(this.StickerPacks, recent);
        }
        public void Update(List<GetStickersKeywordsResponse.DictionaryInfo> dictionaries)
        {
            this.StickerDictionary = new Dictionary<string, List<Attachment.StickerAtt>>();
            dictionaries.ForEach((dict) =>
            {
                dict.Words.ForEach((word) =>
                {
                    if (!this.StickerDictionary.ContainsKey(word)) this.StickerDictionary.Add(word, new List<Attachment.StickerAtt>());
                    dict.UserStickers.ForEach((sticker) => this.StickerDictionary[word].Add(sticker));
                    string wordEng = Utils.RuToEng(word);
                    if (wordEng != null && wordEng != word)
                    {
                        if (!this.StickerDictionary.ContainsKey(wordEng)) this.StickerDictionary.Add(wordEng, new List<Attachment.StickerAtt>());
                        dict.UserStickers.ForEach((sticker) => this.StickerDictionary[wordEng].Add(sticker));
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
                var avatar = this.GetConversation(peer_id).Settings.Photos?.Photo200;
                return avatar ?? "https://vk.com/images/camera_200.png?ava=1";
            }
            else
            {
                if (peer_id < 0)
                {
                    return this.GetGroup(peer_id).Photo200;
                }
                else return this.GetUser(peer_id).Photo200;
            }
        }

        public string GetName(int id)
        {
            if (id > Limits.Messages.PEERSTART)
            {
                return this.GetConversation(id).Settings.Title;
            }
            else
            {
                if (id < 0)
                {
                    return this.GetGroup(id).Name;
                }
                else
                {
                    var user = this.GetUser(id);
                    return user.FirstName + " " + user.LastName;
                }
            }
        }

        public User GetUser(int UserId)
        {
            if (!this.Users.Exists(u => u.UserId == UserId)) this.Update(UserId);
            return this.Users.Find(u => u.UserId == UserId);
        }

        public Group GetGroup(int group_id)
        {
            if (!this.Groups.Exists(g => g.Id == group_id)) this.Update(group_id);
            return this.Groups.Find(g => g.Id == group_id);
        }

        public ConversationInfo GetConversation(int peer_id)
        {
            if (!this.Conversations.Exists(c => c.Peer.Id == peer_id)) App.VK.Messages.GetConversationsById(new List<int> { peer_id }, "photo_200,online_info");
            return this.Conversations.Find(c => c.Peer.Id == peer_id);
        }

        public void Update(int id)
        {
            if (id > 0)
            {
                if (id > Limits.Messages.PEERSTART)
                {
                    App.VK.Messages.GetConversationsById(new List<int> { id }, "photo_200,online_info");
                }
                else App.VK.Users.Get(new List<int> { id }, "photo_200,online_info");
            }
            else App.VK.Groups.GetById(new List<int> { id }, "photo_200");
        }
    }
}
