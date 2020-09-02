using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Lottie;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Alika.UI
{
    class MessageBox : ListViewItem
    {
        public MessageGrid message { get; set; }

        public MessageBox(Message msg, int peer_id)
        {
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.HorizontalContentAlignment = msg.from_id == App.vk.user_id ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            this.message = new MessageGrid(msg, peer_id);
            this.Content = (this.message);
        }

        public class MessageGrid : Grid
        {
            public TextBubble textBubble { get; set; }
            public Border avatar { get; set; } = new Border
            {
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(60, 60, 60, 60),
                VerticalAlignment = VerticalAlignment.Bottom
            };

            public MessageGrid(Message msg, int peer_id)
            {
                this.MinWidth = 200;
                this.MaxWidth = 700;

                this.textBubble = new TextBubble(msg, peer_id);

                this.LoadAvatar(msg.from_id);

                if (msg.from_id == App.vk.user_id)
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition());
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                    Grid.SetColumn(this.textBubble, 0);
                    Grid.SetColumn(this.avatar, 1);
                }
                else
                {
                    this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                    this.ColumnDefinitions.Add(new ColumnDefinition());
                    Grid.SetColumn(this.avatar, 0);
                    Grid.SetColumn(this.textBubble, 1);
                }

                this.Children.Add(this.textBubble);
                this.Children.Add(this.avatar);
            }

            public async void LoadAvatar(int user_id)
            {
                string url;
                if (user_id > 0)
                {
                    if (!App.cache.Users.Exists(u => u.user_id == user_id)) App.vk.users.Get(new List<int> { user_id }, fields: "photo_200");
                    url = App.cache.Users.Find(u => u.user_id == user_id).photo_200;
                }
                else
                {
                    if (!App.cache.Groups.Exists(g => g.id == user_id)) App.vk.groups.GetById(new List<int> { user_id }, fields: "photo_200");
                    url = App.cache.Groups.Find(g => g.id == user_id).photo_200;
                }

                if (url != null)
                {
                    this.avatar.Height = 40;
                    this.avatar.Width = 40;

                    ImageBrush ava = new ImageBrush();
                    ava.ImageSource = await ImageCache.Instance.GetFromCacheAsync(new Uri(url));
                    ava.Stretch = Stretch.Fill;
                    this.avatar.Background = ava;
                }
                Thickness margin = this.avatar.Margin;
                margin.Bottom = this.textBubble.border.Margin.Bottom + (this.textBubble.text.Margin.Bottom / 2);
                this.avatar.Margin = margin;
            }
        }
        public class TextBubble : Grid
        {
            public Message message { get; set; }
            public Border border = new Border
            {
                BorderThickness = new Thickness(1),
                Background = App.systemDarkTheme ? Coloring.MessageBox.TextBubble.Dark : Coloring.MessageBox.TextBubble.Light,
                Margin = new Thickness(10, 5, 10, 5),
                MinHeight = 50,
                MinWidth = 50,
                CornerRadius = new CornerRadius(10)
            };
            public RichTextBlock text = new RichTextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5),
                Padding = new Thickness(5)
            };
            public TextBlock name = new TextBlock
            {
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(12, 5, 12, 1)
            };
            public Grid textGrid = new Grid();
            public Grid attachGrid = new Grid();
            public Grid keyboardGrid = new Grid();
            public Grid borderGrid = new Grid();
            public int peer_id;

            public TextBubble(Message msg, int peer_id)
            {
                this.message = msg;
                this.peer_id = peer_id;
                this.LoadText();
                this.LoadName();
                this.textGrid.Children.Add(this.text);

                this.borderGrid.RowDefinitions.Add(new RowDefinition());
                Grid.SetRow(this.textGrid, 0);
                if (this.message.text.Length > 0 || this.message.attachments.Count == 0) this.borderGrid.Children.Add(this.textGrid);
                if (this.message.attachments.Count > 0)
                {
                    this.borderGrid.RowDefinitions.Add(new RowDefinition());
                    Grid.SetRow(this.attachGrid, 1);
                    this.borderGrid.Children.Add(this.attachGrid);
                    this.LoadAttachments();
                }
                if (this.message.keyboard != null)
                {
                    this.borderGrid.RowDefinitions.Add(new RowDefinition());
                    Grid.SetRow(this.keyboardGrid, 2);
                    this.borderGrid.Children.Add(this.keyboardGrid);
                    this.LoadButtons();
                }
                this.border.Child = this.borderGrid;

                this.RowDefinitions.Add(new RowDefinition());
                this.RowDefinitions.Add(new RowDefinition());

                Grid.SetRow(this.name, 0);
                Grid.SetRow(this.border, 1);

                this.Children.Add(this.name);
                this.Children.Add(border);
            }

            public void LoadText()
            {
                string text = this.message.text;
                if (text != null)
                {
                    this.text.Blocks.Clear();
                    List<Match> markdown = new List<Match>();

                    MatchCollection pushes = new Regex(@"\[(id|club)\d+\|[^\]]*]").Matches(text);
                    MatchCollection links = new Regex(@"((http|https)\:\/\/)?[\w]*\.(com|online|ru|net|ua|su|tk|ml|ga|cf|gq|gg|me|рф|org|biz|info|cc|ws|pro|tv|in|xyz|рф)(\/[\w\?\-\=\&.]*)*").Matches(text);
                    if (pushes.Count > 0) foreach (Match push in pushes) markdown.Add(push);
                    if (links.Count > 0) foreach (Match link in links) markdown.Add(link);

                    Paragraph p = new Paragraph();
                    if (markdown.Count > 0)
                    {
                        for (int i = 0; i < markdown.Count; i++)
                        {
                            Match m = markdown[i];
                            if (i == 0 && !text.StartsWith(m.Value)) p.Inlines.Add(new Run { Text = text.Substring(0, m.Index) });
                            if (i > 0 && markdown[i - 1].Index != m.Index)
                            {
                                Match prev = markdown[i - 1];
                                int start = prev.Index + prev.Length;
                                p.Inlines.Add(new Run { Text = text.Substring(start, m.Index - start) });
                            }
                            if (!m.Value.Contains("["))
                            {
                                string lnk = m.Value;
                                if (!lnk.StartsWith("http://") && !lnk.StartsWith("https://")) lnk = "http://" + lnk;
                                Hyperlink link = new Hyperlink { NavigateUri = new Uri(lnk) };
                                link.Inlines.Add(new Run { Text = m.Value });
                                p.Inlines.Add(link);
                            }
                            else
                            {
                                Hyperlink link = new Hyperlink { NavigateUri = new Uri("https://vk.com/" + m.Value.Split("|")[0].Replace("[", "")) };
                                link.Inlines.Add(new Run { Text = m.Value.Split("|")[1].Replace("]", "") });
                                p.Inlines.Add(link);
                            }
                            if (i == markdown.Count - 1 && !text.EndsWith(m.Value)) p.Inlines.Add(new Run { Text = text.Substring(m.Index + m.Length) });
                        }
                    }
                    else p.Inlines.Add(new Run { Text = text });
                    this.text.Blocks.Add(p);
                }
            }

            public void LoadName()
            {
                if (this.message.from_id > 0)
                {
                    if (!App.cache.Users.Exists(u => u.user_id == this.message.from_id)) App.vk.users.Get(new List<int> { this.message.from_id }, fields: "photo_200");
                    User user = App.cache.Users.Find(u => u.user_id == this.message.from_id);
                    this.name.Text = user.first_name + " " + user.last_name;
                }
                else
                {
                    if (!App.cache.Groups.Exists(g => g.id == this.message.from_id)) App.vk.groups.GetById(new List<int> { this.message.from_id }, fields: "photo_200");
                    this.name.Text = App.cache.Groups.Find(g => g.id == this.message.from_id).name;
                }
                if (this.message.from_id == App.vk.user_id) this.name.HorizontalTextAlignment = TextAlignment.Right;
            }

            public void LoadAttachments()
            {
                if (this.message.attachments != null && this.message.attachments.Count > 0)
                {
                    this.message.attachments.ForEach(async (Attachment att) =>
                    {
                        this.attachGrid.RowDefinitions.Add(new RowDefinition());
                        if (att.type == "photo")
                        {
                            MessageAttachment.Photo photo = new MessageAttachment.Photo(att.photo);
                            photo.Preview.Width = this.Width;
                            Grid.SetRow(photo, this.attachGrid.RowDefinitions.Count - 1);
                            this.attachGrid.Children.Add(photo);
                        }
                        else if (att.type == "sticker")
                        {
                            if (att.sticker.animation_url == null)
                            {
                                Image img = new Image
                                {
                                    Height = 160,
                                    Width = 160,
                                    Margin = new Thickness(10),
                                    Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(att.sticker.GetBestQuality(App.systemDarkTheme)))
                                };
                                Grid.SetRow(img, this.attachGrid.RowDefinitions.Count - 1);
                                this.attachGrid.Children.Add(img);
                            }
                            else
                            {
                                AnimatedVisualPlayer img = new AnimatedVisualPlayer
                                {
                                    Height = 160,
                                    Width = 160,
                                    Margin = new Thickness(10),
                                    Source = new LottieVisualSource
                                    {
                                        UriSource = new Uri(App.systemDarkTheme ? att.sticker.animation_url.Replace(".json", "b.json") : att.sticker.animation_url)
                                    }
                                };
                                Grid.SetRow(img, this.attachGrid.RowDefinitions.Count - 1);
                                this.attachGrid.Children.Add(img);
                            }
                            this.border.Background = Coloring.Transparent.Full;
                        }
                        else if (att.type == "audio_message")
                        {

                            MediaPlayerElement audio = new MediaPlayerElement
                            {
                                Background = Coloring.Transparent.Full,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                MaxWidth = 400,
                                Margin = new Thickness(10)
                            };
                            audio.Loaded += (object s, RoutedEventArgs e) => (s as MediaPlayerElement).Source = MediaSource.CreateFromUri(new Uri(att.audio_message.link_mp3));
                            audio.AreTransportControlsEnabled = true;
                            audio.TransportControls = new MediaTransportControls
                            {
                                IsCompact = true,
                                IsRepeatEnabled = false,
                                IsFullWindowButtonVisible = false,
                                IsZoomButtonVisible = false,
                                IsCompactOverlayButtonVisible = false,
                                VerticalAlignment = VerticalAlignment.Center,
                                VerticalContentAlignment = VerticalAlignment.Center
                            };

                            this.border.Background = new SolidColorBrush(Coloring.FromHash(App.systemDarkTheme ? "000000" : "ffffff"));
                            Grid.SetRow(audio, this.attachGrid.RowDefinitions.Count - 1);
                            this.attachGrid.Children.Add(audio);
                        }
                        else if (att.type == "graffiti")
                        {
                            Image img = new Image
                            {
                                Height = att.graffiti.height / 2,
                                Width = att.graffiti.width / 2,
                                Source = await ImageCache.Instance.GetFromCacheAsync(new Uri(att.graffiti.url)),
                                Margin = new Thickness(5)
                            };
                            Grid.SetRow(img, this.attachGrid.RowDefinitions.Count - 1);
                            this.attachGrid.Children.Add(img);
                            this.border.Background = Coloring.Transparent.Full;
                        }
                        else if (att.type == "doc")
                        {
                            Button doc = new MessageAttachment.Document(att.document);
                            Grid.SetRow(doc, this.attachGrid.RowDefinitions.Count - 1);
                            this.attachGrid.Children.Add(doc);
                        }
                    });
                }
            }

            public void LoadButtons()
            {
                if (this.message.keyboard != null && this.message.keyboard.inline)
                {
                    this.message.keyboard.buttons.ForEach((List<Message.Keyboard.Button> buttons) =>
                    {
                        Grid grid = new Grid();
                        Grid.SetRow(grid, this.keyboardGrid.RowDefinitions.Count);
                        this.keyboardGrid.RowDefinitions.Add(new RowDefinition());
                        buttons.ForEach((Message.Keyboard.Button button) =>
                        {
                            Button btn = new Button
                            {
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                VerticalAlignment = VerticalAlignment.Stretch,
                                Margin = new Thickness(5),
                                CornerRadius = this.border.CornerRadius
                            };
                            if (button.color != "default") btn.Background = new SolidColorBrush(Coloring.FromHash(Coloring.MessageBox.Keyboard.GetColor(button.color)));
                            btn.Content = new TextBlock
                            {
                                Text = button.action.label,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            btn.Click += (object sender, RoutedEventArgs e) => Task.Factory.StartNew(() => App.vk.messages.Send(this.peer_id, text: button.action.label, payload: button.action.payload));
                            Grid.SetColumn(btn, grid.ColumnDefinitions.Count);
                            grid.ColumnDefinitions.Add(new ColumnDefinition());
                            grid.Children.Add(btn);
                        });
                        this.keyboardGrid.Children.Add(grid);
                    });
                }
            }
        }
    }
}
