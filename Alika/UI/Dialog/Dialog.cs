using Alika.Libs;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Alika.UI.Dialog
{

    /// <summary>
    /// Dialog grid
    /// </summary>
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
        public Grid stickers_suggestions = new Grid
        {
            Height = 100,
            Background = Coloring.Transparent.Percent(25),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Visibility = Visibility.Collapsed,
            CornerRadius = new CornerRadius(10)
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
            this.top_menu = new UI.Dialog.TopMenu(this.peer_id);
            this.MessagesList = new MessagesList(this.peer_id);

            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60, GridUnitType.Auto) });

            Grid.SetRow(this.top_menu, 0);
            Grid.SetRow(this.MessagesList, 1);
            Grid.SetRow(this.bottom_menu, 2);

            this.Children.Add(this.top_menu);
            this.Children.Add(this.MessagesList);
            this.Children.Add(this.bottom_menu);

            this.send_text.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            this.send_text.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

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
            Grid.SetRow(this.bottom_buttons_grid, 2);

            this.bottom_menu.Children.Add(scroll);
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
    }
}
