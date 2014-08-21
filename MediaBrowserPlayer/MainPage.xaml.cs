﻿using MediaBrowserPlayer.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MediaBrowserPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private AppSettings appSettings = new AppSettings();
        private bool navigationStarted = false;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;

            this.Loaded += MainPage_Loaded;

            mainWebPage.NavigationStarting += OnNavigate;

            mainWebPage.LoadCompleted += mainWebPage_LoadCompleted;
        }

        private async void mainWebPage_LoadCompleted(object sender, NavigationEventArgs e)
        {
            bool canSetRemote = false;

            // extractte user data from the browser
            try
            {
                AppSettings settings = new AppSettings();

                string[] userIdCall = { "Dashboard.getCurrentUserId()" };
                string userIdData = await mainWebPage.InvokeScriptAsync("eval", userIdCall);

                settings.SaveUserId(userIdData);

                string[] accessTokenCall = { "Dashboard.getAccessToken()" };
                string accessTokenData = await mainWebPage.InvokeScriptAsync("eval", accessTokenCall);

                settings.SaveAccessToken(accessTokenData);

                if(string.IsNullOrEmpty(userIdData) == false && string.IsNullOrEmpty(accessTokenData) == false)
                {
                    canSetRemote = true;
                }
            }
            catch (Exception exp)
            {
                App.AddNotification(new Notification() { Title = "Error Extracting Current User Info", Message = exp.Message });
            }

            if (canSetRemote == false)
            {
                return; // cant set remote so return
            }

            // now set the remote control target
            try
            {
                // try to call set player in client
                ApiClient client = new ApiClient();
                SessionInfo session = await client.GetSessionInfo();

                string userId = null;

                if (session != null)
                {
                    string itemObj = "{\"deviceName\":\"Media Browser Player\",\"id\":\"" + session.Id + "\",\"name\":\"BMP\",\"playableMediaTypes\":\"Video\",\"supportedCommands\":\"PlayNow\"}";

                    string[] args = { "MediaController.setActivePlayer(\"Remote Control\", " + itemObj + ")" };

                    await mainWebPage.InvokeScriptAsync("eval", args);                    
                }
            }
            catch(Exception exp)
            {
                App.AddNotification(new Notification() { Title = "Error Setting Remote Control Target", Message = exp.Message });
            }
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMainPage();
        }

        public void LoadMainPage(bool overRide = false)
        {
            if (navigationStarted == false || overRide)
            {
                navigationStarted = true;

                try
                {
                    string server = appSettings.GetServer();
                    if (server != null)
                    {
                        Uri mb = new Uri("http://" + server + "/mediabrowser");
                        mainWebPage.Navigate(mb);
                    }
                }
                catch (Exception exeption)
                {
                    App.AddNotification(new Notification() { Title = "Error Loading Main Page", Message = exeption.Message });
                }
            }
        }

        private void OnNavigate(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Uri destination = args.Uri;

            // block if not to media portal
            string server = appSettings.GetServerPort();
            if (destination.Host != server)
            {
                args.Cancel = true;
                App.AddNotification(new Notification() { Title = "Navigation Error", Message = "Remote sites not allowed" });
            }

        }

        private async void AppBarButton_Back(object sender, RoutedEventArgs e)
        {
            mainWebPage.GoBack();
        }

        private void AppBarButton_Refresh(object sender, RoutedEventArgs e)
        {
            mainWebPage.Refresh();
        }

        private void AppBarButton_Home(object sender, RoutedEventArgs e)
        {
            LoadMainPage(true);
        }

    }
}
