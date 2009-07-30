using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
   public class ac3_arguments
    {

       public ac3_arguments()
        {
        }

       public ac3_arguments Clone()
        {
            return (ac3_arguments)this.MemberwiseClone();
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

        private double _quality = 0.5;
        public double quality
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

    }
}
