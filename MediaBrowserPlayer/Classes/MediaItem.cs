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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowserPlayer.Classes
{
    public class MediaItem
    {
        public long Duration;
        public string Id;
        public string Name;
        public string Series;
        public int Season;
        public int EpisodeIndex;
        public string Type;
        public int Year;
        public string SeriesId;

        public List<MediaStreamInfo> mediaStreams;
    }

    public class MediaStreamInfo
    {
        public int Index;
        public string Type;
        public string Language;
        public string Codec;
        public bool IsTextSubtitleStream;
    }
}
