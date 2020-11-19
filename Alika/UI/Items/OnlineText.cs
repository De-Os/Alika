using Alika.Libs;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Alika.UI.Items
{
    [Bindable]
    public class OnlineText : ContentControl
    {
        private int LastOnline;
        private int UserId;

        private TextBlock Text;

        public OnlineText(int user_id)
        {
            this.UserId = user_id;

            this.Text = ThemeHelpers.GetThemedText();
            this.Text.TextTrimming = Windows.UI.Xaml.TextTrimming.CharacterEllipsis;

            this.Content = this.Text;

            Task.Factory.StartNew(() =>
            {
                var user = App.Cache.GetUser(this.UserId);
                this.LastOnline = user.OnlineInfo.LastSeen;
                this.SetOnline(user.OnlineInfo.IsOnline);
            });
            this.UpdateOffline();

            App.LP.UserOnline += (a) =>
            {
                if (a.UserId == this.UserId) this.SetOnline(true);
            };
            App.LP.UserOffline += (a) =>
            {
                if (a.UserId == this.UserId)
                {
                    this.LastOnline = a.Timestamp;
                    this.SetOnline(false);
                }
            };
        }

        private void SetOnline(bool online)
        {
            App.UILoop.AddAction(new UITask
            {
                Action = () =>
                {
                    if (online)
                    {
                        this.Text.Text = Utils.LocString("Time/Online");
                    }
                    else
                    {
                        string text = null;
                        double final = 0;
                        var time = this.LastOnline.ToDateTime();
                        var curr = DateTime.Now;
                        if (time > curr.AddMinutes(-1))
                        {
                            text = Utils.LocString("Time/RightNow");
                        }
                        else
                        {
                            if (time > curr.AddHours(-1))
                            {
                                final = (curr - time).TotalMinutes;
                                text = "Minutes";
                            }
                            else
                            {
                                if (time > curr.AddDays(-1))
                                {
                                    final = (curr - time).TotalHours;
                                    text = "Hours";
                                }
                                else
                                {
                                    if (time > curr.AddDays(-16))
                                    {
                                        final = (curr - time).Days;
                                        text = "Days";
                                    }
                                    else
                                    {
                                        this.Text.Text = Utils.LocString("Time/LastSeen".Replace("%date%", time.ToString("dd MMMM" + (time.Year == curr.Year ? "" : " yyyy"))));
                                        return;
                                    }
                                }
                            }
                        }
                        final = Math.Round(final);
                        this.Text.Text = Utils.LocString("Time/LastSeen").Replace("%date%", final.ToString() + " " + Utils.LocString("Time/" + text + this.Format(final)) + " " + Utils.LocString("Time/Ago"));
                    }
                }
            });
        }

        public void UpdateOffline()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromMinutes(3));
                    var user = App.VK.Users.Get(new System.Collections.Generic.List<int> { this.UserId }, fields: "online_info")[0];
                    this.LastOnline = user.OnlineInfo.LastSeen;
                    this.SetOnline(user.OnlineInfo.IsOnline);
                }
            });
        }

        private string Format(double num)
        {
            if (num >= 10 && num <= 20) return "From0To0";
            var end = int.Parse(num.ToString().Last().ToString());
            if (end == 1) return "From1To1";
            if (end >= 2 && end <= 4) return "From2To4";
            return "From5To9";
        }
    }
}