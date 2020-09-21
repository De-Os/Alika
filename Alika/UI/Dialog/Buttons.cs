using Alika.Libs;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI.Dialog
{
    public class Buttons
    {
        public class Send : Button
        {
            public Send()
            {
                this.CornerRadius = new CornerRadius(5);
                this.Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("send.svg"))),
                    Height = 20
                };
                this.Width = 50;
                this.Margin = new Thickness(5, 10, 20, 10);
                this.HorizontalAlignment = HorizontalAlignment.Right;
                this.Background = Coloring.Transparent.Full;
            }
        }

        public class Attachment : Button
        {
            public Attachment()
            {
                this.CornerRadius = new CornerRadius(5);
                this.Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("clip.svg"))),
                    Width = 20,
                    Height = 20
                };
                this.Width = 50;
                this.Margin = new Thickness(20, 10, 5, 10);
                this.HorizontalAlignment = HorizontalAlignment.Left;
                this.Background = Coloring.Transparent.Full;

                this.RightTapped += (a, b) => App.UILoop.RunAction(new UITask
                {
                    Action = () =>
                    {
                        var gw = new GraffitiWindow();
                        var popup = new Popup
                        {
                            Content = gw,
                            Title = Utils.LocString("Attachments/Graffiti")
                        };
                        gw.OnClose += () => popup.Hide();
                        App.main_page.popup.Children.Add(popup);
                    }
                });
            }
        }

        public class Stickers : Button
        {
            public Stickers()
            {
                this.CornerRadius = new CornerRadius(5);
                this.Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("sticker.svg"))),
                    Height = 20
                };
                this.Width = 50;
                this.Margin = new Thickness(5, 10, 5, 10);
                this.HorizontalAlignment = HorizontalAlignment.Right;
                this.Background = Coloring.Transparent.Full;
            }
        }
    }
}
