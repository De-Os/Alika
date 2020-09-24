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
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{
    public class MessageAttachment
    {
        /// <summary>
        /// Photo holder
        /// </summary>
        [Windows.UI.Xaml.Data.Bindable]
        public class Photo : Grid
        {
            public Attachment.Photo Picture { get; set; }
            public Image Preview = new Image();

            public Photo(Attachment.Photo photo)
            {
                this.Picture = photo;

                this.Children.Add(this.Preview);

                this.PointerPressed += (a, b) => new MediaViewer(new Attachment
                {
                    type = "photo",
                    photo = this.Picture
                });

                this.LoadPreview();
            }

            public async void LoadPreview()
            {
                this.Preview.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().url));
            }
        }

        /// <summary>
        /// Document holder
        /// </summary>
        [Windows.UI.Xaml.Data.Bindable]
        public class Document : Button
        {
            public Attachment.Document document { get; set; }

            public Document(Attachment.Document doc)
            {
                this.document = doc;

                this.Margin = new Thickness(5);
                this.Background = Coloring.Transparent.Full;
                this.CornerRadius = new CornerRadius(10);

                Grid content = new Grid
                {
                    Margin = new Thickness(0, 5, 5, 5),
                };
                content.ColumnDefinitions.Add(new ColumnDefinition());
                content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Image img = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("document.svg"))),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    MaxHeight = 35,
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Grid.SetColumn(img, 0);
                content.Children.Add(img);

                Grid text = new Grid();
                text.RowDefinitions.Add(new RowDefinition());
                text.RowDefinitions.Add(new RowDefinition());

                TextBlock name = new TextBlock
                {
                    Text = this.document.title,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetRow(name, 0);
                text.Children.Add(name);

                TextBlock desc = new TextBlock
                {
                    Text = Utils.FormatSize(this.document.size, 2) + " • " + this.document.extension,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
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
                savePicker.FileTypeChoices.Add(this.document.extension.ToUpper(), new List<string>() { "." + this.document.extension });
                savePicker.SuggestedFileName = this.document.title;
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);
                    await FileIO.WriteBytesAsync(file, new RestClient(new Uri(this.document.url)).DownloadData(new RestRequest()));
                    await CachedFileManager.CompleteUpdatesAsync(file);
                }
            }
        }

        /// <summary>
        /// Sticker holder
        /// </summary>
        [Windows.UI.Xaml.Data.Bindable]
        public class Sticker : Grid
        {
            public Attachment.Sticker StickerSource;

            public UIElement Image;

            public Sticker(Attachment.Sticker sticker)
            {
                this.StickerSource = sticker;

                this.Height = 160;
                this.Width = 160;
                this.Margin = new Thickness(10);

                this.LoadSticker();
            }

            public async void LoadSticker()
            {
                if (this.StickerSource.animation_url != null)
                {
                    this.Image = new AnimatedVisualPlayer
                    {
                        Source = new LottieVisualSource
                        {
                            UriSource = new Uri(App.systemDarkTheme ? this.StickerSource.animation_url.Replace(".json", "b.json") : this.StickerSource.animation_url)
                        }
                    };
                }
                else
                {
                    this.Image = new Image
                    {
                        Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.StickerSource.GetBestQuality(App.systemDarkTheme)))
                    };
                }
                this.Children.Add(this.Image);
            }
        }

        /// <summary>
        /// Voice message holder
        /// </summary>
        [Windows.UI.Xaml.Data.Bindable]
        public class AudioMessage : Grid
        {
            public MediaPlayer media = new MediaPlayer
            {
                AutoPlay = false,
                Volume = 100
            };
            public Attachment.AudioMessage audio;

            public Image image = new Image
            {
                Source = new SvgImageSource(new Uri(Utils.AssetTheme("play.svg"))),
                Width = 25,
                Height = 25
            };
            public StackPanel wave = new StackPanel
            {
                Margin = new Thickness(5, 0, 5, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Orientation = Orientation.Horizontal
            };
            public TextBlock time = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };

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
                        this.image.Source = new SvgImageSource(new Uri(Utils.AssetTheme("pause.svg")));
                    }
                    else
                    {
                        this.media.Pause();
                        this.image.Source = new SvgImageSource(new Uri(Utils.AssetTheme("play.svg")));
                    }
                    this._playing = value;
                }
            }

            public AudioMessage(Attachment.AudioMessage audio)
            {
                while (audio.waveform.Count < 128) audio.waveform.Add(0);
                this.audio = audio;

                this.media.Source = MediaSource.CreateFromUri(new Uri(this.audio.link_mp3));

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                Grid.SetColumn(this.image, 0);
                Grid.SetColumn(this.wave, 1);
                Grid.SetColumn(this.time, 2);

                grid.Children.Add(this.image);
                grid.Children.Add(this.wave);
                grid.Children.Add(this.time);

                this.Children.Add(grid);
                this.Margin = new Thickness(5);
                this.Padding = new Thickness(5);
                this.CornerRadius = new CornerRadius(10);

                this.time.Text = TimeSpan.FromSeconds(this.audio.duration).ToString(@"m\:ss");
                this.GenerateWaveforms();

                this.wave.PointerEntered += (a, b) => this._onWaves = true;
                this.wave.PointerExited += (a, b) => this._onWaves = false;
                this.PointerPressed += this.OnClick;
                this.media.MediaEnded += (a, b) => App.UILoop.AddAction(new UITask
                {
                    Action = () => this.Playing = false
                });
                this.media.PlaybackSession.PositionChanged += this.PlayStateChanged;
                this.PointerEntered += (a, b) => this.Background = Coloring.Transparent.Percent(50);
                this.PointerExited += (a, b) => this.Background = Coloring.Transparent.Full;
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
                var partTime = TimeSpan.FromSeconds((double)decimal.Divide(this.audio.duration, 128));
                for (int x = 0; x < 128; x++)
                {
                    int wave = this.audio.waveform[x];
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

                public Brush FillColor = App.systemDarkTheme ? Coloring.MessageBox.VoiceMessage.Light : Coloring.MessageBox.VoiceMessage.Dark;
                public Brush NoFillColor = Coloring.Transparent.Percent(100);

                public WaveHolder(int wave, TimeSpan time)
                {
                    this.Time = time;
                    this.Rectangle.Fill = this.FillColor;
                    this.Rectangle.Height = 10 * (wave / 10);
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

        [Windows.UI.Xaml.Data.Bindable]
        public class Graffiti : Grid
        {
            public Image Image = new Image
            {
                Margin = new Thickness(5)
            };
            public Attachment.Graffiti Attachment;
            public Graffiti(Attachment.Graffiti att)
            {
                this.Attachment = att;
                this.Children.Add(this.Image);

                this.LoadImage();
            }

            public async void LoadImage()
            {
                this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Attachment.url));
            }
        }

        /// <summary>
        /// Uploaded file holder
        /// </summary>
        [Windows.UI.Xaml.Data.Bindable]
        public class Uploaded : Grid
        {
            public Button Remove = new Button
            {
                Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("close.svg"))),
                    Width = 10,
                    Height = 10,
                },
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Coloring.Transparent.Full

            };

            public Grid Preview = new Grid();

            public Attachment.Photo Picture;
            public Attachment.Document Document;

            public string Attach { get; set; }

            public Uploaded(Attachment.Photo pic = null, Attachment.Document doc = null)
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
                (this.Preview.Children[0] as Image).Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().url));
            }

            public void LoadDocument()
            {
                Grid grid = new Grid
                {
                    Width = this.Width,
                    Height = this.Height
                };
                Image img = new Image
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Windows.UI.Xaml.Media.Stretch.Fill,
                    MaxWidth = 100,
                    MaxHeight = 40,
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("document.svg")))
                };
                grid.Children.Add(img);
                if (this.Document.title != null)
                {
                    TextBlock text = new TextBlock
                    {
                        Text = this.Document.title,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 10),
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };
                    grid.Children.Add(text);
                }
                this.Preview.Children.Add(grid);
            }

            public class ImageViewer : ContentDialog
            {
                public Attachment.Photo Picture { get; set; }
                public Image Image = new Image();

                public ImageViewer(Attachment.Photo pic)
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

                    Button close = new Button
                    {
                        Content = new TextBlock { Text = Utils.LocString("Dialog/Close") },
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
                    this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().url));

                }
            }
        }
    }
}
