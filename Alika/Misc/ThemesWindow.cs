using Alika.Libs;
using Alika.UI;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using static Alika.Theme;

namespace Alika.Misc
{
    [Bindable]
    public class ThemesWindow : Popup
    {
        private StackPanel _content = new StackPanel();

        public ThemesWindow()
        {
            this.Title = Utils.LocString("Themes/Name");
            this.Content = new ScrollViewer
            {
                HorizontalScrollMode = ScrollMode.Disabled,
                Content = this._content
            };
            LoadCurrent();
            App.MainPage.Popup.Children.Add(this);
        }

        private async void LoadCurrent()
        {
            var folder = ApplicationData.Current.LocalFolder;
            var themesFolder = await folder.GetFolderAsync("themes");

            foreach (var file in await themesFolder.GetFilesAsync())
            {
                AddElement(await FileIO.ReadTextAsync(file), file.Name);
            }

            AddElement(File.ReadAllText("light_theme.json"));
            AddElement(File.ReadAllText("dark_theme.json"));

            void AddElement(string file, string filename = null)
            {
                var element = new ThemeElement(file, filename);
                element.ThemeChanged += () =>
                {
                    foreach (var item in this._content.Children)
                    {
                        if (item is ThemeElement el && el != element) el.Icon.Glyph = Glyphs.Star;
                    }
                };
                this._content.Children.Add(element);
            }

            var btnstack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10)
            };
            var istack = new StackPanel { Orientation = Orientation.Horizontal };
            istack.Children.Add(new ThemedFontIcon
            {
                Glyph = Glyphs.Import,
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 15
            });
            var text = ThemeHelpers.GetThemedText();
            text.Text = Utils.LocString("Themes/Import");
            text.FontWeight = FontWeights.SemiBold;
            istack.Children.Add(text);
            var import = new Button
            {
                Background = App.Theme.Colors.Transparent,
                Content = istack,
                Margin = new Thickness(0, 0, 10, 0)
            };
            import.Click += (a, b) => this.ImportTheme();
            btnstack.Children.Add(import);

