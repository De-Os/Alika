using Alika.Libs;
using Alika.UI;
using Alika.UI.Dialog;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

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
                if (value == this._peer_id) return;

                this._peer_id = value;

                if(this.dialog.Children.Count > 0)
                {
                    if(this.dialog.Children.Any(i => i is Dialog))
                    {
                        var old = this.dialog.Children.First(i => i is Dialog) as Dialog;
                        if (old.peer_id != value)
                        {
                            this.dialog.PreviewKeyDown -= old.PreviewKeyEvent;
                            (old.stickers.Flyout as Flyout).Content = null; // Remove previous flyout to prevent crash on stickers opening
                            App.cache.StickersSelector.StickerSent -= old.HideFlyout;
                            this.dialog.Children.Clear();
                        } else return;
                    }
                    this.dialog.Children.Clear();
                }

                if (value == 0)
                {
                    this.dialog.Children.Add(this.NoChatSelected);
                    if(this.chats_grid.Content is ChatsHolder holder)
                    {
                        holder.PinnedChats.SelectedItem = null;
                        holder.Chats.SelectedItem = null;
                    }
                }
                else
                {
                    var list = new Dialog(value);
                    this.dialog.PreviewKeyDown += list.PreviewKeyEvent;
                    this.dialog.Children.Add(list);
                    this.dialog.Children.Add(list.stickers_suggestions);
                }
            }
        }

        private StackPanel NoChatSelected = new StackPanel
        {
            HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
        };

        public MainPage()
        {
            this.InitializeComponent();

            this.chats_grid.Content = new ChatsHolder();

            this.NoChatSelected.Children.Add(new Image
            {
                Source = new SvgImageSource(new System.Uri(Utils.AssetTheme("chat.svg"))),
                Width = 100,
                Height = 100,
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center
            });
            this.NoChatSelected.Children.Add(new TextBlock { 
                Text = Utils.LocString("Dialog/SelectChat"),
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
                Margin = new Windows.UI.Xaml.Thickness(0, 10, 0, 0),
                FontSize = 16,
                TextAlignment = Windows.UI.Xaml.TextAlignment.Center
            });
            this.NoChatSelected.Transitions.Add(new PopupThemeTransition());
            this.dialog.Children.Add(this.NoChatSelected);
            this.dialog.PreviewKeyDown += (a, b) => {
                if (b.Key == Windows.System.VirtualKey.Escape) this.peer_id = 0;
            };

            // Updating stickers cache on app startup
            Task.Factory.StartNew(() =>
            {
                var stickers = App.vk.GetStickers();
                if (stickers?.items == null || stickers.items.Count == 0) return;
                App.UILoop.AddAction(new UITask
                {
                    Action = () => App.cache.Update(stickers.items),
                    Priority = Windows.UI.Core.CoreDispatcherPriority.Low
                });
            });
            Task.Factory.StartNew(() => App.cache.Update(App.vk.GetStickersKeywords().dictionary));

        }
    }
}
