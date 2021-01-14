using Alika.Libs;
using Alika.Libs.VK;
using Alika.Misc;
using Alika.UI;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using static Alika.Theme;

namespace Alika
{
    public sealed partial class LoginPage : Page
    {
        public delegate void OnLogin();

        public event OnLogin OnSuccesful;

        private readonly TextBlock Title;
        private readonly TextBox Number;
        private readonly TextBox Password;
        private readonly Button LoginButton;
        private readonly Button LoginByTokenButton;
        private readonly Button SettingButton;
        private readonly Button ThemesButton;

        public LoginPage()
        {
            this.InitializeComponent();

            this.Background = new ThemedAcrylicBrush();

            this.Title = ThemeHelpers.GetThemedText();
            this.Title.Text = Utils.LocString("Login/Title");
            this.Title.FontWeight = FontWeights.Bold;
            this.Title.FontSize = 49;
            this.titleHolder.Content = this.Title;

            this.Number = new ThemedTextBox
            {
                PlaceholderText = Utils.LocString("Login/NumberPlaceholder"),
                Width = 500
            };
            this.numHolder.Content = this.Number;

            this.Password = new ThemedTextBox
            {
                PlaceholderText = Utils.LocString("Login/PasswordPlaceholder"),
                Width = 500
            };
            this.pswdHolder.Content = this.Password;

            var loginTxt = ThemeHelpers.GetThemedText();
            loginTxt.Text = Utils.LocString("Login/ButtonText");
            this.LoginButton = new ThemedButton
            {
                Content = loginTxt,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(this.LoginButton, 0);
            this.Buttons.Children.Add(this.LoginButton);

            this.LoginByTokenButton = new ThemedButton
            {
                Content = new ThemedFontIcon
                {
                    Glyph = Glyphs.Fingerprint
                },
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(5, 0, 5, 0)
            };
            Grid.SetColumn(this.LoginByTokenButton, 1);
            this.Buttons.Children.Add(this.LoginByTokenButton);

            this.ThemesButton = new ThemedButton
            {
                Content = new ThemedFontIcon
                {
                    Glyph = Glyphs.Marker
                },
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(5, 0, 5, 0)
            };
            Grid.SetColumn(this.ThemesButton, 2);
            this.Buttons.Children.Add(this.ThemesButton);

            this.SettingButton = new ThemedButton
            {
                Content = new ThemedFontIcon
                {
                    Glyph = Glyphs.Settings
                },
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Right,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(this.SettingButton, 3);
            this.Buttons.Children.Add(this.SettingButton);

            this.RegisterEvents();
        }

        private void RegisterEvents()
        {
            // Arrow navigation
            this.Number.KeyDown += (object sender, KeyRoutedEventArgs e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Down)
                {
                    this.Number.Focus(FocusState.Programmatic);
                    this.Password.Focus(FocusState.Pointer);
                }
            };
            this.Password.KeyDown += (object sender, KeyRoutedEventArgs e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    this.Password.Focus(FocusState.Programmatic);
                    this.LoginClick(null, null);
                }
                else if (e.Key == Windows.System.VirtualKey.Up)
                {
                    this.Password.Focus(FocusState.Programmatic);
                    this.Number.Focus(FocusState.Pointer);
                }
            };
            // Validating number
            this.Number.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (this.Number.Text.Length > 0)
                {
                    if (!Regex.IsMatch(this.Number.Text.Last().ToString(), @"[\+\d]"))
                    {
                        this.Number.Text = this.Number.Text.Substring(0, this.Number.Text.Length - 1);
                        this.Number.SelectionStart = this.Number.Text.Length;
                    }
                }
            };

            this.LoginButton.Click += this.LoginClick;
            this.LoginByTokenButton.Click += this.TokenClick;
            this.SettingButton.Click += this.OpenSettings;
            this.ThemesButton.Click += (a, b) => new ThemesWindow();
        }

