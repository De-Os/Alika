using Alika.Libs;
using Alika.Libs.VK.Responses;
using Alika.UI.Dialog;
using Alika.UI.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{
    /// <summary>
    /// Message box which holds MessageGrid (needed for future features)
    /// </summary>
    [Bindable]
    public class MessageBox : ContentControl
    {
        public MessageGrid message { get; set; }
        public bool Read = false;

        public MessageBox(Message msg, int peer_id, bool isStatic = false)
        {
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.HorizontalContentAlignment = msg.from_id == App.vk.user_id ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            this.message = new MessageGrid(msg, peer_id, isStatic);
            this.Content = this.message;

            this.Read = msg.read_state == 1;
        }

        /// <summary>
        /// Grid with avatar & message content
        /// </summary>
        [Bindable]
        public class MessageGrid : Grid
        {
            public TextBubble textBubble { get; set; }
            public Avatar avatar;
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
                            HorizontalAlignment = App.vk.user_id == this.textBubble.message.from_id ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Source = new SvgImageSource(new Uri(Utils.AssetTheme("pen.svg")))
                        })
                    });
                    this._edited = true;
                }
            }
            private List<Message> _editions = new List<Message>();

            public MessageGrid(Message msg, int peer_id, bool isStatic = false)
            {
                this.states.HorizontalAlignment = App.vk.user_id == msg.from_id ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                this.MinWidth = 200;

                this.textBubble = new TextBubble(msg, peer_id, isStatic);
                this.LoadAvatar(msg.from_id);

                var date = msg.date.ToDateTime();
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
                if (msg.from_id == App.vk.user_id)
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition());
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

                    if (!isStatic)
                    {
                        var conv = App.cache.GetConversation(msg.peer_id);
                        var readState = new Image
                        {
                            Width = 15,
                            Height = 15,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Source = new SvgImageSource(new Uri(Utils.AssetTheme(msg.id <= (conv.in_read > conv.out_read ? conv.in_read : conv.out_read) ? "double_check.svg" : "check.svg")))
                        };
                        App.lp.OnReadMessage += (rs) =>
                        {
                            if (rs.peer_id == msg.peer_id && rs.msg_id >= msg.id) App.UILoop.AddAction(new UITask
                            {
                                Priority = Windows.UI.Core.CoreDispatcherPriority.Low,
                                Action = () => readState.Source = new SvgImageSource(new Uri(Utils.AssetTheme("double_check.svg")))
                            });
                        };
                        this.states.Children.Add(readState);
                    }

                    Grid.SetColumn(stateHolder, 0);
                    Grid.SetColumn(this.textBubble, 1);
                    Grid.SetColumn(this.avatar, 2);
                }
                else
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                    this.ColumnDefinitions.Add(new ColumnDefinition());
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    Grid.SetColumn(this.avatar, 0);
                    Grid.SetColumn(this.textBubble, 1);
                    Grid.SetColumn(stateHolder, 2);
                }

                if (!isStatic)
                {
                    if (msg.update_time > 0) this.Edited = true;
                    App.lp.OnMessageEdition += (m) =>
                    {
                        if (msg.id == m.id)
                        {
                            this.Edited = true;
                            this._editions.Add(m);
                        }
                    };
                }

                stateHolder.Children.Add(this.states);
                stateHolder.Children.Add(this.time);
                this.Children.Add(stateHolder);
                this.Children.Add(this.textBubble);
                this.Children.Add(this.avatar);

                if (!isStatic) this.RightTapped += (a, b) => new MessageFlyout(msg, this._editions).ShowAt(this, b.GetPosition(b.OriginalSource as UIElement));
            }

            public void LoadAvatar(int user_id)
            {
                this.avatar = new Avatar(user_id)
                {
                    Height = 40,
                    Width = 40,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Thickness margin = this.avatar.Margin;
                margin.Bottom = this.textBubble.border.Margin.Bottom + (this.textBubble.Margin.Bottom / 2);
                this.avatar.Margin = margin;
            }
        }

        /// <summary>
        /// Content bubble
        /// </summary>
        [Bindable]
        public class TextBubble : StackPanel
        {
            public Message message;
            public Border border = new Border
            {
                BorderThickness = new Thickness(1),
                Background = App.systemDarkTheme ? Coloring.MessageBox.TextBubble.Dark : Coloring.MessageBox.TextBubble.Light,
                Margin = new Thickness(10, 5, 10, 5),
                MinHeight = 30,
                MinWidth = 50,
                CornerRadius = new CornerRadius(10)
            };
            public TextBlock name = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(12, 5, 12, 1)
            };

            public TextBubble(Message msg, int peer_id, bool isStatic = false)
            {
                this.message = msg;

                this.LoadName();

                this.border.Child = new MessageContent(this.message);
                if (this.message.attachments?.Count > 0 && this.message.attachments.Any(i => i.graffiti != null || i.sticker != null)) this.border.Background = Coloring.Transparent.Full;

                this.Children.Add(this.name);
                this.Children.Add(this.border);

                if (!isStatic)
                {
                    App.lp.OnMessageEdition += (m) =>
                    {
                        if (m.id == this.message.id)
                        {
                            this.message = m;
                            App.UILoop.AddAction(new UITask
                            {
                                Action = () =>
                                {
                                    this.border.Child = new MessageContent(this.message);
                                }
                            });
                        }
                    };
                }
            }

            public void LoadName()
            {
                if (this.message.from_id > 0)
                {
                    if (!App.cache.Users.Exists(u => u.user_id == this.message.from_id)) App.vk.Users.Get(new List<int> { this.message.from_id }, fields: "photo_200");
                    User user = App.cache.Users.Find(u => u.user_id == this.message.from_id);
                    this.name.Text = user.first_name + " " + user.last_name;
                }
                else
                {
                    if (!App.cache.Groups.Exists(g => g.id == this.message.from_id)) App.vk.Groups.GetById(new List<int> { this.message.from_id }, fields: "photo_200");
                    this.name.Text = App.cache.Groups.Find(g => g.id == this.message.from_id).name;
                }
                if (this.message.from_id == App.vk.user_id) this.name.HorizontalTextAlignment = TextAlignment.Right;
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

                private readonly Message message;

                public MessageContent(Message msg)
                {
                    this.message = msg;
                    this.MaxWidth = 600;

                    if (this.message.reply_message != null)
                    {
                        var reply = new Dialog.Dialog.ReplyMessage(this.message.reply_message)
                        {
                            CrossEnabled = false,
                            LineWidth = 1,
                        };

                        var margin = reply.Margin;
                        margin.Bottom = 0;
                        reply.Margin = margin;
                        this.Children.Add(reply);
                    }

                    if (this.message.attachments != null && this.message.attachments.Count > 0 && this.message.attachments.Any(i => i.gift != null))
                    {
                        this.LoadAttachments();
                        this.LoadText();
                    }
                    else
                    {
                        this.LoadText();
                        this.LoadAttachments();
                    }

                    if (this.message.fwd_messages?.Count > 0)
                    {
                        var forwards = this.message.fwd_messages.Select(i => new ForwardGrid(i)).ToList();
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

                    if (this.message.keyboard != null) this.Children.Add(new ButtonsGrid(msg.keyboard, msg.peer_id));
                }

                private void LoadText()
                {
                    if (this.message?.text?.Length > 0)
                    {
                        Task.Factory.StartNew(() =>
                        {
                            List<Match> markdown = new List<Match>();

                            MatchCollection pushes = new Regex(@"\[(id|club)\d+\|[^\]]*]").Matches(this.message.text);
                            MatchCollection links = new Regex(@"((http|https)\:\/\/)?[\w]*\.[a-zA-Z]{1,6}(\/[\w\?\-\=\&.]*)*").Matches(this.message.text);
                            if (pushes.Count > 0) markdown.AddRange(pushes);
                            if (links.Count > 0) markdown.AddRange(links);

                            var parsed = new List<ParsedText>();

                            if (markdown.Count > 0)
                            {
                                for (int i = 0; i < markdown.Count; i++)
                                {
                                    var m = markdown[i];
                                    if (i == 0 && !this.message.text.StartsWith(m.Value)) parsed.Add(new ParsedText { Text = this.message.text.Substring(0, m.Index) });
                                    if (i > 0 && markdown[i - 1].Index != m.Index)
                                    {
                                        try
                                        {
                                            Match prev = markdown[i - 1];
                                            int start = prev.Index + prev.Length;
                                            parsed.Add(new ParsedText { Text = this.message.text.Substring(start, m.Index - start) });
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
                                    if (i == markdown.Count - 1 && !this.message.text.EndsWith(m.Value)) parsed.Add(new ParsedText { Text = this.message.text.Substring(m.Index + m.Length) });
                                }
                            }
                            else parsed.Add(new ParsedText { Text = this.message.text });

                            App.UILoop.AddAction(new UITask
                            {
                                Action = () =>
                                {
                                    Paragraph p = new Paragraph();
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
                                    this.Text.Blocks.Add(p);
                                }
                            });
                        });
                        this.Children.Add(this.Text);
                    }
                }
                private void LoadAttachments()
                {
                    if (this.message.attachments?.Count > 0)
                    {
                        foreach (var att in this.message.attachments.OrderBy(i => i.type))
                        {
                            FrameworkElement attach = null;
                            switch (att.type)
                            {
                                case "photo":
                                    attach = new MessageAttachment.Photo(att.photo)
                                    {
                                        Width = this.Width
                                    };
                                    break;
                                case "sticker":
                                    attach = new MessageAttachment.Sticker(att.sticker);
                                    break;
                                case "doc":
                                    attach = new MessageAttachment.Document(att.document);
                                    break;
                                case "audio_message":
                                    attach = new MessageAttachment.AudioMessage(att.audio_message);
                                    break;
                                case "graffiti":
                                    attach = new MessageAttachment.Graffiti(att.graffiti);
                                    this.MaxWidth = 256;
                                    break;
                                case "gift":
                                    attach = new MessageAttachment.Gift(att.gift);
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
                public class ButtonsGrid : StackPanel
                {
                    public ButtonsGrid(Message.Keyboard keyboard, int peer_id)
                    {
                        this.HorizontalAlignment = HorizontalAlignment.Stretch;
                        foreach (var btns in keyboard.buttons)
                        {
                            var grid = new Grid
                            {
                                HorizontalAlignment = HorizontalAlignment.Stretch
                            };
                            foreach (var button in btns)
                            {
                                Button btn = new Button
                                {
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    Margin = new Thickness(5),
                                    CornerRadius = new CornerRadius(5)
                                };
                                if (button.color != "default") btn.Background = new SolidColorBrush(Coloring.FromHash(Coloring.MessageBox.Keyboard.GetColor(button.color)));
                                btn.Content = new TextBlock
                                {
                                    Text = button.action.label,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center
                                };
                                btn.Click += (object sender, RoutedEventArgs e) => Task.Factory.StartNew(() => App.vk.Messages.Send(peer_id, text: button.action.label, payload: button.action.payload));

                                Grid.SetColumn(btn, grid.ColumnDefinitions.Count);
                                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                                grid.Children.Add(btn);
                            }
                            this.Children.Add(grid);
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
                        topContent.PointerPressed += (a, b) => new ChatInformation(msg.from_id);
                        topContent.Children.Add(new Avatar(msg.from_id)
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
                            Text = App.cache.GetName(msg.from_id),
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
                        Glyph = "\uE8CA"
                    },
                    Text = Utils.LocString("Dialog/Reply")
                };
                reply.Click += (a, b) =>
                {
                    var r = (App.main_page.dialog.Children[0] as Dialog.Dialog).reply_grid;
                    if (r.Content is Dialog.Dialog.ReplyMessage prev && prev.Message.id == msg.id) return;
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
                    edHistory.Click += (a, b) => App.main_page.popup.Children.Add(new Popup
                    {
                        Content = new MessageEditHistory(editions),
                        Title = Utils.LocString("Dialog/EditHistory")
                    });
                    this.Items.Add(edHistory);
                }
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
