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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace SmartPlayer
{
    public sealed partial class SettingsFlyoutMain : SettingsFlyout
    {
        public SettingsFlyoutMain()
        {
            this.InitializeComponent();
        }

        private void SettingsFlyout_Unloaded(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            string original_server_name = (string)localSettings.Values["device_name"];
            localSettings.Values["device_name"] = setting_device_name.Text.Trim();

            //
            // load server list
            //
            ObservableCollection<ServerListItem> servers = null;
            //List<ServerListItem> servers = null;
            if(serverList.ItemsSource != null)
            {
                servers = serverList.ItemsSource as ObservableCollection<ServerListItem>;
            }
            if(servers == null)
            {
                servers = new ObservableCollection<ServerListItem>();
            }

            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<ServerListItem>));
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, servers);
            sw.Flush();
            localSettings.Values["server_list"] = sw.ToString();

            int selectedServer = serverList.SelectedIndex;
            localSettings.Values["server_selected"] = selectedServer;

            // reload page and web socket
            Frame rootFrame = Window.Current.Content as Frame;
            var p = rootFrame.Content as MainPage;
            if (p != null)
            {
                p.LoadMainPage();
            }

        }

        private string GetSetting(string name)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey(name))
            {
                return ((string)localSettings.Values[name]).Trim();
            }
            else
            {
                return "";
            }
        }

        private void SettingsFlyout_Loaded(object sender, RoutedEventArgs e)
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            setting_device_name.Text = GetSetting("device_name");

            string serversXml = localSettings.Values["server_list"] as string;
            int? selectedServer = localSettings.Values["server_selected"] as int?;

            ObservableCollection<ServerListItem> servers = null;
            if (string.IsNullOrEmpty(serversXml) == false)
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(ObservableCollection<ServerListItem>));
                StringReader sr = new StringReader(serversXml);
                servers = (ObservableCollection<ServerListItem>)deserializer.Deserialize(sr);
            }

            if (servers == null)
            {
                servers = new ObservableCollection<ServerListItem>();
            }
            serverList.ItemsSource = servers;

            if (selectedServer == null)
            {
                selectedServer = -1;
            }

            if(servers.Count > 0)
            {
                serverList.SelectedIndex = (int)selectedServer;
            }
        }

        public void DataReceived(string discoverData)
        {
            try
            {
                JObject server_info = JObject.Parse(discoverData);
                string discovered_server_host = (string)server_info["Address"];
                if (discovered_server_host != null)
                {
                    Uri serverUri = new Uri(discovered_server_host);
                    string server_host = serverUri.Host;
                    string server_port = serverUri.Port.ToString();

                    Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        setting_server.Text = server_host;
                        setting_port.Text = server_port;
                    });

                }
            }
            catch (Exception exep)
            {
                App.AddNotification(new Notification() { Title = "Error Receiving Discover Data", Message = exep.Message });
            }

        }

        private void DetectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DiscoverServer discoverer = new DiscoverServer();
                discoverer.dataReceived += DataReceived;
                discoverer.DiscoverNow(null);
            }
            catch (Exception exep)
            {
                App.AddNotification(new Notification() { Title = "Error Sending Discover Data", Message = exep.Message });
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ServerListItem server = serverList.SelectedItem as ServerListItem;

            if (server != null)
            {
                server.host = setting_server.Text.Trim();
                server.port = setting_port.Text.Trim();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ServerListItem> list = serverList.ItemsSource as ObservableCollection<ServerListItem>;

            ServerListItem server = new ServerListItem();
            server.host = setting_server.Text.Trim();
            server.port = setting_port.Text.Trim();

            list.Add(server);
            int selectedIndex = serverList.SelectedIndex;
            if (selectedIndex == -1 && list.Count > 0)
            {
                serverList.SelectedIndex = 0;
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<ServerListItem> list = serverList.ItemsSource as ObservableCollection<ServerListItem>;

            if (serverList.SelectedIndex != -1)
            {
                list.RemoveAt(serverList.SelectedIndex);
                if(list.Count > 0)
                {
                    serverList.SelectedIndex = 0;
                }
            }
        }

        private void serverList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ServerListItem server = serverList.SelectedItem as ServerListItem;

            if (server != null)
            {
                setting_server.Text = server.host;
                setting_port.Text = server.port;
            }
        }
    }
}