        private async void LoginClick(object sender, RoutedEventArgs e)
        {
            if (this.Number.Text.Length == 0)
            {
                await new MessageDialog(Utils.LocString("Login/ErrorNoNumber"), Utils.LocString("Error")).ShowAsync();
            }
            else
            {
                if (this.Password.Text.Length == 0)
                {
                    await new MessageDialog(Utils.LocString("Login/ErrorNoPassword"), Utils.LocString("Error")).ShowAsync();
                }
                else
                {
                    this.Login(this.Number.Text, this.Password.Text);
                }
            }
        }

        private void OpenSettings(object sender, RoutedEventArgs e) => new Settings();

        private void TokenClick(object sender, RoutedEventArgs e)
        {
            var content = new Grid();
            content.RowDefinitions.Add(new RowDefinition());
            content.RowDefinitions.Add(new RowDefinition());

            var tokenInput = new ThemedTextBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 200,
                Margin = new Thickness(5, 10, 5, 5),
                PlaceholderText = Utils.LocString("Login/TokenPlaceholder")
            };
            var btnText = ThemeHelpers.GetThemedText();
            btnText.Text = Utils.LocString("Login/ButtonText");
            var okBtn = new ThemedButton
            {
                Content = btnText,
                Margin = new Thickness(5, 5, 5, 10),
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(tokenInput, 0);
            Grid.SetRow(okBtn, 1);
            content.Children.Add(tokenInput);
            content.Children.Add(okBtn);

            var popup = new Popup
            {
                Title = Utils.LocString("Login/LoginByToken"),
                Content = content
            };
            okBtn.Click += async (a, b) =>
            {
                if (tokenInput.Text.Length > 0)
                {
                    try
                    {
                        var token = tokenInput.Text;
                        var vk = new VK(new VK.Settings
                        {
                            ApiDomain = App.Settings.vk.domain,
                            Token = token
                        });
                        vk.Messages.GetConversations(count: 1);
                        vk.Friends.Get(count: 1);
                        Thread.Sleep(TimeSpan.FromSeconds(0.7));
                        var vault = new PasswordVault();
                        vault.Add(new PasswordCredential(App.appName, "default", token));
                        this.OnSuccesful?.Invoke();
                    }
                    catch
                    {
                        await new MessageDialog(Utils.LocString("Login/WrongToken"), Utils.LocString("Error")).ShowAsync();
                    }
                }
                else popup.Hide();
            };

            this.Popup.Children.Add(popup);
        }

        private async void Login(string number, string password, string captcha_sid = null, string captcha_key = null, string code = null)
        {
            var http = new RestClient(App.Settings.vk.login.domain);
            if (App.Settings.proxy != null) http.Proxy = App.Settings.proxy.ToWebProxy();
            var request = new RestRequest();
            request.AddParameter("password", password);
            request.AddParameter("grant_type", "password");
            request.AddParameter("client_id", App.Settings.vk.login.client_id);
            request.AddParameter("client_secret", App.Settings.vk.login.client_secret);
            request.AddParameter("username", number);
            request.AddParameter("scope", String.Join(",", App.Settings.vk.login.scope));
            request.AddParameter("v", VK.API_VER);
            request.AddParameter("2fa_supported", 1);
            if (captcha_sid != null)
            {
                request.AddParameter("captcha_sid", captcha_sid);
                request.AddParameter("captcha_key", captcha_key);
            }
            if (code != null) request.AddParameter("code", code);
            var response = http.Post(request).Content;
            if (response != null && response.Length > 0)
            {
                JObject parsed = JObject.Parse(response);
                if (parsed.ContainsKey("error"))
                {
                    string error = (string)parsed["error"];
                    if (error == "need_validation" && parsed.ContainsKey("validation_type"))
                    {
                        CodeDialog codeDialog = new CodeDialog((string)parsed["validation_type"], parsed.ContainsKey("phone_mask") ? (string)parsed["phone_mask"] : null);
                        await codeDialog.ShowAsync();
                        this.Login(number, password, code: codeDialog.text.Text);
                    }
                    else if (error == "need_captcha")
                    {
                        Captcha captcha = new Captcha((string)parsed["captcha_img"]);
                        await captcha.ShowAsync();
                        this.Login(number, password, (string)parsed["captcha_sid"], captcha.text.Text);
                    }
                    else await new MessageDialog((string)parsed["error_description"], Utils.LocString("Error")).ShowAsync();
                }
                else
                {
                    var vault = new PasswordVault();
                    vault.Add(new PasswordCredential(App.appName, "default", (string)parsed["access_token"]));
                    this.OnSuccesful?.Invoke();
                }
            }
            else await new MessageDialog(Utils.LocString("Login/CheckInternet"), Utils.LocString("Error")).ShowAsync();
        }

