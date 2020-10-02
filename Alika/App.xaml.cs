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
using Windows.UI.Xaml.Navigation;

namespace Alika
{
    sealed partial class App : Application
    {
        public static bool systemDarkTheme = new UISettings().GetColorValue(UIColorType.Background).ToString() == "#FF000000"; // Bool for detecting system theme
        public static Caching cache = new Caching(); // Global caching
        public static Config settings;
        public static string appName = "alika.vk"; // Appname for password vault
        public static UITasksLoop UILoop = new UITasksLoop();
        public static VK vk; // VK lib
        public static MainPage main_page; // Main page
        public static LongPoll lp; // LongPoll
        public static LoginPage login_page; // Login page
        public static double TitleBarHeight;

        public App()
        {
            ImageCache.Instance.CacheDuration = TimeSpan.FromDays(7); // TODO: Make setting to change cache duration
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected async override void OnLaunched(LaunchActivatedEventArgs e)
        {
            await Config.EnsureCreated();

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
                        App.login_page = new LoginPage();
                        login_page.OnSuccesful += this.LoadMain;
                        rootFrame.Content = App.login_page;
                    }
                }
                Window.Current.Activate();

                ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
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
            App.vk = new VK(new VK.Settings
            {
                ApiVer = App.settings.vk.api,
                Token = vault.Retrieve(App.appName, "default").Password,
                ApiDomain = App.settings.vk.domain
            });
            this.LoadProxy();
            App.lp = vk.GetLP();
            App.main_page = new MainPage();

            App.settings.OnSettingUpdated += (a) =>
            {
                switch (a)
                {
                    case "vk.domain":
                        App.vk.domain = App.settings.vk.domain;
                        break;
                    case "vk.api":
                        App.vk.api_ver = App.settings.vk.api;
                        break;
                    case "proxy":
                        this.LoadProxy();
                        break;
                }
            };

            (Window.Current.Content as Frame).Content = App.main_page;
        }
        private async void LoadProxy()
        {
            if (App.settings?.proxy == null) return;
            if (!App.settings.proxy.enabled)
            {
                if (App.vk != null) App.vk.proxy = null;
                if (App.lp != null) App.lp.proxy = null;
                return;
            }
            var testClient = new RestClient(App.settings.vk.ping_url)
            {
                Proxy = App.settings.proxy.ToWebProxy(),
                Timeout = 15000
            };
            try
            {
                var response = testClient.Get(new RestRequest());
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    if (App.vk != null) App.vk.proxy = App.settings.proxy.ToWebProxy();
                    if (App.lp != null) App.lp.proxy = App.settings.proxy.ToWebProxy();
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
