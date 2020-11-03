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
using Windows.UI.Xaml.Media.Imaging;

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
                    Text = this.Doc.Title,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetRow(name, 0);
                text.Children.Add(name);

                TextBlock desc = new TextBlock
                {
                    Text = Utils.FormatSize(this.Doc.Size, 2) + " • " + this.Doc.Extension,
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

                this.LoadSticker();
            }

            public async void LoadSticker()
            {
                if (this.StickerSource.AnimationUrl != null)
                {
                    this.Image = new AnimatedVisualPlayer
                    {
                        Source = new LottieVisualSource
                        {
                            UriSource = new Uri(App.DarkTheme ? this.StickerSource.AnimationUrl.Replace(".json", "b.json") : this.StickerSource.AnimationUrl)
                        }
                    };
                }
                else
                {
                    this.Image = new Image
                    {
                        Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.StickerSource.GetBestQuality(App.DarkTheme)))
                    };
                }
                this.Children.Add(this.Image);
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

            public AudioMessage(Attachment.AudioMessageAtt audio)
            {
                while (audio.Waveform.Count < 128) audio.Waveform.Add(0);
                this.Audio = audio;

                this.media.Source = MediaSource.CreateFromUri(new Uri(this.Audio.LinkMP3));

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
                        Content = new Image
                        {
                            Source = new SvgImageSource(new Uri(Utils.AssetTheme("fly_menu.svg"))),
                            Height = 10,
                            Width = 10,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        CornerRadius = new CornerRadius(10),
                        Background = Coloring.Transparent.Full,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    var trans_text = new TextBlock
                    {
                        Text = this.Audio.Transcript,
                        Visibility = Visibility.Collapsed,
                        TextWrapping = TextWrapping.Wrap
                    };
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
                this.PointerEntered += (a, b) => this.Background = Coloring.Transparent.Percent(50);
                this.PointerExited += (a, b) => this.Background = Coloring.Transparent.Full;

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

                public Brush FillColor = App.DarkTheme ? Coloring.MessageBox.VoiceMessage.Light : Coloring.MessageBox.VoiceMessage.Dark;
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

        /// <summary>
        /// Uploaded file holder
        /// </summary>
        [Bindable]
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
                if (this.Document.Title != null)
                {
                    TextBlock text = new TextBlock
                    {
                        Text = this.Document.Title,
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
                    this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().Url));
                }
            }
        }
    }
}