﻿using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    [Serializable]
    public class qaac_arguments
    {
        public qaac_arguments()
        {
        }

        public qaac_arguments Clone()
        {
            return (qaac_arguments)this.MemberwiseClone();
        }

        private Settings.AudioEncodingModes _encodingmode = Settings.AudioEncodingModes.VBR;
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

        private string _aacprofile = "AAC-LC";
        public string aacprofile
        {
            get
            {
                return _aacprofile;
            }
            set
            {
                _aacprofile = value;
            }
        }

        private int _quality = 10;
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

        private int _accuracy = 2;
        public int accuracy
        {
            get
            {
                return _accuracy;
            }
            set
            {
                _accuracy = value;
            }
        }

        private bool _no_delay = false;
        public bool no_delay
        {
            get
            {
                return _no_delay;
            }
            set
            {
                _no_delay = value;
            }
        }

        private int _gapless_mode = 0;
        public int gapless_mode
        {
            get
            {
                return _gapless_mode;
            }
            set
            {
                _gapless_mode = value;
            }
        }
    }
}
