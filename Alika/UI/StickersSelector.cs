using Alika.Libs;
using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{
    /// <summary>
    /// Main selector
    /// </summary>
    [Windows.UI.Xaml.Data.Bindable]
    public class StickersSelector : Grid
    {
        public int peer_id { get; set; }

        public delegate void Event();
        public Event StickerSent;

        public TextBox Search = new TextBox
        {
            PlaceholderText = Utils.LocString("Search")
        };
        public SemanticZoom Semantic = new SemanticZoom
        {
            Height = 500,
            Width = 420
        };

        public StickersSelector(List<GetStickersResponse.StickerPackInfo> stickers)
        {
            this.RowDefinitions.Add(new RowDefinition());
            this.RowDefinitions.Add(new RowDefinition());

            Grid.SetRow(this.Search, 0);
            this.Children.Add(this.Search);

            ListView names = new ListView();
            foreach (var pack in stickers.FindAll(s => s.product.purchased == 1 && s.product.base_id == 0))
            {
                if (pack.product.style_ids != null)
                {
                    if (pack.product.active == 1
                        || stickers.Any(i =>
                            i.product.base_id == pack.product.id
                            && i.product.purchased == 1
                            && i.product.active == 1))
                    {
                        names.Items.Add(new StickerName(pack)
                        {
                            Styles = stickers.FindAll(i =>
                                i.product.base_id == pack.product.id
                                && i.product.purchased == 1
                                && i.product.active == 1)
                        });
                    }
                }
                else if (pack.product.active == 1) names.Items.Add(new StickerName(pack));
            }
            this.Semantic.ZoomedOutView = names;
            this.Semantic.IsZoomedInViewActive = false;
            this.Semantic.ViewChangeStarted += this.PackChoosed;
            Grid.SetRow(this.Semantic, 1);
            this.Children.Add(this.Semantic);

            this.Search.TextChanged += this.SearchChanged;
        }

        private void SearchChanged(object sender, TextChangedEventArgs e)
        {
            ListView list = this.Semantic.ZoomedOutView as ListView;
            for (int x = 0; x < list.Items.Count; x++)
            {
                StickerName item = list.Items[x] as StickerName;
                if (Regex.IsMatch(item.Pack.product.title, this.Search.Text, RegexOptions.IgnoreCase)) item.Visibility = Visibility.Visible; else item.Visibility = Visibility.Collapsed;
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
                    Content = new Image
                    {
                        Source = new SvgImageSource(new Uri(Utils.AssetTheme("back.svg"))),
                        Width = 20,
                        Height = 20
                    },
                    Width = 40,
                    Background = Coloring.Transparent.Full
                };
                back.Click += (a, c) => (sender as SemanticZoom).IsZoomedInViewActive = false;
                TextBlock title = new TextBlock
                {
                    Text = (e.SourceItem.Item as StickerName).Pack.product.title,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0),
                    FontWeight = FontWeights.Bold,
                    FontSize = 20,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(back, 0);
                Grid.SetColumn(title, 1);
                top.Children.Add(back);
                top.Children.Add(title);
                pack.Items.Add(top);

                var item = e.SourceItem.Item as StickerName;
                var set = new StickerSet(item.Pack, item.Styles);
                set.StickerSent += () => this.StickerSent?.Invoke();
                pack.Items.Add(set);
                (sender as SemanticZoom).ZoomedInView = pack;
                App.cache.StickersSelector.Search.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Reset search
                App.cache.StickersSelector.Search.Visibility = Visibility.Visible;
                App.cache.StickersSelector.Search.Text = "";
                ListView list = App.cache.StickersSelector.Semantic.ZoomedOutView as ListView;
                for (int x = 0; x < list.Items.Count; x++)
                {
                    (list.Items[x] as StickerName).Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Pack name with thumbnail
        /// </summary>
        [Windows.UI.Xaml.Data.Bindable]
        public class StickerName : ListViewItem
        {
            public GetStickersResponse.StickerPackInfo Pack { get; set; }
            public List<GetStickersResponse.StickerPackInfo> Styles { get; set; }

            public Image Image = new Image
            {
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Width = 38,
                Height = 38
            };
            public TextBlock Title = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            public StickerName(GetStickersResponse.StickerPackInfo pack)
            {
                this.Pack = pack;
                this.Render();
            }

            private async void Render()
            {
                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                this.Title.Text = this.Pack.product.title;

                Grid.SetColumn(this.Image, 0);
                Grid.SetColumn(this.Title, 1);
                grid.Children.Add(this.Image);
                grid.Children.Add(this.Title);
                this.Content = grid;

                this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Pack.product.previews.Find(i => i.width == this.Pack.product.previews.Max(g => g.width)).url));

            }
        }

        /// <summary>
        /// Pack stickers 
        /// </summary>
        [Windows.UI.Xaml.Data.Bindable]
        public class StickerSet : StackPanel
        {
            public GetStickersResponse.StickerPackInfo PackInfo;
            public List<GetStickersResponse.StickerPackInfo> Styles;

            public delegate void Event();
            public Event StickerSent;

            public StickerSet(GetStickersResponse.StickerPackInfo pack, List<GetStickersResponse.StickerPackInfo> styles = null)
            {
                this.PackInfo = pack;

                var packs = new List<GetStickersResponse.StickerPackInfo>();
                if (pack.product.active == 1 && styles?.Count == 0)
                {
                    packs.Add(pack);
                }
                else
                {
                    if (styles?.Count > 0 && styles.Any(i => i.product.purchased == 1))
                    {
                        packs.AddRange(styles.FindAll(i => i.product.purchased == 1).OrderByDescending(i => i.product.active));
                    }
                    packs.Add(pack);
                }

                foreach (var p in packs)
                {
                    if (packs.Count > 1) this.Children.Add(new TextBlock
                    {
                        Text = p.product.title,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(5)
                    });
                    var temp = new StackPanel { Orientation = Orientation.Horizontal };
                    foreach (var sticker in p.product.stickers)
                    {
                        if (temp.Children.Count == 4)
                        {
                            this.Children.Add(temp);
                            temp = new StackPanel { Orientation = Orientation.Horizontal };
                        }
                        StickerHolder img = new StickerHolder(sticker);
                        img.StickerSent += () => this.StickerSent?.Invoke();
                        temp.Children.Add(img);
                    }
                    if (temp.Children.Count > 0) this.Children.Add(temp);
                }
            }

            [Windows.UI.Xaml.Data.Bindable]
            public class StickerHolder : Grid
            {
                public delegate void Event();
                public Event StickerSent;

                public Attachment.Sticker Sticker;
                public Image Image = new Image
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Height = 90,
                    Width = 90,
                    Margin = new Thickness(5)
                };
                public StickerHolder(Attachment.Sticker sticker)
                {
                    this.Sticker = sticker;
                    this.CornerRadius = new CornerRadius(10);
                    this.PointerPressed += (a, b) =>
                    {
                        Task.Factory.StartNew(() => App.vk.Messages.Send(App.cache.StickersSelector.peer_id, sticker_id: this.Sticker.sticker_id));
                        this.StickerSent?.Invoke();
                    };
                    this.PointerEntered += (a, b) => this.Background = Coloring.Transparent.Percent(50); // Sticker "selection" by background color
                    this.PointerExited += (a, b) => this.Background = Coloring.Transparent.Full; // Remove selection
                    this.Children.Add(this.Image);
                    this.LoadImage();
                }

                public async void LoadImage()
                {
                    this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri((App.systemDarkTheme ? this.Sticker.images_with_background : this.Sticker.images).Find(s => s.width == 128).url));
                }
            }
        }
    }

    /// <summary>
    /// Suggestions holder
    /// </summary>
    public class StickerSuggestionHolder : Grid // TODO: Non-static images for animated stickers
    {
        public Attachment.Sticker Sticker { get; set; }
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
                this.Background = value ? this.Background = Coloring.Transparent.Percent(75) : Coloring.Transparent.Full;
            }
        }
        public StickerSuggestionHolder(Attachment.Sticker sticker)
        {
            this.Sticker = sticker;

            this.Margin = new Thickness(5);
            this.CornerRadius = new CornerRadius(10);

            this.PointerEntered += (a, b) => this.Background = Coloring.Transparent.Percent(75); // Don't using this.Selected because it can bring issues with arrow keys choosing
            this.PointerExited += (a, b) => { if (!this._selected) this.Background = Coloring.Transparent.Full; }; // Same^

            this.Children.Add(this.Image);

            this.LoadImage();
        }
        public async void LoadImage()
        {
            this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri((App.systemDarkTheme ? this.Sticker.images_with_background : this.Sticker.images).Find(s => s.width == 128).url));
        }
    }
}
