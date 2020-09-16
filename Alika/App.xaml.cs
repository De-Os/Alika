using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Longpoll;
using Alika.UI;
using Microsoft.Toolkit.Uwp.UI;
using Newtonsoft.Json;
using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Security.Credentials;
using Windows.UI;
using Windows.UI.Text;
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
        public static Config settings = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Utils.AppPath("settings.json")));
        public static string appName = "alika.vk"; // Appname for password vault
        public static UITasksLoop UILoop = new UITasksLoop();
        public static VK vk; // VK lib
        public static MainPage main_page; // Main page
        public LongPoll lp; // LongPoll
        public LoginPage login_page; // Login page

        public App()
        {
            ImageCache.Instance.CacheDuration = TimeSpan.FromDays(7); // TODO: Make setting to change cache duration
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
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
                        this.login_page = new LoginPage();
                        login_page.OnSuccesful += this.LoadMain;
                        rootFrame.Content = this.login_page;
                    }
                    CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                    ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
                    titleBar.ButtonBackgroundColor = Colors.Transparent;
                    titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                }
                Window.Current.Activate();
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
            App.vk = new VK(vault.Retrieve(App.appName, "default").Password, App.settings.vk.api, new CaptchaSettings
            {
                Title = new TextBlock { Text = Utils.LocString("Login/CaptchaTitle"), FontWeight = FontWeights.Bold },
                Placeholder = Utils.LocString("Login/CaptchaPlaceholder"),
                Button = new TextBlock { Text = Utils.LocString("Login/OkButton") }
            });

            App.main_page = new MainPage();

            this.lp = vk.GetLP();
            this.lp.Event += App.main_page.OnLpUpdates;
            (Window.Current.Content as Frame).Content = App.main_page;
        }
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
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
