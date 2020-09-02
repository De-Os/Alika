using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.Libs.VK
{
    public class CaptchaSettings
    {
        public UIElement Title { get; set; }
        public UIElement Button { get; set; }
        public string Placeholder { get; set; }
    }

    public class CaptchaDialog : ContentDialog
    {
        public Grid content;
        public Image img;
        public TextBox text = new TextBox
        {
            Margin = new Thickness(10)
        };

        public CaptchaDialog(string url, CaptchaSettings settings)
        {
            if (settings.Title != null) this.Title = settings.Title;
            if (settings.Placeholder != null) this.text.PlaceholderText = settings.Placeholder;

            this.content = new Grid();
            this.content.RowDefinitions.Add(new RowDefinition());
            this.content.RowDefinitions.Add(new RowDefinition());
            this.content.RowDefinitions.Add(new RowDefinition());

            this.img = new Image();
            this.img.Source = new BitmapImage(new Uri(url));
            Grid.SetRow(this.img, 0);
            this.content.Children.Add(img);

            Grid.SetRow(this.text, 1);
            this.content.Children.Add(this.text);

            Button close = new Button
            {
                Content = settings.Button,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(close, 2);
            close.Click += (object sender, RoutedEventArgs e) =>
            {
                if (this.text.Text.Length > 0)
                {
                    this.Hide();
                }
            };
            this.KeyDown += (object s, KeyRoutedEventArgs e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter && this.text.Text.Length > 0) this.Hide();
            };
            this.content.Children.Add(close);

            this.Content = this.content;
        }
    }
}
