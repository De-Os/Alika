using Alika.Libs;
using Alika.Libs.VK.Responses;
using Alika.UI;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Alika.Misc
{
    [Bindable]
    public class ExportPopup : StackPanel
    {
        public Button Confirm = new Button
        {
            HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right,
            Margin = new Windows.UI.Xaml.Thickness(10),
            CornerRadius = new Windows.UI.Xaml.CornerRadius(5),
            Content = new TextBlock { Text = Utils.LocString("Dialog/ExportCreate") }
        };

        public ExportPopup(int peer_id)
        {
            var buttons = new RadioButtons
            {
                Header = Utils.LocString("Dialog/ExportMode"),
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
                Margin = new Windows.UI.Xaml.Thickness(10)
            };
            buttons.Items.Add(new ExportButton
            {
                Mode = DialogExport.ExportMode.MESSAGES,
                Content = new TextBlock { Text = Utils.LocString("Dialog/ExportModeMessages") },
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch
            });
            buttons.Items.Add(new ExportButton
            {
                Mode = DialogExport.ExportMode.ATTACHMENTS,
                Content = new TextBlock { Text = Utils.LocString("Dialog/ExportModeAttachments") },
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch
            });
            buttons.Items.Add(new ExportButton
            {
                Mode = DialogExport.ExportMode.ALL,
                Content = new TextBlock { Text = Utils.LocString("Dialog/ExportModeAll") },
                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch
            });
            buttons.SelectedIndex = 0;
            this.Children.Add(buttons);

            this.Confirm.Click += (a, b) => new DialogExport(peer_id, (buttons.SelectedItem as ExportButton).Mode); ;
            this.Children.Add(this.Confirm);
        }

        private class ExportButton : RadioButton
        {
            public DialogExport.ExportMode Mode { get; set; }
        }
    }

    public class DialogExport
    {
        public enum ExportMode
        {
            MESSAGES,
            ATTACHMENTS,
            ALL
        }

        public int PeerId;
        private readonly ExportMode Mode;

        public DialogExport(int peer_id, ExportMode mode)
        {
            this.PeerId = peer_id;
            this.Mode = mode;

            Task.Factory.StartNew(() => this.Export());
        }

        public async void Export()
        {
            App.UILoop.AddAction(new UITask
            {
                Action = () =>
                {
                    var export = (App.MainPage.Chats.Content as ChatsHolder).MsgExport;
                    export.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    export.Flyout = new Flyout
                    {
                        Content = new TextBlock
                        {
                            Text = Utils.LocString("Dialog/Exporting").Replace("%chat%", this.PeerId.ToString()).Replace("%ready%", "0").Replace("%total%", "0")
                        }
                    };
                }
            });
            var folder = await this.CreateFolder();
            var att_folder = await folder.GetFolderAsync("attachments");
            var attachs = new
            {
                photos = await att_folder.GetFolderAsync("photos"),
                voice_messages = await att_folder.GetFolderAsync("voice_messages")
            };
            var final = new ExportedDialog
            {
                peer_id = this.PeerId,
                export_time = DateTime.UtcNow.ToUnixTime(),
                messages = new List<Message>()
            };
            int offset = 0;

            var data = App.VK.Messages.GetHistory(this.PeerId, rev: true, count: 200);
            while (data.Items.Count > 0)
            {
                try
                {
                    foreach (var msg in data.Items)
                    {
                        if (!final.messages.Any(x => x.Id == msg.Id))
                        {
                            if (this.Mode != ExportMode.ATTACHMENTS) final.messages.Add(msg);
                            if (this.Mode != ExportMode.MESSAGES && msg.Attachments?.Count > 0)
                            {
                                foreach (var att in msg.Attachments)
                                {
                                    switch (att.Type)
                                    {
                                        case "photo":
                                            var photo = await attachs.photos.CreateFileAsync(att.Photo.ToAttachFormat() + ".jpg");
                                            await FileIO.WriteBytesAsync(photo, new RestClient(new Uri(att.Photo.GetBestQuality().Url)).DownloadData(new RestRequest()));
                                            break;

                                        case "audio_message":
                                            var voice = await attachs.voice_messages.CreateFileAsync(att.AudioMessage.ToAttachFormat() + ".mp3");
                                            await FileIO.WriteBytesAsync(voice, new RestClient(new Uri(att.AudioMessage.LinkMP3)).DownloadData(new RestRequest()));
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () =>
                        {
                            (((App.MainPage.Chats.Content as ChatsHolder).MsgExport.Flyout as Flyout).Content as TextBlock).Text = Utils.LocString("Dialog/Exporting").Replace("%chat%", this.PeerId.ToString()).Replace("%ready%", final.messages.Count.ToString()).Replace("%total%", data.Count.ToString());
                        },
                        Priority = Windows.UI.Core.CoreDispatcherPriority.Low
                    });
                    offset += data.Items.Count;
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    data = App.VK.Messages.GetHistory(this.PeerId, rev: true, offset: offset, count: 200);
                }
                catch
                {
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                }
            }

            var json = await folder.CreateFileAsync("dialog" + this.PeerId.ToString() + ".vkmsg");
            await FileIO.WriteTextAsync(json, JsonConvert.SerializeObject(final));

            App.UILoop.AddAction(new UITask
            {
                Action = async () =>
                {
                    (App.MainPage.Chats.Content as ChatsHolder).MsgExport.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

                    var savePicker = new Windows.Storage.Pickers.FileSavePicker
                    {
                        SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
                    };
                    if (this.Mode == ExportMode.MESSAGES)
                    {
                        savePicker.SuggestedFileName = "dialog" + this.PeerId.ToString() + ".vkmsg";
                        savePicker.FileTypeChoices.Add("VKMSG", new List<string> { ".vkmsg" });
                    }
                    else
                    {
                        savePicker.SuggestedFileName = "dialog" + this.PeerId.ToString() + ".zip";
                        savePicker.FileTypeChoices.Add("ZIP", new List<string> { ".zip" });
                    }
                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null) _ = Task.Factory.StartNew(async () =>
                    {
                        if (this.Mode == ExportMode.MESSAGES)
                        {
                            await FileIO.WriteBytesAsync(file, await json.ReadBytesAsync());
                        }
                        else
                        {
                            if (this.Mode == ExportMode.ATTACHMENTS) await json.DeleteAsync();
                            ZipFile.CreateFromDirectory(folder.Path, ApplicationData.Current.TemporaryFolder.Path + "\\alika_extract_result.zip");
                            var zip = await ApplicationData.Current.TemporaryFolder.GetFileAsync("alika_extract_result.zip");
                            await FileIO.WriteBytesAsync(file, await zip.ReadBytesAsync());
                        }
                    });
                }
            });
        }

        private async Task<StorageFolder> CreateFolder()
        {
            var folder = ApplicationData.Current.TemporaryFolder;
            if (await folder.FileExistsAsync("alika_extract_result.zip"))
            {
                var f = await folder.GetFileAsync("alika_extract_result.zip");
                await f.DeleteAsync();
            }
            try
            {
                folder = await folder.GetFolderAsync("alika_export");
                await folder.DeleteAsync();
                folder = ApplicationData.Current.TemporaryFolder;
            }
            catch { }
            folder = await folder.CreateFolderAsync("alika_export");
            var attachs = await folder.CreateFolderAsync("attachments");
            await attachs.CreateFolderAsync("photos");
            await attachs.CreateFolderAsync("voice_messages");
            return folder;
        }

        public struct ExportedDialog
        {
            [JsonProperty("peer_id")]
            public int peer_id;

            [JsonProperty("export_time")]
            public int export_time;

            [JsonProperty("messages")]
            public List<Message> messages;
        }
    }

    public class DialogExportReader
    {
        public DialogExportReader(bool oldFormat = false) => this.ChooseFile(oldFormat);

        private async void ChooseFile(bool oldFormat)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            picker.FileTypeFilter.Add(oldFormat ? ".json" : ".vkmsg");
            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var loading = new Popup
                {
                    Content = new Windows.UI.Xaml.Controls.ProgressRing
                    {
                        Height = 50,
                        Width = 50,
                        IsActive = true
                    }
                };
                App.MainPage.Popup.Children.Add(loading);
                _ = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var json = JsonConvert.DeserializeObject<DialogExport.ExportedDialog>(await FileIO.ReadTextAsync(file));
                        App.UILoop.AddAction(new UITask
                        {
                            Action = () =>
                            {
                                if (json.messages.Count > 0)
                                {
                                    var content = new Grid();
                                    content.RowDefinitions.Add(new RowDefinition { Height = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Star) });
                                    content.RowDefinitions.Add(new RowDefinition { Height = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Auto) });

                                    var viewer = new Viewer(json.messages)
                                    {
                                        Width = 600,
                                        HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch
                                    };
                                    var scroll = new ScrollViewer
                                    {
                                        Content = viewer,
                                        HorizontalScrollMode = ScrollMode.Disabled
                                    };
                                    scroll.ViewChanged += (a, b) =>
                                    {
                                        if (scroll.VerticalOffset >= scroll.ScrollableHeight) viewer.LoadMessages(250);
                                    };
                                    var search = new Button
                                    {
                                        Content = new TextBlock { Text = Utils.LocString("Search") },
                                        Margin = new Windows.UI.Xaml.Thickness(0, 5, 5, 0),
                                        CornerRadius = new Windows.UI.Xaml.CornerRadius(5),
                                        HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right
                                    };
                                    search.Click += (a, b) =>
                                    {
                                        App.MainPage.Popup.Children.Add(new Popup
                                        {
                                            Content = new ViewerWithSearch(json.messages)
                                        });
                                    };

                                    Grid.SetRow(scroll, 0);
                                    Grid.SetRow(search, 1);
                                    content.Children.Add(scroll);
                                    content.Children.Add(search);
                                    App.MainPage.Popup.Children.Add(new Popup
                                    {
                                        Content = content,
                                        Title = Utils.LocString("Dialog/ExportInfo").Replace("%chat%", json.peer_id.ToString()).Replace("%date%", json.export_time.ToDateTime().ToString("f"))
                                    });
                                }
                                loading.Hide();
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine(e);
                    }
                });
            }
        }

        [Bindable]
        public class ViewerWithSearch : Grid
        {
            public ViewerWithSearch(List<Message> messages, bool loadAll = false)
            {
                this.RowDefinitions.Add(new RowDefinition { Height = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Auto) });
                this.RowDefinitions.Add(new RowDefinition { Height = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Star) });

                var topgrid = new Grid();
                topgrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Star) });
                topgrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Auto) });
                topgrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Auto) });
                topgrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Auto) });

                var searchbar = new TextBox
                {
                    PlaceholderText = Utils.LocString("Dialog/TextBoxPlaceholder"),
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
                    Margin = new Windows.UI.Xaml.Thickness(5),
                };
                var calendar = new CalendarDatePicker
                {
                    Margin = new Windows.UI.Xaml.Thickness(0, 5, 2.5, 5)
                };
                var button = new Button
                {
                    Content = new TextBlock { Text = Utils.LocString("Search") },
                    Margin = new Windows.UI.Xaml.Thickness(0, 5, 5, 5),
                    CornerRadius = new Windows.UI.Xaml.CornerRadius(5)
                };

                Grid.SetColumn(searchbar, 0);
                Grid.SetColumn(calendar, 1);
                Grid.SetColumn(button, 2);
                topgrid.Children.Add(searchbar);
                topgrid.Children.Add(calendar);
                topgrid.Children.Add(button);

                var holder = new ScrollViewer
                {
                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch,
                    Width = 600,
                    Height = 500
                };

                Grid.SetRow(topgrid, 0);
                Grid.SetRow(holder, 1);
                this.Children.Add(topgrid);
                this.Children.Add(holder);

                button.Click += (a, b) => Search();
                searchbar.PreviewKeyDown += (a, b) =>
                {
                    if (b.Key == Windows.System.VirtualKey.Enter) Search();
                };
                searchbar.TextChanged += (a, b) =>
                {
                    if (searchbar.Text == null || searchbar.Text.Length == 0) Search();
                };
                holder.ViewChanged += (a, b) =>
                {
                    if (holder.Content is Viewer viewer)
                    {
                        if (holder.VerticalOffset >= holder.ScrollableHeight) viewer.LoadMessages(250);
                    }
                };

                if (loadAll)
                {
                    var all = new List<Message>();
                    all.AddRange(messages);
                    holder.Content = new Viewer(all)
                    {
                        HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch
                    };
                }

                void Search()
                {
                    var text = searchbar.Text;
                    var calend = calendar.Date;

                    Task.Factory.StartNew(() =>
                    {
                        var result = new List<Message>();
                        if (text?.Length > 0) result.AddRange(messages.Where(i => i.Text != null && Regex.IsMatch(i.Text, text, RegexOptions.IgnoreCase)));
                        if (calend.HasValue)
                        {
                            var date = calend.Value.DateTime.Date;
                            if (result.Count > 0)
                            {
                                result.RemoveAll(i => i.Date.ToDateTime().Date != date.Date);
                            }
                            else result.AddRange(messages.Where(i => i.Date.ToDateTime().Date == date.Date));
                        }
                        System.Diagnostics.Debug.WriteLine(result.Count);

                        App.UILoop.AddAction(new UITask
                        {
                            Action = () =>
                            {
                                if (loadAll && result.Count == 0) result.AddRange(messages);
                                holder.Content = new Viewer(result)
                                {
                                    HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch
                                };
                                holder.ChangeView(null, 0, null);
                            }
                        });
                    });
                }
            }
        }

        [Bindable]
        private class Viewer : ItemsControl
        {
            private List<Message> Messages;
            private bool _loading = false;

            public Viewer(List<Message> messages)
            {
                this.Margin = new Windows.UI.Xaml.Thickness(10);
                this.Messages = messages;

                var users = messages.Select(i => i.FromId).Where(i => i > 0).Distinct().ToList();
                var groups = messages.Select(i => i.FromId).Where(i => i < 0).Select(i => -i).Distinct().ToList();
                if (users.Count > 0) App.VK.Users.Get(users, fields: "photo_200");
                if (groups.Count > 0) App.VK.Groups.GetById(groups, fields: "photo_200");

                this.LoadMessages(500);
            }

            public void LoadMessages(int count)
            {
                if (this._loading) return; else this._loading = true;
                var offset = 0;
                if (count > this.Messages.Count) count = this.Messages.Count;
                while (offset < count)
                {
                    var msg = this.Messages[offset];
                    var message = new MessageBox(msg, msg.PeerId, true);

                    if (this.Items.Count > 0 && this.Items.Last() is MessageBox prev)
                    {
                        if (prev.Message.Bubble.Message.Date.ToDateTime().Date != msg.Date.ToDateTime().Date)
                        {
                            this.Items.Add(new DateSeparator(msg.Date.ToDateTime()));
                        }
                        else
                        {
                            if (prev.Message.Bubble.Message.FromId == msg.FromId)
                            {
                                prev.Message.Ava.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                                message.Message.Bubble.UserName.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            }
                        }
                    }
                    else this.Items.Add(new DateSeparator(msg.Date.ToDateTime()));

                    this.Items.Add(message);
                    offset++;
                }
                this.Messages.RemoveRange(0, count);
                this._loading = false;
            }
        }
    }
}