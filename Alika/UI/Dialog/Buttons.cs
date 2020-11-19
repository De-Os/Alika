using Alika.Libs;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Alika.Theme;

namespace Alika.UI.Dialog
{
    public class Buttons
    {
        [Windows.UI.Xaml.Data.Bindable]
        public class Send : Button
        {
            public Send()
            {
                this.CornerRadius = new CornerRadius(5);
                this.Content = new ThemedFontIcon
                {
                    FontSize = 20,
                    Glyph = Glyphs.Send
                };
                this.Width = 50;
                this.Margin = new Thickness(5, 10, 20, 10);
                this.HorizontalAlignment = HorizontalAlignment.Right;
                this.Background = App.Theme.Colors.Transparent;
            }
        }

        [Windows.UI.Xaml.Data.Bindable]
        public class Attachment : Button
        {
            public Attachment()
            {
                this.CornerRadius = new CornerRadius(5);
                this.Content = new ThemedFontIcon
                {
                    FontSize = 20,
                    Glyph = Glyphs.Attach
                };
                this.Width = 50;
                this.Margin = new Thickness(20, 10, 5, 10);
                this.HorizontalAlignment = HorizontalAlignment.Left;
                this.Background = App.Theme.Colors.Transparent;

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
                        App.MainPage.Popup.Children.Add(popup);
                    }
                });
            }
        }

        [Windows.UI.Xaml.Data.Bindable]
        public class Stickers : Button
        {
            public Stickers()
            {
                this.CornerRadius = new CornerRadius(5);
                this.Content = new ThemedFontIcon
                {
                    FontSize = 20,
                    Glyph = Glyphs.Emoji
                };
                this.Width = 50;
                this.Margin = new Thickness(5, 10, 5, 10);
                this.HorizontalAlignment = HorizontalAlignment.Right;
                this.Background = App.Theme.Colors.Transparent;
            }
        }
    }
}