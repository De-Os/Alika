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

        public ScrollViewer Scroll = new ScrollViewer
        {
            HorizontalScrollMode = ScrollMode.Disabled,
            VerticalScrollMode = ScrollMode.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
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
            Task.Run(() =>
            {
                App.UILoop.AddAction(new UITask
                {
                    Action = () =>
                    {
                        this.Messages = new MessagesListView(this.peer_id)
                        {
                            SelectionMode = ListViewSelectionMode.None
                        };
                        this.Children.Clear();
                        this.Children.Add(this.Scroll);
                        this.Scroll.Content = this.Messages;

                        this.Messages.Loaded += (a, b) =>
                        {
                            this.Scroll.ChangeView(null, double.MaxValue, null);
                            this.Messages.SizeChanged += (c, d) => this.NewMessageScroll();
                        };
                    }
                });
            });
        }

        private void NewMessageScroll()
        {
            if (this.Messages.Items.LastOrDefault(l => l != this.Messages.Items.LastOrDefault() as UIElement) is UIElement msg)
            {
                if (this.Scroll.IsElementVisible(msg))
                {
                    this.Scroll.ChangeView(null, double.MaxValue, null);
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

                var messages = App.vk.Messages.GetHistory(this.peer_id).messages;
                messages.Reverse();
                messages.ForEach((Message msg) => this.AddNewMessage(msg));

                App.lp.OnNewMessage += (msg) =>
                {
                    if (msg.peer_id == this.peer_id) App.UILoop.RunAction(new UITask
                    {
                        Action = () => this.AddNewMessage(msg)
                    });
                };
            }

            public void AddNewMessage(Message message)
            {
                var msg = new MessageBox(message, this.peer_id);
                App.UILoop.AddAction(new UITask
                {
                    Action = () =>
                    {
                        msg.Loaded += (a, b) => this.OnNewMessage?.Invoke(true);
                        if (this.Items.Count > 0)
                        {
                            if (this.Items.LastOrDefault() is SwipeMessage s && s.Message is MessageBox prev && prev.message.textBubble.message.from_id == message.from_id)
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
                        this.Items.Add(new SwipeMessage(msg));
                    }
                });
            }

            public void AddOldMessage(Message message)
            {
                var msg = new MessageBox(message, this.peer_id);
                App.UILoop.AddAction(new UITask
                {
                    Action = () =>
                    {
                        if (this.Items.Count > 0)
                        {
                            if (this.Items.FirstOrDefault() is SwipeMessage s && s.Message is MessageBox next && next.message.textBubble.message.from_id == message.from_id)
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
                        this.Items.Add(new SwipeMessage(msg));
                    }
                });
            }

            public class SwipeMessage : SwipeControl
            {
                public MessageBox Message;

                public SwipeMessage(MessageBox message)
                {
                    this.Message = message;
                    this.Content = this.Message;

                    this.GenerateItems();
                }

                public void GenerateItems()
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
                        Text = Utils.LocString("Dialog/Reply")
                    };
                    item.Invoked += (a, b) =>
                    {
                        var reply = (App.main_page.dialog.Children[0] as Dialog).reply_grid;
                        var msg = this.Message.message.textBubble.message;
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
