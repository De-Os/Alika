using Alika.Libs;
using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI.Dialog
{
    [Windows.UI.Xaml.Data.Bindable]
    public class GraffitiWindow : Grid
    {
        public delegate void CloseEvent();
        public event CloseEvent OnClose;

        private InkCanvas Canvas = new InkCanvas
        {
            Width = 448,
            Height = 448
        };
        private Button Send = new Button
        {
            CornerRadius = new CornerRadius(10),
            Content = new TextBlock
            {
                Text = Utils.LocString("Dialog/Send"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            },
            Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        public GraffitiWindow()
        {
            this.Canvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

            this.RowDefinitions.Add(new RowDefinition());
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            this.RowDefinitions.Add(new RowDefinition());
            var toolbar = new InkToolbar
            {
                TargetInkCanvas = this.Canvas
            };
            var image = new Button
            {
                CornerRadius = new CornerRadius(5),
                Content = new Image
                {
                    Source = new SvgImageSource(new Uri(Utils.AssetTheme("camera.svg"))),
                    Width = 20,
                    Height = 20
                },
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = Coloring.Transparent.Full
            };
            image.Click += this.UsePhoto;
            Grid.SetRow(toolbar, 0);
            Grid.SetRow(image, 0);
            Grid.SetRow(this.Canvas, 1);
            Grid.SetRow(this.Send, 2);
            this.Children.Add(toolbar);
            this.Children.Add(image);
            this.Children.Add(this.Canvas);
            this.Children.Add(this.Send);

            this.Send.Click += Send_Click;
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Wait, 0);
            try
            {
                byte[] bytes = await this.Canvas.ToByteArray();
                if (bytes != null)
                {
                    App.vk.Messages.Send(App.main_page.peer_id, attachments: new List<string> {
                        (App.vk.Messages.UploadDocument(bytes, App.main_page.peer_id, "graffiti") as Attachment.Graffiti).ToAttachFormat()
                    });
                }
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
            }
            this.OnClose?.Invoke();
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        private async void UsePhoto(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".png");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Wait, 0);
                try
                {
                    byte[] bytes = await file.ReadBytesAsync();
                    if (bytes != null)
                    {
                        App.vk.Messages.Send(App.main_page.peer_id, attachments: new List<string> {
                        (App.vk.Messages.UploadDocument(bytes, App.main_page.peer_id, "graffiti") as Attachment.Graffiti).ToAttachFormat()
                    });
                    }
                }
                catch (Exception exc)
                {
                    await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
                }
                this.OnClose?.Invoke();
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
        }
    }
}
