using Alika.Libs;
using Alika.Libs.VK;
using Alika.UI;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.Security.Credentials;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;

namespace Alika
{
    public sealed partial class LoginPage : Page
    {
        public delegate void OnLogin();

        public event OnLogin OnSuccesful;

        public LoginPage()
        {
            this.InitializeComponent();

            this.title.Text = Utils.LocString("Login/Title");
            this.number.PlaceholderText = Utils.LocString("Login/NumberPlaceholder");
            this.password.PlaceholderText = Utils.LocString("Login/PasswordPlaceholder");
            this.login.Content = new TextBlock { Text = Utils.LocString("Login/ButtonText") };
            this.settings.Content = new Image
            {
                Source = new SvgImageSource(new Uri(Utils.AssetTheme("settings.svg"))),
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            this.RegisterEvents();
        }

        private void RegisterEvents()
        {
            // Arrow navigation
            this.number.KeyDown += (object sender, KeyRoutedEventArgs e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Down)
                {
                    this.number.Focus(FocusState.Programmatic);
                    this.password.Focus(FocusState.Pointer);
                }
            };
            this.password.KeyDown += (object sender, KeyRoutedEventArgs e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    this.password.Focus(FocusState.Programmatic);
                    this.LoginClick(null, null);
                }
                else if (e.Key == Windows.System.VirtualKey.Up)
                {
                    this.password.Focus(FocusState.Programmatic);
                    this.number.Focus(FocusState.Pointer);
                }
            };
            // Validating number
            this.number.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                if (this.number.Text.Length > 0)
                {
                    if (!Regex.IsMatch(this.number.Text.Last().ToString(), @"[\+\d]"))
                    {
                        this.number.Text = this.number.Text.Substring(0, this.number.Text.Length - 1);
                        this.number.SelectionStart = this.number.Text.Length;
                    }
                }
            };
        }

        private async void LoginClick(object sender, RoutedEventArgs e)
        {
            if (this.number.Text.Length == 0)
            {
                await new MessageDialog(Utils.LocString("Login/ErrorNoNumber"), Utils.LocString("Error")).ShowAsync();
            }
            else
            {
                if (this.password.Password.Length == 0)
                {
                    await new MessageDialog(Utils.LocString("Login/ErrorNoPassword"), Utils.LocString("Error")).ShowAsync();
                }
                else
                {
                    this.Login(this.number.Text, this.password.Password);
                }
            }
        }

        private void OpenSettings(object sender, RoutedEventArgs e) => new Settings();

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
            string response = http.Post(request).Content;
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
                    else await new MessageDialog("Unknown auth error occured: " + (string)parsed["error_description"], "Error!").ShowAsync();
                }
                else
                {
                    var vault = new PasswordVault();
                    vault.Add(new PasswordCredential(App.appName, "default", (string)parsed["access_token"]));
                    this.OnSuccesful?.Invoke();
                }
            }
            else await new MessageDialog("Check your internet connection", "Error!").ShowAsync();
        }

        /// <summary>
        /// Dialog when captcha needed
        /// </summary>
        public class Captcha : ContentDialog
        {
            public Grid content;
            public Image img;

            public TextBox text = new TextBox
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
        public class CodeDialog : ContentDialog
        {
            public Grid content;

            public TextBox text = new TextBox
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

                TextBlock desc = new TextBlock
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