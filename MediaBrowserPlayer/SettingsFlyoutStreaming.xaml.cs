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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Settings Flyout item template is documented at http://go.microsoft.com/fwlink/?LinkId=273769

namespace SmartPlayer
{
    public sealed partial class SettingsFlyoutStreaming : SettingsFlyout
    {
        AppSettings settings = new AppSettings();

        public SettingsFlyoutStreaming()
        {
            this.InitializeComponent();

            InterfaceHelpers.SetupVideoBitrate(videoBitrate);
            InterfaceHelpers.SetupVideoMaxWidth(videoMaxWidth);
            InterfaceHelpers.SetupAudioBitrate(audioBitrate);
            InterfaceHelpers.SetupAudioChannel(audioChannels);
            InterfaceHelpers.SetupAudioCodec(audioCodecs);
            InterfaceHelpers.SetupEnableStreamCopy(enableStreamCopy);
        }

        private void audioChannels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            settings.SaveAppSettingInt("audio_channels", selected.DataValueInt);
        }

        private void audioCodec_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            settings.SaveAppSettingString("audio_codec", selected.DataValueString);
        }

        private void enableStreamCopy_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            settings.SaveAppSettingString("stream_copy", selected.DataValueString);
        }

        private void videoBitrate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            settings.SaveAppSettingInt("video_bitrate", selected.DataValueInt);
        }

        private void videoMaxWidth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            settings.SaveAppSettingInt("video_max_width", selected.DataValueInt);
        }

        private void audioBitrate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxData selected = (sender as ComboBox).SelectedItem as ComboBoxData;
            settings.SaveAppSettingInt("audio_bitrate", selected.DataValueInt);
        }
    }
}
