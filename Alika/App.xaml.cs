using Alika.Libs;
using Alika.Libs.VK;
using Alika.Libs.VK.Longpoll;
using Microsoft.Toolkit.Uwp.UI;
using System;
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
        public static string appName = "alika.vk"; // Appname for password vault
        public static PasswordVault vault = new PasswordVault(); // Password vault instance
        public static VK vk; // VK lib
        public LongPoll lp; // LongPoll
        public MainPage main_page; // Main page
        public LoginPage login_page; // Login page

        public App()
        {
            ImageCache.Instance.CacheDuration = TimeSpan.FromDays(7); // TODO: Make setting to change cache duration
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    if (App.vault.RetrieveAll().Count > 0)
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
        public void LoadMain()
        {
            App.vk = new VK(App.vault.Retrieve(App.appName, "default").Password, "5.122", new CaptchaSettings
            {
                Title = new TextBlock { Text = Utils.LocString("Login/CaptchaTitle"), FontWeight = FontWeights.Bold },
                Placeholder = Utils.LocString("Login/CaptchaPlaceholder"),
                Button = new TextBlock { Text = Utils.LocString("Login/OkButton") }
            });

            this.main_page = new MainPage();

            this.lp = vk.GetLP();
            this.lp.Event += this.main_page.OnLpUpdates;
            (Window.Current.Content as Frame).Content = this.main_page;
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
