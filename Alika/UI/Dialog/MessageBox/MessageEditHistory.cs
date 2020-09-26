using Alika.Libs;
using Alika.Libs.VK.Responses;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace Alika.UI.Dialog
{
    public class MessageEditHistory : Grid
    {
        public MessageEditHistory(List<Message> Messages)
        {
            this.MaxWidth = 500;

            var list = new StackPanel { 
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch
            };

            foreach(Message msg in Messages)
            {
                var item = new Button
                {
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
                    CornerRadius = new Windows.UI.Xaml.CornerRadius(10),
                    Content = new TextBlock
                    {
                        Text = msg.update_time.ToDateTime().ToString("HH:mm:ss, dd.MM.yy"),
                        VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
                        FontSize = 20
                    },
                    Background = Coloring.Transparent.Full
                };
                item.Click += (a, b) => App.main_page.popup.Children.Add(new Popup { 
                    Content = new ScrollViewer { 
                        HorizontalScrollMode = ScrollMode.Disabled,
                        VerticalScrollMode = ScrollMode.Auto,
                        Content = new MessageBox.MessageGrid(msg, msg.peer_id, true)
                        {
                            MaxWidth = 500
                        }
                    },
                    Title = msg.update_time.ToDateTime().ToString("HH:mm:ss, dd.MM.yy")
                });
                list.Children.Add(item);
            }
            this.Children.Add(list);
        }
    }
}
