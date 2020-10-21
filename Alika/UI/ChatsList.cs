using Alika.Libs;
using Alika.Libs.VK.Responses;
using Alika.Misc;
using Alika.UI.Misc;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
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
        public ChatsList Chats;
        public PinnedChatsList PinnedChats;

        public Button MsgExport = new Button
        {
            Content = new ProgressRing
            {
                IsActive = true,
                Width = 20,
                Height = 20
            },
            Background = Coloring.Transparent.Full,
            CornerRadius = new CornerRadius(10),
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(5, 0, 0, 0)
        };

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
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            Load();
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


            async void Load()
            {
                var pinned = await Config.GetPinnedChats();
                this.Chats = new ChatsList(pinned);
                this.PinnedChats = new PinnedChatsList(pinned);

                Grid.SetRow(this.PinnedChats, 0);
                Grid.SetRow(this.Chats, 1);

                grid.Children.Add(this.PinnedChats);
                grid.Children.Add(this.Chats);
                grid.Children.Add(this.FoundChats);

                this.Scroll.ViewChanging += (a, b) =>
                {
                    if (b.FinalView.VerticalOffset == (a as ScrollViewer).ScrollableHeight)
                    {
                        this.Chats.LoadChats(0, start_msg_id: this.Chats.Items.Select(i => i as ChatsList.ChatItem).ToList().Last().message.id);
                    }
                };
                this.Chats.SelectionChanged += (a, b) => { if (this.Chats.SelectedIndex != -1) this.PinnedChats.SelectedIndex = -1; };
                this.PinnedChats.SelectionChanged += (a, b) => { if (this.PinnedChats.SelectedIndex != -1) this.Chats.SelectedIndex = -1; };
            }
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
            var menu = new Button
            {
                Background = Coloring.Transparent.Full,
                Content = new Image
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Source = new SvgImageSource(new System.Uri(Utils.AssetTheme("fly_menu.svg"))),
                    Width = 20,
                    Height = 20
                },
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0),
                Flyout = this.GetMenu()
            };
            this.Menu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.AddMenuElement(menu);

            this.SearchBar.TextChanging += this.SearchChat;
            this.Menu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.AddMenuElement(this.SearchBar);

            this.Menu.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.AddMenuElement(this.MsgExport);
        }

        private MenuFlyout GetMenu()
        {
            var menu = new MenuFlyout();

            var settings = new MenuFlyoutItem
            {
                Icon = new FontIcon
                {
                    Glyph = "\uE713"
                },
                Text = Utils.LocString("Settings")
            };
            settings.Click += (a, b) => new Settings();
            menu.Items.Add(settings);

            var loadExport = new MenuFlyoutItem
            {
                Icon = new FontIcon
                {
                    Glyph = "\uE8DE"
                },
                Text = Utils.LocString("Dialog/ExportLoad")
            };
            loadExport.Click += (a, b) => new DialogExportReader();
            loadExport.RightTapped += (a, b) => new DialogExportReader(Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down));
            menu.Items.Add(loadExport);

            return menu;
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

                    App.UILoop.AddAction(new UITask
                    {
                        Action = () =>
                        {
                            foreach (var conv in convs.conversations)
                            {
                                this.FoundChats.Items.Add(new ChatsList.ChatItem(conv.peer.id));
                            }
                        }
                    });
                }
            });
        }
    }

    [Windows.UI.Xaml.Data.Bindable]
    public class ChatsList : ListView
    {
        public List<int> IgnorePeers = new List<int>();
        public ChatsList(List<int> pinned = null)
        {
            if (pinned != null) this.IgnorePeers = pinned;
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
            this.LoadChats(0);
            App.lp.OnNewMessage += this.ProcessMessage;
        }

        public void LoadChats(int offset, int count = 50, int start_msg_id = 0)
        {
            Task.Factory.StartNew(() =>
              {
                  if (start_msg_id > 0) offset++;
                  var conversations = App.vk.Messages.GetConversations(count: count, offset: offset, fields: "photo_200,online_info", start_message_id: start_msg_id).conversations;
                  foreach (var conv in conversations)
                  {
                      if (!this.IgnorePeers.Contains(conv.conversation.peer.id))
                      {
                          App.UILoop.RunAction(new UITask
                          {
                              Action = () => this.Items.Add(new ChatItem(conv.conversation.peer.id, conv.last_message, unread: conv.conversation.unread_count)),
                              Priority = CoreDispatcherPriority.High
                          });
                      }
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
            public delegate void Pin();
            public Pin OnPin;
            public Pin OnUnPin;

            public int peer_id;
            public Message message;
            public Grid grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center
            };
            public Grid textGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
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
            public Image PinImage = new Image
            {
                Source = new SvgImageSource(new System.Uri(Utils.AssetTheme("pin.svg"))),
                Height = 10,
                Width = 10,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 0, 5, 0),
                Visibility = Visibility.Collapsed
            };
            private bool _pinned = false;
            private bool Pinned
            {
                get
                {
                    return this._pinned;
                }
                set
                {
                    var chats = App.main_page.chats_grid.Content as ChatsHolder;
                    this.RemoveParent();
                    if (value)
                    {
                        chats.PinnedChats.Items.Add(this);
                        this.PinImage.Visibility = Visibility.Visible;
                        this.OnPin?.Invoke();
                        Config.AddPinnedChat(this.peer_id);
                    }
                    else
                    {
                        chats.Chats.Items.Insert(0, this);
                        this.PinImage.Visibility = Visibility.Collapsed;
                        this.OnUnPin?.Invoke();
                        Config.RemovePinnedChat(this.peer_id);
                    }
                    this._pinned = value;
                }
            }
            private MenuFlyout Flyout = new MenuFlyout();
            private Border _unreadCount = new Border
            {
                BorderThickness = new Thickness(0),
                Background = Coloring.InvertedTransparent.Percent(20),
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Margin = new Thickness(5, 2, 5, 2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = "0",
                    FontSize = 12.5
                },
                Visibility = Visibility.Collapsed,
                MinWidth = 20
            };
            private int UnreadCount
            {
                get
                {
                    return int.Parse((this._unreadCount.Child as TextBlock).Text);
                }
                set
                {
                    (this._unreadCount.Child as TextBlock).Text = value.ToString();
                    this._unreadCount.Visibility = value == 0 ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            private TextBlock Date = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            private Image ReadState = new Image
            {
                Width = 12.5,
                Height = 12.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Source = new SvgImageSource(new Uri(Utils.AssetTheme("check.svg"))),
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 5, 0)
            };

            public ChatItem(int peer_id, Message last_msg, bool pinned = false, int unread = 0)
            {
                this.peer_id = peer_id;
                this._pinned = pinned;

                this.Render();

                this.UpdateMsg(last_msg);
                this.UnreadCount = unread;

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
                App.lp.OnNewMessage += (a) =>
                {
                    if (a.peer_id == this.peer_id && a.from_id != App.vk.user_id) App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.UnreadCount++
                    });
                };
                App.lp.OnReadMessage += (a) =>
                {
                    if (a.peer_id == this.peer_id)
                    {
                        App.UILoop.AddAction(new UITask
                        {
                            Action = () =>
                            {
                                if (this.message.id <= a.msg_id)
                                {
                                    this.ReadState.Source = new SvgImageSource(new Uri(Utils.AssetTheme("double_check.svg")));
                                }
                                this.UnreadCount = a.unread;
                            }
                        });
                    }
                };
                var pin = new MenuFlyoutItem
                {
                    Text = pinned ? Utils.LocString("Dialog/Unpin") : Utils.LocString("Dialog/Pin"),
                    Icon = new FontIcon
                    {
                        Glyph = pinned ? "\uE735" : "\uE734"
                    }
                };
                this.Flyout.Items.Add(pin);
                this.OnPin += () =>
                {
                    pin.Text = Utils.LocString("Dialog/Unpin");
                    pin.Icon = new FontIcon
                    {
                        Glyph = "\uE735"
                    };
                };
                this.OnUnPin += () =>
                {
                    pin.Text = Utils.LocString("Dialog/Pin");
                    pin.Icon = new FontIcon
                    {
                        Glyph = "\uE734"
                    };
                };
                pin.Click += (a, b) => this.Pinned = !this.Pinned;
            }

            public ChatItem(int peer_id)
            {
                this.peer_id = peer_id;
                this.Render();
            }

            private void Render()
            {
                this.Height = 70;
                this.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                this.grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Avatar
                this.grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Text fields
                this.grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Pin icon
                this.textGrid.RowDefinitions.Add(new RowDefinition()); // Chat name
                this.textGrid.RowDefinitions.Add(new RowDefinition()); // Message text
                this.LoadAvatar();
                this.nameBlock.Text = App.cache.GetName(this.peer_id);
                Grid.SetRow(this.nameBlock, 0);
                Grid.SetRow(this.textBlock, 1);
                this.textGrid.Children.Add(this.nameBlock);
                this.textGrid.Children.Add(this.textBlock);
                Grid.SetColumn(this.textGrid, 1);
                this.grid.Children.Add(this.textGrid);

                var indicators = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(2.5, 0, 0, 0)
                };
                var topIndicators = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 2.5, 0, 2.5)
                };
                var bottomIndicators = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 2.5, 0, 2.5)
                };

                topIndicators.Children.Add(this.ReadState);
                topIndicators.Children.Add(this.Date);
                bottomIndicators.Children.Add(this._unreadCount);
                bottomIndicators.Children.Add(this.PinImage);

                indicators.Children.Add(topIndicators);
                indicators.Children.Add(bottomIndicators);
                Grid.SetColumn(indicators, 2);
                this.grid.Children.Add(indicators);
                this.Content = this.grid;

                if (this._pinned) this.PinImage.Visibility = Visibility.Visible;

                var export = new MenuFlyoutItem
                {
                    Icon = new FontIcon
                    {
                        Glyph = "\uEE71"
                    },
                    Text = Utils.LocString("Dialog/Export")
                };
                export.Click += (a, b) =>
                {
                    if ((App.main_page.chats_grid.Content as ChatsHolder).MsgExport.Visibility == Visibility.Collapsed)
                    {
                        var popup = new Popup
                        {
                            Title = Utils.LocString("Dialog/Export")
                        };
                        var exportPopup = new ExportPopup(this.peer_id);
                        exportPopup.Confirm.Click += (c, d) => popup.Hide();
                        popup.Content = exportPopup;
                        App.main_page.popup.Children.Add(popup);
                    }
                };
                this.Flyout.Items.Add(export);
                this.RightTapped += (a, b) => this.Flyout.ShowAt(this);
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

                var date = msg.date.ToDateTime();
                var now = DateTime.Now;
                string d = "";
                if (date.Date == now.Date)
                {
                    d = date.ToString("HH:mm");
                }
                else
                {
                    if (date >= now.Date.AddDays(-7))
                    {
                        d = date.ToString("ddd");
                    }
                    else d = date.ToString("dd.MM.yy");
                }
                this.Date.Text = d;

                if (msg.from_id == App.vk.user_id)
                {
                    this.ReadState.Visibility = Visibility.Visible;
                    this.ReadState.Source = new SvgImageSource(new Uri(Utils.AssetTheme(msg.read_state == 1 ? "double_check.svg" : "check.svg")));
                }
                else this.ReadState.Visibility = Visibility.Collapsed;
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

    [Windows.UI.Xaml.Data.Bindable]
    public class PinnedChatsList : ListView
    {
        public PinnedChatsList(List<int> pinned)
        {
            this.Background = new AcrylicBrush
            {
                TintOpacity = 0.7,
                BackgroundSource = AcrylicBackgroundSource.Backdrop,
                TintColor = Coloring.Transparent.Percent(100).Color
            };
            this.SelectionChanged += (a, b) =>
            {
                if (this.SelectedItem is ChatsList.ChatItem i)
                {
                    if (i.peer_id != App.main_page.peer_id) App.main_page.peer_id = i.peer_id;
                }
            };

            if (pinned.Count > 0)
            {
                Task.Factory.StartNew(() =>
                {
                    var convs = App.vk.Messages.GetConversationsById(pinned);
                    var messages = App.vk.Messages.GetById(convs.conversations.Select(c => c.last_message_id).ToList()).messages;
                    foreach (var conv in convs.conversations)
                    {
                        App.UILoop.RunAction(new UITask
                        {
                            Action = () => this.Items.Add(new ChatsList.ChatItem(conv.peer.id, messages.Find(i => i.id == conv.last_message_id), true)),
                            Priority = CoreDispatcherPriority.High
                        });
                    }
                });
            }
        }
    }
}
