using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    [Serializable]
    public class x264_arguments
    {
        public x264_arguments()
        {
        }

        public x264_arguments(int codec_preset)
        {
            //Выставляем значения в соответствии с пресетом
            if (codec_preset == 0)
            {
                //Ultrafast
                _adaptivedct = false;
                _analyse = "none";
                _aqmode = 0;
                _b_adapt = 0;
                _bframes = 0;
                _bpyramid = 0;
                _cabac = false;
                _deblocking = false;
                _direct = "none";//
                _lookahead = 0;
                _me = "dia";
                _mixedrefs = false;
                _no_mbtree = true;
                _psyrdo = 0.0m;//
                _reference = 1;
                _subme = 0;
                _trellis = 0;
                _weightb = false;
                _weightp = 0;
                //--scenecut 0
            }
            else if (codec_preset == 1)
            {
                //Superfast
                _analyse = "i8x8,i4x4";
                _lookahead = 0;
                _me = "dia";
                _mixedrefs = false;
                _no_mbtree = true;
                _reference = 1;
                _subme = 1;
                _trellis = 0;
                _weightp = 0;
            }
            else if (codec_preset == 2)
            {
                //Veryfast
                _lookahead = 10;
                _mixedrefs = false;
                _reference = 1;
                _subme = 2;
                _trellis = 0;
                _weightp = 0;
            }
            else if (codec_preset == 3)
            {
                //Faster
                _lookahead = 20;
                _mixedrefs = false;
                _reference = 2;
                _subme = 4;
                _weightp = 1;
            }
            else if (codec_preset == 4)
            {
                //Fast
                _reference = 2;
                _subme = 6;
                _lookahead = 30;
            }
            else if (codec_preset == 5)
            {
                //Medium
                //(defaults)
            }
            else if (codec_preset == 6)
            {
                //Slow
                _b_adapt = 2;
                _direct = "auto";
                _lookahead = 50;
                _me = "umh";
                _reference = 5;
                _subme = 8;
            }
            else if (codec_preset == 7)
            {
                //Slower
                _analyse = "all";
                _b_adapt = 2;
                _direct = "auto";
                _lookahead = 60;
                _me = "umh";
                _reference = 8;
                _subme = 9;
                _trellis = 2;
            }
            else if (codec_preset == 8)
            {
                //Veryslow
                _analyse = "all";
                _b_adapt = 2;
                _bframes = 8;
                _direct = "auto";
                _lookahead = 60;
                _me = "umh";
                _merange = 24;
                _reference = 16;
                _subme = 10;
                _trellis = 2;
            }
            else if (codec_preset == 9)
            {
                //Placebo
                _analyse = "all";
                _b_adapt = 2;
                _bframes = 16;
                _direct = "auto";
                _lookahead = 60;
                _me = "tesa";
                _merange = 24;
                _no_fastpskip = true;
                _reference = 16;
                _slow_frstpass = true;
                _subme = 10;
                _trellis = 2;
            }
        }

        public x264_arguments Clone()
        {
            return (x264_arguments)this.MemberwiseClone();
        }

        private int _preset = 5;
        public int preset
        {
            get
            {
                return _preset;
            }
            set
            {
                _preset = value;
            }
        }

        private string _profile = "auto";
        public string profile
        {
            get
            {
                return _profile;
            }
            set
            {
                _profile = value;
            }
        }

        private string _level = "unrestricted";
        public string level
        {
            get
            {
                return _level;
            }
            set
            {
                _level = value;
            }
        }

        private int _reference = 3;
        public int reference
        {
            get
            {
                return _reference;
            }
            set
            {
                _reference = value;
            }
        }

        private string _analyse = "p8x8,b8x8,i8x8,i4x4";
        public string analyse
        {
            get
            {
                return _analyse;
            }
            set
            {
                _analyse = value;
            }
        }

        private bool _deblocking = true;
        public bool deblocking
        {
            get
            {
                return _deblocking;
            }
            set
            {
                _deblocking = value;
            }
        }

        private int _deblocks = 0;
        public int deblocks
        {
            get
            {
                return _deblocks;
            }
            set
            {
                _deblocks = value;
            }
        }

        private int _deblockt = 0;
        public int deblockt
        {
            get
            {
                return _deblockt;
            }
            set
            {
                _deblockt = value;
            }
        }

        private int _subme = 7;
        public int subme
        {
            get
            {
                return _subme;
            }
            set
            {
                _subme = value;
            }
        }

        private string _me = "hex";
        public string me
        {
            get
            {
                return _me;
            }
            set
            {
                _me = value;
            }
        }

        private int _merange = 16;
        public int merange
        {
            get
            {
                return _merange;
            }
            set
            {
                _merange = value;
            }
        }

        private bool _no_chroma = false;
        public bool no_chroma
        {
            get
            {
                return _no_chroma;
            }
            set
            {
                _no_chroma = value;
            }
        }

        private int _bframes = 3;
        public int bframes
        {
            get
            {
                return _bframes;
            }
            set
            {
                _bframes = value;
            }
        }

        private string _direct = "spatial";
        public string direct
        {
            get
            {
                return _direct;
            }
            set
            {
                _direct = value;
            }
        }

        private int _bpyramid = 2;
        public int bpyramid
        {
            get
            {
                return _bpyramid;
            }
            set
            {
                _bpyramid = value;
            }
        }

        private bool _weightb = true;
        public bool weightb
        {
            get
            {
                return _weightb;
            }
            set
            {
                _weightb = value;
            }
        }

        private int _weightp = 2;
        public int weightp
        {
            get
            {
                return _weightp;
            }
            set
            {
                _weightp = value;
            }
        }

        private bool _adaptivedct = true;
        public bool adaptivedct
        {
            get
            {
                return _adaptivedct;
            }
            set
            {
                _adaptivedct = value;
            }
        }

        private int _trellis = 1;
        public int trellis
        {
            get
            {
                return _trellis;
            }
            set
            {
                _trellis = value;
            }
        }

        private bool _mixedrefs = true;
        public bool mixedrefs
        {
            get
            {
                return _mixedrefs;
            }
            set
            {
                _mixedrefs = value;
            }
        }

        private bool _cabac = true;
        public bool cabac
        {
            get
            {
                return _cabac;
            }
            set
            {
                _cabac = value;
            }
        }

        private bool _no_fastpskip = false;
        public bool no_fastpskip
        {
            get
            {
                return _no_fastpskip;
            }
            set
            {
                _no_fastpskip = value;
            }
        }

        private bool _no_dctdecimate = false;
        public bool no_dctdecimate
        {
            get
            {
                return _no_dctdecimate;
            }
            set
            {
                _no_dctdecimate = value;
            }
        }

        private string _custommatrix;
        public string custommatrix
        {
            get
            {
                return _custommatrix;
            }
            set
            {
                _custommatrix = value;
            }
        }

        private int _min_quant = 0;
        public int min_quant
        {
            get
            {
                return _min_quant;
            }
            set
            {
                _min_quant = value;
            }
        }

        private int _max_quant = 69;
        public int max_quant
        {
            get
            {
                return _max_quant;
            }
            set
            {
                _max_quant = value;
            }
        }

        private int _step_quant = 4;
        public int step_quant
        {
            get
            {
                return _step_quant;
            }
            set
            {
                _step_quant = value;
            }
        }

        private string _aqstrength = "1.0";
        public string aqstrength
        {
            get
            {
                return _aqstrength;
            }
            set
            {
                _aqstrength = value;
            }
        }

        private int _aqmode = 1;
        public int aqmode
        {
            get
            {
                return _aqmode;
            }
            set
            {
                _aqmode = value;
            }
        }

        private bool _aud = false;
        public bool aud
        {
            get
            {
                return _aud;
            }
            set
            {
                _aud = value;
            }
        }

        private bool _pictiming = false;
        public bool pictiming
        {
            get
            {
                return _pictiming;
            }
            set
            {
                _pictiming = value;
            }
        }

        private bool _no_psy = false;
        public bool no_psy
        {
            get
            {
                return _no_psy;
            }
            set
            {
                _no_psy = value;
            }
        }

        private decimal _psyrdo = 1.0m;
        public decimal psyrdo
        {
            get
            {
                return _psyrdo;
            }
            set
            {
                _psyrdo = value;
            }
        }

        private decimal _psytrellis = 0.0m;
        public decimal psytrellis
        {
            get
            {
                return _psytrellis;
            }
            set
            {
                _psytrellis = value;
            }
        }

        private string _threads = "auto";
        public string threads
        {
            get
            {
                return _threads;
            }
            set
            {
                _threads = value;
            }
        }

        private bool _thread_input = false;
        public bool thread_input
        {
            get
            {
                return _thread_input;
            }
            set
            {
                _thread_input = value;
            }
        }

        private int _b_adapt = 1;
        public int b_adapt
        {
            get
            {
                return _b_adapt;
            }
            set
            {
                _b_adapt = value;
            }
        }

        private decimal _qcomp = 0.60m;
        public decimal qcomp
        {
            get
            {
                return _qcomp;
            }
            set
            {
                _qcomp = value;
            }
        }

        private int _vbv_maxrate = 0;
        public int vbv_maxrate
        {
            get
            {
                return _vbv_maxrate;
            }
            set
            {
                _vbv_maxrate = value;
            }
        }

        private int _vbv_bufsize = 0;
        public int vbv_bufsize
        {
            get
            {
                return _vbv_bufsize;
            }
            set
            {
                _vbv_bufsize = value;
            }
        }

        private decimal _vbv_init = 0.90m;
        public decimal vbv_init
        {
            get
            {
                return _vbv_init;
            }
            set
            {
                _vbv_init = value;
            }
        }

        private int _qp_offset = 0;
        public int qp_offset
        {
            get
            {
                return _qp_offset;
            }
            set
            {
                _qp_offset = value;
            }
        }

        private bool _slow_frstpass = false;
        public bool slow_frstpass
        {
            get
            {
                return _slow_frstpass;
            }
            set
            {
                _slow_frstpass = value;
            }
        }

        private bool _no_mbtree = false;
        public bool no_mbtree
        {
            get
            {
                return _no_mbtree;
            }
            set
            {
                _no_mbtree = value;
            }
        }

        private int _lookahead = 40;
        public int lookahead
        {
            get
            {
                return _lookahead;
            }
            set
            {
                _lookahead = value;
            }
        }

        private string _nal_hrd = "none";
        public string nal_hrd
        {
            get
            {
                return _nal_hrd;
            }
            set
            {
                _nal_hrd = value;
            }
        }

        private int _gop_min = 0;
        public int gop_min
        {
            get
            {
                return _gop_min;
            }
            set
            {
                _gop_min = value;
            }
        }

        private int _gop_max = 250;
        public int gop_max
        {
            get
            {
                return _gop_max;
            }
            set
            {
                _gop_max = value;
            }
        }

        private string _open_gop = "none";
        public string open_gop
        {
            get
            {
                return _open_gop;
            }
            set
            {
                _open_gop = value;
            }
        }

        private decimal _ratio_ip = 1.40m;
        public decimal ratio_ip
        {
            get
            {
                return _ratio_ip;
            }
            set
            {
                _ratio_ip = value;
            }
        }

        private decimal _ratio_pb = 1.30m;
        public decimal ratio_pb
        {
            get
            {
                return _ratio_pb;
            }
            set
            {
                _ratio_pb = value;
            }
        }

        private int _slices = 0;
        public int slices
        {
            get
            {
                return _slices;
            }
            set
            {
                _slices = value;
            }
        }

        private bool _pic_struct = false;
        public bool pic_struct
        {
            get
            {
                return _pic_struct;
            }
            set
            {
                _pic_struct = value;
            }
        }

        private bool _fake_int = false;
        public bool fake_int
        {
            get
            {
                return _fake_int;
            }
            set
            {
                _fake_int = value;
            }
        }

        private bool _full_range = false;
        public bool full_range
        {
            get
            {
                return _full_range;
            }
            set
            {
                _full_range = value;
            }
        }

        private string _colorprim = "Undefined";
        public string colorprim
        {
            get
            {
                return _colorprim;
            }
            set
            {
                _colorprim = value;
            }
        }

        private string _transfer = "Undefined";
        public string transfer
        {
            get
            {
                return _transfer;
            }
            set
            {
                _transfer = value;
            }
        }

        private string _colormatrix = "Undefined";
        public string colormatrix
        {
            get
            {
                return _colormatrix;
            }
            set
            {
                _colormatrix = value;
            }
        }

        private bool _non_deterministic = false;
        public bool non_deterministic
        {
            get
            {
                return _non_deterministic;
            }
            set
            {
                _non_deterministic = value;
            }
        }

        private string _extra_cli = "";
        public string extra_cli
        {
            get
            {
                return _extra_cli;
            }
            set
            {
                _extra_cli = value;
            }
        }
    }
}