        /// <summary>
        /// Dialog when captcha needed
        /// </summary>
        [Bindable]
        public class Captcha : ContentDialog
        {
            public Grid content;
            public Image img;

            public TextBox text = new ThemedTextBox
            {
                PlaceholderText = Utils.LocString("Login/CaptchaPlaceholder"),
                Margin = new Thickness(10)
            };

            public Captcha(string url)
            {
                this.Title = new TextBlock { Text = Utils.LocString("Login/CaptchaTitle"), FontWeight = FontWeights.Bold };

                this.content = new Grid();
                this.content.RowDefinitions.Add(new RowDefinition());
                this.content.RowDefinitions.Add(new RowDefinition());
                this.content.RowDefinitions.Add(new RowDefinition());

                this.img = new Image
                {
                    Source = new BitmapImage(new Uri(url))
                };
                Grid.SetRow(this.img, 0);
                this.content.Children.Add(img);

                Grid.SetRow(this.text, 1);
                this.content.Children.Add(this.text);

                Button close = new Button
                {
                    Content = new TextBlock { Text = Utils.LocString("Login/OkButton") },
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(close, 2);
                close.Click += async (object sender, RoutedEventArgs e) =>
                {
                    if (this.text.Text.Length > 0)
                    {
                        this.Hide();
                    }
                    else await new MessageDialog(Utils.LocString("Login/ErrorNoCaptcha"), Utils.LocString("Error")).ShowAsync();
                };
                this.KeyDown += (object s, KeyRoutedEventArgs e) =>
                {
                    if (this.text.Text.Length > 0 && e.Key == Windows.System.VirtualKey.Enter) this.Hide();
                };
                this.content.Children.Add(close);

                this.Content = this.content;
            }
        }

        /// <summary>
        /// Dialog when 2fa needed
        /// </summary>
        [Bindable]
        public class CodeDialog : ContentDialog
        {
            public Grid content;

            public TextBox text = new ThemedTextBox
            {
                PlaceholderText = Utils.LocString("Login/CodePlaceholder"),
                Margin = new Thickness(10)
            };

            public CodeDialog(string type, string phone_mask)
            {
                this.Title = new TextBlock { Text = Utils.LocString("Login/CodeTitle"), FontWeight = FontWeights.Bold };

                this.content = new Grid();
                this.content.RowDefinitions.Add(new RowDefinition());
                this.content.RowDefinitions.Add(new RowDefinition());
                this.content.RowDefinitions.Add(new RowDefinition());

                var desc = new TextBlock
                {
                    Text = Utils.LocString("Login/CodeText").Replace("%auth_app%", Utils.LocString("Login/Code_" + type)).Replace("%number%", phone_mask)
                };
                desc.Text += ":";
                Grid.SetRow(desc, 0);
                this.content.Children.Add(desc);

                Grid.SetRow(this.text, 1);
                this.content.Children.Add(this.text);

                Button close = new Button
                {
                    Content = new TextBlock { Text = Utils.LocString("Login/OkButton") },
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(close, 2);
                close.Click += async (object sender, RoutedEventArgs e) =>
                {
                    if (this.text.Text.Length > 0)
                    {
                        this.Hide();
                    }
                    else await new MessageDialog(Utils.LocString("Login/ErrorNoCode"), Utils.LocString("Error")).ShowAsync();
                };
                this.KeyDown += (object s, KeyRoutedEventArgs e) =>
                {
                    if (this.text.Text.Length > 0 && e.Key == Windows.System.VirtualKey.Enter) this.Hide();
                };

                this.content.Children.Add(close);

                this.Content = this.content;
            }
        }
    }
}