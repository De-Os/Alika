using Alika.Libs.VK.Responses;
using Alika.UI.Misc;
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
        public Avatar image;

        //TODO: Rewrite it.
        public ChatItem(int peer_id, string avatar, string name, Message last_msg)
        {
            this.peer_id = peer_id;
            this.name = name;
            this.Height = 70;

            this.grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Avatar
            this.grid.ColumnDefinitions.Add(new ColumnDefinition()); // Text fields
            this.textGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // Top margin
            this.textGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) }); // Chat name
            this.textGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) }); // Message x
            this.LoadAvatar();
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

        public void LoadAvatar()
        {
            this.image = new Avatar(this.peer_id) { 
                Height = 50,
                Width = 50,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10),
                OpenInfoOnClick = false
            };
            Grid.SetColumn(this.image, 0);
            this.grid.Children.Add(this.image);
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
