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

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartPlayer.Classes
{
    public class ServerListItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string _host = string.Empty;
        public string _port = string.Empty;

        public override string ToString()
        {
            return _host + ":" + _port;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string host
        {
            get
            {
                return this._host;
            }

            set
            {
                if (value != this._host)
                {
                    this._host = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string port
        {
            get
            {
                return this._port;
            }

            set
            {
                if (value != this._port)
                {
                    this._port = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}