            var astack = new StackPanel { Orientation = Orientation.Horizontal };
            astack.Children.Add(new ThemedFontIcon
            {
                Glyph = Glyphs.Add,
                Margin = new Thickness(0, 0, 5, 0),
                FontSize = 15
            });
            var atext = ThemeHelpers.GetThemedText();
            atext.Text = Utils.LocString("Themes/Create");
            atext.FontWeight = FontWeights.SemiBold;
            astack.Children.Add(atext);
            var add = new Button
            {
                Background = App.Theme.Colors.Transparent,
                Content = astack
            };
            add.Click += (a, b) =>
            {
                var editor = new ThemeEditorWindow(App.Theme.Colors);
                editor.ThemeSaved += (a, b) =>
                {
                    var element = new ThemeElement(a, filename: b, enable: true);
                    element.ThemeChanged += () =>
                    {
                        foreach (var item in this._content.Children)
                        {
                            if (item is ThemeElement el && el != element) el.Icon.Glyph = Glyphs.Star;
                        }
                    };
                    this._content.Children.Insert(0, element);
                };
            };
            btnstack.Children.Add(add);
            this._content.Children.Add(btnstack);
        }

        private async void ImportTheme()
        {
            var filepicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads
            };
            filepicker.FileTypeFilter.Add(".at");
            if (await filepicker.PickSingleFileAsync() is StorageFile file)
            {
                try
                {
                    var text = await FileIO.ReadTextAsync(file);
                    var json = JsonConvert.DeserializeObject<Theme.ThemeData>(text);
                    if (json != null)
                    {
                        var themesFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("themes");
                        var toWrite = await themesFolder.CreateFileAsync(Path.GetRandomFileName());
                        await FileIO.WriteTextAsync(toWrite, text);
                        var element = new ThemeElement(text);
                        element.ThemeChanged += () =>
                        {
                            foreach (var item in this._content.Children)
                            {
                                if (item is ThemeElement el && el != element) el.Icon.Glyph = Glyphs.Star;
                            }
                        };
                        this._content.Children.Insert(this._content.Children.Count - 1, element);
                    }
                }
                catch
                {
                    await new MessageDialog(Utils.LocString("Error")).ShowAsync();
                }
            }
        }

        [Bindable]
        private class ThemeElement : Grid
        {
            public ThemedFontIcon Icon = new ThemedFontIcon
            {
                Glyph = Glyphs.Star,
                Margin = new Thickness(10, 5, 10, 5)
            };

            public delegate void ThemeChangedEvent();

            public event ThemeChangedEvent ThemeChanged;

            public ThemeElement(string data, string filename = null, bool enable = false)
            {
                var theme = JsonConvert.DeserializeObject<ThemeData>(data);

                this.Width = 400;

                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                var content = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };
                Grid.SetColumn(content, 0);
                content.Children.Add(this.Icon);

                var text = ThemeHelpers.GetThemedText();
                text.FontWeight = FontWeights.Bold;
                text.FontSize = 15;
                text.Text = Utils.LocString("Themes/Description").Replace("%name%", theme.Name).Replace("%author%", theme.Author);
                text.VerticalAlignment = VerticalAlignment.Center;
                content.Children.Add(text);

                var apply = new Button
                {
                    Background = App.Theme.Colors.Transparent,
                    Content = content,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                this.Children.Add(apply);

                if (filename?.Length > 0)
                {
                    var delete = new Button
                    {
                        Content = new ThemedFontIcon
                        {
                            Glyph = Glyphs.Delete,
                            Margin = new Thickness(5, 5, 5, 5),
                            VerticalAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        Background = App.Theme.Colors.Transparent
                    };
                    Grid.SetColumn(delete, 1);
                    this.Children.Add(delete);
                    delete.Click += async (a, b) =>
                    {
                        var themesFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("themes");
                        var file = await themesFolder.GetFileAsync(filename);
                        await file.DeleteAsync();
                        if (this.Icon.Glyph == Glyphs.FilledStar)
                        {
                            file = await ApplicationData.Current.LocalFolder.GetFileAsync("theme.json");
                            await file.DeleteAsync();
                            await App.Theme.LoadDefaultTheme(new UISettings().GetColorValue(UIColorType.Background).ToString() == "#FF000000");
                        }
                        this.Visibility = Visibility.Collapsed;
                    };
                }

                if (App.Theme.Current == data) this.Icon.Glyph = Glyphs.FilledStar;

                if (enable) EnableTheme();
                apply.Click += (a, b) =>
                {
                    if (this.Icon.Glyph == Glyphs.Star)
                    {
                        EnableTheme();
                    }
                };
                App.Theme.ThemeChanged += () => this.Icon.Glyph = App.Theme.Current == data ? Glyphs.FilledStar : Glyphs.Star;

                async void EnableTheme()
                {
                    if (!await ApplicationData.Current.LocalFolder.FileExistsAsync("theme.json")) await ApplicationData.Current.LocalFolder.CreateFileAsync("theme.json");
                    await FileIO.WriteTextAsync(await ApplicationData.Current.LocalFolder.GetFileAsync("theme.json"), data);
                    App.Theme.LoadTheme(data);
                    this.Icon.Glyph = Glyphs.FilledStar;
                    this.ThemeChanged?.Invoke();
                };
            }
        }
    }

    [Bindable]
    public class ThemeEditorWindow : Popup
    {
        public delegate void ThemeSavedEvent(string data, string file_name);

        public ThemeSavedEvent ThemeSaved;

        private StackPanel _content = new StackPanel
        {
            MinWidth = 500
        };

        private ThemeData TempData = new ThemeData();

        public ThemeEditorWindow(ThemeData.ColorsData current)
        {
            this.Title = Utils.LocString("Themes/NewTheme");

            this.Content = new ScrollViewer
            {
                HorizontalScrollMode = ScrollMode.Disabled,
                Content = this._content
            };
            this.LoadElements(current);

            App.MainPage.Popup.Children.Add(this);
        }

        private void LoadElements(ThemeData.ColorsData colors)
        {
            var namebox = new ThemedTextBox
            {
                PlaceholderText = Utils.LocString("Themes/NewThemeName"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 5)
            };
            namebox.TextChanged += (a, b) => this.TempData.Name = namebox.Text;
            this._content.Children.Add(namebox);

            var authorbox = new ThemedTextBox
            {
                PlaceholderText = Utils.LocString("Themes/NewThemeAuthor"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 5)
            };
            authorbox.TextChanged += (a, b) => this.TempData.Author = authorbox.Text;
            this._content.Children.Add(authorbox);

            var dboxText = ThemeHelpers.GetThemedText();
            dboxText.Text = Utils.LocString("Themes/NewThemeIsDark");
            var darkbox = new CheckBox
            {
                Content = dboxText,
                Margin = new Thickness(0, 5, 0, 10)
            };
            darkbox.Checked += (a, b) => this.TempData.IsDark = true;
            darkbox.Unchecked += (a, b) => this.TempData.IsDark = false;
            this._content.Children.Add(darkbox);

            CreateColorBtn("Themes/NewThemeColorAccent", colors.Accent).ColorChanged += (a) => this.TempData.Colors.accent = a.ToHex();
            CreateColorBtn("Themes/NewThemeColorSubAccent", colors.SubAccent).ColorChanged += (a) => this.TempData.Colors.subaccent = a.ToHex();
            CreateColorBtn("Themes/NewThemeColorAcrylic", colors.Acrylic).ColorChanged += (a) => this.TempData.Colors.acrylic = a.ToHex();
            CreateColorBtn("Themes/NewThemeColorMain", colors.Main).ColorChanged += (a) => this.TempData.Colors.main = a.ToHex();
            CreateColorBtn("Themes/NewThemeColorContrast", colors.Contrast).ColorChanged += (a) => this.TempData.Colors.contrast = a.ToHex();
            CreateColorBtn("Themes/NewThemeColorMessage", colors.Message).ColorChanged += (a) => this.TempData.Colors.message = a.ToHex();
            CreateColorBtn("Themes/NewThemeColorTextDefault", colors.Text.Default).ColorChanged += (a) => this.TempData.Colors.Text._default = a.ToHex();
            CreateColorBtn("Themes/NewThemeColorTextInverted", colors.Text.Inverted).ColorChanged += (a) => this.TempData.Colors.Text._inverted = a.ToHex();

            var savetext = ThemeHelpers.GetThemedText();
            savetext.Text = Utils.LocString("Save");
            var savebtn = new Button
            {
                Margin = new Thickness(0, 10, 0, 0),
                Content = savetext,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = App.Theme.Colors.Transparent
            };
            this._content.Children.Add(savebtn);
            savebtn.Click += (a, b) => this.Save();

            PickerPopup CreateColorBtn(string loc, Color background)
            {
                loc = Utils.LocString(loc);
                var popup = new PickerPopup()
                {
                    Title = loc
                };
                var text = ThemeHelpers.GetThemedText();
                text.FontWeight = FontWeights.SemiBold;
                text.Text = loc;
                var btn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Content = text,
                    Margin = new Thickness(0, 0, 0, 5),
                };
                UpdateBtnColors(background);
                popup.ColorChanged += UpdateBtnColors;
                btn.Click += (a, b) => App.MainPage.Popup.Children.Add(popup);
                this._content.Children.Add(btn);
                return popup;

                void UpdateBtnColors(Color color)
                {
                    btn.Background = new SolidColorBrush(color);
                    text.Foreground = new SolidColorBrush(color.Invert());
                }
            }
        }

        private async void Save()
        {
            try
            {
                CheckValues();

                var filepicker = new FileSavePicker
                {
                    SuggestedFileName = this.TempData.Name + "_by_" + this.TempData.Author + ".at",
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };
                filepicker.FileTypeChoices.Add("Alika theme", new List<string>() { ".at" });

                var file = await filepicker.PickSaveFileAsync();
                if (file != null)
                {
                    var json = JsonConvert.SerializeObject(this.TempData);
                    await FileIO.WriteTextAsync(file, json);

                    var themesFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("themes");
                    var filename = Path.GetRandomFileName();
                    var toWrite = await themesFolder.CreateFileAsync(filename);
                    await FileIO.WriteTextAsync(toWrite, json);

                    this.ThemeSaved?.Invoke(json, filename);
                    this.Hide();
                }
            }
            catch (Exception exc)
            {
                await new MessageDialog(exc.Message, Utils.LocString("Error")).ShowAsync();
            }

            void CheckValues()
            {
                if (this.TempData.Name?.Length > 0)
                {
                    if (this.TempData.Author?.Length > 0)
                    {
                        if (this.TempData.Colors.accent?.Length > 0
                            && this.TempData.Colors.acrylic?.Length > 0
                            && this.TempData.Colors.contrast?.Length > 0
                            && this.TempData.Colors.main?.Length > 0
                            && this.TempData.Colors.message?.Length > 0
                            && this.TempData.Colors.Text._default?.Length > 0
                            && this.TempData.Colors.Text._inverted?.Length > 0
                            )
                        {
                            return;
                        }
                        else Error("NewThemeWrongColors");
                    }
                    else Error("NewThemeWrongAuthor");
                }
                else Error("NewThemeWrongName");

                static void Error(string loc) => throw new Exception(Utils.LocString("Themes/" + loc));
            }
        }

        [Bindable]
        private class PickerPopup : Popup
        {
            public delegate void ColorChangedEvent(Color color);

            public ColorChangedEvent ColorChanged;

            public PickerPopup()
            {
                var picker = new ColorPicker
                {
                    IsMoreButtonVisible = true,
                    Margin = new Thickness(10)
                };
                picker.ColorChanged += (a, b) => this.ColorChanged?.Invoke(picker.Color);
                this.Content = new ScrollViewer
                {
                    Content = picker,
                    HorizontalScrollMode = ScrollMode.Disabled
                };
            }
        }
    };
}