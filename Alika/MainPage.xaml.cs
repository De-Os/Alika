﻿using Alika.Libs.VK.Responses;
using Alika.UI;
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
        public ChatsList chats_list;
        public MainPage()
        {
            this.InitializeComponent();
            this.chats_list = new ChatsList();
            ScrollViewer chats = this.FindName("chats_scroll") as ScrollViewer;
            chats.Content = this.chats_list;
            chats.ViewChanged += this.OnChatsScroll;
            this.chats_list.SelectionChanged += this.OnChatSelection;
            Task.Factory.StartNew(async () =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    App.cache.Update(App.vk.GetStickers().items);
                });
                App.cache.Update(App.vk.GetStickersKeywords().dictionary);
            });
        }

        private async void OnChatSelection(object sender, SelectionChangedEventArgs e)
        {
            await Task.Run(async () =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ChatItem selected = this.chats_list.SelectedItem as ChatItem;
                    if (this.dialog.Children.Count > 0)
                    {
                        MessagesList old = this.dialog.Children[0] as MessagesList;
                        if (old.peer_id == selected.peer_id) return;
                        (old.stickers.Flyout as Flyout).Content = null;
                    }
                    this.dialog.Children.Clear();
                    var list = new MessagesList(selected.peer_id);
                    this.dialog.Children.Add(list);
                    this.dialog.Children.Add(list.stickers_suggestions);
                });
            });
        }

        public async void OnLpUpdates(JToken updates)
        {
            await Task.Run(async () =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
                                if (update[7] != null) msg = App.vk.messages.GetById(new List<int> { msg.id }).messages[0];
                                if (selected.peer_id == msg.peer_id)
                                {
                                    MessagesList list = this.dialog.Children[0] as MessagesList;
                                    list.AddMessage(msg, true);
                                    (this.FindName("chats_scroll") as ScrollViewer).ChangeView(null, 0, null);
                                }
                            }
                        }
                    }
                });
            });
        }

        public async void OnChatsScroll(object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ScrollViewer chats = this.FindName("chats_scroll") as ScrollViewer;
                    if (chats.VerticalOffset == chats.ScrollableHeight)
                    {
                        double height = chats.ScrollableHeight;
                        chats.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                        chats.VerticalScrollMode = ScrollMode.Disabled;
                        this.chats_list.LoadChats(offset: 1, count: 25, start_msg_id: this.chats_list.Items.Cast<ChatItem>().Select(item => item as ChatItem).ToList().Last().message.id);
                        chats.ChangeView(null, height, null);
                        chats.VerticalScrollMode = ScrollMode.Enabled;
                        chats.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    }
                });
            }
        }
    }
}
