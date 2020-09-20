using Alika.UI;
using Alika.UI.Dialog;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Alika
{
    public sealed partial class MainPage : Page
    {
        public ChatsList chats_list = new ChatsList();
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

            this.chats_scroll.Content = this.chats_list;
            this.chats_scroll.ViewChanged += this.OnChatsScroll;

            App.lp.OnNewMessage += (msg) =>
            {
                if (msg.peer_id == this.peer_id)
                {
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.chats_scroll.ChangeView(null, 0, null)
                    });
                }
            };

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

        /// <summary>
        /// Loading new chats when user scrolled to bottom
        /// </summary>
        public void OnChatsScroll(object sender, ScrollViewerViewChangedEventArgs e) // TODO: Fix scrolling
        {
            if (e.IsIntermediate)
            {
                App.UILoop.AddAction(new UITask
                {
                    Action = () =>
                    {
                        if (this.chats_scroll.VerticalOffset == this.chats_scroll.ScrollableHeight)
                        {
                            double height = this.chats_scroll.ScrollableHeight;
                            this.chats_scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                            this.chats_scroll.VerticalScrollMode = ScrollMode.Disabled;
                            this.chats_list.LoadChats(offset: 1, count: 25, start_msg_id: this.chats_list.Items.Cast<ChatsList.ChatItem>().Select(item => item as ChatsList.ChatItem).ToList().Last().message.id);
                            this.chats_scroll.ChangeView(null, height, null);
                            this.chats_scroll.VerticalScrollMode = ScrollMode.Enabled;
                            this.chats_scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                        }
                    }
                });
            }
        }
    }
}
