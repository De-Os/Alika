using Alika.Libs;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Helpers;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace Alika
{
    public class Theme
    {
        public delegate void ThemeChangedEvent();

        public ThemeChangedEvent ThemeChanged;

        public string Current;

        public ThemeData.ColorsData Colors;

        public bool IsDark;

        public Theme()
        {
            var listener = new ThemeListener();
            listener.ThemeChanged += async (a) =>
            {
                if (!await ApplicationData.Current.LocalFolder.FileExistsAsync("theme.json"))
                {
                    _ = this.LoadDefaultTheme(a.CurrentTheme == ApplicationTheme.Dark);
                }
            };
        }

        public void LoadTheme(string data)
        {
            var json = JsonConvert.DeserializeObject<ThemeData>(data);

            if (json != null)
            {
                var colors = json.Colors;
                if (colors?.accent != null && colors?.acrylic != null && colors?.main != null)
                {
                    this.Current = data;
                    this.Colors = colors;
                    this.IsDark = json.IsDark;

                    this.ReloadTheme();
                }
            }
        }

        public async Task LoadDefaultTheme(bool dark)
        {
            var folder = ApplicationData.Current.LocalFolder;
            await folder.CreateFolderAsync("themes", CreationCollisionOption.OpenIfExists);
            if (await folder.FileExistsAsync("theme.json"))
            {
                this.LoadTheme(await folder.ReadTextFromFileAsync("theme.json"));
                return;
            }
            this.LoadTheme(File.ReadAllText(Utils.AppPath((dark ? "dark" : "light") + "_theme.json")));
            return;
        }

        public void ReloadTheme()
        {
            this.Colors.Main.ApplyToResource(
                        "TextControlBackgroundFocused",
                        "TextControlBackgroundPointerOver",
                        "CalendarDatePickerBackgroundFocused",
                        "CalendarDatePickerBackground",
                        "CalendarDatePickerBackgroundPressed"
                        );
            this.Colors.Text.Default.ApplyToResource(
                "SystemControlHighlightAltBaseHighBrush",
                "TextControlForegroundFocused",
                "CalendarDatePickerForeground"
                );
            this.Colors.Accent.ApplyToResource(
                "TextControlButtonForegroundPressed",
                "CalendarDatePickerBackgroundPointerOver",
                "CalendarDatePickerTextForegroundSelected",
                "CalendarDatePickerBorderBrush"
                );
            this.Colors.SubAccent.ApplyToResource(
                "TextControlButtonForegroundPointerOver",
                "CalendarDatePickerBorderBrushPointerOver",
                "CalendarDatePickerBorderBrushPressed"
                );
            this.Colors.Acrylic.ApplyToResource(
                "FlyoutPresenterBackground"
                );

            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonForegroundColor = this.Colors.Accent;

            this.ThemeChanged?.Invoke();
        }

        [Bindable]
        public class ThemedFontIcon : FontIcon
        {
            public ThemedFontIcon(ThemeHelpers.TextTypes? type = null)
            {
                UpdateColor();
                App.Theme.ThemeChanged += UpdateColor;

                void UpdateColor() => this.Foreground = new SolidColorBrush(type.HasValue ? type.Value.TypeColor() : App.Theme.Colors.Accent);
            }
        }

        [Bindable]
        public class ThemedMenuFlyout : MenuFlyout
        {
            public ThemedMenuFlyout() => this.MenuFlyoutPresenterStyle = ThemedAcrylicBrush.GetInStyle(typeof(MenuFlyoutPresenter));
        }

        [Bindable]
        public class ThemedMenuFlyoutItem : MenuFlyoutItem
        {
            public ThemedMenuFlyoutItem()
            {
                UpdateColor();
                App.Theme.ThemeChanged += UpdateColor;

                void UpdateColor() => this.Foreground = new SolidColorBrush(App.Theme.Colors.Text.Default);
            }
        }

        [Bindable]
        public class ThemedFlyout : Flyout
        {
            public ThemedFlyout()
            {
                //
            }
        }

        [Bindable]
        public class ThemedAcrylicBrush : AcrylicBrush
        {
            public enum ColorType
            {
                Accent,
                SubAccent,
                Acrylic,
                Main,
                Contrast,
                Message
            }

            public ThemedAcrylicBrush(ColorType colorType = ColorType.Acrylic)
            {
                this.TintOpacity = 0.7;

                UpdateColor();
                App.Theme.ThemeChanged += UpdateColor;

                void UpdateColor() => this.TintColor = colorType.TypeColor();
            }

            public static Style GetInStyle(Type target, DependencyProperty property = null)
            {
                if (property == null) property = Control.BackgroundProperty;
                var s = new Style
                {
                    TargetType = typeof(MenuFlyoutPresenter)
                };
                s.Setters.Add(new Setter(property, new ThemedAcrylicBrush
                {
                    BackgroundSource = AcrylicBackgroundSource.Backdrop
                }
                        ));
                return s;
            }
        }

        [Bindable]
        public class ThemedTextBox : TextBox
        {
            public ThemedTextBox()
            {
                UpdateColors();
                App.Theme.ThemeChanged += UpdateColors;

                void UpdateColors()
                {
                    this.Background = new SolidColorBrush(App.Theme.Colors.Main);
                    this.PlaceholderForeground = new SolidColorBrush(App.Theme.Colors.Text.Default);
                    this.Foreground = new SolidColorBrush(App.Theme.Colors.Text.Default);
                    this.SelectionHighlightColor = new SolidColorBrush(App.Theme.Colors.SubAccent);
                }
            }
        }

        [Bindable]
        public class ThemedButton : Button
        {
            public ThemedButton()
            {
                UpdateColors();
                App.Theme.ThemeChanged += UpdateColors;

                void UpdateColors()
                {
                    this.Foreground = new SolidColorBrush(App.Theme.Colors.Main);
                }
            }
        }

        public class ThemeData
        {
            [JsonProperty("name")]
            public string Name;

            [JsonProperty("author")]
            public string Author;

            [JsonProperty("is_dark")]
            public bool IsDark = false;

            [JsonProperty("colors")]
            public ColorsData Colors = new ColorsData();

            public class ColorsData
            {
                [JsonProperty("accent")]
                public string accent;

                [JsonIgnore]
                public Color Accent { get => this.accent.ToColor(); }

                [JsonProperty("subaccent")]
                public string subaccent;

                [JsonIgnore]
                public Color SubAccent { get => this.subaccent.ToColor(); }

                [JsonProperty("acrylic")]
                public string acrylic;

                [JsonIgnore]
                public Color Acrylic { get => this.acrylic.ToColor(); }

                [JsonProperty("main")]
                public string main;

                [JsonIgnore]
                public Color Main { get => this.main.ToColor(); }

                [JsonProperty("contrast")]
                public string contrast;

                [JsonIgnore]
                public Color Contrast { get => this.contrast.ToColor(); }

                [JsonProperty("message")]
                public MessageColors Message;

                [JsonProperty("text")]
                public TextColors Text;

                [JsonIgnore]
                public SolidColorBrush Transparent => new SolidColorBrush(new Color
                {
                    A = 0,
                    R = 0,
                    G = 0,
                    B = 0
                });

                public struct TextColors
                {
                    [JsonProperty("default")]
                    public string _default;

                    public Color Default { get => this._default.ToColor(); }

                    [JsonProperty("inverted")]
                    public string _inverted;

                    public Color Inverted { get => this._inverted.ToColor(); }
                }

                public struct MessageColors
                {
                    [JsonProperty("out")]
                    public string _out;

                    public Color Out { get => this._out.ToColor(); }

                    [JsonProperty("in")]
                    public string _in;

                    public Color In { get => this._in.ToColor(); }
                }
            }
        }
    }

    public static class ThemeHelpers
    {
        public enum TextTypes
        {
            Default,
            Inverted,
            Message,
            Link
        }

        public static TextBlock GetThemedText(TextTypes type = TextTypes.Default)
        {
            var text = new TextBlock();

            UpdateColor();
            App.Theme.ThemeChanged += UpdateColor;

            return text;

            void UpdateColor() => text.Foreground = new SolidColorBrush(type.TypeColor());
        }

        public static Hyperlink GetHyperlink()
        {
            var link = new Hyperlink();
            UpdateColors();
            App.Theme.ThemeChanged += UpdateColors;
            return link;

            void UpdateColors() => link.Foreground = new SolidColorBrush(App.Theme.Colors.Accent);
        }

        public static Run GetRun(TextTypes type = TextTypes.Default)
        {
            var text = new Run();

            UpdateColor();
            App.Theme.ThemeChanged += UpdateColor;

            return text;

            void UpdateColor() => text.Foreground = new SolidColorBrush(type.TypeColor());
        }

        public static Color TypeColor(this TextTypes type)
        {
            switch (type)
            {
                case TextTypes.Default: return App.Theme.Colors.Text.Default;
                case TextTypes.Inverted: return App.Theme.Colors.Text.Inverted;
                case TextTypes.Link: return App.Theme.Colors.Accent;
                default: return App.Theme.Colors.Main;
            }
        }

        public static Color TypeColor(this Theme.ThemedAcrylicBrush.ColorType type)
        {
            switch (type)
            {
                case Theme.ThemedAcrylicBrush.ColorType.Accent: return App.Theme.Colors.Accent;
                case Theme.ThemedAcrylicBrush.ColorType.Acrylic: return App.Theme.Colors.Acrylic;
                case Theme.ThemedAcrylicBrush.ColorType.Contrast: return App.Theme.Colors.Contrast;
                case Theme.ThemedAcrylicBrush.ColorType.SubAccent: return App.Theme.Colors.SubAccent;
                default: return App.Theme.Colors.Contrast;
            }
        }

        public static void ApplyToResource(this Color color, params string[] names)
        {
            foreach (var name in names)
            {
                if (Application.Current.Resources[name] is SolidColorBrush brush)
                {
                    brush.Color = color;
                }
                else if (Application.Current.Resources[name] is AcrylicBrush oldAcryl)
                {
                    oldAcryl.TintColor = color;
                    oldAcryl.FallbackColor = color;
                    oldAcryl.TintOpacity = 0.7;
                }
                else if (Application.Current.Resources[name] is Microsoft.UI.Xaml.Media.AcrylicBrush acryl)
                {
                    acryl.TintColor = color;
                    acryl.FallbackColor = color;
                    acryl.TintOpacity = 0.7;
                }
            }
        }
    }
}