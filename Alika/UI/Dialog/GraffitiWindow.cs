using Alika.Libs;
using Alika.Libs.VK.Responses;
using System;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Alika.UI.Dialog
{
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
            Grid.SetRow(toolbar, 0);
            Grid.SetRow(this.Canvas, 1);
            Grid.SetRow(this.Send, 2);
            this.Children.Add(toolbar);
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
    }
}
