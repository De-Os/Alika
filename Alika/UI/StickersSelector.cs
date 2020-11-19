using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using static Alika.Theme;

namespace Alika.UI
{
    /// <summary>
    /// Main stickers selector
    /// </summary>
    [Bindable]
    public class StickersSelector : Grid
    {
        public int peer_id { get; set; }

        public delegate void Event(Attachment.StickerAtt sticker);

        public Event StickerSent;

        public ThemedTextBox Search = new ThemedTextBox
        {
            PlaceholderText = Utils.LocString("Search")
        };

        public SemanticZoom Semantic = new SemanticZoom
        {
            Height = 500,
            Width = 420
        };

        public RecentStickerName Recent;

        public StickersSelector(List<StickerPackInfo> stickers, List<Attachment.StickerAtt> recents)
        {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(this.Search, 0);
            this.Children.Add(this.Search);

            ListView names = new ListView();

            this.Recent = new RecentStickerName(recents);
            this.StickerSent += this.UpdateRecents;

            names.Items.Add(this.Recent);
            foreach (var pack in stickers.FindAll(s => s.Product.Purchased == 1 && s.Product.BaseId == 0))
            {
                if (pack.Product.StyleIds != null)
                {
                    if (pack.Product.Active == 1
                        || stickers.Any(i =>
                            i.Product.BaseId == pack.Product.Id
                            && i.Product.Purchased == 1
                            && i.Product.Active == 1))
                    {
                        names.Items.Add(new StickerName(pack)
                        {
                            Styles = stickers.FindAll(i =>
                                i.Product.BaseId == pack.Product.Id
                                && i.Product.Purchased == 1
                                && i.Product.Active == 1)
                        });
                    }
                }
                else if (pack.Product.Active == 1) names.Items.Add(new StickerName(pack));
            }
            this.Semantic.ZoomedOutView = names;
            this.Semantic.IsZoomedInViewActive = false;
            this.Semantic.ViewChangeStarted += this.PackChoosed;
            Grid.SetRow(this.Semantic, 1);
            this.Children.Add(this.Semantic);

            this.Search.TextChanged += this.SearchChanged;
        }

