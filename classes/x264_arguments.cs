using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
   public class x264_arguments
    {
       public x264_arguments()
       {
       }

       public x264_arguments Clone()
       {
           return (x264_arguments)this.MemberwiseClone();
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

        private bool _chroma = true;
        public bool chroma
        {
            get
            {
                return _chroma;
            }
            set
            {
                _chroma= value;
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

        private int _bpyramid = 0;
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

        private bool _fastpskip = true;
        public bool fastpskip
        {
            get
            {
                return _fastpskip;
            }
            set
            {
                _fastpskip = value;
            }
        }

        private bool _dctdecimate = true;
        public bool dctdecimate
        {
            get
            {
                return _dctdecimate;
            }
            set
            {
                _dctdecimate = value;
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

        private int _minquant = 10;
        public int minquant
        {
            get
            {
                return _minquant;
            }
            set
            {
                _minquant = value;
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

        private string _aqmode = "1";
        public string aqmode
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

        private decimal _psyrdo = 1;
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

        private decimal _psytrellis = 0;
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

        private decimal _qcomp = 0.6m;
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

       private string _qp_offset = "0";
       public string qp_offset
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

    }
}
