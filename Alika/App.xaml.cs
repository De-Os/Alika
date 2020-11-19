using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Longpoll;
using Alika.UI;
using Microsoft.Toolkit.Uwp.UI;
using RestSharp;
using System;
using System.Net;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Security.Credentials;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Alika
{
    sealed partial class App : Application
    {
        public static Caching Cache = new Caching(); // Global caching
        public static Config Settings;
        public static string appName = "alika.vk"; // Appname for password vault
        public static FontFamily Icons = new FontFamily("Assets/Fonts/icons.ttf#icomoon"); // Custom FontIcons
        public static UITasksLoop UILoop = new UITasksLoop(); // UI Tasks
        public static VK VK; // VK lib
        public static MainPage MainPage; // Main page
        public static LongPoll LP; // LongPoll
        public static LoginPage LoginPage; // Login page
        public static Theme Theme = new Theme(); // Themes!

        public App()
        {
            ImageCache.Instance.CacheDuration = TimeSpan.FromDays(7); // TODO: Make setting to change cache duration
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            await Config.EnsureCreated();
            var theme = new Theme();
            await theme.LoadDefaultTheme(new UISettings().GetColorValue(UIColorType.Background).ToString() == "#FF000000");
            App.Theme = theme;

            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    if (this.DoesPasswordExists())
                    {
                        this.LoadMain();
                    }
                    else
                    {
                        App.LoginPage = new LoginPage();
                        LoginPage.OnSuccesful += this.LoadMain;
                        rootFrame.Content = App.LoginPage;
                    }
                }
                Window.Current.Activate();

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            }
        }

        private bool DoesPasswordExists()
        {
            try
            {
                var vault = new PasswordVault();
                return vault.Retrieve(App.appName, "default") != null;
            }
            catch
            {
                return false;
            }
        }

        public void LoadMain()
        {
            var vault = new PasswordVault();
            App.VK = new VK(new VK.Settings
            {
                Token = vault.Retrieve(App.appName, "default").Password,
                ApiDomain = App.Settings.vk.domain
            });
            this.LoadProxy();
            App.LP = App.VK.GetLP();
            App.MainPage = new MainPage();

            App.Settings.OnSettingUpdated += (a) =>
            {
                switch (a)
                {
                    case "vk.domain":
                        App.VK.Domain = App.Settings.vk.domain;
                        break;

                    case "proxy":
                        this.LoadProxy();
                        break;
                }
            };

            (Window.Current.Content as Frame).Content = App.MainPage;
        }

        private async void LoadProxy()
        {
            if (App.Settings?.proxy == null) return;
            if (!App.Settings.proxy.enabled)
            {
                if (App.VK != null) App.VK.Proxy = null;
                return;
            }
            var testClient = new RestClient(App.Settings.vk.ping_url)
            {
                Proxy = App.Settings.proxy.ToWebProxy(),
                Timeout = 15000
            };
            try
            {
                var response = testClient.Get(new RestRequest());
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (App.VK != null) App.VK.Proxy = App.Settings.proxy.ToWebProxy();
                }
                else
                {
                    await new MessageDialog(response.StatusCode.ToString(), Utils.LocString("Error")).ShowAsync();
                }
            }
            catch (Exception err)
            {
                await new MessageDialog(err.Message, Utils.LocString("Error")).ShowAsync();
            }
        }

        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}