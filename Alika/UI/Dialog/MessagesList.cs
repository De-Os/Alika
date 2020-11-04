using Alika.Libs;
using Alika.Libs.VK.Responses;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Alika.UI.Dialog
{
    [Windows.UI.Xaml.Data.Bindable]
    public class MessagesList : Grid
    {
        public int PeerId;

        public MessagesListView Messages;

        public MessagesList(int peer_id)
        {
            this.PeerId = peer_id;
            this.Children.Add(new ProgressRing
            {
                Height = 50,
                Width = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsActive = true
            });

            this.Load();
        }

        private void Load()
        {
            this.Messages = new MessagesListView(this.PeerId)
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
                this.AddReadScroll();
            };
        }

        public void AddReadScroll()
        {
            if (this.Parent is ScrollViewer scroll)
            {
                scroll.ViewChanging += (a, b) =>
                {
                    var msgs = this.Messages.Items.Where(i =>
                        i is SwipeControl s
                        && s.Content is MessageBox msg
                        && msg.Message != null
                        && msg.Message.Bubble.Message.FromId != App.VK.UserId
                        && !msg.Read).Select(i => (i as SwipeControl).Content as MessageBox).ToList();
                    if (msgs.Count > 0)
                    {
                        msgs.Reverse();
                        foreach (var msg in msgs)
                        {
                            if (scroll.IsElementVisible(msg))
                            {
                                try
                                {
                                    if (App.VK.Messages.MarkAsRead(this.PeerId, new System.Collections.Generic.List<int> { msg.Message.Bubble.Message.Id }) == 1)
                                    {
                                        msg.Read = true;
                                    }
                                }
                                catch (Exception e)
                                {
                                    System.Diagnostics.Debug.WriteLine(e.Message);
                                }
                                return;
                            }
                        }
                    }
                };
            }
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
            public int PeerId;

            public delegate void MessageAdded(bool isNew);

            public event MessageAdded OnNewMessage;

            public MessagesListView(int peer_id)
            {
                this.PeerId = peer_id;

                Task.Factory.StartNew(() =>
                {
                    var messages = App.VK.Messages.GetHistory(this.PeerId).Items;
                    messages.Reverse();
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () =>
                        {
                            foreach (var msg in messages) this.AddNewMessage(msg);
                        }
                    });
                });

                App.LP.OnNewMessage += (msg) =>
                {
                    if (msg.PeerId == this.PeerId) this.AddNewMessage(msg);
                };
            }

            public void AddNewMessage(Message message)
            {
                App.UILoop.AddAction(new UITask
                {
                    Action = () =>
                    {
                        var msg = new MessageBox(message);
                        msg.Loaded += (a, b) => this.OnNewMessage?.Invoke(true);
                        if (this.Items.Count > 0)
                        {
                            if ((this.Items.Last(i => i is SwipeControl) as SwipeControl).Content is MessageBox prev && prev.Message != null && msg.Message != null)
                            {
                                if (prev.Message.Bubble.Message.Date.ToDateTime().Date != message.Date.ToDateTime().Date)
                                {
                                    this.Items.Add(new ListViewItem
                                    {
                                        Content = new DateSeparator(message.Date.ToDateTime()),
                                        HorizontalAlignment = HorizontalAlignment.Center,
                                        HorizontalContentAlignment = HorizontalAlignment.Center
                                    });
                                }
                                if (prev.Message.Bubble.Message.FromId == message.FromId && this.Items.Last() is SwipeControl)
                                {
                                    prev.Message.Ava.Visibility = Visibility.Collapsed;
                                    Thickness prevMargin = prev.Message.Bubble.Border.Margin;
                                    prevMargin.Bottom = 2.5;
                                    prev.Message.Bubble.Border.Margin = prevMargin;
                                    CornerRadius corners = prev.Message.Bubble.Border.CornerRadius;
                                    CornerRadius msg_corners = msg.Message.Bubble.Border.CornerRadius;
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
                                    prev.Message.Bubble.Border.CornerRadius = corners;
                                    msg.Message.Bubble.Border.CornerRadius = msg_corners;
                                    msg.Message.Bubble.UserName.Visibility = Visibility.Collapsed;
                                    msg.Message.Bubble.Border.Margin = new Thickness(10, 2.5, 10, 5);
                                }
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
                        var msg = new MessageBox(message);
                        if (this.Items.Count > 0)
                        {
                            if ((this.Items.First(i => i is SwipeControl) as SwipeControl).Content is MessageBox next && next.Message.Bubble.Message.FromId == message.FromId && next.Message != null && msg.Message != null)
                            {
                                next.Message.Ava.Visibility = Visibility.Visible;
                                next.Message.Bubble.UserName.Visibility = Visibility.Collapsed;
                                msg.Message.Ava.Visibility = Visibility.Collapsed;
                                Thickness prevMargin = next.Message.Bubble.Border.Margin;
                                prevMargin.Top = 2.5;
                                next.Message.Bubble.Border.Margin = prevMargin;
                                CornerRadius corners = next.Message.Bubble.Border.CornerRadius;
                                CornerRadius msg_corner = msg.Message.Bubble.Border.CornerRadius;
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
                                msg.Message.Bubble.Border.CornerRadius = msg_corner;
                                next.Message.Bubble.Border.CornerRadius = corners;
                                msg.Message.Bubble.Border.Margin = new Thickness(10, 5, 10, 2.5);
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
                        var reply = (App.MainPage.Dialog.Children[0] as Dialog).ReplyGrid;
                        var message = msg.Message.Bubble.Message;
                        if (reply.Content is Dialog.ReplyMessage prev && prev.Message.Id == message.Id) return;
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
                        var reply = (App.MainPage.Dialog.Children[0] as Dialog).ReplyGrid;
                        var msg = message.Message.Bubble.Message;
                        if (reply.Content is Dialog.ReplyMessage prev && prev.Message.Id == msg.Id) return;
                        reply.Content = new Dialog.ReplyMessage(msg);
                    };
                    leftItems.Add(item);

                    this.LeftItems = leftItems;
                }
            }
        }
    }
}