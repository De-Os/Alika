using Alika.Libs;
using Alika.Libs.VK.Responses;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Alika.UI.Dialog
{
    [Windows.UI.Xaml.Data.Bindable]
    public class MessagesList : Grid
    {
        public int peer_id;

        public MessagesListView Messages;

        public MessagesList(int peer_id)
        {
            this.peer_id = peer_id;
            this.Children.Add(new ProgressRing
            {
                Height = 50,
                Width = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsActive = true
            });

            this.Loaded += (a, b) => this.Load();
        }

        private void Load()
        {
            this.Messages = new MessagesListView(this.peer_id)
            {
                SelectionMode = ListViewSelectionMode.None,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            this.Children.Clear();
            this.Children.Add(this.Messages);

            this.Messages.Loaded += (a, b) =>
            {
                (this.Parent as ScrollViewer).ChangeView(null, double.MaxValue, null);
                this.Messages.SizeChanged += (c, d) => this.NewMessageScroll();
                this.SizeChanged += (c, d) => this.NewMessageScroll();
            };
        }

        private void NewMessageScroll()
        {
            if (this.Messages.Items.LastOrDefault(l => l != this.Messages.Items.LastOrDefault()) is UIElement msg)
            {

                if (this.Parent is ScrollViewer scroll)
                {
                    if (scroll.IsElementVisible(msg))
                    {
                        scroll.ChangeView(null, double.MaxValue, null);
                    }
                }
            }
        }

        [Windows.UI.Xaml.Data.Bindable]
        public class MessagesListView : ListView
        {
            public int peer_id;

            public delegate void MessageAdded(bool isNew);
            public event MessageAdded OnNewMessage;

            public MessagesListView(int peer_id)
            {
                this.peer_id = peer_id;

                Task.Factory.StartNew(() =>
                {
                    var messages = App.vk.Messages.GetHistory(this.peer_id).messages;
                    messages.Reverse();
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () =>
                        {
                            foreach (var msg in messages) this.AddNewMessage(msg);
                        }
                    });
                });

                App.lp.OnNewMessage += (msg) =>
                {
                    if (msg.peer_id == this.peer_id) this.AddNewMessage(msg);
                };
            }

            public void AddNewMessage(Message message)
            {
                App.UILoop.AddAction(new UITask
                {
                    Action = () =>
                    {
                        var msg = new MessageBox(message, this.peer_id);
                        msg.Loaded += (a, b) => this.OnNewMessage?.Invoke(true);
                        if (this.Items.Count > 0)
                        {
                            if (this.Items.LastOrDefault() is SwipeControl s && s.Content is MessageBox prev && prev.message.textBubble.message.from_id == message.from_id)
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
                                prev.message.textBubble.border.CornerRadius = corners;
                                msg.message.textBubble.border.CornerRadius = msg_corners;
                                msg.message.textBubble.name.Visibility = Visibility.Collapsed;
                                msg.message.textBubble.border.Margin = new Thickness(10, 2.5, 10, 5);
                            }
                        }
                        this.Items.Add(this.GetSwipeMessage(msg));
                    }
                });
            }

            public void AddOldMessage(Message message)
            {
                App.UILoop.AddAction(new UITask
                {
                    Action = () =>
                    {
                        var msg = new MessageBox(message, this.peer_id);
                        if (this.Items.Count > 0)
                        {
                            if (this.Items.FirstOrDefault() is SwipeControl s && s.Content is MessageBox next && next.message.textBubble.message.from_id == message.from_id)
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
                        msg.Loaded += (a, b) => this.OnNewMessage?.Invoke(false);
                        this.Items.Insert(0, this.GetSwipeMessage(msg));
                    }
                });
            }

            private SwipeControl GetSwipeMessage(MessageBox msg)
            {
                var leftItems = new SwipeItems
                {
                    Mode = SwipeMode.Execute
                };
                var item = new SwipeItem
                {
                    IconSource = new FontIconSource
                    {
                        Glyph = "\uE8CA"
                    },
                    Text = Utils.LocString("Dialog/Reply"),
                    Background = Coloring.Transparent.Percent(100),
                    Foreground = Coloring.InvertedTransparent.Percent(100),
                };
                item.Invoked += (a, b) =>
                    {
                        var reply = (App.main_page.dialog.Children[0] as Dialog).reply_grid;
                        var message = msg.message.textBubble.message;
                        if (reply.Content is Dialog.ReplyMessage prev && prev.Message.id == message.id) return;
                        reply.Content = new Dialog.ReplyMessage(message);
                    };
                leftItems.Add(item);
                return new SwipeControl
                {
                    Content = msg,
                    LeftItems = leftItems
                };
            }

            // Crashes on release, idk why. Use GetSwipeMessage.
            [Windows.UI.Xaml.Data.Bindable]
            public class SwipeMessage : SwipeControl
            {

                public SwipeMessage(MessageBox message)
                {
                    this.Content = message;
                    var leftItems = new SwipeItems
                    {
                        Mode = SwipeMode.Execute
                    };
                    var item = new SwipeItem
                    {
                        IconSource = new FontIconSource
                        {
                            Glyph = "\uE8CA"
                        },
                        Text = Utils.LocString("Dialog/Reply")
                    };
                    item.Invoked += (a, b) =>
                    {
                        var reply = (App.main_page.dialog.Children[0] as Dialog).reply_grid;
                        var msg = message.message.textBubble.message;
                        if (reply.Content is Dialog.ReplyMessage prev && prev.Message.id == msg.id) return;
                        reply.Content = new Dialog.ReplyMessage(msg);
                    };
                    leftItems.Add(item);

                    this.LeftItems = leftItems;
                }
            }
        }
    }
}
