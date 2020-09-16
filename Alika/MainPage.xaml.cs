using Alika.Libs.VK.Responses;
using Alika.UI;
using Alika.UI.Dialog;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Alika
{
    public sealed partial class MainPage : Page
    {
        public ChatsList chats_list = new ChatsList();
        public int peer_id;

        public MainPage()
        {
            this.InitializeComponent();

            this.chats_scroll.Content = this.chats_list;
            this.chats_scroll.ViewChanged += this.OnChatsScroll;
            this.chats_list.SelectionChanged += this.OnChatSelection;

            Task.Factory.StartNew(() =>
            {
                // Updating cache stickers on app startup
                App.UILoop.AddAction(new UITask
                {
                    Action = () => App.cache.Update(App.vk.GetStickers().items)
                });
                App.cache.Update(App.vk.GetStickersKeywords().dictionary);
            });
        }

        private void OnChatSelection(object sender, SelectionChangedEventArgs e)
        {
            App.UILoop.AddAction(new UITask
            {
                Action = () =>
                {
                    ChatItem selected = this.chats_list.SelectedItem as ChatItem;
                    if (this.dialog.Children.Count > 0)
                    {
                        MessagesList old = this.dialog.Children[0] as MessagesList;
                        if (old.peer_id == selected.peer_id) return;
                        (old.stickers.Flyout as Flyout).Content = null; // Remove previous flyout to prevent crash on stickers opening
                    }
                    this.dialog.Children.Clear();
                    var list = new MessagesList(selected.peer_id);
                    this.dialog.Children.Add(list);
                    this.dialog.Children.Add(list.stickers_suggestions);
                    this.peer_id = selected.peer_id;
                },
                Priority = CoreDispatcherPriority.High
            });
        }

        /// <summary>
        /// LongPoll updates processing
        /// </summary>
        /// <param name="updates">Updates</param>
        public void OnLpUpdates(JToken updates)
        {
            App.UILoop.AddAction(new UITask
            {
                Action = () =>
                {
                    this.chats_list.ProcessUpdates(updates);
                    if (this.chats_list.SelectedIndex != -1)
                    {
                        ChatItem selected = this.chats_list.SelectedItem as ChatItem;
                        foreach (JToken update in updates)
                        {
                            if ((int)update[0] == 4)
                            {
                                Message msg = new Message(update);
                                if (update[7] != null) msg = App.vk.Messages.GetById(new List<int> { msg.id }).messages[0];
                                if (selected.peer_id == msg.peer_id)
                                {
                                    MessagesList list = this.dialog.Children[0] as MessagesList;
                                    list.AddMessage(msg, true);
                                    this.chats_scroll.ChangeView(null, 0, null);
                                }
                            }
                        }
                    }
                },
                Priority = CoreDispatcherPriority.Low
            });
        }

        /// <summary>
        /// Loading new chats when user scrolled to bottom
        /// </summary>
        public void OnChatsScroll(object sender, ScrollViewerViewChangedEventArgs e) // TODO: Fix scrolling
        {
            if (e.IsIntermediate)
            {
                App.UILoop.AddAction(new UITask
                {
                    Action = () => {
                        if (this.chats_scroll.VerticalOffset == this.chats_scroll.ScrollableHeight)
                        {
                            double height = this.chats_scroll.ScrollableHeight;
                            this.chats_scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                            this.chats_scroll.VerticalScrollMode = ScrollMode.Disabled;
                            this.chats_list.LoadChats(offset: 1, count: 25, start_msg_id: this.chats_list.Items.Cast<ChatItem>().Select(item => item as ChatItem).ToList().Last().message.id);
                            this.chats_scroll.ChangeView(null, height, null);
                            this.chats_scroll.VerticalScrollMode = ScrollMode.Enabled;
                            this.chats_scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                        }
                    }
                });
            }
        }
    }
}
