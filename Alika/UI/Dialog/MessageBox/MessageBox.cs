using Alika.Libs;
using Alika.Libs.VK.Responses;
using Alika.UI.Dialog;
using Alika.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{
    /// <summary>
    /// Message box which holds MessageGrid (needed for future features)
    /// </summary>
    [Bindable]
    public class MessageBox : ContentControl
    {
        public MessageGrid Message;
        public bool Read = false;

        public MessageBox(Message msg, bool isStatic = false)
        {
            this.HorizontalAlignment = HorizontalAlignment.Stretch;

            System.Diagnostics.Debug.WriteLine(ObjectDumper.Dump(msg));
            if (msg.Action == null)
            {
                this.HorizontalContentAlignment = msg.FromId == App.VK.UserId ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                this.Message = new MessageGrid(msg, isStatic);
                this.Content = this.Message;
            }
            else
            {
                this.HorizontalContentAlignment = HorizontalAlignment.Center;
                this.Content = new MessageAction(msg);
            }

            this.Read = msg.ReadState == 1;
        }

        /// <summary>
        /// Grid with avatar & message content
        /// </summary>
        [Bindable]
        public class MessageGrid : Grid
        {
            public TextBubble Bubble;
            public Avatar Ava;

            public TextBlock time = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Coloring.InvertedTransparent.Percent(50)
            };

            public StackPanel states = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            public Button Reply = new Button
            {
                Content = new FontIcon
                {
                    Glyph = "\uE97A",
                    FontSize = 15
                },
                Background = Coloring.Transparent.Full,
                CornerRadius = new CornerRadius(15),
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5)
            };

            private bool _edited = false;

            private bool Edited
            {
                set
                {
                    if (this._edited) return;
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.states.Children.Insert(0, new Image
                        {
                            Width = 15,
                            Height = 15,
                            HorizontalAlignment = App.VK.UserId == this.Bubble.Message.FromId ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Source = new SvgImageSource(new Uri(Utils.AssetTheme("pen.svg")))
                        })
                    });
                    this._edited = true;
                }
            }

            private readonly List<Message> _editions = new List<Message>();

            public MessageGrid(Message msg, bool isStatic = false)
            {
                this.MinWidth = 200;
                this.states.HorizontalAlignment = App.VK.UserId == msg.FromId ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                this.Bubble = new TextBubble(msg, isStatic);
                this.LoadAvatar(msg.FromId);

                var date = msg.Date.ToDateTime();
                if (date.Date != DateTime.Today.Date)
                {
                    if (date.Year != DateTime.Today.Year)
                    {
                        this.time.Text = date.ToString("HH:mm, dd.MM.yy");
                    }
                    else this.time.Text = date.ToString("HH:mm, dd.MM");
                }
                else this.time.Text = date.ToString("HH:mm");

                var stateHolder = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                // Changing avatar position if message from current user
                if (msg.FromId == App.VK.UserId)
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition());
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

                    if (!isStatic)
                    {
                        var conv = App.Cache.GetConversation(msg.PeerId);
                        var readState = new Image
                        {
                            Width = 15,
                            Height = 15,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Source = new SvgImageSource(new Uri(Utils.AssetTheme(msg.Id <= (conv.InRead > conv.OutRead ? conv.InRead : conv.OutRead) ? "double_check.svg" : "check.svg")))
                        };
                        App.LP.OnReadMessage += (rs) =>
                        {
                            if (rs.PeerId == msg.PeerId && rs.MsgId >= msg.Id) App.UILoop.AddAction(new UITask
                            {
                                Priority = Windows.UI.Core.CoreDispatcherPriority.Low,
                                Action = () => readState.Source = new SvgImageSource(new Uri(Utils.AssetTheme("double_check.svg")))
                            });
                        };
                        this.states.Children.Add(readState);
                    }

                    Grid.SetColumn(this.Reply, 0);
                    Grid.SetColumn(stateHolder, 1);
                    Grid.SetColumn(this.Bubble, 2);
                    Grid.SetColumn(this.Ava, 3);
                }
                else
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                    this.ColumnDefinitions.Add(new ColumnDefinition());
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetColumn(this.Ava, 0);
                    Grid.SetColumn(this.Bubble, 1);
                    Grid.SetColumn(stateHolder, 2);
                    Grid.SetColumn(this.Reply, 3);
                }

                stateHolder.Children.Add(this.states);
                stateHolder.Children.Add(this.time);
                this.Children.Add(stateHolder);
                this.Children.Add(this.Bubble);
                this.Children.Add(this.Ava);

                if (!isStatic)
                {
                    if (msg.UpdateTime > 0) this.Edited = true;
                    App.LP.OnMessageEdition += (m) =>
                    {
                        if (msg.Id == m.Id)
                        {
                            this.Edited = true;
                            this._editions.Add(m);
                        }
                    };
                    this.RightTapped += (a, b) => new MessageFlyout(msg, this._editions).ShowAt(this, b.GetPosition(b.OriginalSource as UIElement));

                    this.Children.Add(this.Reply);
                    this.Reply.Click += (a, b) =>
                    {
                        var reply = (App.MainPage.Dialog.Children[0] as Dialog.Dialog).ReplyGrid;
                        if (reply.Content is Dialog.Dialog.ReplyMessage prev && prev.Message.Id == msg.Id) return;
                        reply.Content = new Dialog.Dialog.ReplyMessage(msg);
                    };
                }
            }

            public void LoadAvatar(int user_id)
            {
                this.Ava = new Avatar(user_id)
                {
                    Height = 40,
                    Width = 40,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Thickness margin = this.Ava.Margin;
                margin.Bottom = this.Bubble.Border.Margin.Bottom + (this.Bubble.Margin.Bottom / 2);
                this.Ava.Margin = margin;
            }
        }

        /// <summary>
        /// Content bubble
        /// </summary>
        [Bindable]
        public class TextBubble : StackPanel
        {
            public Message Message;

            public Border Border = new Border
            {
                BorderThickness = new Thickness(1),
                Background = App.DarkTheme ? Coloring.MessageBox.TextBubble.Dark : Coloring.MessageBox.TextBubble.Light,
                Margin = new Thickness(10, 5, 10, 5),
                MinHeight = 30,
                MinWidth = 50,
                CornerRadius = new CornerRadius(10)
            };

            public TextBlock UserName = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(12, 5, 12, 1)
            };

            public TextBubble(Message msg, bool isStatic = false)
            {
                this.Message = msg;

                this.UserName.Text = App.Cache.GetName(this.Message.FromId);
                if (this.Message.FromId == App.VK.UserId) this.UserName.HorizontalTextAlignment = TextAlignment.Right;

                this.Border.Child = new MessageContent(this.Message);
                if (this.Message.Attachments?.Count > 0 && this.Message.Attachments.Any(i => i.Graffiti != null || i.Sticker != null)) this.Border.Background = Coloring.Transparent.Full;

                this.Children.Add(this.UserName);
                this.Children.Add(this.Border);

                if (!isStatic)
                {
                    App.LP.OnMessageEdition += (m) =>
                    {
                        if (m.Id == this.Message.Id)
                        {
                            this.Message = m;
                            App.UILoop.AddAction(new UITask
                            {
                                Action = () =>
                                {
                                    this.Border.Child = new MessageContent(this.Message);
                                }
                            });
                        }
                    };
                }
            }

            /// <summary>
            /// Message text, attachments, keyboard, etc.
            /// </summary>
            public class MessageContent : StackPanel
            {
                public RichTextBlock Text = new RichTextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(5),
                    Padding = new Thickness(5),
                    ContextFlyout = null
                };

                private readonly Message Message;

                public MessageContent(Message msg)
                {
                    this.Message = msg;
                    this.MaxWidth = 600;

                    if (this.Message.ReplyMessage != null)
                    {
                        var reply = new Dialog.Dialog.ReplyMessage(this.Message.ReplyMessage)
                        {
                            CrossEnabled = false,
                            LineWidth = 1,
                        };

                        var margin = reply.Margin;
                        margin.Bottom = 0;
                        reply.Margin = margin;
                        this.Children.Add(reply);
                    }

                    if (this.Message.Attachments?.Count > 0 && this.Message.Attachments.Any(i => i.Gift != null))
                    {
                        this.LoadAttachments();
                        this.LoadText();
                    }
                    else
                    {
                        this.LoadText();
                        this.LoadAttachments();
                    }

                    if (this.Message.FwdMessages?.Count > 0)
                    {
                        var forwards = this.Message.FwdMessages.Select(i => new ForwardGrid(i)).ToList();
                        for (int i = 0; i < forwards.Count; i++)
                        {
                            var fwd = forwards[i];

                            var margin = new Thickness();
                            var radius = new CornerRadius();

                            margin.Left = 2;

                            if (i == 0)
                            {
                                radius.TopLeft = 1.5;
                                radius.TopRight = 1.5;
                                margin.Top = 3;
                            }
                            if (i == forwards.Count - 1)
                            {
                                radius.BottomLeft = 1.5;
                                radius.BottomRight = 1.5;
                                margin.Bottom = 3;
                            }
                            fwd.Border.Margin = margin;
                            fwd.Border.CornerRadius = radius;

                            this.Children.Add(fwd);
                        }
                    };

                    if (this.Message.Keyboard != null) this.Children.Add(new Keyboard(this.Message.Keyboard, this.Message.PeerId, this.Message.Id));
                }

                private void LoadText()
                {
                    if (this.Message.Text?.Length > 0)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            List<Match> markdown = new List<Match>();

                            MatchCollection pushes = new Regex(@"\[(id|club)\d+\|[^\]]*]").Matches(this.Message.Text);
                            MatchCollection links = new Regex(@"((http|https)\:\/\/)?[\w]*\.[a-zA-Z]{1,6}(\/[\w\?\-\=\&.]*)*").Matches(this.Message.Text);
                            if (pushes.Count > 0) markdown.AddRange(pushes);
                            if (links.Count > 0) markdown.AddRange(links);

                            var parsed = new List<ParsedText>();

                            if (markdown.Count > 0)
                            {
                                for (int i = 0; i < markdown.Count; i++)
                                {
                                    var m = markdown[i];
                                    if (i == 0 && !this.Message.Text.StartsWith(m.Value)) parsed.Add(new ParsedText { Text = this.Message.Text.Substring(0, m.Index) });
                                    if (i > 0 && markdown[i - 1].Index != m.Index)
                                    {
                                        try
                                        {
                                            Match prev = markdown[i - 1];
                                            int start = prev.Index + prev.Length;
                                            parsed.Add(new ParsedText { Text = this.Message.Text.Substring(start, m.Index - start) });
                                        }
                                        catch { }
                                    }
                                    // TODO: Fix crash on some messages
                                    try
                                    {
                                        if (!m.Value.Contains("["))
                                        {
                                            string link = m.Value;
                                            if (!link.StartsWith("http://") && !link.StartsWith("https://")) link = "http://" + link;
                                            parsed.Add(new ParsedText
                                            {
                                                Text = m.Value,
                                                Link = true,
                                                Url = link
                                            });
                                        }
                                        else
                                        {
                                            parsed.Add(new ParsedText
                                            {
                                                Text = m.Value.Split("|")[1].Replace("]", ""),
                                                Link = true,
                                                Url = "https://vk.com/" + m.Value.Split("|")[0].Replace("[", "")
                                            });
                                        }
                                    }
                                    catch { }
                                    if (i == markdown.Count - 1 && !this.Message.Text.EndsWith(m.Value)) parsed.Add(new ParsedText { Text = this.Message.Text.Substring(m.Index + m.Length) });
                                }
                            }
                            else parsed.Add(new ParsedText { Text = this.Message.Text });

                            App.UILoop.AddAction(new UITask
                            {
                                Action = () =>
                                {
                                    Paragraph p = new Paragraph();
                                    try
                                    {
                                        foreach (var t in parsed)
                                        {
                                            if (t.Link)
                                            {
                                                Hyperlink link = new Hyperlink { NavigateUri = new Uri(t.Url) };
                                                link.Inlines.Add(new Run { Text = t.Text });
                                                p.Inlines.Add(link);
                                            }
                                            else p.Inlines.Add(new Run { Text = t.Text });
                                        }
                                    }
                                    catch
                                    {
                                        p.Inlines.Add(new Run { Text = this.Message.Text });
                                    }
                                    this.Text.Blocks.Add(p);
                                }
                            });
                        });
                        this.Children.Add(this.Text);
                    }
                }

                private void LoadAttachments()
                {
                    if (this.Message.Attachments?.Count > 0)
                    {
                        foreach (var att in this.Message.Attachments.OrderBy(i => i.Type))
                        {
                            FrameworkElement attach = null;
                            switch (att.Type)
                            {
                                case "photo":
                                    attach = new MessageAttachment.Photo(att.Photo)
                                    {
                                        Width = this.Width
                                    };
                                    break;

                                case "sticker":
                                    attach = new MessageAttachment.Sticker(att.Sticker);
                                    break;

                                case "doc":
                                    attach = new MessageAttachment.Document(att.Document);
                                    break;

                                case "audio_message":
                                    attach = new MessageAttachment.AudioMessage(att.AudioMessage);
                                    break;

                                case "graffiti":
                                    attach = new MessageAttachment.Graffiti(att.Graffiti);
                                    this.MaxWidth = 256;
                                    break;

                                case "gift":
                                    attach = new MessageAttachment.Gift(att.Gift);
                                    break;

                                case "link":
                                    attach = new MessageAttachment.Link(att.Link);
                                    break;

                                case "wall":
                                    attach = new MessageAttachment.Wall(att.Wall);
                                    break;

                                case "wall_reply":
                                    attach = new MessageAttachment.WallReply(att.WallReply);
                                    break;

                                case "money_transfer":
                                    attach = new MessageAttachment.MoneyTransfer(att.MoneyTransfer);
                                    break;

                                case "story":
                                    attach = new MessageAttachment.Story(att.Story);
                                    break;
                            }
                            if (attach != null)
                            {
                                attach.Loaded += (a, b) => this.Height += attach.Height;
                                this.Children.Add(attach);
                            }
                        }
                    }
                }

                private struct ParsedText
                {
                    public string Text;
                    public bool Link;
                    public string Url;
                }

                [Bindable]
                /// <summary>
                /// Keyboard
                /// </summary>
                public class Keyboard : StackPanel
                {
                    public Keyboard(Message.MsgKeyboard keyboard, int peer_id, int msg_id = 0)
                    {
                        this.HorizontalAlignment = HorizontalAlignment.Stretch;
                        foreach (var btns in keyboard.Buttons)
                        {
                            var grid = new Grid
                            {
                                HorizontalAlignment = HorizontalAlignment.Stretch
                            };
                            foreach (var button in btns)
                            {
                                var btn = new Button(button, peer_id, msg_id);
                                Grid.SetColumn(btn, grid.ColumnDefinitions.Count);
                                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                grid.Children.Add(btn);
                            }
                            this.Children.Add(grid);
                        }
                    }

                    [Bindable]
                    private class Button : Windows.UI.Xaml.Controls.Button
                    {
                        private string EventId;

                        public Button(Message.MsgKeyboard.Button button, int peer_id, int msg_id = 0)
                        {
                            this.HorizontalAlignment = HorizontalAlignment.Stretch;
                            this.CornerRadius = new CornerRadius(5);
                            this.Margin = new Thickness(5);
                            SetText();
                            if (button.Action.Type == "callback")
                            {
                                App.LP.Callback += (cb) =>
                                {
                                    if (this.EventId != null && cb.EventId == this.EventId)
                                    {
                                        App.UILoop.AddAction(new UITask
                                        {
                                            Action = async () =>
                                            {
                                                if (this.Content is ProgressRing) SetText();
                                                if (cb.Action.Type == "show_snackbar")
                                                {
                                                    // TODO: make TeachingTip without
                                                    // The program '[304] Alika.exe' has exited with code -1073741819 (0xc0000005) 'Access violation'.
                                                    await new MessageDialog(cb.Action.Text, Utils.LocString("Dialog/СallbackMessageFrom").Replace("%user%", App.Cache.GetName(cb.OwnerId))).ShowAsync();
                                                }
                                                else
                                                {
                                                    string link = "";
                                                    if (cb.Action.Type == "open_link")
                                                    {
                                                        link = cb.Action.Link;
                                                    }
                                                    else
                                                    {
                                                        link += "https://vk.com/app" + cb.Action.AppId;
                                                        if (cb.Action.OwnerId.HasValue) link += "_" + cb.Action.OwnerId.Value;
                                                        if (cb.Action.Hash != null) link += "#" + cb.Action.Hash;
                                                    }
                                                    await Windows.System.Launcher.LaunchUriAsync(new Uri(link));
                                                }
                                            }
                                        });
                                    }
                                };
                                this.Click += (a, b) =>
                                {
                                    if (this.Content is TextBlock)
                                    {
                                        this.Content = new ProgressRing
                                        {
                                            Width = 20,
                                            Height = 20,
                                            IsActive = true
                                        };
                                        Task.Factory.StartNew(() =>
                                        {
                                            this.EventId = App.VK.Messages.SendMessageEvent(peer_id, button.Action.Payload, msg_id);
                                            Thread.Sleep(TimeSpan.FromSeconds(1));
                                            App.UILoop.AddAction(new UITask
                                            {
                                                Action = () =>
                                                {
                                                    if (this.Content is ProgressRing) SetText();
                                                }
                                            });
                                        });
                                    }
                                };
                            }
                            else
                            {
                                this.Click += (object sender, RoutedEventArgs e) => Task.Factory.StartNew(() => App.VK.Messages.Send(peer_id, text: button.Action.Label, payload: button.Action.Payload));
                            }

                            void SetText() => this.Content = new TextBlock
                            {
                                Text = button.Action.Label,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                        }
                    }
                }

                /// <summary>
                /// Forward message holder
                /// </summary>
                [Bindable]
                public class ForwardGrid : StackPanel
                {
                    public Border Border = new Border
                    {
                        VerticalAlignment = VerticalAlignment.Stretch,
                        BorderThickness = new Thickness(0),
                        Background = Coloring.InvertedTransparent.Percent(100),
                        Width = 2.5
                    };

                    public MessageContent Message;

                    public ForwardGrid(Message msg)
                    {
                        this.Orientation = Orientation.Horizontal;
                        this.Margin = new Thickness(2.5, 0, 7.5, 0);

                        var content = new StackPanel();

                        var topContent = new StackPanel { Orientation = Orientation.Horizontal };
                        topContent.PointerPressed += (a, b) => new ChatInformation(msg.FromId);
                        topContent.Children.Add(new Avatar(msg.FromId)
                        {
                            Width = 20,
                            Height = 20,
                            OpenInfoOnClick = false,
                            Margin = new Thickness(5, 5, 0, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                        topContent.Children.Add(new TextBlock
                        {
                            FontWeight = FontWeights.Bold,
                            Text = App.Cache.GetName(msg.FromId),
                            Margin = new Thickness(5, 5, 0, 0),
                            VerticalAlignment = VerticalAlignment.Center
                        });

                        this.Children.Add(this.Border);
                        this.Children.Add(content);

                        this.Message = new MessageContent(msg);
                        this.Message.Text.Margin = new Thickness(5, 2, 5, 5);
                        this.Message.Text.Padding = new Thickness(0);

                        content.Children.Add(topContent);
                        content.Children.Add(this.Message);
                    }
                }
            }
        }

        [Bindable]
        public class MessageFlyout : MenuFlyout
        {
            public MessageFlyout(Message msg, List<Message> editions = null)
            {
                var reply = new MenuFlyoutItem
                {
                    Icon = new FontIcon
                    {
                        Glyph = "\uE97A"
                    },
                    Text = Utils.LocString("Dialog/Reply")
                };
                reply.Click += (a, b) =>
                {
                    var r = (App.MainPage.Dialog.Children[0] as Dialog.Dialog).ReplyGrid;
                    if (r.Content is Dialog.Dialog.ReplyMessage prev && prev.Message.Id == msg.Id) return;
                    r.Content = new Dialog.Dialog.ReplyMessage(msg);
                };
                this.Items.Add(reply);

                if (editions != null && editions.Count > 0)
                {
                    var edHistory = new MenuFlyoutItem
                    {
                        Icon = new FontIcon
                        {
                            Glyph = "\uEC92"
                        },
                        Text = Utils.LocString("Dialog/EditHistory")
                    };
                    edHistory.Click += (a, b) => App.MainPage.Popup.Children.Add(new Popup
                    {
                        Content = new MessageEditHistory(editions),
                        Title = Utils.LocString("Dialog/EditHistory")
                    });
                    this.Items.Add(edHistory);
                }
            }
        }

        [Bindable]
        public class MessageAction : ContentControl
        {
            public MessageAction(Message msg)
            {
                this.Padding = new Thickness(5);
                this.VerticalAlignment = VerticalAlignment.Center;
                var content = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                this.Content = content;
                var menu = new MenuFlyout();
                this.PointerPressed += (a, b) =>
                {
                    if (menu.Items.Count > 0) menu.ShowAt(this);
                };
                string text = Utils.LocString("MessageAction/" + msg.Action.Type.ToUpper());
                if (text.Length == 0) text = Utils.LocString("MessageAction/Unknown");
                if (text.Contains("%user%"))
                {
                    text = text.Replace("%user%", App.Cache.GetName(msg.FromId));
                    if (msg.FromId != App.VK.UserId) menu.Items.Add(this.GetUserItem(msg.FromId));
                }
                if (text.Contains("%member%"))
                {
                    text = text.Replace("%member%", App.Cache.GetName(msg.Action.MemberId));
                    if (msg.Action.MemberId != App.VK.UserId) menu.Items.Add(this.GetUserItem(msg.Action.MemberId));
                }
                if (text.Contains("%text%"))
                {
                    text = text.Replace("%text%", msg.Action.Text);
                }
                content.Text = text;
            }

            private MenuFlyoutItem GetUserItem(int user_id)
            {
                var item = new MenuFlyoutItem
                {
                    Icon = new FontIcon
                    {
                        Glyph = "\uEE57"
                    },
                    Text = App.Cache.GetName(user_id)
                };
                item.Click += (a, b) => new ChatInformation(user_id);
                return item;
            }
        }
    }

    [Bindable]
    public class DateSeparator : ContentControl
    {
        public DateSeparator(DateTime time)
        {
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.HorizontalContentAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
            this.Padding = new Thickness(5);
            this.CornerRadius = new CornerRadius(10);
            this.Content = new TextBlock
            {
                Text = time.ToString(time.Date.Year == DateTime.Now.Year ? "M" : "D")
            };
        }
    }
}