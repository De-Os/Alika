using Alika.Libs;
using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.UI;
using RestSharp;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{
    public class MessageAttachment
    {
        public class Photo : Grid
        {
            public Attachment.Photo Picture { get; set; }
            public Image Preview = new Image();

            public Photo(Attachment.Photo photo)
            {
                this.Picture = photo;

                this.Children.Add(this.Preview);

                this.PointerPressed += Photo_PointerPressed;

                this.LoadPreview();
            }

            private async void Photo_PointerPressed(object sender, PointerRoutedEventArgs e)
            {
                ContentDialog viewer = new Viewer(this.Picture);
                viewer.Height = Window.Current.Bounds.Height;
                viewer.Width = Window.Current.Bounds.Width;
                await viewer.ShowAsync();
            }

            public async void LoadPreview()
            {
                this.Preview.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().url));
            }

            public class Viewer : ContentDialog
            {
                public Attachment.Photo Picture { get; set; }
                public Image Image = new Image();

                public Viewer(Attachment.Photo pic)
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
                    this.Title = new TextBlock
                    {
                        Text = "photo" + this.Picture.owner_id + "_" + this.Picture.id,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };

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

                    RelativePanel buttons = new RelativePanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    Grid.SetRow(buttons, 1);

                    Button download = new Button
                    {
                        Content = new TextBlock { Text = Utils.LocString("Dialog/Save") },
                        Margin = new Thickness(0, 5, 10, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                    };
                    download.Click += async (object s, RoutedEventArgs e) =>
                    {
                        var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                        savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                        savePicker.FileTypeChoices.Add("JPG", new List<string>() { ".jpg" });
                        savePicker.SuggestedFileName = (this.Title as TextBlock).Text;
                        StorageFile file = await savePicker.PickSaveFileAsync();
                        if (file != null)
                        {
                            CachedFileManager.DeferUpdates(file);
                            await FileIO.WriteBytesAsync(file, new RestClient(new Uri(this.Picture.GetBestQuality().url)).DownloadData(new RestRequest()));
                            await CachedFileManager.CompleteUpdatesAsync(file);
                        }
                    };
                    Button close = new Button
                    {
                        Content = new TextBlock { Text = Utils.LocString("Dialog/Close") },
                        Margin = new Thickness(0, 5, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                    };
                    close.Click += (object s, RoutedEventArgs e) => this.Hide();
                    RelativePanel.SetLeftOf(download, close);
                    RelativePanel.SetAlignLeftWithPanel(download, true);
                    RelativePanel.SetAlignRightWithPanel(close, true);
                    buttons.Children.Add(download);
                    buttons.Children.Add(close);

                    content.Children.Add(buttons);

                    this.Content = content;
                }

                public async void LoadImage()
                {
                    this.Image.Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.Picture.GetBestQuality().url));

                }
            }
        }

        public class Document : Button
        {
            public Attachment.Document document { get; set; }

            public Document(Attachment.Document doc)
            {
                this.document = doc;

                this.Margin = new Thickness(5);
                this.Background = Coloring.Transparent.Full;

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
