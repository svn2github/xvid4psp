using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace XviD4PSP
{
    [Serializable]
    public class AudioStream
    {
        public AudioStream()
        {
            //this.aac_options = new aac_arguments();
            //this.mp3_options = new mp3_arguments();
        }

        public AudioStream Clone()
        {
            AudioStream temp = (AudioStream)this.MemberwiseClone();
            //temp.aac_options = this.aac_options.Clone();
            //temp.mp3_options = this.mp3_options.Clone();
            return temp;
        }

        private string _codec;
        public string codec
        {
            get
            {
                return _codec;
            }
            set
            {
                _codec = value;
            }
        }

        private string _codecshort;
        public string codecshort
        {
            get
            {
                return _codecshort;
            }
            set
            {
                _codecshort = value;
            }
        }

        private string _ff_codec;
        public string ff_codec
        {
            get
            {
                return _ff_codec;
            }
            set
            {
                _ff_codec = value;
            }
        }

        private int _bitrate;
        public int bitrate
        {
            get
            {
                return _bitrate;
            }
            set
            {
                _bitrate = value;
            }
        }

        private int _channels = 0;
        public int channels
        {
            get
            {
                return _channels;
            }
            set
            {
                _channels = value;
            }
        }

        private int _bits = 0;
        public int bits
        {
            get
            {
                return _bits;
            }
            set
            {
                _bits = value;
            }
        }

        private int _ff_bits = 0;
        public int ff_bits
        {
            get
            {
                return _ff_bits;
            }
            set
            {
                _ff_bits = value;
            }
        }

        private int _delay = 0;
        public int delay
        {
            get
            {
                return _delay;
            }
            set
            {
                _delay = value;
            }
        }

        private string _audiopath;
        public string audiopath
        {
            get
            {
                return _audiopath;
            }
            set
            {
                _audiopath = value;
            }
        }

        private string[] _audiofiles;
        public string[] audiofiles
        {
            get
            {
                return _audiofiles;
            }
            set
            {
                _audiofiles = value;
            }
        }

        private int _mi_id = 0;
        public int mi_id
        {
            get
            {
                return _mi_id;
            }
            set
            {
                _mi_id = value;
            }
        }

        private int _mi_order = -1;
        public int mi_order
        {
            get
            {
                return _mi_order;
            }
            set
            {
                _mi_order = value;
            }
        }

        private int _ff_order = 0;
        public int ff_order
        {
            get
            {
                return _ff_order;
            }
            set
            {
                _ff_order = value;
            }
        }

        private int _ff_order_filtered = 0;
        public int ff_order_filtered
        {
            get
            {
                return _ff_order_filtered;
            }
            set
            {
                _ff_order_filtered = value;
            }
        }

        private string _samplerate;
        public string samplerate
        {
            get
            {
                return _samplerate;
            }
            set
            {
                _samplerate = value;
            }
        }

        private string _gain = "0.0";
        public string gain
        {
            get
            {
                return _gain;
            }
            set
            {
                _gain = value;
            }
        }

        private bool _gaindetected = false;
        public bool gaindetected
        {
            get
            {
                return _gaindetected;
            }
            set
            {
                _gaindetected = value;
            }
        }

        private string _language;
        public string language
        {
            get
            {
                return _language;
            }
            set
            {
                _language = value;
            }
        }

        private AviSynthScripting.Decoders _decoder;
        public AviSynthScripting.Decoders decoder
        {
            get
            {
                return _decoder;
            }
            set
            {
                _decoder = value;
            }
        }

        private AudioOptions.ChannelConverters _channelconverter = AudioOptions.ChannelConverters.KeepOriginalChannels;
        public AudioOptions.ChannelConverters channelconverter
        {
            get
            {
                return _channelconverter;
            }
            set
            {
                _channelconverter = value;
            }
        }

        private string _passes;
        public string passes
        {
            get
            {
                return _passes;
            }
            set
            {
                _passes = value;
            }
        }

        private string _encoding;
        public string encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                _encoding = value;
            }
        }

        private bool _badmixing = false;
        public bool badmixing
        {
            get
            {
                return _badmixing;
            }
            set
            {
                _badmixing = value;
            }
        }

        private string _nerotemp;
        public string nerotemp
        {
            get
            {
                return _nerotemp;
            }
            set
            {
                _nerotemp = value;
            }
        }
    }
}
