using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Responses;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{

    /// <summary>
    /// Dialog grid
    /// </summary>
    public partial class MessagesList : Grid
    {
        public int peer_id { get; set; }
        public Grid top_menu = new Grid();
        public ListView messages = new ListView
        {
            SelectionMode = ListViewSelectionMode.None
        };
        public ScrollViewer msg_scroll = new ScrollViewer
        {
            HorizontalScrollMode = ScrollMode.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        public Grid bottom_menu = new Grid();
        public Grid attach_grid = new Grid
        {
            MaxHeight = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        public Grid stickers_suggestions = new Grid
        {
            Height = 100,
            Background = Coloring.Transparent.Percent(25),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Collapsed,
            CornerRadius = new CornerRadius(10)
        };
        public Grid bottom_buttons_grid = new Grid();
        public Button send_button = new Button
        {
            Content = new Image
            {
                Source = new SvgImageSource(new Uri(Utils.AssetTheme("send.svg"))),
                Height = 20
            },
            Width = 50,
            Margin = new Thickness(5, 10, 20, 10),
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = Coloring.Transparent.Full
        };
        public Button stickers = new Button
        {
            Content = new Image
            {
                Source = new SvgImageSource(new Uri(Utils.AssetTheme("sticker.svg"))),
                Height = 20
            },
            Width = 50,
            Margin = new Thickness(5, 10, 5, 10),
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = Coloring.Transparent.Full
        };
        public TextBox send_text = new TextBox
        {
            PlaceholderText = Utils.LocString("Dialog/TextBoxPlaceholder"),
            AcceptsReturn = true,
            MaxHeight = 150,
            Margin = new Thickness(5, 10, 5, 10),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        public Button attach_button = new Button
        {
            Content = new Image
            {
                Source = new SvgImageSource(new Uri(Utils.AssetTheme("clip.svg"))),
                Width = 20,
                Height = 20
            },
            Width = 50,
            Margin = new Thickness(20, 10, 5, 10),
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = Coloring.Transparent.Full
        };

        public MessagesList(int peer_id)
        {
            this.peer_id = peer_id;
            App.cache.StickersSelector.peer_id = this.peer_id;

            this.Render();

            this.LoadMessages();
            this.RegisterEvents();
        }

        public void Render()
        {
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60, GridUnitType.Auto) });

            Grid.SetRow(this.top_menu, 0);
            Grid.SetRow(this.msg_scroll, 1);
            Grid.SetRow(this.bottom_menu, 2);

            this.Children.Add(this.top_menu);
            this.Children.Add(this.msg_scroll);
            this.Children.Add(this.bottom_menu);

            this.LoadTopMenu();

            this.msg_scroll.Content = this.messages;

            this.send_text.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            this.send_text.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

            this.bottom_menu.RowDefinitions.Add(new RowDefinition());
            this.bottom_menu.RowDefinitions.Add(new RowDefinition());
            this.bottom_menu.RowDefinitions.Add(new RowDefinition());

            ScrollViewer scroll = new ScrollViewer
            {
                Content = attach_grid,
                VerticalScrollMode = ScrollMode.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(scroll, 0);
            Grid.SetRow(this.bottom_buttons_grid, 2);

            this.bottom_menu.Children.Add(scroll);
            this.bottom_menu.Children.Add(this.bottom_buttons_grid);

            this.bottom_buttons_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.bottom_buttons_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.bottom_buttons_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.bottom_buttons_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            Grid.SetColumn(this.attach_button, 0);
            Grid.SetColumn(this.send_text, 1);
            Grid.SetColumn(this.stickers, 2);
            Grid.SetColumn(this.send_button, 3);

            this.bottom_buttons_grid.Children.Add(this.attach_button);
            this.bottom_buttons_grid.Children.Add(this.send_text);
            this.bottom_buttons_grid.Children.Add(this.send_button);
            this.bottom_buttons_grid.Children.Add(this.stickers);

            this.stickers_suggestions.Children.Add(new ScrollViewer
            {
                VerticalScrollMode = ScrollMode.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            });
        }

        public void LoadTopMenu()
        {
            this.top_menu.ColumnDefinitions.Add(new ColumnDefinition());
            this.top_menu.ColumnDefinitions.Add(new ColumnDefinition());

            string title = null;
            string desc = null;
            if (this.peer_id > 0)
            {
                if (this.peer_id > Limits.Messages.PEERSTART)
                {
                    if (!App.cache.Conversations.Exists(c => c.conversation.peer.id == this.peer_id)) App.vk.messages.GetConversationsById(new List<int> { this.peer_id }, fields: "photo_200,online_info");
                    var conv = App.cache.Conversations.Find(c => c.conversation.peer.id == this.peer_id);
                    title = conv.conversation.settings.title;
                    desc = "Беседа";
                }
                else
                {
                    if (!App.cache.Users.Exists(u => u.user_id == this.peer_id)) App.vk.users.Get(new List<int> { this.peer_id }, fields: "photo_200,online_info");
                    var user = App.cache.Users.Find(u => u.user_id == this.peer_id);
                    title = user.first_name + " " + user.last_name;
                    if (user.online_info.is_online)
                    {
                        desc = Utils.LocString("Dialog/Online");
                    }
                    else
                    {
                        DateTime online = user.online_info.last_seen.ToDateTime();
                        if (online.Day == DateTime.Today.Day)
                        {
                            desc = Utils.LocString("Dialog/LastSeen").Replace("%date%", online.ToString("HH:mm"));
                        }
                        else desc = Utils.LocString("Dialog/LastSeen").Replace("%date%", online.ToString("HH:mm d.M"));
                    }
                }
            }
            else
            {
                if (!App.cache.Groups.Exists(g => g.id == this.peer_id)) App.vk.groups.GetById(new List<int> { this.peer_id }, fields: "photo_200");
                var group = App.cache.Groups.Find(g => g.id == this.peer_id);
                title = group.name;
                desc = "Сообщество";
            }

            Grid text = new Grid
            {
                Margin = new Thickness(10, 0, 0, 0)
            };
            text.RowDefinitions.Add(new RowDefinition());
            text.RowDefinitions.Add(new RowDefinition());
            Grid.SetColumn(text, 0);
            TextBlock name = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Text = title,
                FontSize = 18,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetRow(name, 0);
            text.Children.Add(name);
            TextBlock about = new TextBlock
            {
                Text = desc,
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetRow(about, 1);
            text.Children.Add(about);
            this.top_menu.Children.Add(text);
        }

        // Scroll after loaded messages list (костыль)
        public void FirstScroll(object s, SizeChangedEventArgs e)
        {
            if ((s as ListView).IsLoaded)
            {
                this.ScrollToDown();
                this.messages.SizeChanged -= this.FirstScroll;
                this.bottom_menu.SizeChanged += this.MsgScroll;
                this.messages.SizeChanged += this.MsgScroll;
            }
        }

        // Scroll on new message (костыль)
        // TODO: Fix it
        public void MsgScroll(object s, SizeChangedEventArgs e)
        {
            if (this.msg_scroll.VerticalOffset >= this.msg_scroll.ScrollableHeight * 0.9) this.ScrollToDown();
        }

        public void ScrollToDown() => this.msg_scroll.ChangeView(null, double.MaxValue, null);

        public void LoadMessages()
        {
            List<Message> messages = App.vk.messages.GetHistory(this.peer_id).messages;
            messages.Reverse();
            messages.ForEach((Message msg) => this.AddMessage(msg, true));
        }

        public async void AddMessage(Message message, bool isNew = false)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MessageBox msg = new MessageBox(message, this.peer_id);

                if (isNew)
                {
                    if (this.messages.Items.Count > 0)
                    {
                        MessageBox prev = this.messages.Items[this.messages.Items.Count - 1] as MessageBox;
                        // Change corner radius on previous message and remove avatar if it is from same user
                        if (prev.message.textBubble.message.from_id == message.from_id)
                        {
                            prev.message.avatar.Visibility = Visibility.Collapsed;
                            Thickness prevMargin = prev.message.textBubble.border.Margin;
                            prevMargin.Bottom = 2.5;
                            prev.message.textBubble.border.Margin = prevMargin;
                            CornerRadius corners = prev.message.textBubble.border.CornerRadius;
                            CornerRadius msg_corners = msg.message.textBubble.border.CornerRadius;
                            if (prev.HorizontalContentAlignment == HorizontalAlignment.Left)
                            {
                                corners.BottomLeft = 0;
                                msg_corners.TopLeft = 0;
                                msg.message.textBubble.border.CornerRadius = msg_corners;
                            }
                            else
                            {
                                corners.BottomRight = 0;
                                msg_corners.TopRight = 0;
                                msg.message.textBubble.border.CornerRadius = msg_corners;
                            }
                            prev.message.textBubble.border.CornerRadius = corners;
                            msg.message.textBubble.name.Visibility = Visibility.Collapsed;
                            msg.message.textBubble.border.Margin = new Thickness(10, 2.5, 10, 5);
                        }
                    }
                    this.messages.Items.Add(msg);
                }
                else
                {
                    if (this.messages.Items.Count > 0)
                    {
                        MessageBox next = this.messages.Items[0] as MessageBox;
                        // Change corner radius on next message and remove avatar if it is from same user
                        if (next.message.textBubble.message.from_id == message.from_id)
                        {
                            next.message.avatar.Visibility = Visibility.Visible;
                            next.message.textBubble.name.Visibility = Visibility.Collapsed;
                            msg.message.avatar.Visibility = Visibility.Collapsed;
                            Thickness prevMargin = next.message.textBubble.border.Margin;
                            prevMargin.Top = 2.5;
                            next.message.textBubble.border.Margin = prevMargin;
                            CornerRadius corners = next.message.textBubble.border.CornerRadius;
                            CornerRadius msg_corner = msg.message.textBubble.border.CornerRadius;
                            if (next.HorizontalContentAlignment == HorizontalAlignment.Left)
                            {
                                corners.TopLeft = 0;
                                msg_corner.BottomLeft = 0;
                                msg.message.textBubble.border.CornerRadius = msg_corner;
                            }
                            else
                            {
                                corners.TopRight = 0;
                                msg_corner.BottomRight = 0;
                                msg.message.textBubble.border.CornerRadius = msg_corner;
                            }
                            next.message.textBubble.border.CornerRadius = corners;
                            msg.message.textBubble.border.Margin = new Thickness(10, 5, 10, 2.5);
                        }
                    }
                    this.messages.Items.Insert(0, msg);
                }
            });
        }
    }
}
