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
                    this.dialog.PreviewKeyDown -= old.PreviewKeyEvent;
                    (old.stickers.Flyout as Flyout).Content = null; // Remove previous flyout to prevent crash on stickers opening
                    this.dialog.Children.Clear();
                }
                var list = new Dialog(value);
                this.dialog.PreviewKeyDown += list.PreviewKeyEvent;
                this.dialog.Children.Add(list);
                this.dialog.Children.Add(list.stickers_suggestions);
            }
        }

        public MainPage()
        {
            this.InitializeComponent();

            this.chats_grid.Content = new ChatsHolder();

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
