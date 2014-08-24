﻿/*
Media Browser Player
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
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace MediaBrowserPlayer.Classes
{
    class ApiClient
    {
        private AppSettings settings = new AppSettings();

        public ApiClient()
        {

        }

        public async Task<string> GetAuthorizationHeader(bool includeUser = true)
        {
            //MediaBrowser UserId="e8837bc1-ad67-520e-8cd2-f629e3155721", Client="Android", Device="Samsung Galaxy SIII", DeviceId="xxx", Version="1.0.0.0"

            string value = "MediaBrowser ";

            if (includeUser)
            {
                value += "UserId=\"" + await GetUserID() + "\", ";
            }

            value += "Client=\"Windows RT\", ";

            string deviceName = settings.GetDeviceName();
            if (string.IsNullOrEmpty(deviceName))
            {
                value += "Device=\"MBP\", ";
            }
            else
            {
                value += "Device=\"MBP-" + settings.GetDeviceName() + "\", ";
            }

            value += "DeviceId=\"" + settings.GetDeviceId() + "\", ";

            value += "Version=\"0.0.1\"";

            return value;
        }

        public async Task<bool> SetCapabilities()
        {
            bool worked = false;
            SessionInfo sessionInfo;
            sessionInfo = await GetSessionInfo();

            if(sessionInfo == null)
            {
                return false;
            }

            string server = settings.GetServer();
            if (server == null)
            {
                throw new Exception("Server not set");
            }

            Uri url = new Uri("http://" + server + "/mediabrowser/Sessions/Capabilities?Id=" + sessionInfo.Id + "&PlayableMediaTypes=Video&SupportedCommands=Play");

            HttpClient httpClient = new HttpClient();

            string authorization = await GetAuthorizationHeader();
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            string authToken = await Authenticate();
            httpClient.DefaultRequestHeaders.Add("X-MediaBrowser-Token", authToken);

            JObject jsonData = new JObject();

            jsonData.Add("Id", sessionInfo.Id);
            jsonData.Add("PlayableMediaTypes", "Video");

            HttpContent myContent = new StringContent(jsonData.ToString(), Encoding.UTF8, "application/json");

            var responce = await httpClient.PostAsync(url, myContent);
            responce.EnsureSuccessStatusCode();

            return worked;
        }

        public async Task<SessionInfo> GetSessionInfo()
        {
            string server = settings.GetServer();
            if (server == null)
            {
                return null;
            }

            Uri url = new Uri("http://" + server + "/mediabrowser/Sessions?DeviceId=" + settings.GetDeviceId() + "&format=json");

            HttpClient httpClient = new HttpClient();

            string authorization = await GetAuthorizationHeader();
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            string authToken = await Authenticate();
            httpClient.DefaultRequestHeaders.Add("X-MediaBrowser-Token", authToken);

            HttpResponseMessage itemResponce = await httpClient.GetAsync(url);
            itemResponce.EnsureSuccessStatusCode();

            string responceText = await itemResponce.Content.ReadAsStringAsync();

            JArray sessionInfoList = JArray.Parse(responceText);

            SessionInfo info = null;

            if (sessionInfoList.Count > 0)
            {
                JObject sessionInfo = (JObject)sessionInfoList[0];

                // build the responce object
                info = new SessionInfo();

                info.Id = (string)sessionInfo["Id"];

                JObject transcodingInfo = (JObject)sessionInfo["TranscodingInfo"];
                if (transcodingInfo != null)
                {
                    info.AudioCodec = (transcodingInfo["AudioCodec"] != null) ? (string)transcodingInfo["AudioCodec"] : "";
                    info.VideoCodec = (transcodingInfo["VideoCodec"] != null) ? (string)transcodingInfo["VideoCodec"] : "";
                    info.Container = (transcodingInfo["Container"] != null) ? (string)transcodingInfo["Container"] : "";
                    info.Bitrate = (transcodingInfo["Bitrate"] != null) ? (int)transcodingInfo["Bitrate"] : 0;
                    info.Framerate = (transcodingInfo["Framerate"] != null) ? (double)transcodingInfo["Framerate"] : 0;
                    info.CompletionPercentage = (transcodingInfo["CompletionPercentage"] != null) ? (double)transcodingInfo["CompletionPercentage"] : 0;
                    info.Width = (transcodingInfo["Width"] != null) ? (int)transcodingInfo["Width"] : 0;
                    info.Height = (transcodingInfo["Height"] != null) ? (int)transcodingInfo["Height"] : 0;
                    info.AudioChannels = (transcodingInfo["AudioChannels"] != null) ? (int)transcodingInfo["AudioChannels"] : 0;
                }
            }

            return info;
        }

        public async void PlaybackCheckinProgress(string itemId, long position)
        {
            HttpClient httpClient = new HttpClient();

            string authorization = await GetAuthorizationHeader();
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            string authToken = await Authenticate();
            httpClient.DefaultRequestHeaders.Add("X-MediaBrowser-Token", authToken);

            JObject jsonData = new JObject();

            JArray queueable = new JArray();
            //queueable.Add("Video");
            jsonData.Add("QueueableMediaTypes", queueable);
            jsonData.Add("CanSeek", true);
            jsonData.Add("ItemId", itemId);
            jsonData.Add("MediaSourceId", "");
            jsonData.Add("IsPaused", false);
            jsonData.Add("IsMuted", false);
            jsonData.Add("PositionTicks", (position * 1000 * 10000));
            jsonData.Add("PlayMethod", "Transcode");

            string playbackData = jsonData.ToString();

            string server = settings.GetServer();
            if (server == null)
            {
                throw new Exception("Server not set");
            }

            Uri url = new Uri("http://" + server + "/mediabrowser/Sessions/Playing/Progress");
            HttpContent myContent = new StringContent(playbackData, Encoding.UTF8, "application/json");
            var responce = await httpClient.PostAsync(url, myContent);
            //responce.EnsureSuccessStatusCode();
        }

        public async void PlaybackCheckinStopped(string itemId, long position)
        {
            HttpClient httpClient = new HttpClient();

            string authorization = await GetAuthorizationHeader();
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            string authToken = await Authenticate();
            httpClient.DefaultRequestHeaders.Add("X-MediaBrowser-Token", authToken);

            JObject jsonData = new JObject();

            JArray queueable = new JArray();
            //queueable.Add("Video");
            jsonData.Add("QueueableMediaTypes", queueable);
            jsonData.Add("CanSeek", true);
            jsonData.Add("ItemId", itemId);
            jsonData.Add("MediaSourceId", "");
            jsonData.Add("IsPaused", false);
            jsonData.Add("IsMuted", false);
            jsonData.Add("PositionTicks", (position * 1000 * 10000));
            jsonData.Add("PlayMethod", "Transcode");

            string playbackData = jsonData.ToString();

            string server = settings.GetServer();
            if (server == null)
            {
                throw new Exception("Server not set");
            }

            Uri url = new Uri("http://" + server + "/mediabrowser/Sessions/Playing/Stopped");
            HttpContent myContent = new StringContent(playbackData, Encoding.UTF8, "application/json");
            var responce = await httpClient.PostAsync(url, myContent);
            //responce.EnsureSuccessStatusCode();
        }

        public async void PlaybackCheckinStarted(string itemId, long position)
        {
            HttpClient httpClient = new HttpClient();

            string authorization = await GetAuthorizationHeader();
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            string authToken = await Authenticate();
            httpClient.DefaultRequestHeaders.Add("X-MediaBrowser-Token", authToken);

            JObject jsonData = new JObject();

            JArray queueable = new JArray();
            //queueable.Add("Video");
            jsonData.Add("QueueableMediaTypes", queueable);
            jsonData.Add("CanSeek", true);
            jsonData.Add("ItemId", itemId);
            jsonData.Add("MediaSourceId", "");
            jsonData.Add("IsPaused", false);
            jsonData.Add("IsMuted", false);
            jsonData.Add("PositionTicks", (position * 1000 * 10000));
            jsonData.Add("PlayMethod", "Transcode");

            string playbackData = jsonData.ToString();

            string server = settings.GetServer();
            if (server == null)
            {
                throw new Exception("Server not set");
            }

            Uri url = new Uri("http://" + server + "/mediabrowser/Sessions/Playing");
            HttpContent myContent = new StringContent(playbackData, Encoding.UTF8, "application/json");
            var responce = await httpClient.PostAsync(url, myContent);
            //responce.EnsureSuccessStatusCode();
        }

        public async Task<string> Authenticate()
        {
            string token = settings.GetAccessToken();

            if(string.IsNullOrEmpty(token))
            {
                throw new Exception("Access Token Not Set");
            }

            return token;
        }

        public async Task<MediaItem> GetItemInfo(string itemId)
        {
            MediaItem item = new MediaItem();

            string userId = await GetUserID();

            string server = settings.GetServer();
            if (server == null)
            {
                throw new Exception("Server not set");
            }

            Uri url = new Uri("http://" + server + "/mediabrowser/Users/" + userId + "/Items/" + itemId + " ?format=json");

            HttpClient httpClient = new HttpClient();

            string authorization = await GetAuthorizationHeader();
            httpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            string authToken = await Authenticate();
            httpClient.DefaultRequestHeaders.Add("X-MediaBrowser-Token", authToken);

            HttpResponseMessage itemResponce = await httpClient.GetAsync(url);
            itemResponce.EnsureSuccessStatusCode();

            string itemResponceText = await itemResponce.Content.ReadAsStringAsync();

            JObject itemInfo = JObject.Parse(itemResponceText);

            if (itemInfo["RunTimeTicks"] != null)
            {
                long runTimeSeconds = (long)itemInfo["RunTimeTicks"];
                runTimeSeconds = (runTimeSeconds / 1000) / 10000;
                item.duration = runTimeSeconds;
            }
            else
            {
                item.duration = 0;
                App.AddNotification(new Notification() { Title = "Media Item Error", Message = "The media item has no duration" });
            }

            item.Name = (itemInfo["Name"] != null) ? (string)itemInfo["Name"] : "";
            item.Year = (itemInfo["ProductionYear"] != null) ? (int)itemInfo["ProductionYear"] : 0;
            item.Series = (itemInfo["SeriesName"] != null) ? (string)itemInfo["SeriesName"] : "";
            item.Type = (itemInfo["Type"] != null) ? (string)itemInfo["Type"] : "";
            item.Season = (itemInfo["ParentIndexNumber"] != null) ? (int)itemInfo["ParentIndexNumber"] : 0;
            item.EpisodeIndex = (itemInfo["IndexNumber"] != null) ? (int)itemInfo["IndexNumber"] : 0;

            return item;
        }

        public async Task<string> GetUserID()
        {
            string userId = settings.GetUserId();
            if(string.IsNullOrEmpty(userId))
            {
                throw new Exception("User ID Not Set");
            }

            return userId;
        }

    }
}