        private void UpdateRecents(Attachment.StickerAtt sticker)
        {
            if (this.Recent.Stickers.Any(i => i.StickerId == sticker.StickerId)) this.Recent.Stickers.RemoveAll(i => i.StickerId == sticker.StickerId);
            this.Recent.Stickers.Insert(0, sticker);
            if (this.Recent.Stickers.Count > Limits.Messages.MAX_RECENT_STICKERS_COUNT) this.Recent.Stickers.RemoveRange(Limits.Messages.MAX_RECENT_STICKERS_COUNT, this.Recent.Stickers.Count - Limits.Messages.MAX_RECENT_STICKERS_COUNT);
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            foreach (var item in (this.Semantic.ZoomedOutView as ListView).Items)
            {
                if (item is StickerName pack)
                {
                    pack.Visibility = Regex.IsMatch(pack.Pack.Product.Title, this.Search.Text, RegexOptions.IgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
                }
                else if (item is RecentStickerName recent) recent.Visibility = (sender as TextBox).Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void PackChoosed(object sender, SemanticZoomViewChangedEventArgs e)
        {
            if (!e.IsSourceZoomedInView)
            {
                ListView pack = new ListView
                {
                    SelectionMode = ListViewSelectionMode.None
                };
                Grid top = new Grid();
                top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                Button back = new Button
                {
                    Content = new ThemedFontIcon
                    {
                        Glyph = Glyphs.Close
                    },
                    Background = App.Theme.Colors.Transparent
                };
                back.Click += (a, c) => (sender as SemanticZoom).IsZoomedInViewActive = false;

                var title = ThemeHelpers.GetThemedText();
                title.VerticalAlignment = VerticalAlignment.Center;
                title.Margin = new Thickness(10, 0, 0, 0);
                title.FontWeight = FontWeights.Bold;
                title.FontSize = 20;
                title.TextTrimming = TextTrimming.CharacterEllipsis;

                Grid.SetColumn(back, 0);
                Grid.SetColumn(title, 1);
                top.Children.Add(back);
                top.Children.Add(title);
                pack.Items.Add(top);

                StickerSet set;
                if (e.SourceItem.Item is StickerName stickerPack)
                {
                    title.Text = stickerPack.Pack.Product.Title;
                    set = new StickerSet(stickerPack.Pack, stickerPack.Styles);
                    set.StickerSent += (sticker) => this.StickerSent?.Invoke(sticker);
                    pack.Items.Add(set);
                }
                else if (e.SourceItem.Item is RecentStickerName recents)
                {
                    title.Text = Utils.LocString("Dialog/RecentStickers");
                    set = new StickerSet(recents.Stickers);
                    set.StickerSent += (sticker) => this.StickerSent?.Invoke(sticker);
                    pack.Items.Add(set);
                }
                (sender as SemanticZoom).ZoomedInView = pack;
                this.Search.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Reset search
                this.Search.Visibility = Visibility.Visible;
                this.Search.Text = "";
                foreach (FrameworkElement element in (this.Semantic.ZoomedOutView as ListView).Items) element.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Pack name with thumbnail
        /// </summary>
        [Bindable]
        public class StickerName : ListViewItem
        {
            public StickerPackInfo Pack { get; set; }
            public List<StickerPackInfo> Styles { get; set; }

            public Image Image = new Image
            {
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 38,
                Height = 38
            };

            public TextBlock Title;

            public StickerName(StickerPackInfo pack)
            {
                this.Pack = pack;
                this.Render();
            }

            private async void Render()
            {
                this.Title = ThemeHelpers.GetThemedText();
                this.Title.VerticalAlignment = VerticalAlignment.Center;
                this.Title.FontWeight = FontWeights.Bold;
                this.Title.TextTrimming = TextTrimming.CharacterEllipsis;

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                this.Title.Text = this.Pack.Product.Title;

                Grid.SetColumn(this.Image, 0);
                Grid.SetColumn(this.Title, 1);
                grid.Children.Add(this.Image);
                grid.Children.Add(this.Title);
                this.Content = grid;

                this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Pack.Product.Previes.Find(i => i.Width == this.Pack.Product.Previes.Max(g => g.Width)).Url));
            }
        }

        [Bindable]
        public class RecentStickerName : ListViewItem
        {
            public List<Attachment.StickerAtt> Stickers;

            public RecentStickerName(List<Attachment.StickerAtt> stickers)
            {
                this.Stickers = stickers;

                var content = new StackPanel { Orientation = Orientation.Horizontal };
                content.Children.Add(new ThemedFontIcon
                {
                    Glyph = Glyphs.History,
                    Margin = new Thickness(0, 0, 5, 0),
                    FontSize = 25,
                    Width = 38,
                    Height = 38,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(App.Theme.Colors.Text.Default)
                });
                var text = ThemeHelpers.GetThemedText();
                text.Text = Utils.LocString("Dialog/RecentStickers");
                text.VerticalAlignment = VerticalAlignment.Center;
                text.FontWeight = FontWeights.Bold;
                text.TextTrimming = TextTrimming.CharacterEllipsis;
                content.Children.Add(text);
                this.Content = content;
            }
        }

        /// <summary>
        /// Pack stickers
        /// </summary>
        [Bindable]
        public class StickerSet : StackPanel
        {
            public StickerPackInfo PackInfo;
            public List<StickerPackInfo> Styles;

            public delegate void Event(Attachment.StickerAtt sticker);

            public Event StickerSent;

            public StickerSet(StickerPackInfo pack, List<StickerPackInfo> styles = null)
            {
                this.PackInfo = pack;

                var packs = new List<StickerPackInfo>();
                if (pack.Product.Active == 1 && styles?.Count == 0)
                {
                    packs.Add(pack);
                }
                else
                {
                    if (styles?.Count > 0 && styles.Any(i => i.Product.Purchased == 1))
                    {
                        packs.AddRange(styles.FindAll(i => i.Product.Purchased == 1).OrderByDescending(i => i.Product.Active));
                    }
                    packs.Add(pack);
                }

                foreach (var p in packs)
                {
                    if (packs.Count > 1)
                    {
                        var title = ThemeHelpers.GetThemedText();
                        title.Text = p.Product.Title;
                        title.FontWeight = FontWeights.Bold;
                        title.Margin = new Thickness(5);
                        this.Children.Add(title);
                    }
                    var temp = new StackPanel { Orientation = Orientation.Horizontal };
                    foreach (var sticker in p.Product.Stickers)
                    {
                        if (temp.Children.Count == 4)
                        {
                            this.Children.Add(temp);
                            temp = new StackPanel { Orientation = Orientation.Horizontal };
                        }
                        StickerHolder img = new StickerHolder(sticker);
                        img.StickerSent += (s) => this.StickerSent?.Invoke(s);
                        temp.Children.Add(img);
                    }
                    if (temp.Children.Count > 0) this.Children.Add(temp);
                }
            }

            public StickerSet(List<Attachment.StickerAtt> recent)
            {
                var temp = new StackPanel { Orientation = Orientation.Horizontal };
                foreach (var sticker in recent)
                {
                    if (temp.Children.Count == 4)
                    {
                        this.Children.Add(temp);
                        temp = new StackPanel { Orientation = Orientation.Horizontal };
                    }
                    StickerHolder img = new StickerHolder(sticker);
                    img.StickerSent += (s) => this.StickerSent?.Invoke(s);
                    temp.Children.Add(img);
                }
                if (temp.Children.Count > 0) this.Children.Add(temp);
            }

            [Bindable]
            public class StickerHolder : Grid
            {
                public delegate void Event(Attachment.StickerAtt sticker);

                public Event StickerSent;

                public Attachment.StickerAtt Sticker;

                public Image Image = new Image
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Height = 90,
                    Width = 90,
                    Margin = new Thickness(5)
                };

                public StickerHolder(Attachment.StickerAtt sticker)
                {
                    this.Sticker = sticker;
                    this.CornerRadius = new CornerRadius(10);
                    this.PointerPressed += (a, b) =>
                    {
                        Task.Factory.StartNew(() => App.VK.Messages.Send(App.MainPage.PeerId, sticker_id: this.Sticker.StickerId));
                        if (!Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) this.StickerSent?.Invoke(sticker);
                    };
                    this.PointerEntered += (a, b) => this.Background = new SolidColorBrush(App.Theme.Colors.Main); // Sticker "selection" by background color
                    this.PointerExited += (a, b) => this.Background = App.Theme.Colors.Transparent; // Remove selection
                    this.Children.Add(this.Image);
                    this.LoadImage();
                    App.Theme.ThemeChanged += this.LoadImage;
                }

                public async void LoadImage()
                {
                    this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri((App.Theme.IsDark ? this.Sticker.ImagesWithBackground : this.Sticker.Images).Find(s => s.Width == 128).Url));
                }
            }
        }
    }

