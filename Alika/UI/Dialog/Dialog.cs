using Alika.Libs;
using Alika.Libs.VK.Responses;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using static Alika.Theme;

namespace Alika.UI.Dialog
{
    /// <summary>
    /// Dialog grid
    /// </summary>
    [Windows.UI.Xaml.Data.Bindable]
    public partial class Dialog : Grid
    {
        public int PeerId;
        public TopMenu TopMenu;
        public MessagesList MessagesList;
        public Grid BottomMenu = new Grid();

        public Grid AttachGrid = new Grid
        {
            MaxHeight = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        public ContentControl ReplyGrid = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };

        public Grid StickerSuggestions = new Grid
        {
            Height = 100,
            Background = new ThemedAcrylicBrush
            {
                BackgroundSource = AcrylicBackgroundSource.Backdrop
            },
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Collapsed,
            CornerRadius = new CornerRadius(10, 10, 0, 0)
        };

        public Grid BottomButtonsGrid = new Grid();
        public Button SendButton = new Buttons.Send();
        public Button Stickers = new Buttons.Stickers();

        public ThemedTextBox SendText = new ThemedTextBox
        {
            PlaceholderText = Utils.LocString("Dialog/TextBoxPlaceholder"),
            AcceptsReturn = true,
            MaxHeight = 150,
            Margin = new Thickness(5, 10, 5, 10),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        public Button AttachButton = new Buttons.Attachment();

        public Dialog(int peer_id)
        {
            this.PeerId = peer_id;

            this.Render();

            this.RegisterEvents();
        }

        public void Render()
        {
            this.TopMenu = new TopMenu(this.PeerId);
            this.MessagesList = new MessagesList(this.PeerId);
            this.BottomMenu.Transitions.Add(new EntranceThemeTransition { IsStaggeringEnabled = true });

            this.Children.Add(new BlurView
            {
                TopMenu = this.TopMenu,
                Content = this.MessagesList,
                BottomMenu = this.BottomMenu,
            });

            this.SendText.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            this.SendText.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

            this.BottomMenu.RowDefinitions.Add(new RowDefinition());
            this.BottomMenu.RowDefinitions.Add(new RowDefinition());
            this.BottomMenu.RowDefinitions.Add(new RowDefinition());

            ScrollViewer scroll = new ScrollViewer
            {
                Content = this.AttachGrid,
                VerticalScrollMode = ScrollMode.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(scroll, 0);
            Grid.SetRow(this.ReplyGrid, 1);
            Grid.SetRow(this.BottomButtonsGrid, 2);

            this.BottomMenu.Children.Add(scroll);
            this.BottomMenu.Children.Add(this.ReplyGrid);
            this.BottomMenu.Children.Add(this.BottomButtonsGrid);

            this.BottomButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.BottomButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.BottomButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.BottomButtonsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            Grid.SetColumn(this.AttachButton, 0);
            Grid.SetColumn(this.SendText, 1);
            Grid.SetColumn(this.Stickers, 2);
            Grid.SetColumn(this.SendButton, 3);

            this.BottomButtonsGrid.Children.Add(this.AttachButton);
            this.BottomButtonsGrid.Children.Add(this.SendText);
            this.BottomButtonsGrid.Children.Add(this.SendButton);
            this.BottomButtonsGrid.Children.Add(this.Stickers);

            this.StickerSuggestions.Children.Add(new ScrollViewer
            {
                VerticalScrollMode = ScrollMode.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            });
        }

        [Windows.UI.Xaml.Data.Bindable]
        public class ReplyMessage : Grid
        {
            public Message Message;
            public bool CrossEnabled { get; set; } = true;
            public double LineWidth { get; set; } = 5;

            public ReplyMessage(Message msg)
            {
                this.Message = msg;

                this.Margin = new Thickness(10, 5, 10, 5);
                this.Transitions.Add(new EntranceThemeTransition { IsStaggeringEnabled = true });

                this.Loaded += (a, b) => this.Load();
            }

            private void Load()
            {
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                this.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

                var rect = new Windows.UI.Xaml.Shapes.Rectangle
                {
                    Fill = new SolidColorBrush(App.Theme.Colors.Accent),
                    Width = this.LineWidth,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                App.Theme.ThemeChanged += () => rect.Fill = new SolidColorBrush(App.Theme.Colors.Accent);
                Grid.SetColumn(rect, 0);
                this.Children.Add(rect);

                var text = new Grid();
                text.RowDefinitions.Add(new RowDefinition());
                text.RowDefinitions.Add(new RowDefinition());
                Grid.SetColumn(text, 1);

                var name = ThemeHelpers.GetThemedText();
                name.VerticalAlignment = VerticalAlignment.Bottom;
                name.TextTrimming = TextTrimming.CharacterEllipsis;
                name.Text = App.Cache.GetName(this.Message.FromId);
                name.FontWeight = FontWeights.Bold;
                Grid.SetRow(name, 0);

                var msg = ThemeHelpers.GetThemedText();
                msg.VerticalAlignment = VerticalAlignment.Top;
                msg.TextTrimming = TextTrimming.CharacterEllipsis;
                msg.Text = this.Message.ToCompactText();
                Grid.SetRow(msg, 1);

                text.Children.Add(msg);
                text.Children.Add(name);

                if (this.CrossEnabled)
                {
                    var cross = new Button
                    {
                        Background = App.Theme.Colors.Transparent,
                        Content = new ThemedFontIcon
                        {
                            Glyph = Glyphs.Close,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    cross.Click += (a, b) => (this.Parent as ContentControl).Content = null;
                    Grid.SetColumn(cross, 2);
                    this.Children.Add(cross);
                }

                this.Children.Add(text);
            }
        }
    }
}