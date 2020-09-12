using System.Linq;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Alika.UI
{
    public class Popup : Grid
    {
        private Grid _content = new Grid
        {
            Background = new AcrylicBrush
            {
                FallbackColor = Coloring.Transparent.Percent(100).Color,
                TintColor = Coloring.Transparent.Percent(100).Color,
                TintOpacity = 0.7,
                BackgroundSource = AcrylicBackgroundSource.Backdrop
            },
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10)
        };

        private TextBlock _title = new TextBlock
        {
            Margin = new Thickness(5, 0, 0, 5),
            FontWeight = FontWeights.Bold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontSize = 20,
            Visibility = Visibility.Collapsed
        };

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
                Grid.SetRow(value, 1);
                this._content.Children.Add(value);
            }
        }
        public bool IsPointerOnContent { get; private set; } = false;

        public Popup()
        {
            this.Children.Add(this._content);

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

        public class Separator : Grid
        {
            public Separator()
            {
                this.HorizontalAlignment = HorizontalAlignment.Stretch;
                this.Height = 10;
            }
        }
    }
}
