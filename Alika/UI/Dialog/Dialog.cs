using Alika.Libs;
using Alika.Libs.VK.Responses;
using System;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika.UI.Dialog
{

    /// <summary>
    /// Dialog grid
    /// </summary>
    [Windows.UI.Xaml.Data.Bindable]
    public partial class Dialog : Grid
    {
        public int peer_id { get; set; }
        public TopMenu top_menu;
        public MessagesList MessagesList;
        public Grid bottom_menu = new Grid();
        public Grid attach_grid = new Grid
        {
            MaxHeight = 100,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        public ContentControl reply_grid = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        public Grid stickers_suggestions = new Grid
        {
            Height = 100,
            Background = new AcrylicBrush
            {
                TintColor = Coloring.Transparent.Percent(25).Color,
                TintOpacity = 0.7
            },
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Collapsed,
            CornerRadius = new CornerRadius(10, 10, 0, 0)
        };
        public Grid bottom_buttons_grid = new Grid();
        public Button send_button = new Buttons.Send();
        public Button stickers = new Buttons.Stickers();
        public TextBox send_text = new TextBox
        {
            PlaceholderText = Utils.LocString("Dialog/TextBoxPlaceholder"),
            AcceptsReturn = true,
            MaxHeight = 150,
            Margin = new Thickness(5, 10, 5, 10),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        public Button attach_button = new Buttons.Attachment();

        public Dialog(int peer_id)
        {
            this.peer_id = peer_id;
            if (App.cache.StickersSelector != null) App.cache.StickersSelector.peer_id = this.peer_id;

            this.Render();

            this.RegisterEvents();
        }

        public void Render()
        {
            this.top_menu = new TopMenu(this.peer_id);
            this.MessagesList = new MessagesList(this.peer_id);
            this.bottom_menu.Transitions.Add(new EntranceThemeTransition { IsStaggeringEnabled = true });

            this.Children.Add(new BlurView
            {
                TopMenu = this.top_menu,
                Content = this.MessagesList,
                BottomMenu = this.bottom_menu,
            });

            this.send_text.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            this.send_text.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);

            this.bottom_menu.RowDefinitions.Add(new RowDefinition());
            this.bottom_menu.RowDefinitions.Add(new RowDefinition());
            this.bottom_menu.RowDefinitions.Add(new RowDefinition());

            ScrollViewer scroll = new ScrollViewer
            {
                Content = attach_grid,
                VerticalScrollMode = ScrollMode.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                HorizontalScrollMode = ScrollMode.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            Grid.SetRow(scroll, 0);
            Grid.SetRow(this.reply_grid, 1);
            Grid.SetRow(this.bottom_buttons_grid, 2);

            this.bottom_menu.Children.Add(scroll);
            this.bottom_menu.Children.Add(this.reply_grid);
            this.bottom_menu.Children.Add(this.bottom_buttons_grid);

            this.bottom_buttons_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.bottom_buttons_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            this.bottom_buttons_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            this.bottom_buttons_grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            Grid.SetColumn(this.attach_button, 0);
            Grid.SetColumn(this.send_text, 1);
            Grid.SetColumn(this.stickers, 2);
            Grid.SetColumn(this.send_button, 3);

            this.bottom_buttons_grid.Children.Add(this.attach_button);
            this.bottom_buttons_grid.Children.Add(this.send_text);
            this.bottom_buttons_grid.Children.Add(this.send_button);
            this.bottom_buttons_grid.Children.Add(this.stickers);

            this.stickers_suggestions.Children.Add(new ScrollViewer
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
                    Fill = new SolidColorBrush(new Windows.UI.Color
                    {
                        A = 255,
                        R = 75,
                        G = 119,
                        B = 168
                    }),
                    Width = this.LineWidth,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                Grid.SetColumn(rect, 0);
                this.Children.Add(rect);

                var text = new Grid();
                text.RowDefinitions.Add(new RowDefinition());
                text.RowDefinitions.Add(new RowDefinition());
                Grid.SetColumn(text, 1);

                var name = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Bottom,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Text = App.cache.GetName(this.Message.from_id),
                    FontWeight = FontWeights.Bold
                };
                Grid.SetRow(name, 0);
                var msg = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Top,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Text = this.Message.ToCompactText()
                };
                Grid.SetRow(msg, 1);

                text.Children.Add(msg);
                text.Children.Add(name);

                if (this.CrossEnabled)
                {
                    var cross = new Button
                    {
                        Background = Coloring.Transparent.Full,
                        Content = new Image
                        {
                            Source = new SvgImageSource(new Uri(Utils.AssetTheme("close.svg"))),
                            Height = 20,
                            Width = 20,
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
