using Alika.Libs;
using Alika.UI;
using Alika.UI.Dialog;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static Alika.Theme;

namespace Alika
{
    public sealed partial class MainPage : Page
    {
        private int _peer_id;

        public int PeerId
        {
            get
            {
                return this._peer_id;
            }
            set
            {
                if (value == this._peer_id) return;

                this._peer_id = value;

                if (this.Dialog.Children.Count > 0)
                {
                    if (this.Dialog.Children.Any(i => i is Dialog))
                    {
                        var old = this.Dialog.Children.First(i => i is Dialog) as Dialog;
                        if (old.PeerId != value)
                        {
                            this.Dialog.PreviewKeyDown -= old.PreviewKeyEvent;
                            if (old.Stickers.Flyout is Flyout oldFlyout)
                            {
                                oldFlyout.Content = null; // Remove previous flyout to prevent crash on stickers opening
                                App.Cache.StickersSelector.StickerSent -= old.HideFlyout;
                            }
                            this.Dialog.Children.Clear();
                        }
                        else return;
                    }
                    this.Dialog.Children.Clear();
                }

                if (value == 0)
                {
                    this.Dialog.Children.Add(this.NoChatSelected);
                    if (this.Chats.Content is ChatsHolder holder)
                    {
                        holder.PinnedChats.SelectedItem = null;
                        holder.Chats.SelectedItem = null;
                    }
                }
                else
                {
                    var list = new Dialog(value);
                    this.Dialog.PreviewKeyDown += list.PreviewKeyEvent;
                    this.Dialog.Children.Add(list);
                    this.Dialog.Children.Add(list.StickerSuggestions);
                }
            }
        }

        public delegate void OnLogout();

        public OnLogout Logout;

        private StackPanel NoChatSelected = new StackPanel
        {
            HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
            VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
        };

        public MainPage()
        {
            this.InitializeComponent();

            this.Chats.Content = new ChatsHolder();

            this.Background = new SolidColorBrush(App.Theme.Colors.Main);
            App.Theme.ThemeChanged += () => this.Background = new SolidColorBrush(App.Theme.Colors.Main);

            this.NoChatSelected.Children.Add(new ThemedFontIcon
            {
                FontFamily = App.Icons,
                Glyph = Glyphs.Custom.Chat,
                FontSize = 100,
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center
            });
            var text = ThemeHelpers.GetThemedText();
            text.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
            text.Margin = new Windows.UI.Xaml.Thickness(0, 10, 0, 0);
            text.FontSize = 16;
            text.Text = Utils.LocString("Dialog/SelectChat");
            text.TextAlignment = Windows.UI.Xaml.TextAlignment.Center;
            this.NoChatSelected.Children.Add(text);
            this.NoChatSelected.Transitions.Add(new PopupThemeTransition());
            this.Dialog.Children.Add(this.NoChatSelected);
            this.Dialog.PreviewKeyDown += (a, b) =>
            {
                if (b.Key == Windows.System.VirtualKey.Escape) this.PeerId = 0;
            };

            // Updating stickers cache on app startup
            Task.Factory.StartNew(() =>
            {
                var recent = App.VK.Messages.GetRecentStickers().Items;
                var stickers = App.VK.GetStickers();
                if (stickers?.Items == null || stickers.Items.Count == 0) return;
                App.UILoop.AddAction(new UITask
                {
                    Action = () => App.Cache.Update(stickers.Items, recent),
                    Priority = Windows.UI.Core.CoreDispatcherPriority.Low
                });
            });
            Task.Factory.StartNew(() => App.Cache.Update(App.VK.GetStickersKeywords().Dictionary));
        }

        public void InvokeLogout() => this.Logout?.Invoke();
    }
}