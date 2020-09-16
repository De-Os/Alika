using Alika.Libs;
using Alika.Libs.VK.Responses;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI.Dialog
{

    /// <summary>
    /// Dialog grid
    /// </summary>
    public partial class MessagesList : Grid
    {
        public int peer_id { get; set; }
        public TopMenu top_menu;
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
            this.top_menu = new UI.Dialog.TopMenu(this.peer_id);

            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60, GridUnitType.Auto) });

            Grid.SetRow(this.top_menu, 0);
            Grid.SetRow(this.msg_scroll, 1);
            Grid.SetRow(this.bottom_menu, 2);

            this.Children.Add(this.top_menu);
            this.Children.Add(this.msg_scroll);
            this.Children.Add(this.bottom_menu);

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
            List<Message> messages = App.vk.Messages.GetHistory(this.peer_id).messages;
            messages.Reverse();
            messages.ForEach((Message msg) => this.AddMessage(msg, true));
        }

        public void AddMessage(Message message, bool isNew = false)
        {
            App.UILoop.AddAction(new UITask
            {
                Action = () =>
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
                                }
                                else
                                {
                                    corners.BottomRight = 0;
                                    msg_corners.TopRight = 0;
                                }
                                msg.message.textBubble.border.CornerRadius = msg_corners;
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
                                }
                                else
                                {
                                    corners.TopRight = 0;
                                    msg_corner.BottomRight = 0;
                                }
                                msg.message.textBubble.border.CornerRadius = msg_corner;
                                next.message.textBubble.border.CornerRadius = corners;
                                msg.message.textBubble.border.Margin = new Thickness(10, 5, 10, 2.5);
                            }
                        }
                        this.messages.Items.Insert(0, msg);
                    }
                }
            });
        }
    }
}
