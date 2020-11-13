using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Responses;
using Alika.UI.Items;
using Alika.UI.Misc;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Alika.UI
{
    [Bindable]
    public class ChatInformation : StackPanel
    {
        public int PeerId;
        public ConversationInfo Conversation;

        public AvatarAndName AvaAndName;

        public Popup Popup = new Popup
        {
            Title = Utils.LocString("Dialog/Conversationinfo")
        };

        public ChatInformation(int peer_id)
        {
            this.PeerId = peer_id;

            if (peer_id == App.VK.UserId) return;

            this.Load();
        }

        public void Load()
        {
            this.Conversation = App.Cache.GetConversation(this.PeerId);

            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.Width = 500;

            this.AvaAndName = new AvatarAndName(this.Conversation);
            this.Children.Add(this.AvaAndName);
            this.AddSeparator();
            this.Children.Add(new AttachmentsList(this.PeerId));

            if (this.PeerId > Limits.Messages.PEERSTART)
            {
                this.AddSeparator();
                var convMenu = new ConversationItems(this.Conversation);
                convMenu.AvatarUpdated += (av) =>
                {
                    if (av == null)
                    {
                        this.AvaAndName.Avatar.ProfilePicture = null;
                    }
                    else this.AvaAndName.LoadImage(av);
                    this.Conversation.Settings.Photos.Photo200 = av;
                    Task.Factory.StartNew(() => App.Cache.Update(this.PeerId));
                };
                convMenu.TitleUpdated += (name) =>
                {
                    this.AvaAndName.Title.Text = name;
                    this.Conversation.Settings.Title = name;
                    (App.MainPage.Dialog.Children[0] as Dialog.Dialog).TopMenu.name.Text = name;
                    Task.Factory.StartNew(() => App.Cache.Update(this.PeerId));
                };
                this.Children.Add(convMenu);
            }

            this.Popup.Content = new ScrollViewer
            {
                HorizontalScrollMode = ScrollMode.Disabled,
                VerticalScrollMode = ScrollMode.Auto,
                Content = this
            };

            App.UILoop.AddAction(new UITask
            {
                Action = () => { App.MainPage.Popup.Children.Add(this.Popup); }
            });
        }

        private void AddSeparator(double height = 30)
        {
            this.Children.Add(new Grid { Height = height });
        }

        [Bindable]
        public class AvatarAndName : Grid
        {
            public PersonPicture Avatar = new PersonPicture
            {
                Width = 75,
                Height = 75,
                Margin = new Thickness(0, 0, 10, 0)
            };

            public TextBlock Title = new TextBlock
            {
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Bottom,
                FontWeight = FontWeights.SemiBold,
                FontSize = 20
            };

            public ConversationInfo conv;

            public AvatarAndName(ConversationInfo conversation)
            {
                this.conv = conversation;
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                this.ColumnDefinitions.Add(new ColumnDefinition());

                Grid.SetColumn(this.Avatar, 0);
                this.Children.Add(this.Avatar);

                Grid text = new Grid();
                text.RowDefinitions.Add(new RowDefinition());
                text.RowDefinitions.Add(new RowDefinition());

                text.Children.Add(this.Title);

                FrameworkElement subtext = new TextBlock
                {
                    TextTrimming = TextTrimming.CharacterEllipsis,
                };
                if (this.conv.Peer.Id > 0)
                {
                    if (this.conv.Peer.Id > Limits.Messages.PEERSTART)
                    {
                        (subtext as TextBlock).Text = Utils.LocString("Dialog/MembersCount").Replace("%count%", this.conv.Settings.MembersCount.ToString());
                    }
                    else
                    {
                        subtext = new OnlineText(this.conv.Peer.Id);
                    }
                }
                subtext.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetRow(subtext, 1);
                text.Children.Add(subtext);

                Grid.SetColumn(text, 1);
                this.Children.Add(text);

                this.LoadTitle();
                this.LoadImage(App.Cache.GetAvatar(this.conv.Peer.Id));
            }

            public void LoadTitle()
            {
                this.Title.Text = App.Cache.GetName(this.conv.Peer.Id);
            }

            public async void LoadImage(string uri)
            {
                this.Avatar.ProfilePicture = await ImageCache.Instance.GetFromCacheAsync(new Uri(uri));
            }
        }

        [Bindable]
        public class AttachmentsList : ContentControl
        {
            public AttachmentsList(int peer_id, string title = "Attachments/Attachments")
            {
                var content = new Popup.Menu(title);
                this.Content = content;
                this.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                AddElement("photo", "Attachments/Photos", "\uEB9F");
                AddElement("doc", "Attachments/Documents", "\uED25");
                AddElement("video", "Attachments/Videos", "\uE8AA");
                AddElement("link", "Attachments/Links", "\uE71B");
                AddElement("audio_message", "Attachments/VoiceMessages", "\uE720");

                void AddElement(string type, string name, string image) => content.Children.Add(new Popup.Menu.Element(
                        name,
                        image,
                        (a, b) => new AttachmentsPopup(peer_id, type, name)
                    ));
            }

            public class AttachmentsPopup
            {
                public int peer_id;
                public string type;

                private string offset;
                private bool _loading = false;

                public Grid content = new Grid();

                public Popup loadingPopup = new Popup
                {
                    Content = new ProgressRing
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        IsActive = true,
                        Width = 50,
                        Height = 50
                    }
                };

                public bool Loading
                {
                    get
                    {
                        return this._loading;
                    }
                    set
                    {
                        this.loadingPopup.Hide();
                        if (value) App.MainPage.Popup.Children.Add(this.loadingPopup);
                        this._loading = value;
                    }
                }

                public Popup popup = new Popup();

                public AttachmentsPopup(int peer_id, string type, string title = null)
                {
                    this.peer_id = peer_id;
                    this.type = type;

                    if (title != null) this.popup.Title = Utils.LocString(title);

                    var scroll = new ScrollViewer
                    {
                        HorizontalScrollMode = ScrollMode.Disabled,
                        VerticalScrollMode = ScrollMode.Auto,
                        Content = this.content
                    };
                    scroll.ViewChanging += (a, b) => { if (b.FinalView.VerticalOffset == scroll.ScrollableHeight) this.Load(); };

                    this.popup.Content = scroll;

                    App.MainPage.Popup.Children.Add(this.popup);

                    this.Load();
                }

                public void Load()
                {
                    if (!this.Loading)
                    {
                        this.Loading = true;
                    }
                    else return;
                    Task.Factory.StartNew(() =>
                    {
                        var response = App.VK.Messages.GetHistoryAttachments(peer_id: this.peer_id, type: this.type, start_from: this.offset, count: 50);
                        App.UILoop.AddAction(new UITask
                        {
                            Action = async () =>
                           {
                               if (response.Items.Count > 0)
                               {
                                   this.offset = response.NextFrom;

                                   StackPanel final = new StackPanel();

                                   if (this.type == "photo")
                                   {
                                       StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };
                                       foreach (var att in response.Items)
                                       {
                                           if (stack.Children.Count == 4)
                                           {
                                               final.Children.Add(stack);
                                               stack = new StackPanel { Orientation = Orientation.Horizontal };
                                           }
                                           var img = new Image
                                           {
                                               Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill,
                                               Width = 100,
                                               Height = 100,
                                               Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(att.Attachment.Photo.GetBestQuality().Url))
                                           };
                                           img.PointerPressed += (a, b) => new MediaViewer(att.Attachment);
                                           stack.Children.Add(new Border
                                           {
                                               Child = img,
                                               CornerRadius = new CornerRadius(10),
                                               Margin = new Thickness(5)
                                           });
                                       }
                                   }
                                   else
                                   {
                                       foreach (var att in response.Items)
                                       {
                                           if (att.Attachment.Type != this.type) continue;
                                           switch (this.type)
                                           {
                                               case "doc":
                                                   if (att.Attachment.Document != null) final.Children.Add(new MessageAttachment.Document(att.Attachment.Document)
                                                   {
                                                       HorizontalAlignment = HorizontalAlignment.Stretch,
                                                       HorizontalContentAlignment = HorizontalAlignment.Left,
                                                       Margin = new Thickness(5)
                                                   });
                                                   break;

                                               case "audio_message":
                                                   final.Children.Add(new MessageAttachment.AudioMessage(att.Attachment.AudioMessage)
                                                   {
                                                       HorizontalAlignment = HorizontalAlignment.Stretch,
                                                       Margin = new Thickness(5)
                                                   });
                                                   break;

                                               case "link":
                                                   final.Children.Add(new MessageAttachment.Link(att.Attachment.Link)
                                                   {
                                                       HorizontalAlignment = HorizontalAlignment.Stretch,
                                                       MaxWidth = 400
                                                   });
                                                   break;
                                           }
                                       }
                                   }

                                   Grid.SetRow(final, this.content.RowDefinitions.Count);
                                   this.content.RowDefinitions.Add(new RowDefinition());
                                   this.content.Children.Add(final);
                               }
                               this.Loading = false;
                           }
                        });
                    });
                }
            }
        }

        public class ConversationItems : ContentControl
        {
            public delegate void Event(string str);

            public event Event AvatarUpdated;

            public event Event TitleUpdated;

            private readonly Popup PermsPopup;
            private readonly Popup MembersPopup;
            private readonly Popup SettingsPopup;
            private readonly Popup LinkPopup;

            public ConversationItems(ConversationInfo peer)
            {
                var content = new Popup.Menu("Dialog/Management");
                this.Content = content;
                this.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                if (peer.Settings.Access.CanInvite)
                {
                    content.Children.Add(new Popup.Menu.Element(
                            "Dialog/InviteUser",
                            "\uE8FA",
                            (a, b) =>
                            {
                                var dialog = new AddUserDialog(peer.Peer.Id);
                                var popup = new Popup
                                {
                                    Content = dialog,
                                    Title = Utils.LocString("Dialog/InviteUser")
                                };
                                dialog.Hide += () => popup.Hide();
                                App.MainPage.Popup.Children.Add(popup);
                            }
                        ));
                }

                if (peer.Settings.Access.CanSeeInviteLink)
                {
                    this.LinkPopup = new Popup
                    {
                        Content = new Link(peer),
                        Title = Utils.LocString("Dialog/InviteLink")
                    };
                    content.Children.Add(new Popup.Menu.Element(
                            "Dialog/InviteLink",
                            "\uF3E2",
                            (a, b) => App.MainPage.Popup.Children.Add(this.LinkPopup)
                        ));
                }
                if (peer.Settings.Access.CanChangeInfo)
                {
                    var settings = new Settings(peer);
                    settings.AvatarUpdated += (s) => this.AvatarUpdated?.Invoke(s);
                    settings.TitleUpdated += (s) => this.TitleUpdated?.Invoke(s);
                    this.SettingsPopup = new Popup
                    {
                        Content = settings,
                        Title = Utils.LocString("Settings")
                    };
                    content.Children.Add(new Popup.Menu.Element(
                            "Settings",
                            "\uE70F",
                            (a, b) => App.MainPage.Popup.Children.Add(this.SettingsPopup)
                        ));
                }
                if (peer.Settings?.Permissions != null && peer.Settings?.OwnerId == App.VK.UserId)
                {
                    this.PermsPopup = new Popup
                    {
                        Content = new Permissions(peer.Settings.Permissions, peer.Peer.Id),
                        Title = Utils.LocString("Dialog/Permissions")
                    };
                    content.Children.Add(new Popup.Menu.Element(
                            "Dialog/Permissions",
                            "settings.svg",
                            (a, b) => App.MainPage.Popup.Children.Add(this.PermsPopup)
                        ));
                }

                this.MembersPopup = new Popup
                {
                    Content = new Members(peer.Peer.Id),
                    Title = Utils.LocString("Dialog/Members")
                };
                content.Children.Add(new Popup.Menu.Element(
                        "Dialog/Members",
                        "\uEBDA",
                        (a, b) => App.MainPage.Popup.Children.Add(this.MembersPopup)
                    ));
            }

            [Bindable]
            public class Permissions : StackPanel
            {
                public static Dictionary<string, Type> Types = new Dictionary<string, Type> {
                    {"invite", new Type
                    {
                        Name = Utils.LocString("Dialog/PermissionsInvite"),
                        Picture = "\uE8FA"
                    } },
                    {"change_info", new Type
                    {
                        Name = Utils.LocString("Dialog/PermissionsChangeInfo"),
                        Picture = "\uE70F"
                    } },
                    {"change_pin", new Type{
                        Name = Utils.LocString("Dialog/PermissionsPin"),
                        Picture = "\uE718"
                    } },
                    {"use_mass_mentions", new Type{
                        Name = Utils.LocString("Dialog/PermissionsMentions"),
                        Picture = "\uE789"
                    } },
                    {"see_invite_link", new Type{
                        Name = Utils.LocString("Dialog/PermissionsLink"),
                        Picture = "\uE910"
                    } },
                    {"call", new Type{
                        Name = Utils.LocString("Dialog/PermissionsCall"),
                        Picture = "\uE717"
                    } },
                    {"change_admins", new Type{
                        Name = Utils.LocString("Dialog/PermissionsChangeAdmins"),
                        Picture = "\uE748",
                        IsAllDisabled = true
                    } }
                };

                private static readonly Dictionary<string, string> StateNamings = new Dictionary<string, string> {
                    {"all", Utils.LocString("Dialog/PermissionsTypeAll")},
                    {"owner_and_admins", Utils.LocString("Dialog/PermissionsTypeOwnerAndAdmins")},
                    {"owner", Utils.LocString("Dialog/PermissionsTypeOwner")}
                };

                public ConversationInfo.PeerSettings.PeerPermissions Perms;
                public int peer_id;

                public Permissions(ConversationInfo.PeerSettings.PeerPermissions permissions, int peer_id)
                {
                    this.peer_id = peer_id;
                    this.Perms = permissions;

                    this.Children.Add(new Element("invite", permissions.Invite));
                    this.Children.Add(new Element("change_info", permissions.ChangeInfo));
                    this.Children.Add(new Element("change_pin", permissions.ChangePin));
                    this.Children.Add(new Element("use_mass_mentions", permissions.UseMassMentions));
                    this.Children.Add(new Element("see_invite_link", permissions.SeeInviteLink));
                    this.Children.Add(new Element("call", permissions.Call));
                    this.Children.Add(new Element("change_admins", permissions.ChangeAdmins));
                }

                [Bindable]
                public class Element : Grid
                {
                    public FontIcon Icon = new FontIcon
                    {
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(0, 0, 10, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    public TextBlock Text = new TextBlock
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        FontWeight = FontWeights.SemiBold,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };

                    public Element(string name, string state)
                    {
                        var type = Types[name];

                        this.HorizontalAlignment = HorizontalAlignment.Stretch;
                        this.Background = Coloring.Transparent.Full;

                        this.Padding = new Thickness(10);
                        this.CornerRadius = new CornerRadius(5);

                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                        this.Icon.Glyph = type.Picture;
                        this.Text.Text = type.Name;

                        var button = new ChangeButton(name, state);

                        Grid.SetColumn(this.Icon, 0);
                        Grid.SetColumn(this.Text, 1);
                        Grid.SetColumn(button, 2);

                        this.Children.Add(this.Icon);
                        this.Children.Add(this.Text);
                        this.Children.Add(button);
                    }

                    [Bindable]
                    public class ChangeButton : Button
                    {
                        private readonly Grid _content = new Grid();

                        public TextBlock state = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        };

                        public ChangeButton(string type, string state)
                        {
                            this.state.Text = StateNamings[state];

                            this.Background = Coloring.Transparent.Full;
                            this.HorizontalAlignment = HorizontalAlignment.Right;
                            this.VerticalAlignment = VerticalAlignment.Center;
                            this.CornerRadius = new CornerRadius(10);
                            this.Margin = new Thickness(10, 0, 0, 0);
                            this.Content = this._content;

                            this._content.ColumnDefinitions.Add(new ColumnDefinition());
                            this._content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                            Grid.SetColumn(this.state, 0);
                            this._content.Children.Add(this.state);
                            var img = new FontIcon
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 10,
                                Margin = new Thickness(5),
                                Glyph = "\uE70D"
                            };
                            Grid.SetColumn(img, 1);
                            this._content.Children.Add(img);

                            var fl = new FlyOut(type, state);
                            this.Flyout = fl;
                            fl.StateChanged += (s) => this.state.Text = StateNamings[s];
                        }

                        [Bindable]
                        public class FlyOut : MenuFlyout
                        {
                            public delegate void Changed(string newtype);

                            public event Changed StateChanged;

                            public FlyOut(string type, string current_state)
                            {
                                foreach (KeyValuePair<string, string> e in StateNamings)
                                {
                                    if (e.Key == Type.AllName && Types[type].IsAllDisabled) continue;
                                    var btn = new MenuFlyoutItem
                                    {
                                        Text = e.Value,
                                        Background = e.Key == current_state ? Coloring.Transparent.Percent(50) : Coloring.Transparent.Full
                                    };
                                    btn.Click += (a, b) =>
                                    {
                                        if (btn.Background == Coloring.Transparent.Full)
                                        {
                                            try
                                            {
                                                App.VK.Call<int>("messages.editChat", new Dictionary<string, dynamic> {
                                                    {"chat_id", App.MainPage.PeerId - Limits.Messages.PEERSTART },
                                                    {"permissions", "{\"" + type + "\": \"" + e.Key + "\"}"  }
                                                });
                                                foreach (var item in this.Items) if (item is MenuFlyoutItem bn && bn != btn) bn.Background = Coloring.Transparent.Full;
                                                btn.Background = Coloring.Transparent.Percent(50);
                                                this.StateChanged?.Invoke(e.Key);
                                            }
                                            catch (Exception exc)
                                            {
                                                Task.Run(async () => await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync());
                                            }
                                            this.Hide();
                                        }
                                    };
                                    this.Items.Add(btn);
                                }
                            }
                        }
                    }
                }

                public class Type
                {
                    public const string AllName = "all";

                    public string Name { get; set; }
                    public string Picture { get; set; }
                    public bool IsAllDisabled { get; set; } = false;
                }
            }

            [Bindable]
            public class Members : Grid
            {
                private readonly int PeerId;

                public Members(int peer_id)
                {
                    this.PeerId = peer_id;
                    this.Children.Add(new ProgressRing
                    {
                        Height = 50,
                        Width = 50,
                        IsActive = true,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(10)
                    });

                    this.Loaded += (a, b) => this.Load();
                }

                public void Load()
                {
                    Task.Factory.StartNew(() =>
                    {
                        var users = App.VK.Messages.GetConversationMembers(this.PeerId, "photo_200,online_info");
                        App.UILoop.AddAction(new UITask
                        {
                            Action = () =>
                            {
                                StackPanel menu = new StackPanel();
                                var search = new TextBox
                                {
                                    PlaceholderText = Utils.LocString("Search"),
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    Margin = new Thickness(5)
                                };
                                menu.Children.Add(search);
                                foreach (var m in users.Items)
                                {
                                    var member = new Member(m, this.PeerId);
                                    search.TextChanged += (a, b) => member.Visibility = Regex.IsMatch(member.name.Text, (a as TextBox).Text, RegexOptions.IgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
                                    menu.Children.Add(member);
                                };
                                this.Children.Clear();
                                this.Children.Add(new ScrollViewer
                                {
                                    HorizontalScrollMode = ScrollMode.Disabled,
                                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                    VerticalScrollMode = ScrollMode.Auto,
                                    Content = menu
                                });
                                this.MinWidth = 500;
                                this.MinHeight = 500;
                            }
                        });
                    });
                }

                [Bindable]
                public class Member : Grid
                {
                    public Button actions = new Button
                    {
                        Content = new FontIcon
                        {
                            Glyph = "\uE70D"
                        },
                        Background = Coloring.Transparent.Full,
                        CornerRadius = new CornerRadius(10),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, 0, 10, 0)
                    };

                    public TextBlock name = new TextBlock
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        FontSize = 15,
                        FontWeight = FontWeights.SemiBold
                    };

                    public ConversationMember User;
                    public int peer_id;

                    public Member(ConversationMember member, int peer_id)
                    {
                        this.User = member;
                        this.peer_id = peer_id;

                        this.HorizontalAlignment = HorizontalAlignment.Stretch;
                        this.Margin = new Thickness(5);

                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });
                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                        var avatar = new Avatar(member.MemberId)
                        {
                            Width = 50,
                            Height = 50,
                            Margin = new Thickness(0, 0, 10, 0)
                        };
                        Grid.SetColumn(avatar, 0);
                        this.Children.Add(avatar);

                        this.GenerateName();
                        Grid.SetColumn(this.name, 1);
                        this.Children.Add(this.name);

                        Button info = new Button
                        {
                            Content = new FontIcon
                            {
                                Glyph = "\uE946"
                            },
                            Background = Coloring.Transparent.Full,
                            CornerRadius = new CornerRadius(10),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                            Flyout = new Information(this.User),
                            Margin = new Thickness(0, 0, 10, 0)
                        };
                        Grid.SetColumn(info, 2);
                        this.Children.Add(info);

                        this.GenerateActions();
                        Grid.SetColumn(this.actions, 3);
                        this.Children.Add(this.actions);
                    }

                    public void GenerateActions()
                    {
                        Actions flyout = new Actions(this.User, this.peer_id);
                        if (flyout.Items.Count == 0)
                        {
                            this.actions.Flyout = null;
                            this.actions.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            flyout.InfoChanged += (a) =>
                            {
                                this.User = a;
                                this.GenerateActions();
                                this.GenerateName();
                            };
                            flyout.Remove += (a) => this.Visibility = Visibility.Collapsed;
                            this.actions.Flyout = flyout;
                            this.actions.Visibility = Visibility.Visible;
                        }
                    }

                    public void GenerateName()
                    {
                        this.name.Text = App.Cache.GetName(this.User.MemberId);
                        if (this.User.IsAdmin) this.name.Text += " ⭐";
                    }

                    [Bindable]
                    public class Actions : MenuFlyout
                    {
                        public delegate void Event(ConversationMember member);

                        public event Event Remove;

                        public event Event InfoChanged;

                        public ConversationMember Member;
                        public int PeerId;

                        public Actions(ConversationMember member, int peer_id)
                        {
                            this.Member = member;
                            this.PeerId = peer_id;

                            bool isOwner = App.Cache.GetConversation(peer_id).Settings.OwnerId == App.VK.UserId;

                            if (isOwner && member.MemberId != App.VK.UserId)
                            {
                                if (member.IsAdmin)
                                {
                                    var demote = new MenuFlyoutItem
                                    {
                                        Text = Utils.LocString("Dialog/Demote"),
                                        Icon = new FontIcon
                                        {
                                            Glyph = "\uE70D"
                                        }
                                    };
                                    demote.Click += async (a, b) =>
                                    {
                                        try
                                        {
                                            App.VK.Messages.SetMemberRole(false, member.MemberId, peer_id);
                                            member.IsAdmin = false;
                                            this.InfoChanged?.Invoke(member);
                                            this.Hide();
                                        }
                                        catch (Exception exc)
                                        {
                                            await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                                        }
                                    };
                                    this.Items.Add(demote);
                                }
                                else
                                {
                                    var promote = new MenuFlyoutItem
                                    {
                                        Text = Utils.LocString("Dialog/Promote"),
                                        Icon = new FontIcon
                                        {
                                            Glyph = "\uE70E"
                                        }
                                    };
                                    promote.Click += async (a, b) =>
                                    {
                                        try
                                        {
                                            App.VK.Messages.SetMemberRole(true, member.MemberId, peer_id);
                                            member.IsAdmin = true;
                                            this.InfoChanged?.Invoke(member);
                                            this.Hide();
                                        }
                                        catch (Exception exc)
                                        {
                                            await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                                        }
                                    };
                                    this.Items.Add(promote);
                                }
                            }

                            if (member.CanKick)
                            {
                                var kick = new MenuFlyoutItem
                                {
                                    Text = Utils.LocString("Dialog/Kick"),
                                    Icon = new FontIcon
                                    {
                                        Glyph = "\uE711"
                                    }
                                };
                                kick.Click += async (a, b) =>
                                {
                                    try
                                    {
                                        App.VK.Messages.RemoveChatUser(peer_id, member.MemberId);
                                        this.Remove?.Invoke(null);
                                        this.Hide();
                                    }
                                    catch (Exception exc)
                                    {
                                        await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                                    }
                                };
                                this.Items.Add(kick);
                            }
                        }
                    }

                    [Bindable]
                    public class Information : Flyout
                    {
                        public Information(ConversationMember member)
                        {
                            StackPanel stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };

                            stack.Children.Add(new TextBlock
                            {
                                Text = Utils.LocString("Dialog/InvitedTime").Replace("%time%", member.JoinDate.ToDateTime().ToString(@"d.M.y H:mm:ss")),
                                VerticalAlignment = VerticalAlignment.Center
                            });

                            if (member.InvitedBy != member.MemberId)
                            {
                                stack.Children.Add(new TextBlock
                                {
                                    Text = Utils.LocString("Dialog/JoinedInfo").Replace("%user%", App.Cache.GetName(member.InvitedBy)),
                                    VerticalAlignment = VerticalAlignment.Center
                                });
                            }

                            this.Content = stack;
                        }
                    }
                }
            }

            [Bindable]
            public class Settings : Grid
            {
                public delegate void Event(string str);

                public event Event AvatarUpdated;

                public event Event TitleUpdated;

                public ConversationInfo Peer;

                public PersonPicture Avatar = new PersonPicture
                {
                    Height = 75,
                    Width = 75,
                    Margin = new Thickness(10),
                    VerticalAlignment = VerticalAlignment.Center
                };

                public TextBox Title = new TextBox
                {
                    AcceptsReturn = false,
                    Height = 15,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5, 0, 0, 0)
                };

                public Button RemovePhoto = new Button
                {
                    Background = Coloring.Transparent.Full,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = new TextBlock
                    {
                        Text = Utils.LocString("Dialog/RemovePhoto"),
                        FontWeight = FontWeights.SemiBold
                    }
                };

                public Settings(ConversationInfo peer)
                {
                    this.Peer = peer;
                    this.Title.Text = this.Peer.Settings.Title;
                    this.Width = 450;
                    this.Children.Add(new ProgressRing
                    {
                        Height = 50,
                        Width = 50,
                        IsActive = true,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(10)
                    });
                    this.LoadImage();
                    this.Load();
                }

                public void Load()
                {
                    StackPanel panel = new StackPanel();
                    Grid grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    Grid.SetColumn(this.Avatar, 0);
                    grid.Children.Add(this.Avatar);

                    Grid titleGrid = new Grid();
                    Grid.SetColumn(titleGrid, 1);
                    grid.Children.Add(titleGrid);

                    titleGrid.RowDefinitions.Add(new RowDefinition());
                    titleGrid.RowDefinitions.Add(new RowDefinition());

                    Grid.SetRow(this.Title, 0);
                    titleGrid.Children.Add(this.Title);
                    Grid.SetRow(this.RemovePhoto, 1);
                    titleGrid.Children.Add(this.RemovePhoto);

                    this.RemovePhoto.Click += async (a, b) =>
                    {
                        if (this.Avatar.ProfilePicture == null) return;
                        try
                        {
                            App.VK.Messages.DeleteChatPhoto(this.Peer.Peer.Id);
                            App.UILoop.AddAction(new UITask
                            {
                                Action = () =>
                                {
                                    this.Avatar.ProfilePicture = null;
                                    this.AvatarUpdated?.Invoke(null);
                                }
                            });
                        }
                        catch (Exception exc)
                        {
                            await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                        }
                    };
                    this.Avatar.PointerPressed += async (a, b) =>
                    {
                        FileOpenPicker picker = new FileOpenPicker
                        {
                            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                            ViewMode = PickerViewMode.Thumbnail,
                        };
                        picker.FileTypeFilter.Add(".png");
                        picker.FileTypeFilter.Add(".jpg");
                        var file = await picker.PickSingleFileAsync();
                        if (file != null)
                        {
                            try
                            {
                                var response = await App.VK.Messages.SetChatPhoto(file, this.Peer.Peer.Id);
                                App.UILoop.AddAction(new UITask
                                {
                                    Action = async () =>
                                    {
                                        this.Avatar.ProfilePicture = await ImageCache.Instance.GetFromCacheAsync(new Uri(response.Chat.Photo200));
                                        this.AvatarUpdated?.Invoke(response.Chat.Photo200);
                                    }
                                });
                            }
                            catch (Exception exc)
                            {
                                await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                            }
                        }
                    };
                    this.Title.PreviewKeyDown += async (a, b) =>
                    {
                        var box = a as TextBox;
                        if (box.Text.Length == 0) return;
                        if (b.Key == Windows.System.VirtualKey.Enter)
                        {
                            b.Handled = true;
                            try
                            {
                                App.VK.Messages.EditTitle(this.Peer.Peer.Id, box.Text);
                                App.UILoop.AddAction(new UITask
                                {
                                    Action = () => this.TitleUpdated(box.Text)
                                });
                            }
                            catch (Exception exc)
                            {
                                await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                            }
                        }
                        else b.Handled = false;
                    };

                    panel.Children.Add(grid);

                    App.UILoop.AddAction(new UITask
                    {
                        Action = () =>
                        {
                            this.Children.Clear();
                            this.Children.Add(panel);
                        }
                    });
                }

                public async void LoadImage() => this.Avatar.ProfilePicture = await ImageCache.Instance.GetFromCacheAsync(new Uri(App.Cache.GetAvatar(this.Peer.Peer.Id)));
            }

            [Bindable]
            public class Link : Grid
            {
                public Link(ConversationInfo chat)
                {
                    this.Children.Add(new ProgressRing
                    {
                        Width = 50,
                        Height = 50,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(10),
                        IsActive = true
                    });
                    Task.Factory.StartNew(() =>
                    {
                        var link = App.VK.Messages.GetInviteLink(chat.Peer.Id);
                        App.UILoop.AddAction(new UITask
                        {
                            Action = () =>
                            {
                                this.Children.Clear();

                                this.RowDefinitions.Add(new RowDefinition { });
                                this.RowDefinitions.Add(new RowDefinition { });

                                var textbox = new TextBox
                                {
                                    IsReadOnly = true,
                                    Text = link,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    Margin = new Thickness(5, 5, 5, 2.5)
                                };
                                Grid.SetRow(textbox, 0);
                                this.Children.Add(textbox);

                                var stack = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    Margin = new Thickness(5, 2.5, 5, 5)
                                };
                                Grid.SetRow(stack, 1);
                                this.Children.Add(stack);

                                var copy = new Button
                                {
                                    Content = new TextBlock
                                    {
                                        Text = Utils.LocString("Copy")
                                    },
                                    Margin = new Thickness(0, 0, 2.5, 0)
                                };
                                stack.Children.Add(copy);
                                copy.Click += (a, b) =>
                                {
                                    var pkg = new DataPackage();
                                    pkg.SetText(textbox.Text);
                                    Clipboard.SetContent(pkg);
                                    var content = copy.Content as TextBlock;
                                    content.Text = Utils.LocString("Copied") + "!";
                                    Task.Factory.StartNew(() =>
                                    {
                                        Thread.Sleep(TimeSpan.FromSeconds(0.7));
                                        App.UILoop.AddAction(new UITask
                                        {
                                            Action = () => content.Text = Utils.LocString("Copy")
                                        });
                                    });
                                };
                                if (chat.Settings.Access.CanChangeInviteLink)
                                {
                                    var reset = new Button
                                    {
                                        Content = new TextBlock
                                        {
                                            Text = Utils.LocString("Reset")
                                        },
                                        Margin = new Thickness(2.5, 0, 0, 0)
                                    };
                                    stack.Children.Add(reset);
                                    reset.Click += (a, b) =>
                                    {
                                        reset.Content = new ProgressRing { IsActive = true };
                                        Task.Factory.StartNew(() =>
                                        {
                                            try
                                            {
                                                var nlink = App.VK.Messages.GetInviteLink(chat.Peer.Id, true);
                                                App.UILoop.AddAction(new UITask
                                                {
                                                    Action = () =>
                                                    {
                                                        reset.Content = new TextBlock
                                                        {
                                                            Text = Utils.LocString("Reset")
                                                        };
                                                        textbox.Text = nlink;
                                                    }
                                                });
                                            }
                                            catch
                                            {
                                                App.UILoop.AddAction(new UITask
                                                {
                                                    Action = () => reset.Visibility = Visibility.Collapsed
                                                });
                                            }
                                        });
                                    };
                                }
                            }
                        });
                    });
                }
            }

            public class AddUserDialog : Grid
            {
                public delegate void Event();

                public Event Hide;

                public AddUserDialog(int peer_id)
                {
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                    this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    this.Width = 500;
                    this.MinHeight = 300;

                    var searchbar = new TextBox
                    {
                        PlaceholderText = Utils.LocString("Search"),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Margin = new Thickness(10)
                    };
                    Grid.SetRow(searchbar, 0);
                    this.Children.Add(searchbar);

                    var userlist = new ListView
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch
                    };
                    var scroll = new ScrollViewer
                    {
                        HorizontalScrollMode = ScrollMode.Disabled,
                        VerticalScrollMode = ScrollMode.Auto,
                        Content = userlist
                    };
                    Grid.SetRow(scroll, 1);
                    this.Children.Add(scroll);
                    var progress = new ProgressRing
                    {
                        Width = 50,
                        Height = 50,
                        IsActive = true,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(20)
                    };
                    Grid.SetRow(progress, 1);
                    this.Children.Add(progress);

                    userlist.SelectionChanged += (a, b) =>
                    {
                        if (userlist.SelectedItem is UserItem item)
                        {
                            var dialog = new AddDialog(item.UserId, peer_id);
                            var popup = new Popup
                            {
                                Title = Utils.LocString("Dialog/InviteUser") + ": " + item.UserName,
                                Content = dialog
                            };
                            dialog.Hide += () =>
                            {
                                popup.Hide();
                                userlist.SelectedItem = null;
                            };
                            dialog.OnSuccess += () => this.Hide?.Invoke();
                            App.MainPage.Popup.Children.Add(popup);
                        }
                    };
                    Task.Factory.StartNew(() =>
                    {
                        int total = -1;
                        var users = new List<User>();
                        var members = App.VK.Messages.GetConversationMembers(peer_id).Items.Select(i => i.MemberId);
                        while (total == -1 || users.Count < total)
                        {
                            var response = App.VK.Friends.Get(count: 5000, offset: users.Count);
                            total = response.Count;
                            users.AddRange(response.Items);
                            if (users.Count < total) Thread.Sleep(TimeSpan.FromSeconds(0.7));
                        }

                        App.UILoop.AddAction(new UITask
                        {
                            Action = () =>
                            {
                                foreach (var user in users)
                                {
                                    if (!members.Contains(user.UserId))
                                    {
                                        var item = new UserItem(user.UserId);
                                        searchbar.TextChanged += (a, b) => item.Visibility = Regex.IsMatch(item.UserName, (a as TextBox).Text, RegexOptions.IgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
                                        userlist.Items.Add(item);
                                    }
                                }
                                this.Children.Remove(progress);
                            }
                        });
                    });
                }

                private class UserItem : ListViewItem
                {
                    public int UserId;
                    public string UserName;

                    public UserItem(int user_id)
                    {
                        this.UserId = user_id;
                        this.UserName = App.Cache.GetName(this.UserId);
                        this.HorizontalAlignment = HorizontalAlignment.Stretch;

                        var content = new StackPanel { Orientation = Orientation.Horizontal };
                        content.Children.Add(new Avatar(this.UserId)
                        {
                            Width = 50,
                            Height = 50,
                            Margin = new Thickness(0, 5, 10, 5)
                        });
                        content.Children.Add(new TextBlock
                        {
                            Text = this.UserName,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontWeight = FontWeights.SemiBold
                        });
                        this.Content = content;
                    }
                }

                private class AddDialog : StackPanel
                {
                    public delegate void Event();

                    public Event Hide;
                    public Event OnSuccess;

                    public AddDialog(int user_id, int peer_id)
                    {
                        var gr = new Grid
                        {
                            Margin = new Thickness(5)
                        };
                        gr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        gr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                        var text = new TextBlock
                        {
                            Text = Utils.LocString("Dialog/InviteUserShowMessages"),
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 20, 0)
                        };
                        Grid.SetColumn(text, 0);
                        gr.Children.Add(text);
                        var num = new Microsoft.UI.Xaml.Controls.NumberBox
                        {
                            Maximum = 1000,
                            Minimum = 0,
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            SpinButtonPlacementMode = Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Inline,
                            Value = 100
                        };
                        Grid.SetColumn(num, 1);
                        gr.Children.Add(num);
                        this.Children.Add(gr);

                        var okbtn = new Button
                        {
                            Content = new TextBlock
                            {
                                Text = Utils.LocString("Add")
                            },
                            CornerRadius = new CornerRadius(10),
                            Margin = new Thickness(5),
                            HorizontalAlignment = HorizontalAlignment.Right
                        };
                        this.Children.Add(okbtn);

                        okbtn.Click += async (a, b) =>
                        {
                            try
                            {
                                App.VK.Messages.AddChatUser(peer_id, user_id, (int)num.Value);
                                this.OnSuccess?.Invoke();
                            }
                            catch (Exception exc)
                            {
                                await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                            }
                            this.Hide?.Invoke();
                        };
                    }
                }
            }
        }
    }
}