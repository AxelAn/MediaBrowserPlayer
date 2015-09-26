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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web;

namespace SmartPlayer.Classes
{

    class WebSocketManager
    {
        private AppSettings settings = new AppSettings();
        private MessageWebSocket webSocket = null;

        public WebSocketManager()
        {

        }

        public bool CloseWebSocket()
        {
            bool worked = false;
            if(webSocket != null)
            {
                try
                {
                    webSocket.Close(0, "App Closing");
                    worked = true;
                }
                catch (Exception)
                { }
                webSocket = null;
            }

            return worked;
        }

        public async Task<bool> SetupWebSocket()
        {
            bool worked = false;
            try
            {
                ServerListItem server = settings.GetServer();
                if (server == null)
                {
                    return false;
                    //throw new Exception("Server not set");
                }

                Uri serverUri = new Uri("ws://" + server + "/mediabrowser");
                webSocket = new MessageWebSocket();
                webSocket.Control.MessageType = SocketMessageType.Utf8;

                webSocket.MessageReceived += MessageReceived;

                webSocket.Closed += Closed;

                await webSocket.ConnectAsync(serverUri);

                DataWriter messageWriter = new DataWriter(webSocket.OutputStream);

                string deviceName = settings.GetDeviceName();
                string value = "SPMB";
                if (string.IsNullOrEmpty(deviceName) == false)
                {
                    value = "SPMB-" + settings.GetDeviceName();
                }

                string identityMessage = "{\"MessageType\":\"Identity\", \"Data\":\"Windows RT|" + settings.GetDeviceId() + "|0.0.1|" + value + "\"}";
                messageWriter.WriteString(identityMessage);
                await messageWriter.StoreAsync();

                worked = true;
            }
            catch(Exception e)
            {
                MetroEventSource.Log.Info("Error Creating WebSocket - " + e.Message);
                string errorString = "Error Creating WebSocket : " + e.Message;
                App.AddNotification(new Notification() { Title = "Error Creating Web Socket", Message = errorString });
            }

            return worked;
        }

        private void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    string read = reader.ReadString(reader.UnconsumedBufferLength);

                    // parse the message 
                    JObject messageObject = null;
                    try
                    {
                        messageObject = JObject.Parse(read);
                    }
                    catch(Exception e)
                    {
                        messageObject = null;
                        App.AddNotification(new Notification() { Title = "Error Parsing WebSocket Data Package", Message = e.Message });
                    }

                    // if we have an object and it is of type play
                    if (messageObject != null && "Play" == (string)messageObject["MessageType"])
                    {
                        Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            Frame rootFrame = Window.Current.Content as Frame;

                            rootFrame.Navigate(typeof(PlayerPage), messageObject);
                        });
                    }


                }
            }
            catch (Exception ex) // For debugging
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
                // Add your specific error-handling code here.
            }
        }

        private void Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            // You can add code to log or display the code and reason
            // for the closure (stored in args.Code and args.Reason)

            // This is invoked on another thread so use Interlocked 
            // to avoid races with the Start/Close/Reset methods.
            if (webSocket != null)
            {
                MessageWebSocket ws = Interlocked.Exchange(ref webSocket, null);
                if (ws != null)
                {
                    ws.Dispose();
                }
            }
        }

    }
}
