using Alika.Libs;
using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.UI;
using RestSharp;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI
{
    public class MediaViewer
    {
        public Grid grid = new Grid
        {
            CornerRadius = new CornerRadius(10),
            Background = new AcrylicBrush
            {
                FallbackColor = Coloring.Transparent.Percent(100).Color,
                TintColor = Coloring.Transparent.Percent(100).Color,
                TintOpacity = 0.7,
                BackgroundSource = AcrylicBackgroundSource.Backdrop
            }
        };
        public Grid menu = new Grid
        {
            CornerRadius = new CornerRadius(10),
            Background = new AcrylicBrush
            {
                FallbackColor = Coloring.Transparent.Percent(100).Color,
                TintColor = Coloring.Transparent.Percent(100).Color,
                TintOpacity = 0.7,
                BackgroundSource = AcrylicBackgroundSource.Backdrop
            },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(5)
        };
        public Attachment attachment;
        public Popup popup = new Popup
        {
            ContentBackground = Coloring.Transparent.Full
        };

        public MediaViewer(Attachment attachment)
        {
            this.attachment = attachment;

            switch (attachment.Type)
            {
                case "photo":
                    this.LoadImage();
                    break;
            }

            this.popup.Content = this.grid;

            this.LoadMenu();

            App.MainPage.Popup.Children.Add(this.popup);
        }

        public void LoadMenu()
        {
            this.menu.ColumnDefinitions.Add(new ColumnDefinition());
            this.menu.ColumnDefinitions.Add(new ColumnDefinition());

            Button download = new Button
            {
                Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("download.svg"))),
                    Height = 30
                },
                Margin = new Thickness(5),
                Background = Coloring.Transparent.Full,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(5)
            };
            download.Click += (a, b) => this.Download();
            Button close = new Button
            {
                Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("close.svg"))),
                    Height = 30
                },
                Margin = new Thickness(5),
                Background = Coloring.Transparent.Full,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(5)
            };
            close.Click += (a, b) => this.popup.Hide();
            Grid.SetColumn(download, 0);
            Grid.SetColumn(close, 1);

            this.menu.Children.Add(download);
            this.menu.Children.Add(close);
        }

        public async void LoadImage()
        {
            Grid grid = new Grid
            {
                Background = new AcrylicBrush
                {
                    FallbackColor = Coloring.Transparent.Percent(100).Color,
                    TintColor = Coloring.Transparent.Percent(100).Color,
                    TintOpacity = 0.7,
                    BackgroundSource = AcrylicBackgroundSource.Backdrop
                }
            };
            grid.Children.Add(new ScrollViewer
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                ZoomMode = ZoomMode.Enabled,
                MinZoomFactor = (float)0.1,
                Width = (Window.Current.Content as Frame).ActualWidth * 0.9,
                Height = (Window.Current.Content as Frame).ActualHeight * 0.9,
                Content = new Image
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.attachment.Photo.GetBestQuality().Url))
                }
            });

            App.UILoop.AddAction(new UITask
            {
                Action = () =>
                {
                    this.grid.Children.Add(grid);
                    this.grid.Children.Add(this.menu);
                }
            });
        }

        public async void Download()
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            string uri = "";
            switch (this.attachment.Type)
            {
                case "photo":
                    uri = this.attachment.Photo.GetBestQuality().Url;
                    savePicker.SuggestedFileName = this.attachment.Photo.ToAttachFormat();
                    savePicker.FileTypeChoices.Add("JPG", new List<string>() { ".jpg" });
                    break;
            }
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                await FileIO.WriteBytesAsync(file, new RestClient(new Uri(uri)).DownloadData(new RestRequest()));
                await CachedFileManager.CompleteUpdatesAsync(file);
            }
        }
    }
}
