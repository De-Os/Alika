﻿using Alika.Libs;
using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Alika.Theme;

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
            Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        public GraffitiWindow()
        {
            this.Canvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Touch;

            var sendtext = ThemeHelpers.GetThemedText();
            sendtext.Text = Utils.LocString("Dialog/Send");
            sendtext.HorizontalAlignment = HorizontalAlignment.Center;
            sendtext.VerticalAlignment = VerticalAlignment.Center;
            this.Send.Content = sendtext;

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
                Content = new ThemedFontIcon
                {
                    FontSize = 15,
                    FontFamily = App.Icons,
                    Glyph = Glyphs.Custom.Camera
                },
                HorizontalAlignment = HorizontalAlignment.Right,
                Background = App.Theme.Colors.Transparent
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
                    App.VK.Messages.Send(App.MainPage.PeerId, attachments: new List<string> {
                        (App.VK.Messages.UploadDocument(bytes, App.MainPage.PeerId, "graffiti") as Attachment.GraffitiAtt).ToAttachFormat()
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
                        App.VK.Messages.Send(App.MainPage.PeerId, attachments: new List<string> {
                        (App.VK.Messages.UploadDocument(bytes, App.MainPage.PeerId, "graffiti") as Attachment.GraffitiAtt).ToAttachFormat()
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