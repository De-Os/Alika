using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Alika.UI.Dialog
{
    public partial class Dialog
    {
        public void RegisterEvents()
        {
            this.send_button.Click += this.Send;
            this.send_text.PreviewKeyDown += this.TextBoxPreviewKeyDown;
            this.send_text.PreviewKeyDown += this.TextPaste;
            this.send_text.TextChanged += this.StickerSuggestion;
            this.attach_button.Click += this.AttachSelection;
            this.Loaded += (object s, RoutedEventArgs e) => this.send_text.Focus(FocusState.Pointer);
            this.stickers.Flyout = new Flyout { Content = App.cache.StickersSelector };
        }

        // Attach photo from Clipboard
        private async void TextPaste(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.V && Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down))
            {
                if (this.attach_grid.Children.Count < Limits.Messages.MAX_ATTACHMENTS)
                {
                    DataPackageView dataPackageView = Clipboard.GetContent();
                    if (dataPackageView.Contains(StandardDataFormats.Bitmap))
                    {
                        var stream = await dataPackageView.GetBitmapAsync();
                        using (var truestream = await stream.OpenReadAsync())
                        {
                            byte[] bytes = truestream.AsStream().ReadToBytes();
                            try
                            {
                                this.AttachUploadByteImage(bytes);
                                e.Handled = true;
                            }
                            catch (Exception error)
                            {
                                await new MessageDialog(Utils.LocString("FilesChoose/UploadError").Replace("%error%", error.Message), Utils.LocString("Error")).ShowAsync();
                                e.Handled = false;
                            }
                        }
                    }
                    else e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                    await new MessageDialog(Utils.LocString("FilesChoose/NoMoreAttachs").Replace("%count%", Limits.Messages.MAX_ATTACHMENTS.ToString()), Utils.LocString("Error")).ShowAsync();
                }
            }
            else e.Handled = false;
        }

        // Some key controls
        public void TextBoxPreviewKeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                // Send if any sticker choosen on stickers suggestion
                if (this.stickers_suggestions.Visibility == Visibility.Visible)
                {
                    e.Handled = true;
                    foreach (StickerSuggestionHolder holder in ((this.stickers_suggestions.Children[0] as ScrollViewer).Content as Grid).Children)
                    {
                        if (holder.Selected)
                        {
                            var reply_id = 0;
                            if (this.reply_grid.Content is ReplyMessage reply)
                            {
                                this.reply_grid.Content = null;
                                reply_id = reply.Message.id;
                            }
                            Task.Factory.StartNew(() => App.vk.Messages.Send(this.peer_id, sticker_id: holder.Sticker.sticker_id, reply_to: reply_id));
                            this.send_text.Text = "";
                            return;
                        }
                    }
                }
                // Send || add line to TextBox
                if (Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down))
                {
                    this.send_text.Text += Environment.NewLine;
                    this.send_text.SelectionStart = this.send_text.Text.Length;
                }
                else this.Send(null, null);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Tab)
            {
                // Tab focusing on stickers suggestion
                if (this.stickers_suggestions.Visibility == Visibility.Visible)
                {
                    ScrollViewer scroll = this.stickers_suggestions.Children[0] as ScrollViewer;
                    Grid stickers = scroll.Content as Grid;
                    bool selection = true;
                    for (int x = 0; x < stickers.Children.Count; x++)
                    {
                        StickerSuggestionHolder holder = stickers.Children[x] as StickerSuggestionHolder;
                        if (holder.Selected)
                        {
                            selection = false;
                            scroll.ChangeView(0, null, null); // Scroll to first sticker when unfocused
                        }
                        holder.Selected = false;
                    }
                    (stickers.Children[0] as StickerSuggestionHolder).Selected = selection;
                    e.Handled = true;
                }
                else e.Handled = false;
            }
            else if (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right)
            {
                if (this.stickers_suggestions.Visibility == Visibility.Visible)
                {
                    /*
                     * Arrow navigation on stickers suggestion
                     * Animation on scrolling disabled due to bugs on fast switching
                     */
                    Grid stickers = (this.stickers_suggestions.Children[0] as ScrollViewer).Content as Grid;
                    double scroll = 0;
                    for (int x = 0; x < stickers.Children.Count; x++)
                    {
                        StickerSuggestionHolder holder = stickers.Children[x] as StickerSuggestionHolder;
                        if (holder.Selected)
                        {
                            ScrollViewer scroller = this.stickers_suggestions.Children[0] as ScrollViewer;
                            if (e.Key == VirtualKey.Left)
                            {
                                if (x > 0)
                                {
                                    (stickers.Children[x - 1] as StickerSuggestionHolder).Selected = true;
                                    holder.Selected = !holder.Selected;

                                    try { scroller.ChangeView(holder.ActualWidth * (x - 1), null, null, true); } catch { }
                                }
                            }
                            else
                            {
                                if (x < stickers.Children.Count - 1)
                                {
                                    (stickers.Children[x + 1] as StickerSuggestionHolder).Selected = true;
                                    holder.Selected = !holder.Selected;
                                    try { scroller.ChangeView(holder.ActualWidth * x, null, null, true); } catch { }
                                }
                            }
                            e.Handled = true;
                            return;
                        }
                        scroll += holder.ActualWidth;
                    }
                    e.Handled = false;
                }
                else e.Handled = false;
            }
            else e.Handled = false;
        }

        // Focusing on message TextBox when char/digit key pressed
        public void PreviewKeyEvent(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (this.send_text.FocusState == FocusState.Unfocused && e.Key != VirtualKey.Enter)
            {
                bool shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
                if (e.Key.IsCharKey(shift) != null)
                {
                    this.send_text.Focus(FocusState.Keyboard);
                    this.send_text.Text += e.Key.GetCharsFromKeys(shift, Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down));
                    this.send_text.SelectionStart = this.send_text.Text.Length;
                }
                e.Handled = true;
            }
            else e.Handled = false;
        }

        // Send message
        public void Send(object sender, RoutedEventArgs e)
        {
            string text = this.send_text.Text.Replace("\r\n", "\n").Replace("\r", "\n"); // i hate \r\n
            int reply = 0;
            if (this.reply_grid.Content is ReplyMessage rep) reply = rep.Message.id;
            this.send_text.Text = "";
            this.reply_grid.Content = null;
            List<string> attachments = new List<String>();
            if (this.attach_grid.Children.Count > 0)
            {
                for (int x = 0; x < this.attach_grid.Children.Count; x++)
                {
                    attachments.Add((this.attach_grid.Children[x] as MessageAttachment.Uploaded).Attach);
                }
                this.attach_grid.Children.Clear();
            }
            Task.Factory.StartNew(() =>
            {
                if (text.Length > 0 || attachments.Count > 0)
                {
                    string temptext;
                    // Send multiple messages if text length > 4096
                    while (text.Length > 0 || attachments.Count > 0)
                    {
                        temptext = text.Substring(0, text.Length > Limits.Messages.MAX_LENGTH ? Limits.Messages.MAX_LENGTH : text.Length);
                        try { App.vk.Messages.Send(this.peer_id, text: temptext, attachments: attachments.Count > 0 ? attachments : null, reply_to: reply); } catch { break; }
                        text = text.Substring(temptext.Length);
                        attachments = new List<string>();
                    }
                }
            });

        }

        // Load new messages when user scrolled to top
        public void OnScroll(object s, ScrollViewerViewChangedEventArgs e) // TODO: Fix it
        {
            /*if (e.IsIntermediate)
            {
                if (this.msg_scroll.VerticalOffset == 0)
                {
                    List<Message> messages = App.vk.Messages.GetHistory(this.peer_id, start_message_id: (this.messages.Items[0] as MessageBox).message.textBubble.message.id).messages;
                    messages.ForEach((Message msg) => this.AddMessage(msg, false));
                    this.msg_scroll.ChangeView(null, 0, null, true);
                }
            }*/
        }

        // Attachment button selection
        public async void AttachSelection(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                ViewMode = PickerViewMode.Thumbnail
            };
            picker.FileTypeFilter.Add("*");
            var files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                if (files.Count <= Limits.Messages.MAX_ATTACHMENTS)
                {
                    foreach (var file in files)
                    {
                        this.AttachUpload(file);
                    }
                }
                else await new MessageDialog(Utils.LocString("FilesChoose/NoMoreAttachs").Replace("%count%", Limits.Messages.MAX_ATTACHMENTS.ToString()), Utils.LocString("Error")).ShowAsync();
            }
        }

        // Upload image from clipboard
        public void AttachUploadByteImage(byte[] bytes)
        {
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Wait, 0);
            var photo = App.vk.Messages.UploadPhoto(bytes, this.peer_id);
            var upl = new MessageAttachment.Uploaded(photo);
            Grid.SetColumn(upl, this.attach_grid.ColumnDefinitions.Count);
            upl.Remove.Click += (object s, RoutedEventArgs e) => this.attach_grid.Children.Remove(upl);
            this.attach_grid.ColumnDefinitions.Add(new ColumnDefinition());
            this.attach_grid.Children.Add(upl);
            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
        }

        // Upload image from FileOpenpicker
        public async void AttachUpload(StorageFile file)
        {

            try
            {
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Wait, 0);
                MessageAttachment.Uploaded upl = null;
                if (Limits.Messages.PHOTO_TYPES.Contains(file.FileType))
                {
                    var photo = await App.vk.Messages.UploadPhoto(file, this.peer_id);
                    upl = new MessageAttachment.Uploaded(pic: photo);
                }
                else
                {
                    var doc = await App.vk.Messages.UploadDocument(file, this.peer_id) as Attachment.Document;
                    upl = new MessageAttachment.Uploaded(doc: doc);
                }
                Grid.SetColumn(upl, this.attach_grid.ColumnDefinitions.Count);
                upl.Remove.Click += (object s, RoutedEventArgs e) => this.attach_grid.Children.Remove(upl);
                this.attach_grid.ColumnDefinitions.Add(new ColumnDefinition());
                this.attach_grid.Children.Add(upl);
                Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            }
            catch (Exception error)
            {
                await new MessageDialog(Utils.LocString("FilesChoose/UploadError").Replace("%error%", error.Message), Utils.LocString("Error")).ShowAsync();
            }

        }

        // Sticker suggestion by word
        public void StickerSuggestion(object sender, TextChangedEventArgs e)
        {
            if (App.cache.StickerDictionary == null) return;
            string text = this.send_text.Text;
            int peer_id = this.peer_id;
            if (App.cache.StickerDictionary.ContainsKey(text) && App.cache.StickerDictionary[text].Count > 0)
            {
                try
                {
                    Grid grid = new Grid();
                    grid.Transitions.Add(new EntranceThemeTransition { IsStaggeringEnabled = true });
                    App.cache.StickerDictionary[text].ForEach((sticker) =>
                    {
                        var holder = new StickerSuggestionHolder(sticker);
                        holder.PointerPressed += (a, m) =>
                        {
                            var reply_id = 0;
                            if (this.reply_grid.Content is ReplyMessage reply)
                            {
                                this.reply_grid.Content = null;
                                reply_id = reply.Message.id;
                            }
                            this.send_text.Text = "";
                            Task.Factory.StartNew(() => App.vk.Messages.Send(peer_id, sticker_id: sticker.sticker_id, reply_to: reply_id));
                        };
                        Grid.SetColumn(holder, grid.ColumnDefinitions.Count);
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                        grid.Children.Add(holder);
                    });
                    App.UILoop.AddAction(new UITask
                    {
                        Action = () =>
                        {
                            (this.stickers_suggestions.Children[0] as ScrollViewer).Content = grid;
                            (this.stickers_suggestions.Children[0] as ScrollViewer).ChangeView(0, null, null, true);
                            this.stickers_suggestions.Margin = new Thickness(this.attach_button.ActualWidth + this.attach_button.Margin.Left + this.attach_button.Margin.Right + this.send_text.Margin.Left, 0, 0, this.bottom_menu.ActualHeight);
                            this.stickers_suggestions.Width = this.send_text.ActualWidth;
                            this.stickers_suggestions.Visibility = Visibility.Visible;
                        },
                        Priority = CoreDispatcherPriority.High
                    });
                }
                catch (Exception err)
                {
                    System.Diagnostics.Debug.WriteLine(err.Message);
                }
            }
            else
            {
                App.UILoop.AddAction(new UITask
                {
                    Action = () => this.stickers_suggestions.Visibility = Visibility.Collapsed // Hide if word suggestions not found
                });
            }
        }
    }
}
