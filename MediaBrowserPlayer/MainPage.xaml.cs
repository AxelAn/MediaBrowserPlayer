/*
Smart Player for Emby
Copyright (C) 2014  Blue Bit Solutions

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>
*/

using SmartPlayer.Classes;
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

namespace SmartPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private AppSettings appSettings = new AppSettings();
        private bool pageLoaded = false;

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
            // extract the luser and security token info
            bool canSetRemote = false;

            // extractte user data
            try
            {
                AppSettings settings = new AppSettings();

                string[] userIdCall = { "if(typeof Dashboard != 'undefined'){Dashboard.getCurrentUserId()}" };
                string userIdData = await mainWebPage.InvokeScriptAsync("eval", userIdCall);

                settings.SaveUserId(userIdData);

                string[] accessTokenCall = { "if(typeof ApiClient != 'undefined'){ApiClient.accessToken()}" };
                string accessTokenData = await mainWebPage.InvokeScriptAsync("eval", accessTokenCall);

                settings.SaveAccessToken(accessTokenData);

                if(string.IsNullOrEmpty(userIdData) == false && string.IsNullOrEmpty(accessTokenData) == false)
                {
                    canSetRemote = true;
                }
            }
            catch (Exception exp)
            {
                MetroEventSource.Log.Info("Error getting user data : " + exp.ToString());
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
                    string itemObj = "{\"deviceName\":\"Smart Player\",\"id\":\"" + session.Id + "\",\"name\":\"BMP\",\"playableMediaTypes\":\"Video\",\"supportedCommands\":\"PlayNow\"}";

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
            if (pageLoaded == false)
            {
                pageLoaded = true;
                LoadMainPage();
            }

            TileNotifications tnu = new TileNotifications();
            tnu.UpdateTileNotifications();
        }

        public void LoadMainPage()
        {
            ServerListItem server = appSettings.GetServer();
            if (server == null)
            {
                SettingsFlyoutMain settingFlyout = new SettingsFlyoutMain();
                settingFlyout.Show();

                return;
            }

            App.ReInitializeWebSocket();

            try
            {
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

        private void OnNavigate(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            Uri destination = args.Uri;
            
            // block if not to media portal
            ServerListItem server = appSettings.GetServer();
            if (server == null || destination.Host != server.host)
            {
                args.Cancel = true;
                App.AddNotification(new Notification() { Title = "Navigation Error", Message = "Remote sites not allowed" });
            }

        }

        private async void AppBarButton_Back(object sender, RoutedEventArgs e)
        {
            try
            {
                mainWebPage.GoBack();
            }
            catch(Exception)
            { }
        }

        private void AppBarButton_Refresh(object sender, RoutedEventArgs e)
        {
            try
            {
                mainWebPage.Refresh();
            }
            catch (Exception)
            { }
        }

        private void AppBarButton_Home(object sender, RoutedEventArgs e)
        {
            LoadMainPage();
        }

    }
}
