using Alika.Libs;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Alika.Theme;

namespace Alika.UI
{
    public class Settings
    {
        public delegate void Save();

        public event Save OnSave;

        private Popup popup = new Popup
        {
            Title = Utils.LocString("Settings")
        };

        private StackPanel menu = new StackPanel
        {
            Width = 500
        };

        public Settings()
        {
            this.OnSave += () => this.popup.Hide();

            this.GenerateSettings(App.Settings.GetType().GetProperties().ToList(), 5, App.Settings, "");
            this.Show();
        }

        private void GenerateSettings(List<PropertyInfo> fields, double margin, object parent, string name)
        {
            foreach (var set in fields)
            {
                var finalName = (name.Length > 0 ? name + "." : "") + set.Name;
                var locName = Utils.LocString("Settings/" + finalName.Replace('.', '_').ToUpper());
                if (set.PropertyType.IsClass && set.PropertyType != typeof(string) && !set.PropertyType.IsGenericType)
                {
                    var lname = ThemeHelpers.GetThemedText();
                    lname.Text = locName;
                    lname.FontSize = 15;
                    lname.FontWeight = Windows.UI.Text.FontWeights.SemiBold;
                    lname.Margin = new Thickness(margin, 0, 0, 0);
                    this.menu.Children.Add(lname);
                    this.GenerateSettings(set.PropertyType.GetProperties().ToList(), margin + 15, set.GetValue(parent), finalName);
                }
                else
                {
                    var grid = new Grid
                    {
                        Margin = new Thickness(margin, 5, 5, 5)
                    };
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    var value = set.GetValue(parent);
                    FrameworkElement input = null;
                    if (set.PropertyType == typeof(int))
                    {
                        input = new NumberBox
                        {
                            Value = (int)value,
                            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Hidden,
                            PlaceholderText = finalName
                        };
                        this.OnSave += () =>
                        {
                            set.SetValue(parent, int.Parse((input as NumberBox).Text));
                            App.Settings.CallUpdateEvent(finalName);
                        };
                    }
                    else if (set.PropertyType == typeof(string))
                    {
                        input = new ThemedTextBox
                        {
                            Text = (string)value,
                            PlaceholderText = finalName
                        };
                        this.OnSave += () =>
                        {
                            set.SetValue(parent, (input as TextBox).Text);
                            App.Settings.CallUpdateEvent(finalName);
                        };
                    }
                    else if (set.PropertyType.IsGenericType)
                    {
                        input = new TextBox
                        {
                            Text = String.Join(",", value as List<string>),
                            PlaceholderText = finalName
                        };
                        this.OnSave += () =>
                        {
                            set.SetValue(parent, (input as TextBox).Text.Split(",").ToList());
                            App.Settings.CallUpdateEvent(finalName);
                        };
                    }
                    else if (set.PropertyType == typeof(bool))
                    {
                        input = new ContentControl
                        {
                            Content = new ToggleSwitch
                            {
                                IsOn = (bool)value
                            }
                        };
                        this.OnSave += () =>
                        {
                            set.SetValue(parent, ((input as ContentControl).Content as ToggleSwitch).IsOn);
                            App.Settings.CallUpdateEvent(finalName);
                        };
                    }

                    if (input != null)
                    {
                        var ntext = ThemeHelpers.GetThemedText();
                        ntext.Text = locName;
                        ntext.VerticalAlignment = VerticalAlignment.Center;
                        ntext.Margin = new Thickness(5);
                        grid.Children.Add(ntext);
                        input.Margin = new Thickness(10, 0, 0, 0);
                        input.VerticalAlignment = VerticalAlignment.Center;
                        input.HorizontalAlignment = HorizontalAlignment.Right;
                        Grid.SetColumn(input, 1);
                        grid.Children.Add(input);
                        this.menu.Children.Add(grid);
                    }
                }
            }
        }

        private void Show()
        {
            var content = new Grid();
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            var scroll = new ScrollViewer
            {
                Content = this.menu,
                HorizontalScrollMode = ScrollMode.Disabled,
                VerticalScrollMode = ScrollMode.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scroll, 0);
            content.Children.Add(scroll);
            var savetext = ThemeHelpers.GetThemedText();
            savetext.Text = Utils.LocString("Save");
            var save = new Button
            {
                Content = savetext,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5)
            };
            save.Click += async (a, b) =>
            {
                this.OnSave?.Invoke();
                await ApplicationData.Current.LocalFolder.WriteTextToFileAsync(JsonConvert.SerializeObject(App.Settings), "settings.json");
            };
            Grid.SetRow(save, 1);
            content.Children.Add(save);
            this.popup.Content = content;
            if (App.MainPage is MainPage page)
            {
                page.Popup.Children.Add(this.popup);
            }
            else if (App.LoginPage is LoginPage login)
            {
                login.Popup.Children.Add(popup);
            }
        }
    }
}