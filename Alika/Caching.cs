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
        public List<ConversationResponse> Conversations { get; set; } = new List<ConversationResponse>();
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
            User us = null;
            if (this.Users.Exists(u => u.user_id == user.user_id))
            {
                us = this.Users.Find(u => u.user_id == user.user_id);
                this.Users.RemoveAll(u => u.user_id == user.user_id);
                foreach (var field in typeof(User).GetFields())
                {
                    var value = field.GetValue(user);
                    if (value != null) field.SetValue(us, value);
                }
            }
            else us = user;
            this.Users.Add(us);
        }
        public void Update(List<Group> groups)
        {
            if (groups != null && groups.Count > 0) groups.ForEach((Group group) => this.Update(group));
        }
        public void Update(Group group)
        {
            Group gr = null;
            if (this.Groups.Exists(g => g.id == group.id))
            {
                gr = this.Groups.Find(g => g.id == group.id);
                this.Groups.RemoveAll(g => g.id == group.id);
                foreach (var field in typeof(Group).GetFields())
                {
                    var value = field.GetValue(group);
                    if (value != null) field.SetValue(gr, value);
                }
            }
            else gr = group;
            this.Groups.Add(gr);
        }
        public void Update(List<ConversationResponse> conversations)
        {
            if (conversations != null && conversations.Count > 0) conversations.ForEach((ConversationResponse conversation) =>
            {
                this.Update(conversation);
            });
        }
        public void Update(ConversationResponse conversation)
        {
            ConversationResponse conv = null;
            if (this.Conversations.Exists(c => c.conversation.peer.id == conversation.conversation.peer.id))
            {
                conv = this.Conversations.Find(c => c.conversation.peer.id == conversation.conversation.peer.id);
                this.Conversations.RemoveAll(c => c.conversation.peer.id == conversation.conversation.peer.id);
                foreach (var field in typeof(ConversationResponse).GetFields())
                {
                    var value = field.GetValue(conversation);
                    if (value != null) field.SetValue(conv, value);
                }
            }
            else conv = conversation;
            this.Conversations.Add(conv);
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
                });
            });
        }
    }
}
