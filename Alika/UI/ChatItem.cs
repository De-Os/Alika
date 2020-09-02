using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Alika.UI
{
    /// <summary>
    /// Chat holder in chats list
    /// </summary>
    class ChatItem : ListViewItem
    {
        public string avatar;
        public string name;
        public int peer_id;
        public Message message;
        public Grid grid = new Grid();
        public Grid textGrid = new Grid();
        public TextBlock nameBlock = new TextBlock
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        public TextBlock textBlock = new TextBlock
        {
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        public Border image = new Border
        {
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(60, 60, 60, 60)
        };

        public ChatItem(int peer_id, string avatar, string name, Message last_msg)
        {
            this.peer_id = peer_id;
            this.avatar = avatar;
            this.name = name;
            this.Height = 70;

            this.grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) }); // Avatar
            this.LoadAvatar();
            this.grid.ColumnDefinitions.Add(new ColumnDefinition()); // Text fields
            this.textGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // Top margin
            this.textGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) }); // Chat name
            this.textGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) }); // Message text
            if (this.name != null) this.nameBlock.Text = this.name;
            Grid.SetRow(this.nameBlock, 1);
            this.UpdateMsg(last_msg);
            Grid.SetRow(this.textBlock, 2);
            this.textGrid.Children.Add(this.nameBlock);
            this.textGrid.Children.Add(this.textBlock);
            Grid.SetColumn(this.textGrid, 1);
            this.grid.Children.Add(this.textGrid);
            this.Content = this.grid;
        }

        public async void LoadAvatar()
        {
            if (this.avatar != null)
            {
                this.image.Height = 50;
                this.image.Width = 50;
                ImageBrush ava = new ImageBrush();
                ava.ImageSource = await ImageCache.Instance.GetFromCacheAsync(new Uri(this.avatar));
                ava.Stretch = Stretch.Fill;
                this.image.Background = ava;
                Grid.SetColumn(this.image, 0);
                this.grid.Children.Add(this.image);
            }
        }

        public void UpdateMsg(Message msg)
        {
            if (msg != null)
            {
                if (msg.text.Length > 0)
                {
                    msg.text = msg.text.Replace("\n", " ");
                    MatchCollection pushes = new Regex(@"\[(id|club)\d+\|[^\]]*]").Matches(msg.text);
                    if (pushes.Count > 0)
                    {
                        foreach (Match push in pushes)
                        {
                            msg.text = msg.text.Replace(push.Value, push.Value.Split("|").Last().Replace("]", ""));
                        }
                    }
                }
                else msg.text = "📁 Вложение";
                this.message = msg;
                this.textBlock.Text = this.message.text;
            }
        }
    }
}
