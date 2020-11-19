using Alika.Libs;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static Alika.Theme;

namespace Alika.UI
{
    [Bindable]
    public class Popup : Grid
    {
        private Grid _content = new Grid
        {
            Background = new ThemedAcrylicBrush
            {
                BackgroundSource = AcrylicBackgroundSource.Backdrop
            },
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            MaxHeight = 600
        };

        private TextBlock _title;

        public Brush ContentBackground
        {
            get
            {
                return this._content.Background;
            }
            set
            {
                this._content.Background = value;
            }
        }

        public string Title
        {
            get
            {
                return this._title.Text;
            }
            set
            {
                this._title.Visibility = value != null && value.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                this._title.Text = value;
            }
        }

        public FrameworkElement Content
        {
            get
            {
                if (this._content.Children.Count > 0)
                {
                    return this._content.Children[1] as FrameworkElement;
                }
                else return null;
            }
            set
            {
                if (this._content.Children.Count > 1 && this._content.Children[1] is FrameworkElement prev) this._content.Children.Remove(prev);
                value.Transitions.Add(new EntranceThemeTransition { IsStaggeringEnabled = true });
                Grid.SetRow(value, 1);
                this._content.Children.Add(value);
            }
        }

        public bool IsPointerOnContent { get; private set; } = false;

        public Popup()
        {
            this.Children.Add(this._content);

            this._title = ThemeHelpers.GetThemedText();
            this._title.Margin = new Thickness(5, 0, 0, 5);
            this._title.FontWeight = FontWeights.Bold;
            this._title.TextTrimming = TextTrimming.CharacterEllipsis;
            this._title.FontSize = 20;
            this._title.Visibility = Visibility.Collapsed;

            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this._content.HorizontalAlignment = HorizontalAlignment.Center;
            this._content.VerticalAlignment = VerticalAlignment.Center;

            this._content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            this._content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(this._title, 0);
            this._content.Children.Add(this._title);

            this.Background = new SolidColorBrush(new Windows.UI.Color
            {
                A = 125,
                R = (byte)0,
                G = (byte)0,
                B = (byte)0
            });

            this.Transitions.Add(new PopupThemeTransition());

            this._content.PointerEntered += (a, b) => this.IsPointerOnContent = true;
            this._content.PointerExited += (a, b) => this.IsPointerOnContent = false;

            this.PointerPressed += (a, b) => { if (!this.IsPointerOnContent) this.Hide(); };

            Window.Current.CoreWindow.KeyDown += this.OnKeyDown;
        }

        public void OnKeyDown(object s, KeyEventArgs e)
        {
            if (e.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                e.Handled = true;
                this.Hide(true);
            }
        }

        public void Hide(bool checkLast = false)
        {
            var parent = VisualTreeHelper.GetParent(this);
            if (parent == null) return;
            if (parent is Panel p)
            {
                if (checkLast && p.Children.Last() != this) return; // Don't hide popup if it is not last
                p.Children.Remove(this);
            }
            if (parent is ContentControl c)
            {
                c.Content = null;
            }

            Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
            // TODO: Other...
        }

        [Bindable]
        public class Menu : StackPanel
        {
            public Menu(string title = null)
            {
                if (title != null)
                {
                    var titletext = ThemeHelpers.GetThemedText();
                    titletext.Margin = new Thickness(5, 0, 0, 2);
                    titletext.Text = Utils.LocString(title);
                    titletext.FontWeight = FontWeights.SemiLight;
                    titletext.FontSize = 12.5;
                    this.Children.Add(titletext);
                }
            }

            [Bindable]
            public class Element : Button
            {
                public Element(string title, string icon, RoutedEventHandler action)
                {
                    this.HorizontalAlignment = HorizontalAlignment.Stretch;
                    this.HorizontalContentAlignment = HorizontalAlignment.Left;
                    this.Background = App.Theme.Colors.Transparent;
                    this.Padding = new Thickness(10);
                    this.CornerRadius = new CornerRadius(5);

                    var content = new Grid();
                    content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var image = new ThemedFontIcon
                    {
                        Glyph = icon
                    };
                    image.Width = 20;
                    image.Height = 20;
                    image.Margin = new Thickness(0, 0, 10, 0);
                    image.VerticalAlignment = VerticalAlignment.Center;

                    var text = ThemeHelpers.GetThemedText();
                    text.VerticalAlignment = VerticalAlignment.Center;
                    text.FontWeight = FontWeights.SemiBold;
                    text.TextTrimming = TextTrimming.CharacterEllipsis;
                    text.Text = Utils.LocString(title);

                    Grid.SetColumn(image, 0);
                    Grid.SetColumn(text, 1);

                    content.Children.Add(image);
                    content.Children.Add(text);

                    this.PointerEntered += (a, b) => Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Hand, 0);
                    this.PointerExited += (a, b) => Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);
                    this.PointerPressed += (a, b) => Window.Current.CoreWindow.PointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);

                    if (action != null) this.Click += action;

                    this.Content = content;
                }
            }
        }
    }
}