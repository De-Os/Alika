using Alika.UI;
using Alika.UI.Dialog;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Alika
{
    public sealed partial class MainPage : Page
    {
        private int _peer_id;
        public int peer_id
        {
            get
            {
                return this._peer_id;
            }
            set
            {
                this._peer_id = value;
                if (this.dialog.Children.Count > 0 && this.dialog.Children[0] is Dialog old)
                {
                    if (old.peer_id == value) return;
                    (old.stickers.Flyout as Flyout).Content = null; // Remove previous flyout to prevent crash on stickers opening
                    this.dialog.Children.Clear();
                }
                var list = new Dialog(value);
                this.dialog.Children.Add(list);
                this.dialog.Children.Add(list.stickers_suggestions);
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            this.chats_grid.Content = new ChatsHolder();

            Task.Factory.StartNew(() =>
            {
                // Updating cache stickers on app startup
                App.UILoop.AddAction(new UITask
                {
                    Action = () => App.cache.Update(App.vk.GetStickers().items)
                });
                App.cache.Update(App.vk.GetStickersKeywords().dictionary);
            });
        }

        /*private void LoadMenu()
        {
            var settings = new Button
            {
                Background = Coloring.Transparent.Full,
                Content = new Image
                {
                    VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                    Source = new SvgImageSource(new System.Uri(Utils.AssetTheme("settings.svg"))),
                    Width = 20,
                    Height = 20
                },
                Margin = new Windows.UI.Xaml.Thickness(5),
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right,
                VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center
            };
            settings.Click += (a, b) => new Settings();
            this.bottomChatsMenu.Children.Add(settings);
            this.BlurView.BottomMenu = this.bottomChatsMenu;
        }*/
    }
}
