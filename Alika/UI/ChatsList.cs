using Alika.Libs;
using Alika.Libs.VK.Responses;
using Alika.UI.Misc;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{
    [Windows.UI.Xaml.Data.Bindable]
    public class ChatsHolder : BlurView
    {
        private ChatsList Chats = new ChatsList();
        private Grid Menu = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 30, 0, 5),
            Padding = new Thickness(5)
        };
        private ListView FoundChats = new ListView
        {
            Visibility = Visibility.Collapsed
        };
        private TextBox SearchBar = new TextBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            PlaceholderText = Utils.LocString("Search")
        };

        public ChatsHolder()
        {
            var grid = new Grid();
            grid.Children.Add(this.Chats);
            grid.Children.Add(this.FoundChats);
            this.TopMenu = this.Menu;
            this.Content = grid;
            this.LoadMenu();

            App.lp.OnNewMessage += (msg) =>
            {
                if (msg.peer_id == App.main_page.peer_id)
                {
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.Scroll.ChangeView(null, 0, null)
                    });
                }
            };

            this.FoundChats.SelectionChanged += (a, b) =>
            {
                if (this.FoundChats.SelectedItem is ChatsList.ChatItem chat)
                {
                    App.main_page.peer_id = chat.peer_id;
                    foreach (var item in this.Chats.Items)
                    {
                        if (item is ChatsList.ChatItem c && c.peer_id == chat.peer_id) this.Chats.SelectedItem = c;
                    }
                    this.FoundChats.Items.Clear();
                    this.FoundChats.Visibility = Visibility.Collapsed;
                    this.SearchBar.Text = "";
                }
            };
        }

        protected override void UpdateColors()
        {
            var brush = new AcrylicBrush
            {
                TintOpacity = 0.7,
                BackgroundSource = AcrylicBackgroundSource.Backdrop,
                TintColor = Coloring.Transparent.Percent(100).Color
            };
            this._topmenu.Background = brush;
            this._bottomMenu.Background = brush;
            this.FoundChats.Background = brush;
        }

        private void LoadMenu()
        {
            var settings = new Button
            {
                Background = Coloring.Transparent.Full,
                Content = new Image
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Source = new SvgImageSource(new System.Uri(Utils.AssetTheme("settings.svg"))),
                    Width = 20,
                    Height = 20
                },
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            settings.Click += (a, b) => new Settings();
            this.Menu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.AddMenuElement(settings);

            this.SearchBar.TextChanging += this.SearchChat;
            this.Menu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.AddMenuElement(this.SearchBar);
        }


        private void AddMenuElement(FrameworkElement element)
        {
            Grid.SetColumn(element, this.Menu.ColumnDefinitions.Count - 1);
            this.Menu.Children.Add(element);
        }

        private async void SearchChat(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            if (sender.Text.Length == 0)
            {
                this.FoundChats.Items.Clear();
                this.FoundChats.Visibility = Visibility.Collapsed;
                return;
            }
            async Task<bool> KeepsTyping()
            {
                string text = sender.Text;
                await Task.Delay(500);
                return text != sender.Text;
            }
            if (await KeepsTyping()) return;
            string query = sender.Text;
            this.FoundChats.Items.Clear();
            await Task.Factory.StartNew(() =>
            {
                var convs = App.vk.Messages.SearchConversations(query, fields: "photo_200,online_info");
                if (convs.conversations.Count > 0)
                {
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.FoundChats.Visibility = Visibility.Visible
                    });
                    foreach (var conv in convs.conversations)
                    {
                        App.UILoop.AddAction(new UITask
                        {
                            Action = () => this.FoundChats.Items.Add(new ChatsList.ChatItem(conv.peer.id))
                        });
                    }
                }
            });
        }
    }

    [Windows.UI.Xaml.Data.Bindable]
    public class ChatsList : ListView
    {
        public ChatsList()
        {
            this.Background = new AcrylicBrush
            {
                TintOpacity = 0.7,
                BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                TintColor = Coloring.Transparent.Percent(100).Color
            };
            this.SelectionChanged += (a, b) =>
            {
                if (this.SelectedItem is ChatItem i)
                {
                    if (i.peer_id != App.main_page.peer_id) App.main_page.peer_id = i.peer_id;
                }
            };
            if (this.Parent is ScrollViewer scroll)
            {
                scroll.ViewChanging += (a, b) =>
                {
                    if (b.FinalView.VerticalOffset == (a as ScrollViewer).ScrollableHeight)
                    {
                        this.LoadChats(this.Items.
                        Cast<ChatsList.ChatItem>().
                        Select(item => item as ChatsList.ChatItem).
                        ToList().
                        Last().message.id);
                    }
                };
            }
            this.LoadChats(0);
            App.lp.OnNewMessage += this.ProcessMessage;
        }

        public void LoadChats(int offset, int count = 50, int start_msg_id = 0)
        {
            Task.Factory.StartNew(() =>
              {
                  var conversations = App.vk.Messages.GetConversations(count: count, offset: offset, fields: "photo_200,online_info", start_message_id: start_msg_id).conversations;
                  List<ListViewItem> items = new List<ListViewItem>();
                  foreach (GetConversationsResponse.ConversationResponse conv in conversations)
                  {
                      App.UILoop.RunAction(new UITask
                      {
                          Action = () => this.Items.Add(new ChatItem(conv.conversation.peer.id, conv.last_message)),
                          Priority = CoreDispatcherPriority.High
                      });
                  }
              });
        }

        public void ProcessMessage(Message msg)
        {
            App.UILoop.AddAction(new UITask
            {
                Action = () =>
                {
                    foreach (var item in this.Items)
                    {
                        if (item is ChatItem chat)
                        {
                            if (msg.peer_id == chat.peer_id && this.Items.IndexOf(chat) != 0)
                            {
                                this.Items.Remove(chat);
                                this.Items.Insert(0, chat);
                                return;
                            }
                        }
                    }
                    //this.Items.Insert(0, new ChatItem(msg.peer_id, msg));
                },
                Priority = CoreDispatcherPriority.Low
            });
        }

        [Windows.UI.Xaml.Data.Bindable]
        public class ChatItem : ListViewItem
        {
            public int peer_id;
            public Message message;
            public Grid grid = new Grid();
            public Grid textGrid = new Grid();
            public TextBlock nameBlock = new TextBlock
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            public TextBlock textBlock = new TextBlock
            {
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Top
            };
            public Avatar image;
            public ChatItem(int peer_id, Message last_msg)
            {
                this.peer_id = peer_id;

                this.Render();

                this.UpdateMsg(last_msg);

                App.lp.OnNewMessage += this.OnNewMessage;
                App.lp.OnMessageEdition += (m) =>
                {
                    if (m.id == this.message.id)
                    {
                        App.UILoop.AddAction(new UITask
                        {
                            Action = () => this.UpdateMsg(m),
                            Priority = CoreDispatcherPriority.Low
                        });
                    }
                };
            }

            public ChatItem(int peer_id)
            {
                this.peer_id = peer_id;
                this.Render();
            }

            private void Render()
            {
                this.Height = 70;
                this.grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Avatar
                this.grid.ColumnDefinitions.Add(new ColumnDefinition()); // Text fields
                this.textGrid.RowDefinitions.Add(new RowDefinition()); // Chat name
                this.textGrid.RowDefinitions.Add(new RowDefinition()); // Message texzt
                this.LoadAvatar();
                this.nameBlock.Text = App.cache.GetName(this.peer_id);
                Grid.SetRow(this.nameBlock, 0);
                Grid.SetRow(this.textBlock, 1);
                this.textGrid.Children.Add(this.nameBlock);
                this.textGrid.Children.Add(this.textBlock);
                Grid.SetColumn(this.textGrid, 1);
                this.grid.Children.Add(this.textGrid);
                this.Content = this.grid;
            }

            private void OnNewMessage(Message msg)
            {
                if (msg.peer_id == this.peer_id)
                {
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.UpdateMsg(msg),
                        Priority = CoreDispatcherPriority.Low
                    });
                }
            }

            public void LoadAvatar()
            {
                this.image = new Avatar(this.peer_id)
                {
                    Height = 50,
                    Width = 50,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0),
                    OpenInfoOnClick = false
                };
                Grid.SetColumn(this.image, 0);
                this.grid.Children.Add(this.image);
            }

            public void UpdateMsg(Message msg)
            {
                this.message = msg;
                string text = this.FormatName(msg.from_id) + ": ";
                this.textBlock.Text = text + this.message.ToCompactText();
            }

            private string FormatName(int id)
            {
                if (id == App.vk.user_id) return Utils.LocString("Dialog/You");
                var name = App.cache.GetName(id);
                if (name.Count(c => c == ' ') != 1 || this.peer_id > Libs.VK.Limits.Messages.PEERSTART) return name;
                return name.Split(" ")[0];
            }
        }
    }
}
