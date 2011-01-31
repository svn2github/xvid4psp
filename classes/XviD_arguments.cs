using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    [Serializable]
    public class XviD_arguments
    {
        public XviD_arguments()
        {
        }

        public XviD_arguments Clone()
        {
            return (XviD_arguments)this.MemberwiseClone();
        }

        private string _fourcc = "XVID";
        public string fourcc
        {
            get
            {
                return _fourcc;
            }
            set
            {
                _fourcc = value;
            }
        }

        private int _quality = 6;
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

        private bool _chromame = true;
        public bool chromame
        {
            get
            {
                return _chromame;
            }
            set
            {
                _chromame = value;
            }
        }

        private string _qmatrix = "H263";
        public string qmatrix
        {
            get
            {
                return _qmatrix;
            }
            set
            {
                _qmatrix = value;
            }
        }

        private bool _trellis = true;
        public bool trellis
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

        private int _vhqmode = 1;
        public int vhqmode
        {
            get
            {
                return _vhqmode;
            }
            set
            {
                _vhqmode = value;
            }
        }

        private bool _cartoon = false;
        public bool cartoon
        {
            get
            {
                return _cartoon;
            }
            set
            {
                _cartoon = value;
            }
        }

        private bool _gray = false;
        public bool gray
        {
            get
            {
                return _gray;
            }
            set
            {
                _gray = value;
            }
        }

        private bool _chroma_opt = false;
        public bool chroma_opt
        {
            get
            {
                return _chroma_opt;
            }
            set
            {
                _chroma_opt = value;
            }
        }

        private bool _packedmode = true;
        public bool packedmode
        {
            get
            {
                return _packedmode;
            }
            set
            {
                _packedmode = value;
            }
        }

        private bool _gmc = false;
        public bool gmc
        {
            get
            {
                return _gmc;
            }
            set
            {
                _gmc = value;
            }
        }

        private bool _qpel = false;
        public bool qpel
        {
            get
            {
                return _qpel;
            }
            set
            {
                _qpel = value;
            }
        }

        private bool _bvhq = false;
        public bool bvhq
        {
            get
            {
                return _bvhq;
            }
            set
            {
                _bvhq = value;
            }
        }

        private bool _closedgop = true;
        public bool closedgop
        {
            get
            {
                return _closedgop;
            }
            set
            {
                _closedgop = value;
            }
        }

        private int _bframes = 2;
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

        private int _masking = 0;
        public int masking
        {
            get
            {
                return _masking;
            }
            set
            {
                _masking = value;
            }
        }

        private int _keyint = 300;
        public int keyint
        {
            get
            {
                return _keyint;
            }
            set
            {
                _keyint = value;
            }
        }

        private int _b_ratio = 150;
        public int b_ratio
        {
            get
            {
                return _b_ratio;
            }
            set
            {
                _b_ratio = value;
            }
        }

        private int _b_offset = 100;
        public int b_offset
        {
            get
            {
                return _b_offset;
            }
            set
            {
                _b_offset = value;
            }
        }

        private int _reaction = 16;
        public int reaction
        {
            get
            {
                return _reaction;
            }
            set
            {
                _reaction = value;
            }
        }

        private int _averaging = 100;
        public int averaging
        {
            get
            {
                return _averaging;
            }
            set
            {
                _averaging = value;
            }
        }

        private int _smoother = 100;
        public int smoother
        {
            get
            {
                return _smoother;
            }
            set
            {
                _smoother = value;
            }
        }

        private int _kboost = 10;
        public int kboost
        {
            get
            {
                return _kboost;
            }
            set
            {
                _kboost = value;
            }
        }

        private int _ostrength = 5;
        public int ostrength
        {
            get
            {
                return _ostrength;
            }
            set
            {
                _ostrength = value;
            }
        }

        private int _oimprove = 5;
        public int oimprove
        {
            get
            {
                return _oimprove;
            }
            set
            {
                _oimprove = value;
            }
        }

        private int _odegrade = 5;
        public int odegrade
        {
            get
            {
                return _odegrade;
            }
            set
            {
                _odegrade = value;
            }
        }

        private int _chigh = 0;
        public int chigh
        {
            get
            {
                return _chigh;
            }
            set
            {
                _chigh = value;
            }
        }

        private int _clow = 0;
        public int clow
        {
            get
            {
                return _clow;
            }
            set
            {
                _clow = value;
            }
        }

        private int _overhead = 24;
        public int overhead
        {
            get
            {
                return _overhead;
            }
            set
            {
                _overhead = value;
            }
        }

        private int _vbvmax = 0;
        public int vbvmax
        {
            get
            {
                return _vbvmax;
            }
            set
            {
                _vbvmax = value;
            }
        }

        private int _vbvsize = 0;
        public int vbvsize
        {
            get
            {
                return _vbvsize;
            }
            set
            {
                _vbvsize = value;
            }
        }

        private int _vbvpeak = 0;
        public int vbvpeak
        {
            get
            {
                return _vbvpeak;
            }
            set
            {
                _vbvpeak = value;
            }
        }

        private int _imin = 2;
        public int imin
        {
            get
            {
                return _imin;
            }
            set
            {
                _imin = value;
            }
        }

        private int _imax = 31;
        public int imax
        {
            get
            {
                return _imax;
            }
            set
            {
                _imax = value;
            }
        }

        private int _pmin = 2;
        public int pmin
        {
            get
            {
                return _pmin;
            }
            set
            {
                _pmin = value;
            }
        }

        private int _pmax = 31;
        public int pmax
        {
            get
            {
                return _pmax;
            }
            set
            {
                _pmax = value;
            }
        }

        private int _bmin = 2;
        public int bmin
        {
            get
            {
                return _bmin;
            }
            set
            {
                _bmin = value;
            }
        }

        private int _bmax = 31;
        public int bmax
        {
            get
            {
                return _bmax;
            }
            set
            {
                _bmax = value;
            }
        }

        private int _mins = 0;
        public int mins
        {
            get
            {
                return _mins;
            }
            set
            {
                _mins = value;
            }
        }

        private bool _full_first_pass = false;
        public bool full_first_pass
        {
            get
            {
                return _full_first_pass;
            }
            set
            {
                _full_first_pass = value;
            }
        }

        private decimal _firstpass_q = 2.0M;
        public decimal firstpass_q
        {
            get
            {
                return _firstpass_q;
            }
            set
            {
                _firstpass_q = value;
            }
        }

        private int _metric = 0;
        public int metric
        {
            get
            {
                return _metric;
            }
            set
            {
                _metric = value;
            }
        }
    }
}
