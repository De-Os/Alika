using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Alika.Theme;

namespace Alika.UI
{
    [Windows.UI.Xaml.Data.Bindable]
    public class BlurView : Grid
    {
        public ScrollViewer Scroll = new ScrollViewer
        {
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        public FrameworkElement Content
        {
            get
            {
                return this.Scroll.Content as FrameworkElement;
            }
            set
            {
                this.Scroll.Content = value;
            }
        }

        protected Grid _topmenu = new Grid
        {
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        public FrameworkElement TopMenu
        {
            get
            {
                if (this._topmenu.Children.Count > 0) return this._topmenu.Children[0] as FrameworkElement;
                return null;
            }
            set
            {
                this._topmenu.Children.Clear();
                this._topmenu.Children.Add(value);
            }
        }

        protected Grid _bottomMenu = new Grid
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        public FrameworkElement BottomMenu
        {
            get
            {
                if (this._bottomMenu.Children.Count > 0) return this._bottomMenu.Children[0] as FrameworkElement;
                return null;
            }
            set
            {
                this._bottomMenu.Children.Clear();
                this._bottomMenu.Children.Add(value);
            }
        }

        public BlurView()
        {
            this.Children.Add(this.Scroll);
            this.Children.Add(this._topmenu);
            this.Children.Add(this._bottomMenu);
            this.UpdateColors();

            this._topmenu.SizeChanged += (a, b) => this.ChangeScrollPadding(b.NewSize.Height, true);
            this._bottomMenu.SizeChanged += (a, b) => this.ChangeScrollPadding(b.NewSize.Height, false);
        }

        private void ChangeScrollPadding(double height, bool top)
        {
            var content = this.Scroll.Content as FrameworkElement;
            var margin = content.Margin;
            if (top) margin.Top = height; else margin.Bottom = height;
            content.Margin = margin;
        }

        protected void UpdateColors()
        {
            var brush = new ThemedAcrylicBrush
            {
                BackgroundSource = AcrylicBackgroundSource.Backdrop
            };
            this._topmenu.Background = brush;
            this._bottomMenu.Background = brush;
        }
    }
}