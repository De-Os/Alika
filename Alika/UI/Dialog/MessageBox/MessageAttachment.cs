using Alika.Libs;
using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Lottie;
using Microsoft.UI.Xaml.Controls;
using RestSharp;
using System;
using System.Collections.Generic;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using static Alika.Theme;

namespace Alika.UI
{
    public class MessageAttachment
    {
        /// <summary>
        /// Photo holder
        /// </summary>
        [Bindable]
        public class Photo : Grid
        {
            public Attachment.PhotoAtt Picture;
            public Image Preview = new Image();

            public Photo(Attachment.PhotoAtt photo)
            {
                this.Picture = photo;

                this.Children.Add(this.Preview);

                this.PointerPressed += (a, b) => new MediaViewer(new Attachment
                {
                    Type = "photo",
                    Photo = this.Picture
                });

                this.LoadPreview();
            }

            public async void LoadPreview()
            {
                this.Preview.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().Url));
            }
        }

        /// <summary>
        /// Document holder
        /// </summary>
        [Bindable]
        public class Document : Button
        {
            public Attachment.DocumentAtt Doc;

            public Document(Attachment.DocumentAtt doc)
            {
                this.Doc = doc;

                this.Margin = new Thickness(5);
                this.Background = new SolidColorBrush(App.Theme.Colors.Main);
                this.CornerRadius = new CornerRadius(10);

                Grid content = new Grid
                {
                    Margin = new Thickness(0, 5, 5, 5),
                };
                content.ColumnDefinitions.Add(new ColumnDefinition());
                content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var img = new ThemedFontIcon
                {
                    FontSize = 20,
                    Glyph = Glyphs.Open,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Grid.SetColumn(img, 0);
                content.Children.Add(img);

                Grid text = new Grid();
                text.RowDefinitions.Add(new RowDefinition());
                text.RowDefinitions.Add(new RowDefinition());

                var name = ThemeHelpers.GetThemedText();
                name.Text = this.Doc.Title;
                name.VerticalAlignment = VerticalAlignment.Center;
                name.FontWeight = FontWeights.Bold;
                name.TextTrimming = TextTrimming.CharacterEllipsis;
                Grid.SetRow(name, 0);
                text.Children.Add(name);

                var desc = ThemeHelpers.GetThemedText();
                desc.Text = Utils.FormatSize(this.Doc.Size, 2) + " • " + this.Doc.Extension;
                desc.VerticalAlignment = VerticalAlignment.Center;
                desc.TextTrimming = TextTrimming.CharacterEllipsis;
                Grid.SetRow(desc, 1);
                text.Children.Add(desc);

                Grid.SetColumn(text, 1);
                content.Children.Add(text);

                this.Content = content;

                this.Click += this.download;
            }

            private async void download(object sender, RoutedEventArgs e)
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                savePicker.FileTypeChoices.Add(this.Doc.Extension.ToUpper(), new List<string>() { "." + this.Doc.Extension });
                savePicker.SuggestedFileName = this.Doc.Title;
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    await FileIO.WriteBytesAsync(file, new RestClient(new Uri(this.Doc.Url)).DownloadData(new RestRequest()));
                    await CachedFileManager.CompleteUpdatesAsync(file);
                }
            }
        }

        /// <summary>
        /// Sticker holder
        /// </summary>
        [Bindable]
        public class Sticker : Grid
        {
            public Attachment.StickerAtt StickerSource;

            public UIElement Image;

            public Sticker(Attachment.StickerAtt sticker)
            {
                this.StickerSource = sticker;

                this.Height = 160;
                this.Width = 160;
                this.Margin = new Thickness(10);

                if (this.StickerSource.AnimationUrl != null) this.Image = new AnimatedVisualPlayer(); else this.Image = new Image();
                this.Children.Add(this.Image);

                this.LoadSticker();
                App.Theme.ThemeChanged += this.LoadSticker;
            }

            public async void LoadSticker()
            {
                if (this.Image is AnimatedVisualPlayer anim)
                {
                    anim.Source = new LottieVisualSource
                    {
                        UriSource = new Uri(App.Theme.IsDark ? this.StickerSource.AnimationUrl.Replace(".json", "b.json") : this.StickerSource.AnimationUrl)
                    };
                }
                else if (this.Image is Image img)
                {
                    img.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.StickerSource.GetBestQuality(App.Theme.IsDark)));
                }
            }
        }

        /// <summary>
        /// Voice message holder
        /// </summary>
        [Bindable]
        public class AudioMessage : StackPanel
        {
            public MediaPlayer media = new MediaPlayer
            {
                AutoPlay = false,
                Volume = 100
            };

            public Attachment.AudioMessageAtt Audio;

            public ThemedFontIcon image = new ThemedFontIcon
            {
                FontSize = 20,
                Glyph = Glyphs.Play
            };

            public StackPanel wave = new StackPanel
            {
                Margin = new Thickness(5, 0, 5, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Orientation = Orientation.Horizontal
            };

            public TextBlock time;

            private bool _onWaves = false;
            private bool _playing = false;

            public bool Playing
            {
                get
                {
                    return this._playing;
                }
                set
                {
                    if (value)
                    {
                        this.media.Play();
                        this.image.Glyph = Glyphs.Pause;
                    }
                    else
                    {
                        this.media.Pause();
                        this.image.Glyph = Glyphs.Play;
                    }
                    this._playing = value;
                }
            }

            public AudioMessage(Attachment.AudioMessageAtt audio)
            {
                while (audio.Waveform.Count < 128) audio.Waveform.Add(0);
                this.Audio = audio;
                this.media.Source = MediaSource.CreateFromUri(new Uri(this.Audio.LinkMP3));

                this.time = ThemeHelpers.GetThemedText();
                this.time.VerticalAlignment = VerticalAlignment.Center;
                this.time.HorizontalAlignment = HorizontalAlignment.Right;

                var TopPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };

                TopPanel.Children.Add(this.image);
                TopPanel.Children.Add(this.wave);
                TopPanel.Children.Add(this.time);

                this.Children.Add(TopPanel);

                if (this.Audio.TranscriptState != null && this.Audio.TranscriptState == "done")
                {
                    var trans_btn = new Button
                    {
                        Content = new ThemedFontIcon
                        {
                            Glyph = Glyphs.More,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        CornerRadius = new CornerRadius(10),
                        Background = App.Theme.Colors.Transparent,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    var trans_text = ThemeHelpers.GetThemedText();
                    trans_text.Text = this.Audio.Transcript;
                    trans_text.Visibility = Visibility.Collapsed;
                    trans_text.TextWrapping = TextWrapping.Wrap;
                    trans_btn.Click += (a, b) => trans_text.Visibility = trans_text.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

                    TopPanel.Children.Add(trans_btn);
                    this.Children.Add(trans_text);
                }

                this.Margin = new Thickness(5);
                this.Padding = new Thickness(5);
                this.CornerRadius = new CornerRadius(10);

                this.time.Text = TimeSpan.FromSeconds(this.Audio.Duration).ToString(@"m\:ss");
                this.GenerateWaveforms();

                this.wave.PointerEntered += (a, b) => this._onWaves = true;
                this.wave.PointerExited += (a, b) => this._onWaves = false;
                this.PointerPressed += this.OnClick;
                this.media.MediaEnded += (a, b) => App.UILoop.AddAction(new UITask
                {
                    Action = () => this.Playing = false
                });
                this.media.PlaybackSession.PositionChanged += this.PlayStateChanged;
                this.PointerEntered += (a, b) => this.Background = new SolidColorBrush(App.Theme.Colors.Main);
                this.PointerExited += (a, b) => this.Background = App.Theme.Colors.Transparent;

                this.Loaded += (a, b) => this.MaxWidth = this.ActualWidth;
            }

            private void OnClick(object sender, RoutedEventArgs e)
            {
                if (this._onWaves && this.Playing) return;
                App.UILoop.AddAction(new UITask
                {
                    Action = () => this.Playing = !this.Playing
                });
            }

            private void PlayStateChanged(MediaPlaybackSession sender, object args)
            {
                App.UILoop.AddAction(new UITask
                {
                    Action = () => this.time.Text = sender.Position.ToString(@"m\:ss"),
                    Priority = CoreDispatcherPriority.Low
                });
            }

            public void GenerateWaveforms()
            {
                var partTime = TimeSpan.FromSeconds((double)decimal.Divide(this.Audio.Duration, 128));
                for (int x = 0; x < 128; x++)
                {
                    int wave = this.Audio.Waveform[x];
                    var wv = new WaveHolder(wave, partTime * x);
                    wv.Rectangle.PointerPressed += (a, b) => this.media.PlaybackSession.Position = wv.Time; //TODO: Vertically stretch for better controls
                    this.media.PlaybackSession.PositionChanged += (a, b) => wv.ChangeFill(a.Position);
                    this.media.MediaEnded += (a, b) => wv.ResetFill();
                    this.wave.Children.Add(wv.Rectangle);
                }
            }

            public class WaveHolder
            {
                public TimeSpan Time;

                public Windows.UI.Xaml.Shapes.Rectangle Rectangle = new Windows.UI.Xaml.Shapes.Rectangle
                {
                    Width = 2,
                    Margin = new Thickness(0.25, 0, 0.25, 0),
                    MinHeight = 1,
                    RadiusX = 1,
                    RadiusY = 1,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                public SolidColorBrush FillColor = new SolidColorBrush(App.Theme.Colors.Accent);
                public SolidColorBrush NoFillColor = new SolidColorBrush(App.Theme.Colors.SubAccent);

                public WaveHolder(int wave, TimeSpan time)
                {
                    this.Time = time;
                    this.Rectangle.Fill = this.FillColor;
                    this.Rectangle.Height = 10 * (wave / 10);

                    App.Theme.ThemeChanged += () =>
                    {
                        this.FillColor.Color = App.Theme.Colors.Accent;
                        this.NoFillColor.Color = App.Theme.Colors.SubAccent;
                    };
                }

                public void ChangeFill(TimeSpan time)
                {
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.Rectangle.Fill = (time >= this.Time) ? this.NoFillColor : this.FillColor
                    });
                }

                public void ResetFill()
                {
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.Rectangle.Fill = this.FillColor
                    });
                }
            }
        }

        [Bindable]
        public class Graffiti : Grid
        {
            public Image Image = new Image
            {
                Margin = new Thickness(5)
            };

            public Attachment.GraffitiAtt Graf;

            public Graffiti(Attachment.GraffitiAtt att)
            {
                this.Graf = att;
                this.Children.Add(this.Image);

                this.LoadImage();
            }

            public async void LoadImage()
            {
                this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Graf.Url));
            }
        }

        [Bindable]
        public class Gift : ContentControl
        {
            public Gift(Attachment.GiftAtt gift) => this.Load(gift);

            private async void Load(Attachment.GiftAtt gift)
            {
                this.Content = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Child = new Image
                    {
                        Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(gift.Thumb256))
                    },
                    BorderThickness = new Thickness(1),
                    Width = 256,
                    Height = 256,
                    Margin = new Thickness(10, 10, 10, 0)
                };
            }
        }

        [Bindable]
        public class Link : LinkHolder
        {
            public Link(Attachment.LinkAtt lnk)
            {
                if (lnk.Photo != null)
                {
                    this.LoadPhoto(lnk.Photo.GetBestQuality().Url);
                }
                else
                {
                    this.Content = new ThemedFontIcon
                    {
                        Glyph = Glyphs.Link
                    };
                }
                this.Title = lnk.Title;
                this.Subtitle = lnk.Caption;
                this.Link = lnk.Url;
            }

            private async void LoadPhoto(string photo) => this.Content = new Image
            {
                Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(photo))
            };
        }

        [Bindable]
        public class Wall : LinkHolder
        {
            public Wall(Attachment.WallAtt wall)
            {
                this.Content = new ThemedFontIcon
                {
                    Glyph = Glyphs.Post
                };
                this.Title = Utils.LocString("Attachments/Wall");
                this.Subtitle = wall.Text.RemovePushes();
                this.Link = "https://vk.com/wall" + (wall.ToId != 0 ? wall.ToId : wall.OwnerId) + "_" + wall.Id;
            }
        }

        [Bindable]
        public class WallReply : LinkHolder
        {
            public WallReply(Attachment.WallReplyAtt reply)
            {
                this.Content = new FontIcon
                {
                    Glyph = Glyphs.PostReply
                };
                this.Title = Utils.LocString("Attachments/WallReply");
                this.Subtitle = reply.Text.RemovePushes();
                this.Link = "https://vk.com/wall" + reply.OwnerId + "_" + reply.PostId + "?reply=" + reply.Id;
            }
        }

        [Bindable]
        public class MoneyTransfer : LinkHolder
        {
            public MoneyTransfer(Attachment.MoneyTransferAtt money)
            {
                this.Content = new ThemedFontIcon
                {
                    Glyph = Glyphs.Payment
                };
                this.Title = money.Amount.Text;
                this.Subtitle = Utils.LocString("Attachments/MoneyTransfer");
                this.Link = "https://vk.com/settings?act=payments&section=transfer";
            }
        }

        [Bindable]
        public class Story : LinkHolder
        {
            public Story(Attachment.StoryAtt story)
            {
                this.Content = new ThemedFontIcon
                {
                    Glyph = Glyphs.History
                };
                this.Title = Utils.LocString("Attachments/History");
                this.Link = "https://vk.com/" + story.ToAttachFormat(false);
            }
        }

        [Bindable]
        public abstract class LinkHolder : Grid
        {
            protected string Link;

            protected FrameworkElement Content
            {
                get
                {
                    return this._border.Child as FrameworkElement;
                }
                set
                {
                    value.VerticalAlignment = VerticalAlignment.Center;
                    value.VerticalAlignment = VerticalAlignment.Center;
                    if (value is FontIcon f) f.FontSize = 30;
                    this._border.Child = value;
                }
            }

            protected string Title
            {
                get
                {
                    return this._title.Text;
                }
                set
                {
                    this._title.Text = value;
                }
            }

            protected string Subtitle
            {
                get
                {
                    return this._subtitle.Text;
                }
                set
                {
                    this._subtitle.Text = value;
                }
            }

            protected FrameworkElement Button
            {
                get
                {
                    return this._button.Content as FrameworkElement;
                }
                set
                {
                    this._button.Visibility = value == null ? Visibility.Collapsed : Visibility.Visible;
                    this._button.Content = value;
                }
            }

            private Border _border = new Border
            {
                CornerRadius = new CornerRadius(10),
                Width = 50,
                Height = 50,
                Margin = new Thickness(0, 0, 5, 0)
            };

            private Button _button = new Button
            {
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(3),
                Visibility = Visibility.Collapsed,
                Foreground = new SolidColorBrush(App.Theme.Colors.Text.Default)
            };

            private TextBlock _title;

            private TextBlock _subtitle;

            public LinkHolder()
            {
                this.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.Padding = new Thickness(10);

                this._title = ThemeHelpers.GetThemedText();
                this._title.TextTrimming = TextTrimming.CharacterEllipsis;
                this._title.FontWeight = FontWeights.Bold;

                this._subtitle = ThemeHelpers.GetThemedText();
                this._subtitle.TextTrimming = TextTrimming.CharacterEllipsis;
                this._subtitle.FontWeight = FontWeights.SemiBold;

                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetColumn(this._border, 0);
                this.Children.Add(this._border);

                var stack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                stack.Children.Add(this._title);
                stack.Children.Add(this._subtitle);
                stack.Children.Add(this._button);
                Grid.SetColumn(stack, 1);
                this.Children.Add(stack);

                this.PointerPressed += async (a, b) => await Windows.System.Launcher.LaunchUriAsync(new Uri(this.Link));
            }
        }

        /// <summary>
        /// Uploaded file holder
        /// </summary>
        [Bindable]
        public class Uploaded : Grid
        {
            public Button Remove = new Button
            {
                Content = new ThemedFontIcon
                {
                    Glyph = Glyphs.Close
                },
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = App.Theme.Colors.Transparent
            };

            public Grid Preview = new Grid();

            public Attachment.PhotoAtt Picture;
            public Attachment.DocumentAtt Document;

            public string Attach;

            public Uploaded(Attachment.PhotoAtt pic = null, Attachment.DocumentAtt doc = null)
            {
                this.Picture = pic;
                this.Document = doc;

                this.Margin = new Thickness(5, 2, 5, 2);

                this.Width = 200;
                this.Height = 100;
                this.MaxWidth = 200;

                this.Children.Add(Preview);
                this.Children.Add(Remove);

                if (this.Picture != null)
                {
                    this.LoadImage();
                    this.Attach = this.Picture.ToAttachFormat();
                }
                else if (this.Document != null)
                {
                    this.LoadDocument();
                    this.Attach = this.Document.ToAttachFormat();
                }
            }

            public void LoadImage()
            {
                Image img = new Image();
                img.PointerPressed += async (object sender, PointerRoutedEventArgs e) => await new ImageViewer(this.Picture).ShowAsync();
                this.Preview.Children.Add(img);
                this.LoadImageSource();
            }

            public async void LoadImageSource()
            {
                (this.Preview.Children[0] as Image).Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().Url));
            }

            public void LoadDocument()
            {
                Grid grid = new Grid
                {
                    Width = this.Width,
                    Height = this.Height
                };
                var img = new ThemedFontIcon
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 30,
                    Glyph = Glyphs.Document
                };
                grid.Children.Add(img);
                if (this.Document.Title != null)
                {
                    var text = ThemeHelpers.GetThemedText();
                    text.Text = this.Document.Title;
                    text.HorizontalAlignment = HorizontalAlignment.Center;
                    text.VerticalAlignment = VerticalAlignment.Bottom;
                    text.TextAlignment = TextAlignment.Center;
                    text.Margin = new Thickness(0, 0, 0, 10);
                    text.TextTrimming = TextTrimming.CharacterEllipsis;
                    grid.Children.Add(text);
                }
                this.Preview.Children.Add(grid);
            }

            public class ImageViewer : ContentDialog
            {
                public Attachment.PhotoAtt Picture;
                public Image Image = new Image();

                public ImageViewer(Attachment.PhotoAtt pic)
                {
                    this.Picture = pic;

                    this.LoadElements();

                    this.KeyDown += (object s, KeyRoutedEventArgs e) =>
                    {
                        if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Space || e.Key == Windows.System.VirtualKey.Escape) this.Hide();
                    };

                    this.LoadImage();
                }

                public void LoadElements()
                {
                    Grid content = new Grid();
                    content.RowDefinitions.Add(new RowDefinition());
                    content.RowDefinitions.Add(new RowDefinition());

                    ScrollViewer scroll = new ScrollViewer
                    {
                        Content = this.Image,
                        ZoomMode = ZoomMode.Enabled
                    };
                    Grid.SetRow(scroll, 0);
                    content.Children.Add(scroll);

                    var closetext = ThemeHelpers.GetThemedText();
                    closetext.Text = Utils.LocString("Dialog/Close");
                    Button close = new Button
                    {
                        Content = closetext,
                        Margin = new Thickness(0, 5, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                    };
                    close.Click += (object s, RoutedEventArgs e) => this.Hide();
                    Grid.SetRow(close, 1);

                    content.Children.Add(close);

                    this.Content = content;
                }

                public async void LoadImage()
                {
                    this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().Url));
                }
            }
        }
    }
}