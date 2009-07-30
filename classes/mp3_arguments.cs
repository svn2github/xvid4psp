using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    public class mp3_arguments
    {

        public mp3_arguments()
        {
        }

        public mp3_arguments Clone()
        {
            return (mp3_arguments)this.MemberwiseClone();
        }

        private Settings.AudioEncodingModes _encodingmode = Settings.AudioEncodingModes.ABR;
        public Settings.AudioEncodingModes encodingmode
        {
            get
            {
                return _encodingmode;
            }
            set
            {
                _encodingmode = value;
            }
        }

        private string _channelsmode = "Stereo";
        public string channelsmode
        {
            get
            {
                return _channelsmode;
            }
            set
            {
                _channelsmode = value;
            }
        }

        private int _quality = 4;
        public int quality
        {
            get
            {
                return _quality;
            }
            set
            {
                _quality = value;
            }
        }

        private int _minb = 32;
        public int minb
        {
            get
            {
                return _minb;
            }
            set
            {
                _minb = value;
            }
        }

        private int _maxb = 320;
        public int maxb
        {
            get
            {
                return _maxb;
            }
            set
            {
                _maxb = value;
            }
        }

        private bool _forcesamplerate = false;
        public bool forcesamplerate
        {
            get
            {
                return _forcesamplerate;
            }
            set
            {
                _forcesamplerate = value;
            }
        }

        private string _encquality = "2 - Recomended";
        public string encquality
        {
            get
            {
                return _encquality;
            }
            set
            {
                _encquality = value;
            }
        }


    }
}
