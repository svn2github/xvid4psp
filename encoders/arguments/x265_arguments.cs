using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    [Serializable]
    public class x265_arguments
    {
        public x265_arguments()
        {
            //(defaults)
        }

        public x265_arguments(x265.Presets preset, x265.Tunes tune, x265.Profiles profile)
        {
            //Выставляем значения в соответствии с пресетом
            if (preset == x265.Presets.Ultrafast)
            {
                _lookahead = 0;
                _ctu = 32;
                _merange = 25;
                _b_adapt = 0;
                _subme = 0;
                _me = "dia";
                _early_skip = true;
                _sao = false;
                _weightp = false;
                _rd = 2;
                _reference = 1;
                _deblocking = false;
                _aqmode = 0; //X265_AQ_NONE
                _aqstrength = "0.0";
                _cutree = false;

                //scenecutThreshold = 0;
                //bEnableSignHiding = 0;
                //bEnableFastIntra = 1;
            }
            else if (preset == x265.Presets.Superfast)
            {
                _lookahead = 10;
                _ctu = 32;
                _merange = 44;
                _b_adapt = 0;
                _subme = 1;
                _early_skip = true;
                _weightp = false;
                _rd = 2;
                _reference = 1;
                _aqmode = 0; //X265_AQ_NONE
                _aqstrength = "0.0";
                _cutree = false;
                _sao = false;

                //bEnableFastIntra = 1;
            }
            else if (preset == x265.Presets.Veryfast)
            {
                _lookahead = 15;
                _ctu = 32;
                _b_adapt = 0;
                _subme = 1;
                _early_skip = true;
                _rd = 2;
                _reference = 1;
                _cutree = false;

                //bEnableFastIntra = 1;
            }
            else if (preset == x265.Presets.Faster)
            {
                _lookahead = 15;
                _b_adapt = 0;
                _early_skip = true;
                _rd = 2;
                _reference = 1;
                _cutree = false;

                //bEnableFastIntra = 1;
            }
            else if (preset == x265.Presets.Fast)
            {
                _lookahead = 15;
                _b_adapt = 0;
                _rd = 2;
                _reference = 2;

                //bEnableFastIntra = 1;
            }
            else if (preset == x265.Presets.Medium)
            {
                //(defaults)
            }
            else if (preset == x265.Presets.Slow)
            {
                _rect = true;
                _lookahead = 25;
                _rd = 4;
                _subme = 3;
                _max_merge = 3;
                _me = "star";
            }
            else if (preset == x265.Presets.Slower)
            {
                _weightb = true;
                _amp = true;
                _rect = true;
                _lookahead = 30;
                _bframes = 8;
                _rd = 6;
                _subme = 3;
                _max_merge = 3;
                _me = "star";
                _b_intra = true;

                //tuQTMaxInterDepth = 2;
                //tuQTMaxIntraDepth = 2;
            }
            else if (preset == x265.Presets.Veryslow)
            {
                _weightb = true;
                _amp = true;
                _rect = true;
                _lookahead = 40;
                _bframes = 8;
                _rd = 6;
                _subme = 4;
                _max_merge = 4;
                _me = "star";
                _reference = 5;
                _b_intra = true;

                //tuQTMaxInterDepth = 3;
                //tuQTMaxIntraDepth = 3;
            }
            else if (preset == x265.Presets.Placebo)
            {
                _weightb = true;
                _amp = true;
                _rect = true;
                _lookahead = 60;
                _merange = 92;
                _bframes = 8;
                _rd = 6;
                _subme = 5;
                _max_merge = 5;
                _me = "star";
                _reference = 5;
                _slow_firstpass = true;
                _b_intra = true;

                //tuQTMaxInterDepth = 4;
                //tuQTMaxIntraDepth = 4;
                //bEnableTransformSkip = 1;
            }

            //Изменения под Tune
            if (tune == x265.Tunes.None)
            {
                //(defaults)
            }
            else if (tune == x265.Tunes.Grain)
            {
                _deblockBeta = -2;
                _deblockTC = -2;
                _psyrdoq = 30;
                _psyrdo = 0.5m;
                _ratio_ip = 1.1m;
                _ratio_pb = 1.1m;
                _aqmode = 1; //X265_AQ_VARIANCE
                _aqstrength = "0.3";
                _qcomp = 0.8m;
                _b_intra = false;
            }
            else if (tune == x265.Tunes.PSNR)
            {
                _aqstrength = "0.0";
                _psyrdoq = 0;
                _psyrdo = 0;
            }
            else if (tune == x265.Tunes.SSIM)
            {
                _aqmode = 2; //X265_AQ_AUTO_VARIANCE
                _psyrdoq = 0;
                _psyrdo = 0;
            }
            else if (tune == x265.Tunes.FastDecode)
            {
                _b_adapt = 0;
                _bframes = 0;
                _lookahead = 0;
                _cutree = false;
                _threads_frames = 1;

                //scenecutThreshold = 0;
            }
        }

        public x265_arguments Clone()
        {
            return (x265_arguments)this.MemberwiseClone();
        }

        private x265.Presets _preset = x265.Presets.Medium;
        public x265.Presets preset
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

        private x265.Profiles _profile = x265.Profiles.Auto;
        public x265.Profiles profile
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

        private x265.Tunes _tune = x265.Tunes.None;
        public x265.Tunes tune
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

        private bool _high_tier = false;
        public bool high_tier
        {
            get
            {
                return _high_tier;
            }
            set
            {
                _high_tier = value;
            }
        }

        private bool _lossless = false;
        public bool lossless
        {
            get
            {
                return _lossless;
            }
            set
            {
                _lossless = value;
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

        private int _deblockTC = 0;
        public int deblockTC
        {
            get
            {
                return _deblockTC;
            }
            set
            {
                _deblockTC = value;
            }
        }

        private int _deblockBeta = 0;
        public int deblockBeta
        {
            get
            {
                return _deblockBeta;
            }
            set
            {
                _deblockBeta = value;
            }
        }

        private bool _sao = true;
        public bool sao
        {
            get
            {
                return _sao;
            }
            set
            {
                _sao = value;
            }
        }

        private int _subme = 2;
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

        private int _merange = 57;
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

        private int _max_merge = 2;
        public int max_merge
        {
            get
            {
                return _max_merge;
            }
            set
            {
                _max_merge = value;
            }
        }

        private int _rd = 3;
        public int rd
        {
            get
            {
                return _rd;
            }
            set
            {
                _rd = value;
            }
        }

        private int _ctu = 64;
        public int ctu
        {
            get
            {
                return _ctu;
            }
            set
            {
                _ctu = value;
            }
        }

        private bool _cu_lossless = false;
        public bool cu_lossless
        {
            get
            {
                return _cu_lossless;
            }
            set
            {
                _cu_lossless = value;
            }
        }

        private bool _early_skip = false;
        public bool early_skip
        {
            get
            {
                return _early_skip;
            }
            set
            {
                _early_skip = value;
            }
        }

        private bool _rect = false;
        public bool rect
        {
            get
            {
                return _rect;
            }
            set
            {
                _rect = value;
            }
        }

        private bool _amp = false;
        public bool amp
        {
            get
            {
                return _amp;
            }
            set
            {
                _amp = value;
            }
        }

        private int _bframes = 4;
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

        private bool _bpyramid = true;
        public bool bpyramid
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

        private bool _weightb = false;
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

        private bool _weightp = true;
        public bool weightp
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

        private bool _constr_intra = false;
        public bool constr_intra
        {
            get
            {
                return _constr_intra;
            }
            set
            {
                _constr_intra = value;
            }
        }

        private bool _b_intra = false;
        public bool b_intra
        {
            get
            {
                return _b_intra;
            }
            set
            {
                _b_intra = value;
            }
        }

        private int _aqmode = 1; //X265_AQ_VARIANCE
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

        private bool _cutree = true;
        public bool cutree
        {
            get
            {
                return _cutree;
            }
            set
            {
                _cutree = value;
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

        private decimal _psyrdoq = 1.0m;
        public decimal psyrdoq
        {
            get
            {
                return _psyrdoq;
            }
            set
            {
                _psyrdoq = value;
            }
        }

        private int _threads = 0;
        public int threads
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

        private int _threads_frames = 0;
        public int threads_frames
        {
            get
            {
                return _threads_frames;
            }
            set
            {
                _threads_frames = value;
            }
        }

        private int _b_adapt = 2; //X265_B_ADAPT_TRELLIS;
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

        private int _chroma_offset_cb = 0;
        public int chroma_offset_cb
        {
            get
            {
                return _chroma_offset_cb;
            }
            set
            {
                _chroma_offset_cb = value;
            }
        }

        private int _chroma_offset_cr = 0;
        public int chroma_offset_cr
        {
            get
            {
                return _chroma_offset_cr;
            }
            set
            {
                _chroma_offset_cr = value;
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

        private bool _slow_firstpass = false;
        public bool slow_firstpass
        {
            get
            {
                return _slow_firstpass;
            }
            set
            {
                _slow_firstpass = value;
            }
        }

        private int _lookahead = 20;
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

        private bool _open_gop = true;
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

        private int _hash = 0;
        public int hash
        {
            get
            {
                return _hash;
            }
            set
            {
                _hash = value;
            }
        }

        private bool _info = true;
        public bool info
        {
            get
            {
                return _info;
            }
            set
            {
                _info = value;
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

        private bool _hrd = false;
        public bool hrd
        {
            get
            {
                return _hrd;
            }
            set
            {
                _hrd = value;
            }
        }

        private bool _headers_repeat = false;
        public bool headers_repeat
        {
            get
            {
                return _headers_repeat;
            }
            set
            {
                _headers_repeat = value;
            }
        }

        private bool _pmode = false;
        public bool pmode
        {
            get
            {
                return _pmode;
            }
            set
            {
                _pmode = value;
            }
        }

        private bool _pme = false;
        public bool pme
        {
            get
            {
                return _pme;
            }
            set
            {
                _pme = value;
            }
        }

        private bool _wpp = true;
        public bool wpp
        {
            get
            {
                return _wpp;
            }
            set
            {
                _wpp = value;
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
