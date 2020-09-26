using Alika.Libs;
using Alika.Libs.VK.Responses;
using Alika.UI.Misc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Alika.UI
{
    [Windows.UI.Xaml.Data.Bindable]
    public class ChatsList : ListView
    {
        public ListView chats = new ListView();
        public ChatsList()
        {
            this.SelectionChanged += (a, b) =>
            {
                if (this.SelectedItem is ChatItem i)
                {
                    if (i.peer_id != App.main_page.peer_id) App.main_page.peer_id = i.peer_id;
                }
            };
            this.LoadChats(0);
            App.lp.OnNewMessage += this.ProcessMessage;
        }

        public void LoadChats(int offset, int count = 50, int start_msg_id = 0)
        {
            Task.Factory.StartNew(() =>
              {
                  var conversations = App.vk.Messages.GetConversations(count: count, offset: offset, fields: "photo_200,online_info", start_message_id: start_msg_id).conversations;
                  List<ListViewItem> items = new List<ListViewItem>();
                  foreach (GetConversationsResponse.ConversationResponse conv in conversations)
                  {
                      App.UILoop.RunAction(new UITask
                      {
                          Action = () => this.Items.Add(new ChatItem(conv.conversation.peer.id, conv.last_message)),
                          Priority = CoreDispatcherPriority.High
                      });
                  }
              });
        }

        public void ProcessMessage(Message msg)
        {
            App.UILoop.AddAction(new UITask
            {
                Action = () =>
                {
                    foreach (var item in this.Items)
                    {
                        if (item is ChatItem chat)
                        {
                            if (msg.peer_id == chat.peer_id && this.Items.IndexOf(chat) != 0)
                            {
                                this.Items.Remove(chat);
                                this.Items.Insert(0, chat);
                                return;
                            }
                        }
                    }
                    //this.Items.Insert(0, new ChatItem(msg.peer_id, msg));
                },
                Priority = CoreDispatcherPriority.Low
            });
        }

        [Windows.UI.Xaml.Data.Bindable]
        public class ChatItem : ListViewItem
        {
            public int peer_id;
            public Message message;
            public Grid grid = new Grid();
            public Grid textGrid = new Grid();
            public TextBlock nameBlock = new TextBlock
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            public TextBlock textBlock = new TextBlock
            {
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Top
            };
            public Avatar image;
            public ChatItem(int peer_id, Message last_msg)
            {
                this.peer_id = peer_id;
                this.Height = 70;

                this.grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Avatar
                this.grid.ColumnDefinitions.Add(new ColumnDefinition()); // Text fields
                this.textGrid.RowDefinitions.Add(new RowDefinition()); // Chat name
                this.textGrid.RowDefinitions.Add(new RowDefinition()); // Message x
                this.LoadAvatar();
                this.nameBlock.Text = App.cache.GetName(this.peer_id);
                Grid.SetRow(this.nameBlock, 0);
                this.UpdateMsg(last_msg);
                Grid.SetRow(this.textBlock, 1);
                this.textGrid.Children.Add(this.nameBlock);
                this.textGrid.Children.Add(this.textBlock);
                Grid.SetColumn(this.textGrid, 1);
                this.grid.Children.Add(this.textGrid);
                this.Content = this.grid;

                App.lp.OnNewMessage += this.OnNewMessage;
                App.lp.OnMessageEdition += (m) =>
                {
                    if (m.id == this.message.id)
                    {
                        App.UILoop.AddAction(new UITask
                        {
                            Action = () => this.UpdateMsg(m),
                            Priority = CoreDispatcherPriority.Low
                        });
                    }
                };
            }

            private void OnNewMessage(Message msg)
            {
                if (msg.peer_id == this.peer_id)
                {
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () => this.UpdateMsg(msg),
                        Priority = CoreDispatcherPriority.Low
                    });
                }
            }

            public void LoadAvatar()
            {
                this.image = new Avatar(this.peer_id)
                {
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
                this.message = msg;
                string text = this.FormatName(msg.from_id) + ": ";
                this.textBlock.Text = text + this.message.ToCompactText();
            }

            private string FormatName(int id)
            {
                if (id == App.vk.user_id) return Utils.LocString("Dialog/You");
                var name = App.cache.GetName(id);
                if (name.Count(c => c == ' ') != 1 || this.peer_id > Libs.VK.Limits.Messages.PEERSTART) return name;
                return name.Split(" ")[0];
            }
        }
    }
}
