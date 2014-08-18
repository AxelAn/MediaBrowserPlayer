﻿using MediaBrowserPlayer.Classes;
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
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MediaBrowserPlayer
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

        public PlayerPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
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

            // reset session info controls
            transcodeInfoACodec.Text = "-";
            transcodeInfoVCodec.Text = "-";
            transcodeInfoRes.Text = "-";
            transcodeInfoChan.Text = "-";
            transcodeInfoBitrate.Text = "-";
            transcodeInfoSpeed.Text = "-";
            transcodeInfoComplete.Text = "-";
            playbackInfoBuffer.Text = "-";
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

            if (mediaItem.duration > 0)
            {
                SetupTimer();

                playbackProgress.Minimum = 0;
                playbackProgress.Maximum = mediaItem.duration;

                PlaybackAction(startIndex);

                mediaDuration.Text = new TimeSpan(0, 0, (int)mediaItem.duration).ToString(@"hh\:mm\:ss");
            }
            else
            {
                itemId = null;
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

                transcodeInfoACodec.Text = info.AudioCodec;
                transcodeInfoVCodec.Text = info.VideoCodec;
                transcodeInfoRes.Text = info.Width + "x" + info.Height;
                transcodeInfoChan.Text = info.AudioChannels.ToString();
                transcodeInfoBitrate.Text = info.Bitrate.ToString("n0");
                transcodeInfoSpeed.Text = info.Framerate.ToString("f1") + " fps";
                transcodeInfoComplete.Text = info.CompletionPercentage.ToString("f1") + "%";

            }
            
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

            playbackInfoBuffer.Text = percent.ToString() + "%";
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

        private void Button_Click(object sender, RoutedEventArgs e)
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
                mediaPlayer.Pause();
            }
            else if (mediaPlayer.CurrentState == MediaElementState.Paused)
            {
                mediaPlayer.Play();
            }
        }

        private void PlaybackAction(long startAtSeconds)
        {
            _seekTo = startAtSeconds;

            long startTicks = startAtSeconds * 1000 * 10000;

            string server = settings.GetServer();

            string mediaFile = "http://" + server + "/mediabrowser/Videos/" + itemId + "/stream.ts" +
                "?audioChannels=2&" +
                "AudioStreamIndex=1&" +
                "deviceId=" + settings.GetDeviceId() + "&" +
                "Static=false&" +
                "mediaSourceId=" + itemId + "&" +
                "VideoCodec=h264&" +
                "AudioCodec=aac&" +
                "maxWidth=1920&" +
                "videoBitrate=5000000&" +
                "audioBitrate=128000&" +
                "EnableAutoStreamCopy=true&" +
                "StartTimeTicks=" + startTicks;

            mediaPlayer.Source = new Uri(mediaFile, UriKind.Absolute);

            client.PlaybackCheckinStarted(itemId, startAtSeconds);
        }

        private void mediaPlayer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (gridAreaProgress.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                gridAreaProgress.Visibility = Windows.UI.Xaml.Visibility.Visible;
                gridAreaInfo.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                gridAreaProgress.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                gridAreaInfo.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}
