using Alika.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Alika.UI.Items
{
    [Bindable]
    class TypeState : ContentControl
    {
        public delegate void BasicEvent();
        public event BasicEvent Show;
        public event BasicEvent Hide;

        private TextBlock Text = new TextBlock
        {
            TextTrimming = Windows.UI.Xaml.TextTrimming.CharacterEllipsis
        };
        private List<int> current = new List<int>();
        private DateTime updated = DateTime.Now;

        public TypeState(int peer_id)
        {
            this.Load();

            this.UpdateState();

            App.LP.Typing += (a) =>
            {
                if (a.Peerid == peer_id)
                {
                    this.current = a.UserIds;
                    this.updated = DateTime.Now;
                }
            };
            App.LP.OnNewMessage += (a) =>
            {
                if (a.PeerId == peer_id && this.current.Contains(a.FromId) && this.current.Count == 1)
                {
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () =>
                        {
                            this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            this.Hide?.Invoke();
                        }
                    });
                }
            };
        }

        private void Load()
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal };
            stack.Children.Add(this.Text);
            this.Content = stack;
        }

        private void UpdateState()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (this.updated <= DateTime.Now.AddSeconds(-5)) this.current.Clear();
                    if (this.current.Count > 0)
                    {
                        string text = String.Join(", ", this.current.Select(i => App.Cache.GetName(i)));
                        App.UILoop.AddAction(new UITask
                        {
                            Action = () =>
                            {
                                this.Visibility = Windows.UI.Xaml.Visibility.Visible;
                                this.Text.Text = text + " " + Utils.LocString("Dialog/Typing" + (this.current.Count > 1 ? "Many" : "")) + "...";
                                this.Show?.Invoke();
                            }
                        });
                    }
                    else App.UILoop.AddAction(new UITask
                    {
                        Action = () =>
{
    this.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
    this.Hide?.Invoke();
}
                    });
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            });
        }
    }
}
