using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    [Serializable]
    public class x262_arguments
    {
        public x262_arguments()
        {
            //(defaults)
        }

        public x262_arguments(x262.Presets preset, x262.Tunes tune, x262.Profiles profile)
        {
            //Выставляем значения в соответствии с пресетом
            if (preset == x262.Presets.Ultrafast)
            {
                _aqmode = 0;
                _b_adapt = 0;
                _bframes = 0;
                _lookahead = 0;
                _me = "dia";
                _no_mbtree = true;
                _subme = 0;
            }
            else if (preset == x262.Presets.Superfast)
            {
                _lookahead = 0;
                _me = "dia";
                _no_mbtree = true;
                _subme = 1;
                _weightp = 0;
            }
            else if (preset == x262.Presets.Veryfast)
            {
                _lookahead = 10;
                _subme = 2;
                _weightp = 0;
            }
            else if (preset == x262.Presets.Faster)
            {
                _lookahead = 20;
                _subme = 4;
                _weightp = 0;
            }
            else if (preset == x262.Presets.Fast)
            {
                _subme = 6;
                _lookahead = 30;
                _weightp = 0;
            }
            else if (preset == x262.Presets.Medium)
            {
                //(defaults)
            }
            else if (preset == x262.Presets.Slow)
            {
                _b_adapt = 2;
                _lookahead = 50;
                _me = "umh";
                _subme = 8;
            }
            else if (preset == x262.Presets.Slower)
            {
                _b_adapt = 2;
                _lookahead = 60;
                _me = "umh";
                _subme = 9;
            }
            else if (preset == x262.Presets.Veryslow)
            {
                _b_adapt = 2;
                _bframes = 8;
                _lookahead = 60;
                _me = "umh";
                _merange = 24;
                _subme = 9;
            }
            else if (preset == x262.Presets.Placebo)
            {
                _b_adapt = 2;
                _bframes = 16;
                _lookahead = 60;
                _me = "tesa";
                _merange = 24;
                _no_fastpskip = true;
                _slow_frstpass = true;
                _subme = 9;
            }

            //Изменения под Tune
            if (tune == x262.Tunes.None)
            {
                //(defaults)
            }
            else if (tune == x262.Tunes.Film)
            {
                //
            }
            else if (tune == x262.Tunes.Animation)
            {
                _bframes += 2;
                _psyrdo = 0.4m;
                _aqstrength = "0.6";
            }
            else if (tune == x262.Tunes.Grain)
            {
                _aqstrength = "0.5";
                _ratio_ip = 1.1m;
                _ratio_pb = 1.1m;
                _qcomp = 0.8m;
            }
            else if (tune == x262.Tunes.StillImage)
            {
                _aqstrength = "1.2";
                _psyrdo = 2.0m;
            }
            else if (tune == x262.Tunes.PSNR)
            {
                _aqmode = 0;
                _no_psy = true;
            }
            else if (tune == x262.Tunes.SSIM)
            {
                _aqmode = 2;
                _no_psy = true;
            }
            else if (tune == x262.Tunes.FastDecode)
            {
                //
            }

            /*if (profile == x262.Profiles.High10)
            {
                //10-bit depth
                _max_quant = 81;
            }*/
        }

        public x262_arguments Clone()
        {
            return (x262_arguments)this.MemberwiseClone();
        }

        private x262.Presets _preset = x262.Presets.Medium;
        public x262.Presets preset
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

        private x262.Profiles _profile = x262.Profiles.Auto;
        public x262.Profiles profile
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

        private x262.Tunes _tune = x262.Tunes.None;
        public x262.Tunes tune
        {
            get
            {
                return _tune;
            }
            set
            {
                _tune = value;
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

        private int _dc = 8;
        public int dc
        {
            get
            {
                return _dc;
            }
            set
            {
                _dc = value;
            }
        }

        /*private string _direct = "none";
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
        }*/

        /*private int _bpyramid = 0;
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
        }*/

        private bool _altscan = false;
        public bool altscan
        {
            get
            {
                return _altscan;
            }
            set
            {
                _altscan = value;
            }
        }

        private int _weightp = 0;
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

        /*private bool _adaptivedct = false;
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
        }*/

        /*private int _trellis = 0;
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
        }*/

        /*private bool _mixedrefs = false;
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
        }*/

        /*private bool _cabac = true;
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
        }*/

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

        private bool _linear_q = false;
        public bool linear_q
        {
            get
            {
                return _linear_q;
            }
            set
            {
                _linear_q = value;
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

        private int _min_quant = 1;
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

        private int _max_quant = 31;
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

        /*private decimal _psytrellis = 0.0m;
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
        }*/

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

        private string _lookahead_threads = "auto";
        public string lookahead_threads
        {
            get
            {
                return _lookahead_threads;
            }
            set
            {
                _lookahead_threads = value;
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

        private bool _open_gop = false;
        public bool open_gop
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

        private string _range_in = "auto";
        public string range_in
        {
            get
            {
                return _range_in;
            }
            set
            {
                _range_in = value;
            }
        }

        private string _range_out = "auto";
        public string range_out
        {
            get
            {
                return _range_out;
            }
            set
            {
                _range_out = value;
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

        private string _colorspace = "I420";
        public string colorspace
        {
            get
            {
                return _colorspace;
            }
            set
            {
                _colorspace = value;
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

        private bool _bluray = false;
        public bool bluray
        {
            get
            {
                return _bluray;
            }
            set
            {
                _bluray = value;
            }
        }
    }
}