    /// <summary>
    /// Suggestions holder
    /// </summary>
    [Bindable]
    public class StickerSuggestionHolder : Grid // TODO: Non-static images for animated stickers
    {
        public Attachment.StickerAtt Sticker { get; set; }

        public Image Image { get; set; } = new Image
        {
            VerticalAlignment = VerticalAlignment.Center,
            Width = 80,
            Height = 80,
            Margin = new Thickness(5)
        };

        private bool _selected = false;

        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                this._selected = value;
                this.Background = value ? this.Background = new SolidColorBrush(App.Theme.Colors.Main) : App.Theme.Colors.Transparent;
            }
        }

        public StickerSuggestionHolder(Attachment.StickerAtt sticker)
        {
            this.Sticker = sticker;

            this.Margin = new Thickness(5);
            this.CornerRadius = new CornerRadius(10);

            this.PointerEntered += (a, b) => this.Background = new SolidColorBrush(App.Theme.Colors.Main); // Don't using this.Selected because it can bring issues with arrow keys choosing
            this.PointerExited += (a, b) => { if (!this._selected) this.Background = App.Theme.Colors.Transparent; }; // Same^

            this.Children.Add(this.Image);

            this.LoadImage();
            App.Theme.ThemeChanged += () =>
            {
                this.LoadImage();
                this.Selected = this.Selected;
            };
        }

        public async void LoadImage()
        {
            this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri((App.Theme.IsDark ? this.Sticker.ImagesWithBackground : this.Sticker.Images).Find(s => s.Width == 128).Url));
        }
    }
}