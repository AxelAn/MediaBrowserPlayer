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
using Microsoft.PlayerFramework;
using Newtonsoft.Json.Linq;
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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SmartPlayer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerPage : Page
    {
        private DispatcherTimer _timer;
        private long _seekTo = 0;
        private string itemId;
        private bool _sliderpressed = false;
        private AppSettings settings = new AppSettings();
        private ApiClient client = new ApiClient();

        private DispatcherTimer _playbackCheckin;

        private int audioStreamSelected = -1;
        private int subStreamSelected = -1;
        private int videoBitrateSelected = -1;
        private int videoMaxWidthSelected = -1;
        private int audioBitrateSelected = -1;
        private int audioChannelSelected = -1;
        private string audioCodecSelected = "";
        private string enableStreamCopySelected = "";

        public PlayerPage()
        {
            this.InitializeComponent();
            //this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            PointerEventHandler pointerpressedhandler = new PointerEventHandler(slider_PointerEntered);
            playbackProgress.AddHandler(Control.PointerPressedEvent, pointerpressedhandler, true);

            PointerEventHandler pointerreleasedhandler = new PointerEventHandler(slider_PointerCaptureLost);
            playbackProgress.AddHandler(Control.PointerCaptureLostEvent, pointerreleasedhandler, true);

            mediaPlayer.MediaFailed += mediaPlayer_MediaFailed;

            mediaPlayer.InteractiveActivationMode = Microsoft.PlayerFramework.InteractionType.None;

            mediaPlayer.BufferingProgressChanged += mediaPlayer_BufferingProgressChanged;

            mediaPlayer.PlayerStateChanged += mediaPlayer_PlayerStateChanged;

            videoBitrateSelected = InterfaceHelpers.SetupVideoBitrate(videoBitrate);
            videoMaxWidthSelected = InterfaceHelpers.SetupVideoMaxWidth(videoMaxWidth);
            audioBitrateSelected = InterfaceHelpers.SetupAudioBitrate(audioBitrate);
            audioChannelSelected = InterfaceHelpers.SetupAudioChannel(audioChannels);
            audioCodecSelected = InterfaceHelpers.SetupAudioCodec(audioCodecs);
            enableStreamCopySelected = InterfaceHelpers.SetupEnableStreamCopy(enableStreamCopy);

            videoBitrate.SelectionChanged += videoBitrate_SelectionChanged;
            videoMaxWidth.SelectionChanged += videoMaxWidth_SelectionChanged;
            audioBitrate.SelectionChanged += audioBitrate_SelectionChanged;
            audioChannels.SelectionChanged += audioChannels_SelectionChanged;
            audioCodecs.SelectionChanged += audioCodecs_SelectionChanged;
            enableStreamCopy.SelectionChanged += enableStreamCopy_SelectionChanged;

            // reset session info controls
            transcodeInfoACodec.Text = ": -";
            transcodeInfoVCodec.Text = ": -";
            transcodeInfoBitrate.Text = ": -";
            transcodeInfoSpeed.Text = ": -";
            transcodeInfoComplete.Text = ": -";
            playbackInfoBuffer.Text = ": -";
            transcodingProgress.Value = 0;

            // process the play action
            JObject playRequestData = e.Parameter as JObject;

            MediaItem mediaItem = null;
            long startIndex = 0;

            try
            {
                JObject data = (JObject)playRequestData["Data"];
                if (data["StartPositionTicks"] != null)
                {
                    startIndex = (long)data["StartPositionTicks"];
                    startIndex = (startIndex / 1000) / 10000;
                }
                JArray itemIds = (JArray)data["ItemIds"];

                itemId = (string)itemIds[0];

                mediaItem = await client.GetItemInfo(itemId);
            }
            catch(Exception exception)
            {
                App.AddNotification(new Notification() { Title = "Error Retreiving Playback Info", Message = exception.Message });
            }

            // set up audio and subtitle selection
            List<ComboBoxData> audioStreamItems = new List<ComboBoxData>();
            audioStreamItems.Add(new ComboBoxData() { DataName = "Auto" , DataValueInt = -1 });
            foreach (MediaStreamInfo mInfo in mediaItem.mediaStreams)
            {
                if (mInfo.Type == "Audio")
                {
                    string langString = mInfo.Language;
                    if(string.IsNullOrWhiteSpace(langString))
                    {
                        langString = "NoLang";
                    }
                    audioStreamItems.Add(new ComboBoxData() { DataName = langString + " (" + mInfo.Codec + ")", DataValueInt = mInfo.Index });
                }
            }
            audioStreamSelector.ItemsSource = audioStreamItems;
            audioStreamSelector.SelectedIndex = 0;
            audioStreamSelector.SelectionChanged += audioStreamSelector_SelectionChanged;

            List<ComboBoxData> subStreamItems = new List<ComboBoxData>();
            subStreamItems.Add(new ComboBoxData() { DataName = "Auto", DataValueInt = -1 });
            foreach (MediaStreamInfo mInfo in mediaItem.mediaStreams)
            {
                if (mInfo.Type == "Subtitle")
                {
                    string langString = mInfo.Language;
                    if (string.IsNullOrWhiteSpace(langString))
                    {
                        langString = "NoLang";
                    }
                    subStreamItems.Add(new ComboBoxData() { DataName = langString + " (" + mInfo.Codec + ")", DataValueInt = mInfo.Index });
                }
            }
            subStreamSelector.ItemsSource = subStreamItems;
            subStreamSelector.SelectedIndex = 0;
            subStreamSelector.SelectionChanged += subStreamSelector_SelectionChanged;

            string logoItemId = "";

            if((mediaItem.Type).Equals("Episode", StringComparison.OrdinalIgnoreCase))
            {
                logoItemId = mediaItem.SeriesId;
                mediaTitle.Text = mediaItem.Series + " (" + mediaItem.Year + ") " + mediaItem.Name + " (s" + mediaItem.Season + "e" + mediaItem.EpisodeIndex + ")";
            }
            else if((mediaItem.Type).Equals("Movie", StringComparison.OrdinalIgnoreCase))
            {
                logoItemId = mediaItem.Id;
                mediaTitle.Text = mediaItem.Name + " (" + mediaItem.Year + ")";
            }
            else
            {
                mediaTitle.Text = "Unknown Media Type : " + mediaItem.Type;
            }

            // Set media item logo
            string logoPath = "http://" + settings.GetServer() + "/mediabrowser/Items/" + logoItemId + "/Images/Logo";
            BitmapImage image = new BitmapImage(new Uri(logoPath, UriKind.Absolute));
            mediaItemLogo.Source = image;

            // start playback
            SetupTimer();

            playbackProgress.Minimum = 0;
            playbackProgress.Maximum = mediaItem.Duration;

            mediaDuration.Text = new TimeSpan(0, 0, (int)mediaItem.Duration).ToString(@"hh\:mm\:ss");

            PlaybackAction(startIndex);

            // set player full screen if required
            bool playFullscreen = settings.GetAppSettingBool("player_start_fullscreen");
            if(playFullscreen)
            {
                gridAreaTitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                gridAreaProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                gridAreaInfo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

        }

        private void mediaPlayer_PlayerStateChanged(object sender, RoutedPropertyChangedEventArgs<Microsoft.PlayerFramework.PlayerState> e)
        {
            if(e.NewValue == Microsoft.PlayerFramework.PlayerState.Ending)
            {
                MetroEventSource.Log.Info("Media Playback Ended");

                mediaPlayer.Close();
                StopTimer();

                if (itemId != null)
                {
                    long possition = (long)playbackProgress.Value;
                    client.PlaybackCheckinStopped(itemId, possition);
                }

                Frame rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(MainPage));
            }
        }

        void slider_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _sliderpressed = true;
        }

        void slider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            long seekTime = (long)playbackProgress.Value;
            PlaybackAction(seekTime);

            _sliderpressed = false;
        }

        private void _timer_Tick(object sender, object e)
        {
            if (!_sliderpressed)
            {
                mediaPossitionText.Text = mediaPlayer.Position.Add(new TimeSpan(0, 0, (int)_seekTo)).ToString(@"hh\:mm\:ss");
                playbackProgress.Value = mediaPlayer.Position.TotalSeconds + _seekTo;
            }
        }

        private async void _playbackCheckin_Tick(object sender, object e)
        {
            long possition = (long)playbackProgress.Value;

            client.PlaybackCheckinProgress(itemId, possition);

            SessionInfo info = await client.GetSessionInfo();

            if (info != null)
            {
                transcodingProgress.Value = info.CompletionPercentage;

                string videoInfo = info.Width + "x" + info.Height + " " + info.VideoCodec;
                if(info.IsVideoDirect)
                {
                    videoInfo += " Direct"; 
                }
                else
                {
                    videoInfo += " Trans"; 
                }

                string audioInfo = info.AudioCodec + " " + info.AudioChannels.ToString() + "Chan";
                if (info.IsAudioDirect)
                {
                    audioInfo += " Direct";
                }
                else
                {
                    audioInfo += " Trans";
                }

                transcodeInfoACodec.Text = ": " + audioInfo;
                transcodeInfoVCodec.Text = ": " + videoInfo;
                transcodeInfoBitrate.Text = ": " + BytesToString(info.Bitrate);//.ToString("n0");
                transcodeInfoSpeed.Text = ": " + info.Framerate.ToString("f1") + " fps";
                transcodeInfoComplete.Text = ": " + info.CompletionPercentage.ToString("f1") + "%";
            }
            
        }

        String BytesToString(long byteCount)
        {
            string[] suf = { " b/s", " Kb/s", " Mb/s", " Gb/s", " Tb/s", " Pb/s", " Eb/s" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);

            _playbackCheckin = new DispatcherTimer();
            _playbackCheckin.Interval = TimeSpan.FromSeconds(5);

            StartTimer();
        }

        private void StartTimer()
        {
            _timer.Tick += _timer_Tick;
            _timer.Start();

            _playbackCheckin.Tick += _playbackCheckin_Tick;
            _playbackCheckin.Start();
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= _timer_Tick;
            }


            if (_playbackCheckin != null)
            {
                _playbackCheckin.Stop();
                _playbackCheckin.Tick -= _playbackCheckin_Tick;
            }
        }

        private void mediaPlayer_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
            int percent = (int)(mediaPlayer.BufferingProgress * 100);

            if (percent > 100)
            {
                percent = 0;
            }

            playbackInfoBuffer.Text = ": " + percent.ToString() + "%";
        }

        private void mediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            string error = e.ErrorMessage;
            if (e.OriginalSource != null)
            {
                error += "\n" + e.OriginalSource.ToString();
            }

            App.AddNotification(new Notification() { Title = "Error Playing Media", Message = error });
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Close();
            StopTimer();

            if (itemId != null)
            {
                long possition = (long)playbackProgress.Value;
                client.PlaybackCheckinStopped(itemId, possition);
            }

            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage));
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayer.CurrentState == MediaElementState.Playing)
            {
                pauseButton.Content = "Play";
                mediaPlayer.Pause();
            }
            else if (mediaPlayer.CurrentState == MediaElementState.Paused)
            {
                pauseButton.Content = "Pause";
                mediaPlayer.Play();
            }
        }

        private void PlaybackAction(long startAtSeconds)
        {
            _seekTo = startAtSeconds;

            long startTicks = startAtSeconds * 1000 * 10000;

            ServerListItem server = settings.GetServer();

            // get streaming values
            int videoBitrateSetting = videoBitrateSelected;
            if (videoBitrateSetting == -1)
            {
                videoBitrateSetting = 10000000;
            }
            int videoMaxWidthSetting = videoMaxWidthSelected;
            if (videoMaxWidthSetting == -1)
            {
                videoMaxWidthSetting = 1920;
            }
            int audioBitrateSetting = audioBitrateSelected;
            if (audioBitrateSetting == -1)
            {
                audioBitrateSetting = 128000;
            }
            int audioChannels = audioChannelSelected;
            if (audioChannels == -1)
            {
                audioChannels = 6;
            }
            string audioCodecs = audioCodecSelected;
            if(string.IsNullOrWhiteSpace(audioCodecs))
            {
                audioCodecs = "aac,ac3";
            }

            string enableStreamCopy = enableStreamCopySelected;
            if (string.IsNullOrWhiteSpace(enableStreamCopy))
            {
                enableStreamCopy = "true";
            }

            string mediaFileUrl = "http://" + server + "/mediabrowser/Videos/" + itemId + "/stream.ts?";

            if(audioStreamSelected != -1)
            {
                mediaFileUrl += "AudioStreamIndex=" + audioStreamSelected + "&";
            }

            if (subStreamSelected != -1)
            {
                mediaFileUrl += "SubtitleStreamIndex=" + subStreamSelected + "&";
                //mediaFileUrl += "SubtitleDeliveryMethod=Encode&";
                //mediaFileUrl += "SubtitleDeliveryMethod=External&";
            }

            mediaFileUrl += "DeviceId=" + settings.GetDeviceId() + "&" +
                "Static=false&" +
                "MediaSourceId=" + itemId + "&" +
                "VideoCodec=h264&" +
                "AudioCodec=" + audioCodecs + "&" +
                "AudioChannels=" + audioChannels + "&" +
                "MaxWidth=" + videoMaxWidthSetting + "&" +
                "VideoBitrate=" + videoBitrateSetting + "&" +
                "AudioBitrate=" + audioBitrateSetting + "&" +
                "EnableAutoStreamCopy=" + enableStreamCopy + "&" +
                "StartTimeTicks=" + startTicks;

            mediaPlayer.Source = new Uri(mediaFileUrl, UriKind.Absolute);

            /*
            // removed for now due to sync issues
            // set up subtitle streaming
            if (subStreamSelected != -1)
            {
                string subtitleUrl = "http://" + server + "/mediabrowser/Videos/" + itemId + "/" + itemId + "/Subtitles/" + subStreamSelected + "/" + startTicks + "/Stream.vtt";

                mediaPlayer.AvailableCaptions.Clear();
                Caption cap = new Caption();
                cap.Source = new Uri(subtitleUrl, UriKind.Absolute);
                cap.Description = "Default";
                mediaPlayer.AvailableCaptions.Add(cap);
                mediaPlayer.SelectedCaption = mediaPlayer.AvailableCaptions.First();
            }
            */

            //mediaPlayer.Stretch = Stretch.Fill;

            client.PlaybackCheckinStarted(itemId, startAtSeconds);
        }

        private void mediaPlayer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (gridAreaProgress.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                gridAreaTitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
                gridAreaProgress.Visibility = Windows.UI.Xaml.Visibility.Visible;
                gridAreaInfo.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                gridAreaTitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                gridAreaProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                gridAreaInfo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void audioStreamSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            audioStreamSelected = selected.DataValueInt;
        }

        private void subStreamSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            subStreamSelected = selected.DataValueInt;
        }

        private void enableStreamCopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            enableStreamCopySelected = selected.DataValueString;
        }

        private void audioCodecs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            audioCodecSelected = selected.DataValueString;
        }

        private void audioChannels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            audioChannelSelected = selected.DataValueInt;
        }

        private void audioBitrate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            audioBitrateSelected = selected.DataValueInt;
        }

        private void videoMaxWidth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            videoMaxWidthSelected = selected.DataValueInt;
        }

        private void videoBitrate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            videoBitrateSelected = selected.DataValueInt;
        }

        private void applyButton_Click(object sender, RoutedEventArgs e)
        {
            long currentPos = (long)(playbackProgress.Value);
            PlaybackAction(currentPos);
        }

        private void streamSettings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (StreamSettingsGrid.Visibility != Visibility.Collapsed)
            {
                streamSettings.Text = "Stream Settings +";
                StreamSettingsGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                streamSettings.Text = "Stream Settings -";
                StreamSettingsGrid.Visibility = Visibility.Visible;
            }
        }
    }
}
