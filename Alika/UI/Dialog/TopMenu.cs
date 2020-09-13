using Alika.Libs;
using Alika.Libs.VK;
using System;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI.Dialog
{
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

            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            this.LoadTitles();
            this.LoadMenu();
        }

        public void LoadTitles()
        {
            string title = App.cache.GetName(this.peer_id).Text;
            string desc;
            if (this.peer_id > 0)
            {
                if (this.peer_id > Limits.Messages.PEERSTART)
                {
                    desc = Utils.LocString("Dialog/Conference");
                }
                else
                {
                    var user = App.cache.GetUser(this.peer_id);
                    desc = Utils.OnlineText(user.online_info.is_online, user.online_info.last_seen);
                }
            }
            else
            {
                desc = Utils.LocString("Dialog/Group");
            }

            Grid text = new Grid
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            text.PointerPressed += (a, b) => new ChatInformation(App.main_page.peer_id);
            text.RowDefinitions.Add(new RowDefinition());
            text.RowDefinitions.Add(new RowDefinition());
            Grid.SetColumn(text, 0);
            this.name.Text = title;
            Grid.SetRow(this.name, 0);
            text.Children.Add(this.name);
            TextBlock about = new TextBlock
            {
                Text = desc,
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetRow(about, 1);
            text.Children.Add(about);
            this.Children.Add(text);
        }
        private void LoadMenu()
        {
            Button button = new Button
            {
                Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("fly_menu.svg"))),
                    Width = 20,
                    Height = 20,
                },
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                Background = Coloring.Transparent.Full,
                Flyout = new FlyoutMenu(this.peer_id)
            };
            Grid.SetColumn(button, this.ColumnDefinitions.Count);
            this.Children.Add(button);
        }

        public class FlyoutMenu : Flyout
        {
            public int peer_id;
            public Grid content = new Grid();

            public FlyoutMenu(int peer_id)
            {
                this.peer_id = peer_id;

                this.Content = this.content;

                this.content.PointerPressed += (a, b) =>
                {
                    b.Handled = false;
                    this.Hide();
                };

                Element info = new Element("Dialog/TopMenuInformation", "info.svg");
                info.PointerPressed += (a, b) => new ChatInformation(App.main_page.peer_id);
                Grid.SetRow(info, this.content.RowDefinitions.Count);
                this.content.RowDefinitions.Add(new RowDefinition());
                this.content.Children.Add(info);

                Element attachs = new Element("Dialog/TopMenuAttachments", "album.svg");
                attachs.PointerPressed += (a, b) =>
                {
                    App.main_page.popup.Children.Add(new Popup
                    {
                        Content = new ChatInformation.AttachmentsList(peer_id),
                        Title = Utils.LocString("Attachments/Attachments")
                    });
                }; ;
                Grid.SetRow(attachs, this.content.RowDefinitions.Count);
                this.content.RowDefinitions.Add(new RowDefinition());
                this.content.Children.Add(attachs);

                /*if (this.peer_id > Limits.Messages.PEERSTART)
                {
                    Element leave = new Element("Dialog/TopMenuLeave", "leave.svg");
                    Grid.SetRow(leave, this.content.RowDefinitions.Count);
                    this.content.RowDefinitions.Add(new RowDefinition());
                    this.content.Children.Add(leave);
                }*/
            }

            public class Element : Grid
            {
                public Image Icon = new Image
                {
                    Width = 15,
                    Height = 15,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                public TextBlock Text = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.SemiBold
                };
                public Element(string title, string icon)
                {
                    this.HorizontalAlignment = HorizontalAlignment.Stretch;
                    this.Padding = new Thickness(10);
                    this.CornerRadius = new CornerRadius(5);

                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    this.Icon.Source = new SvgImageSource(new Uri(Utils.AssetTheme(icon)));
                    this.Text.Text = Utils.LocString(title);

                    Grid.SetColumn(this.Icon, 0);
                    Grid.SetColumn(this.Text, 1);

                    this.Children.Add(this.Icon);
                    this.Children.Add(this.Text);

                    this.PointerEntered += (a, b) =>
                    {
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
                        this.Background = Coloring.Transparent.Percent(100);
                    };
                    this.PointerExited += (a, b) =>
                    {
                        Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                        this.Background = Coloring.Transparent.Full;
                    };
                    this.PointerPressed += (a, b) => Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                }
            }
        }
    }
}
