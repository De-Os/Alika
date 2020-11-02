using Alika.Libs;
using Alika.Libs.VK.Responses;
using Alika.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Alika.Misc
{
    public class ImportantMessages
    {
        public ImportantMessages(int peer_id = 0)
        {
            var popup = new Popup
            {
                Content = new ProgressRing
                {
                    Width = 50,
                    Height = 50,
                    IsActive = true,
                    Margin = new Windows.UI.Xaml.Thickness(10)
                },
                Title = Utils.LocString("Dialog/ImportantMessages")
            };
            App.MainPage.Popup.Children.Add(popup);
            Task.Factory.StartNew(() =>
            {
                var messages = new List<Message>();
                var total = -1;
                while (total == -1 || messages.Count < total)
                {
                    var response = App.VK.Messages.GetImportantMessages(count: 200, start_message_id: messages.Count > 0 ? messages.Last().Id : 0).Messages;
                    total = response.Count;
                    messages.AddRange(response.Items);
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                if (peer_id != 0) messages.RemoveAll(i => i.PeerId != peer_id);
                App.UILoop.AddAction(new UITask
                {
                    Action = () => popup.Content = new DialogExportReader.ViewerWithSearch(messages, true),
                    Priority = Windows.UI.Core.CoreDispatcherPriority.High
                });
            });
        }
    }
}
