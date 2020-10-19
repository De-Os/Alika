using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Responses;
using Alika.UI.Items;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{
    [Windows.UI.Xaml.Data.Bindable]
    public class ChatInformation : StackPanel
    {
        public int peer_id;
        public GetConversationsResponse.ConversationResponse.ConversationInfo conversation;

        public AvatarAndName avatarAndName;

        public Popup popup = new Popup
        {
            Title = Utils.LocString("Dialog/Conversationinfo")
        };

        public ChatInformation(int peer_id)
        {
            this.peer_id = peer_id;

            if (peer_id == App.vk.user_id) return;

            this.Load();
        }

        public void Load()
        {
            this.conversation = App.cache.GetConversation(this.peer_id);

            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.Width = 500;

            this.AddSeparator();
            this.avatarAndName = new AvatarAndName(this.conversation);
            this.Children.Add(this.avatarAndName);
            this.AddSeparator();
            this.Children.Add(new AttachmentsList(this.peer_id));
            this.AddSeparator();

            if (this.peer_id > Limits.Messages.PEERSTART)
            {
                var convMenu = new ConversationItems(this.conversation);
                convMenu.AvatarUpdated += (av) =>
                {
                    if (av == null)
                    {
                        this.avatarAndName.Avatar.ProfilePicture = null;
                    }
                    else this.avatarAndName.LoadImage(av);
                    this.conversation.settings.photos.photo_200 = av;
                    Task.Factory.StartNew(() => App.cache.Update(this.peer_id));
                };
                convMenu.TitleUpdated += (name) =>
                {
                    this.avatarAndName.Title.Text = name;
                    this.conversation.settings.title = name;
                    (App.main_page.dialog.Children[0] as Dialog.Dialog).top_menu.name.Text = name;
                    Task.Factory.StartNew(() => App.cache.Update(this.peer_id));
                };
                this.Children.Add(convMenu);
            }

            this.popup.Content = new ScrollViewer
            {
                HorizontalScrollMode = ScrollMode.Disabled,
                VerticalScrollMode = ScrollMode.Auto,
                Content = this
            };


            App.UILoop.AddAction(new UITask
            {
                Action = () => { App.main_page.popup.Children.Add(this.popup); }
            });

        }

        private void AddSeparator(double height = 30)
        {
            this.Children.Add(new Grid { Height = height });
        }

        [Windows.UI.Xaml.Data.Bindable]
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
            public GetConversationsResponse.ConversationResponse.ConversationInfo conv;
            public AvatarAndName(GetConversationsResponse.ConversationResponse.ConversationInfo conversation)
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
                if (this.conv.peer.id > 0)
                {
                    if (this.conv.peer.id > Limits.Messages.PEERSTART)
                    {
                        (subtext as TextBlock).Text = this.conv.settings.members_count.ToString() + " members";
                    }
                    else
                    {
                        subtext = new OnlineText(this.conv.peer.id);
                    }
                }
                if (this.conv.peer.id > Limits.Messages.PEERSTART)
                {
                    (subtext as TextBlock).Text = Utils.LocString("Dialog/MembersCount").Replace("%count%", this.conv.settings.members_count.ToString());
                }
                subtext.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetRow(subtext, 1);
                text.Children.Add(subtext);

                Grid.SetColumn(text, 1);
                this.Children.Add(text);

                this.LoadTitle();
                this.LoadImage(App.cache.GetAvatar(this.conv.peer.id));
            }

            public void LoadTitle()
            {
                this.Title.Text = App.cache.GetName(this.conv.peer.id);
            }

            public async void LoadImage(string uri)
            {
                this.Avatar.ProfilePicture = await ImageCache.Instance.GetFromCacheAsync(new Uri(uri));
            }
        }

        [Windows.UI.Xaml.Data.Bindable]
        public class AttachmentsList : ContentControl
        {
            public AttachmentsList(int peer_id, string title = "Attachments/Attachments")
            {
                var content = new Popup.Menu(title);
                this.Content = content;
                this.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                AddElement("photo", "Attachments/Photos", "camera.svg");
                AddElement("doc", "Attachments/Documents", "document.svg");
                AddElement("video", "Attachments/Videos", "video.svg");
                AddElement("link", "Attachments/Links", "link.svg");
                AddElement("audio_message", "Attachments/VoiceMessages", "microphone.svg");

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
                        if (value) App.main_page.popup.Children.Add(this.loadingPopup);
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

                    App.main_page.popup.Children.Add(this.popup);

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
                        var response = App.vk.Messages.GetHistoryAttachments(peer_id: this.peer_id, type: this.type, start_from: this.offset, count: 50);
                        App.UILoop.AddAction(new UITask
                        {
                            Action = async () =>
                           {
                               if (response.items.Count > 0)
                               {
                                   this.offset = response.next_from;

                                   StackPanel final = new StackPanel();

                                   if (this.type == "photo")
                                   {
                                       StackPanel stack = new StackPanel { Orientation = Orientation.Horizontal };
                                       foreach (var att in response.items)
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
                                               Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(att.attachment.photo.GetBestQuality().url))
                                           };
                                           img.PointerPressed += (a, b) => new MediaViewer(att.attachment);
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
                                       foreach (var att in response.items)
                                       {
                                           switch (this.type)
                                           {
                                               case "doc":
                                                   if (att.attachment.document != null) final.Children.Add(new MessageAttachment.Document(att.attachment.document)
                                                   {
                                                       HorizontalAlignment = HorizontalAlignment.Stretch,
                                                       HorizontalContentAlignment = HorizontalAlignment.Left,
                                                       Margin = new Thickness(5)
                                                   });
                                                   break;
                                               case "audio_message":
                                                   final.Children.Add(new MessageAttachment.AudioMessage(att.attachment.audio_message)
                                                   {
                                                       HorizontalAlignment = HorizontalAlignment.Stretch,
                                                       Margin = new Thickness(5)
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

            private Popup PermsPopup;
            private Popup MembersPopup;
            private Popup SettingsPopup;

            public ConversationItems(GetConversationsResponse.ConversationResponse.ConversationInfo peer)
            {
                var content = new Popup.Menu("Dialog/Management");
                this.Content = content;
                this.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                if (peer.settings.access.can_change_info)
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
                            "edit.svg",
                            (a, b) => App.main_page.popup.Children.Add(this.SettingsPopup)
                        ));
                }
                if (peer.settings?.permissions != null && peer.settings?.owner_id == App.vk.user_id)
                {
                    this.PermsPopup = new Popup
                    {
                        Content = new Permissions(peer.settings.permissions, peer.peer.id),
                        Title = Utils.LocString("Dialog/Permissions")
                    };
                    content.Children.Add(new Popup.Menu.Element(
                            "Dialog/Permissions",
                            "settings.svg",
                            (a, b) => App.main_page.popup.Children.Add(this.PermsPopup)
                        ));
                }

                this.MembersPopup = new Popup
                {
                    Content = new Members(peer.peer.id),
                    Title = Utils.LocString("Dialog/Members")
                };
                content.Children.Add(new Popup.Menu.Element(
                        "Dialog/Members",
                        "person.svg",
                        (a, b) => App.main_page.popup.Children.Add(this.MembersPopup)
                    ));
            }

            [Windows.UI.Xaml.Data.Bindable]
            public class Permissions : StackPanel
            {
                public static Dictionary<string, Type> Types = new Dictionary<string, Type> {
                    {"invite", new Type
                    {
                        Name = Utils.LocString("Dialog/PermissionsInvite"),
                        Picture = Utils.AssetTheme("add_user.svg")
                    } },
                    {"change_info", new Type
                    {
                        Name = Utils.LocString("Dialog/PermissionsChangeInfo"),
                        Picture = Utils.AssetTheme("edit.svg")
                    } },
                    {"change_pin", new Type{
                        Name = Utils.LocString("Dialog/PermissionsPin"),
                        Picture = Utils.AssetTheme("pin.svg")
                    } },
                    {"use_mass_mentions", new Type{
                        Name = Utils.LocString("Dialog/PermissionsMentions"),
                        Picture = Utils.AssetTheme("mention.svg")
                    } },
                    {"see_invite_link", new Type{
                        Name = Utils.LocString("Dialog/PermissionsLink"),
                        Picture = Utils.AssetTheme("eye.svg")
                    } },
                    {"call", new Type{
                        Name = Utils.LocString("Dialog/PermissionsCall"),
                        Picture = Utils.AssetTheme("call.svg")
                    } },
                    {"change_admins", new Type{
                        Name = Utils.LocString("Dialog/PermissionsChangeAdmins"),
                        Picture = Utils.AssetTheme("user.svg"),
                        IsAllDisabled = true
                    } }
                };
                private static Dictionary<string, string> StateNamings = new Dictionary<string, string> {
                    {"all", Utils.LocString("Dialog/PermissionsTypeAll")},
                    {"owner_and_admins", Utils.LocString("Dialog/PermissionsTypeOwnerAndAdmins")},
                    {"owner", Utils.LocString("Dialog/PermissionsTypeOwner")}
                };

                public GetConversationsResponse.ConversationResponse.ConversationInfo.PeerSettings.Permissions permissions;
                public int peer_id;

                public Permissions(GetConversationsResponse.ConversationResponse.ConversationInfo.PeerSettings.Permissions permissions, int peer_id)
                {
                    this.peer_id = peer_id;
                    this.permissions = permissions;

                    this.Children.Add(new Element("invite", permissions.invite));
                    this.Children.Add(new Element("change_info", permissions.change_info));
                    this.Children.Add(new Element("change_pin", permissions.change_pin));
                    this.Children.Add(new Element("use_mass_mentions", permissions.use_mass_mentions));
                    this.Children.Add(new Element("see_invite_link", permissions.see_invite_link));
                    this.Children.Add(new Element("call", permissions.call));
                    this.Children.Add(new Element("change_admins", permissions.change_admins));
                }

                [Windows.UI.Xaml.Data.Bindable]
                public class Element : Grid
                {
                    public Image Icon = new Image
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

                        this.Icon.Source = new SvgImageSource(new Uri(type.Picture));
                        this.Text.Text = type.Name;

                        var button = new ChangeButton(name, state);

                        Grid.SetColumn(this.Icon, 0);
                        Grid.SetColumn(this.Text, 1);
                        Grid.SetColumn(button, 2);

                        this.Children.Add(this.Icon);
                        this.Children.Add(this.Text);
                        this.Children.Add(button);
                    }

                    [Windows.UI.Xaml.Data.Bindable]
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
                            var img = new Image
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                Height = 15,
                                Width = 15,
                                Margin = new Thickness(5),
                                Source = new SvgImageSource(new Uri(Utils.AssetTheme("fly_menu.svg")))
                            };
                            Grid.SetColumn(img, 1);
                            this._content.Children.Add(img);

                            var fl = new FlyOut(type, state);
                            this.Flyout = fl;
                            fl.StateChanged += (s) => this.state.Text = StateNamings[s];
                        }

                        [Windows.UI.Xaml.Data.Bindable]
                        public class FlyOut : Flyout
                        {
                            public delegate void Changed(string newtype);
                            public event Changed StateChanged;

                            public FlyOut(string type, string current_state)
                            {
                                StackPanel stack = new StackPanel();
                                this.Content = stack;

                                foreach (KeyValuePair<string, string> e in StateNamings)
                                {
                                    if (e.Key == Type.AllName && Types[type].IsAllDisabled) continue;
                                    Button btn = new Button
                                    {
                                        Content = new TextBlock
                                        {
                                            Text = e.Value,
                                            TextTrimming = TextTrimming.CharacterEllipsis
                                        },
                                        Padding = new Thickness(7.5),
                                        Margin = new Thickness(2.5),
                                        CornerRadius = new CornerRadius(10),
                                        HorizontalAlignment = HorizontalAlignment.Stretch,
                                        HorizontalContentAlignment = HorizontalAlignment.Center,
                                        Background = e.Key == current_state ? Coloring.Transparent.Percent(50) : Coloring.Transparent.Full
                                    };
                                    btn.Click += (a, b) =>
                                    {
                                        if (btn.Background == Coloring.Transparent.Full)
                                        {
                                            try
                                            {
                                                App.vk.Call<int>("messages.editChat", new Dictionary<string, dynamic> {
                                                    {"chat_id", App.main_page.peer_id - Limits.Messages.PEERSTART },
                                                    {"permissions", "{\"" + type + "\": \"" + e.Key + "\"}"  }
                                                });
                                                for (int x = 0; x < stack.Children.Count; x++) if (stack.Children[x] is Button bn && bn != btn) bn.Background = Coloring.Transparent.Full;
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
                                    stack.Children.Add(btn);
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

            [Windows.UI.Xaml.Data.Bindable]
            public class Members : Grid
            {
                private int peer_id;
                public Members(int peer_id)
                {
                    this.peer_id = peer_id;
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
                        var users = App.vk.Messages.GetConversationMembers(this.peer_id, "photo_200,online_info");
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
                                foreach (var m in users.members)
                                {
                                    var member = new Member(m, this.peer_id);
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

                [Windows.UI.Xaml.Data.Bindable]
                public class Member : Grid
                {
                    public Button actions = new Button
                    {
                        Content = new Image
                        {
                            Source = new SvgImageSource(new Uri(Utils.AssetTheme("fly_menu.svg"))),
                            Width = 15,
                            Height = 15
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
                    public GetConversationMembersResponse.Member member;
                    public int peer_id;

                    public Member(GetConversationMembersResponse.Member member, int peer_id)
                    {
                        this.member = member;
                        this.peer_id = peer_id;

                        this.HorizontalAlignment = HorizontalAlignment.Stretch;
                        this.Margin = new Thickness(5);

                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 });
                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                        this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                        var avatar = new Misc.Avatar(member.member_id)
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
                            Content = new Image
                            {
                                Source = new SvgImageSource(new Uri(Utils.AssetTheme("info.svg"))),
                                Width = 15,
                                Height = 15
                            },
                            Background = Coloring.Transparent.Full,
                            CornerRadius = new CornerRadius(10),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                            Flyout = new Information(member),
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
                        Actions flyout = new Actions(this.member, this.peer_id);
                        if (flyout.stack.Children.Count == 0)
                        {
                            this.actions.Flyout = null;
                            this.actions.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            flyout.InfoChanged += (a) =>
                            {
                                this.member = a;
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
                        this.name.Text = App.cache.GetName(this.member.member_id);
                        if (member.is_admin) this.name.Text += " ⭐";
                    }

                    [Windows.UI.Xaml.Data.Bindable]
                    public class Actions : Flyout
                    {
                        public delegate void Event(GetConversationMembersResponse.Member member);
                        public event Event Remove;
                        public event Event InfoChanged;

                        public Grid content = new Grid();

                        public GetConversationMembersResponse.Member member;
                        public int peer_id;

                        public StackPanel stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };

                        public Actions(GetConversationMembersResponse.Member member, int peer_id)
                        {
                            this.member = member;
                            this.peer_id = peer_id;

                            bool isOwner = App.cache.GetConversation(peer_id).settings.owner_id == App.vk.user_id;

                            if (isOwner && member.member_id != App.vk.user_id)
                            {
                                if (member.is_admin)
                                {
                                    Button demote = this.GetBtn("Dialog/Demote");
                                    demote.Click += async (a, b) =>
                                    {
                                        try
                                        {
                                            App.vk.Messages.SetMemberRole(false, member.member_id, peer_id);
                                            member.is_admin = false;
                                            this.InfoChanged?.Invoke(member);
                                            this.Hide();
                                        }
                                        catch (Exception exc)
                                        {
                                            await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                                        }
                                    };
                                    this.stack.Children.Add(demote);
                                }
                                else
                                {
                                    Button promote = this.GetBtn("Dialog/Promote");
                                    promote.Click += async (a, b) =>
                                    {
                                        try
                                        {
                                            App.vk.Messages.SetMemberRole(true, member.member_id, peer_id);
                                            member.is_admin = true;
                                            this.InfoChanged?.Invoke(member);
                                            this.Hide();
                                        }
                                        catch (Exception exc)
                                        {
                                            await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                                        }
                                    };
                                    this.stack.Children.Add(promote);
                                }
                            }

                            if (member.can_kick)
                            {
                                Button kick = this.GetBtn("Dialog/Kick");
                                kick.Click += async (a, b) =>
                                {
                                    try
                                    {
                                        App.vk.Messages.RemoveChatUser(peer_id, member.member_id);
                                        this.Remove?.Invoke(null);
                                        this.Hide();
                                    }
                                    catch (Exception exc)
                                    {
                                        await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                                    }
                                };
                                this.stack.Children.Add(kick);
                            }

                            this.Content = this.stack;
                        }

                        private Button GetBtn(string text)
                        {
                            return new Button
                            {
                                Background = Coloring.Transparent.Full,
                                Content = new TextBlock
                                {
                                    Text = Utils.LocString(text)
                                },
                                HorizontalContentAlignment = HorizontalAlignment.Left,
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                CornerRadius = new CornerRadius(10),
                                Margin = new Thickness(0, 5, 0, 5)
                            };
                        }
                    }

                    [Windows.UI.Xaml.Data.Bindable]
                    public class Information : Flyout
                    {
                        public Information(GetConversationMembersResponse.Member member)
                        {
                            StackPanel stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch };

                            stack.Children.Add(new TextBlock
                            {
                                Text = Utils.LocString("Dialog/InvitedTime").Replace("%time%", member.join_date.ToDateTime().ToString(@"d.M.y H:mm:ss")),
                                VerticalAlignment = VerticalAlignment.Center
                            });

                            if (member.invited_by != member.member_id)
                            {
                                stack.Children.Add(new TextBlock
                                {
                                    Text = Utils.LocString("Dialog/JoinedInfo").Replace("%user%", App.cache.GetName(member.invited_by)),
                                    VerticalAlignment = VerticalAlignment.Center
                                });
                            }

                            this.Content = stack;
                        }
                    }
                }
            }

            [Windows.UI.Xaml.Data.Bindable]
            public class Settings : Grid
            {
                public delegate void Event(string str);
                public event Event AvatarUpdated;
                public event Event TitleUpdated;

                public GetConversationsResponse.ConversationResponse.ConversationInfo peer;
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

                public Settings(GetConversationsResponse.ConversationResponse.ConversationInfo peer)
                {
                    this.peer = peer;
                    this.Title.Text = this.peer.settings.title;
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
                    this.Loaded += (a, b) => this.Load();
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
                            App.vk.Messages.DeleteChatPhoto(this.peer.peer.id);
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
                                var response = await App.vk.Messages.SetChatPhoto(file, peer.peer.id);
                                App.UILoop.AddAction(new UITask
                                {
                                    Action = async () =>
                                    {
                                        this.Avatar.ProfilePicture = await ImageCache.Instance.GetFromCacheAsync(new Uri(response.chat.photo_200));
                                        this.AvatarUpdated?.Invoke(response.chat.photo_200);
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
                                App.vk.Messages.EditTitle(this.peer.peer.id, box.Text);
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

                public async void LoadImage() => this.Avatar.ProfilePicture = await ImageCache.Instance.GetFromCacheAsync(new Uri(App.cache.GetAvatar(this.peer.peer.id)));
            }
        }
    }
}
