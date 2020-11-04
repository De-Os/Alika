using Alika.Libs;
using Alika.Libs.VK;
using Alika.Misc;
using Alika.UI.Items;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Alika.UI.Dialog
{
    [Windows.UI.Xaml.Data.Bindable]
    public class TopMenu : Grid
    {
        public int peer_id;

        public TextBlock name = new TextBlock
        {
            FontWeight = FontWeights.Bold,
            FontSize = 18,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        public TopMenu(int peer_id)
        {
            this.peer_id = peer_id;

            this.Margin = new Thickness(0, 25, 0, 10);
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            this.LoadTitles();
            this.LoadMenu();
        }

        public void LoadTitles()
        {
            string title = App.Cache.GetName(peer_id);
            var about = new ContentControl
            {
                VerticalAlignment = VerticalAlignment.Top
            };
            FrameworkElement defAbout;
            var typing = new TypeState(this.peer_id);
            if (this.peer_id < 0 || this.peer_id > Limits.Messages.PEERSTART)
            {
                defAbout = new TextBlock
                {
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Text = Utils.LocString(this.peer_id < 0 ? "Dialog/Group" : "Dialog/Conference")
                };
            }
            else
            {
                defAbout = new OnlineText(this.peer_id);
            }
            about.Content = defAbout;
            typing.Show += () => about.Content = typing;
            typing.Hide += () => about.Content = defAbout;

            Grid text = new Grid
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            text.PointerPressed += (a, b) => new ChatInformation(App.MainPage.PeerId);
            text.RowDefinitions.Add(new RowDefinition());
            text.RowDefinitions.Add(new RowDefinition());
            Grid.SetColumn(text, 0);
            this.name.Text = title;
            Grid.SetRow(this.name, 0);
            text.Children.Add(this.name);
            Grid.SetRow(about, 1);
            text.Children.Add(about);
            this.Children.Add(text);
        }

        private void LoadMenu()
        {
            Button button = new Button
            {
                Content = new FontIcon
                {
                    Glyph = "\uE712",
                    FontSize = 20
                },
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 10, 5, 0),
                Background = Coloring.Transparent.Full,
                Flyout = new FlyoutMenu(this.peer_id)
            };
            Grid.SetColumn(button, this.ColumnDefinitions.Count);
            this.Children.Add(button);
        }

        public class FlyoutMenu : MenuFlyout
        {
            public FlyoutMenu(int peer_id)
            {
                var info = new MenuFlyoutItem
                {
                    Icon = new FontIcon
                    {
                        Glyph = "\uE946"
                    },
                    Text = Utils.LocString("Dialog/TopMenuInformation")
                };
                info.Click += (a, b) => new ChatInformation(peer_id);
                this.Items.Add(info);

                var attachs = new MenuFlyoutItem
                {
                    Icon = new FontIcon
                    {
                        Glyph = "\uED25"
                    },
                    Text = Utils.LocString("Dialog/TopMenuAttachments")
                };
                attachs.Click += (a, b) =>
                {
                    App.MainPage.Popup.Children.Add(new Popup
                    {
                        Content = new ChatInformation.AttachmentsList(peer_id, null),
                        Title = Utils.LocString("Attachments/Attachments")
                    });
                }; ;
                this.Items.Add(attachs);

                var openImportant = new MenuFlyoutItem
                {
                    Icon = new FontIcon
                    {
                        Glyph = "\uE734"
                    },
                    Text = Utils.LocString("Dialog/ImportantMessages")
                };
                openImportant.Click += (a, b) => new ImportantMessages(peer_id);
                this.Items.Add(openImportant);

                if (peer_id > Limits.Messages.PEERSTART)
                {
                    var conv = App.Cache.GetConversation(peer_id);

                    if (conv.Settings.Access.CanInvite)
                    {
                        var addUser = new MenuFlyoutItem
                        {
                            Icon = new FontIcon
                            {
                                Glyph = "\uE8FA"
                            },
                            Text = Utils.LocString("Dialog/InviteUser")
                        };
                        addUser.Click += (a, b) =>
                        {
                            var dialog = new ChatInformation.ConversationItems.AddUserDialog(peer_id);
                            var popup = new Popup
                            {
                                Content = dialog,
                                Title = Utils.LocString("Dialog/InviteUser")
                            };
                            dialog.Hide += () => popup.Hide();
                            App.MainPage.Popup.Children.Add(popup);
                        };
                        this.Items.Add(addUser);
                    }
                    /*Element leave = new Element("Dialog/TopMenuLeave", "leave.svg");
                    Grid.SetRow(leave, this.content.RowDefinitions.Count);
                    this.content.RowDefinitions.Add(new RowDefinition());
                    this.content.Children.Add(leave);*/
                }
            }
        }
    }
}