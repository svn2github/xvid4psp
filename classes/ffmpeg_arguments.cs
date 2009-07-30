using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
   public class ffmpeg_arguments
    {
  
       public ffmpeg_arguments()
       {
       }

       public ffmpeg_arguments Clone()
       {
           return (ffmpeg_arguments)this.MemberwiseClone();
       }

        private int _memethod = 2;
        public int memethod
        {
            get
            {
                return _memethod;
            }
            set
            {
                _memethod = value;
            }
        }

        private string _intramatrix;
        public string intramatrix
        {
            get
            {
                return _intramatrix;
            }
            set
            {
                _intramatrix = value;
            }
        }

        private string _intermatrix;
        public string intermatrix
        {
            get
            {
                return _intermatrix;
            }
            set
            {
                _intermatrix = value;
            }
        }

        private bool _trellis = false;
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

        private bool _aiv = false;
        public bool aiv
        {
            get
            {
                return _aiv;
            }
            set
            {
                _aiv = value;
            }
        }

        private bool _cbp = false;
        public bool cbp
        {
            get
            {
                return _cbp;
            }
            set
            {
                _cbp = value;
            }
        }

        private int _cmp = 0;
        public int cmp
        {
            get
            {
                return _cmp;
            }
            set
            {
                _cmp = value;
            }
        }

        private bool _obmc = false;
        public bool obmc
        {
            get
            {
                return _obmc;
            }
            set
            {
                _obmc = value;
            }
        }

        private bool _aic = false;
        public bool aic
        {
            get
            {
                return _aic;
            }
            set
            {
                _aic = value;
            }
        }

        private bool _qprd = false;
        public bool qprd
        {
            get
            {
                return _qprd;
            }
            set
            {
                _qprd = value;
            }
        }

        private string _mvectors = "Disabled";
        public string mvectors
        {
            get
            {
                return _mvectors;
            }
            set
            {
                _mvectors = value;
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

        private bool _closedgop = false;
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

        private bool _bitexact = false;
        public bool bitexact
        {
            get
            {
                return _bitexact;
            }
            set
            {
                _bitexact = value;
            }
        }

        private bool _intra = false;
        public bool intra
        {
            get
            {
                return _intra;
            }
            set
            {
                _intra = value;
            }
        }

        private int _bframes = 0;
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

        private string _bdecision = "Disabled";
        public string bdecision
        {
            get
            {
                return _bdecision;
            }
            set
            {
                _bdecision = value;
            }
        }

        private string _brefine = "Disabled";
        public string brefine
        {
            get
            {
                return _brefine;
            }
            set
            {
                _brefine = value;
            }
        }

        private string _mbd = "simple";
        public string mbd
        {
            get
            {
                return _mbd;
            }
            set
            {
                _mbd = value;
            }
        }

        private int _gopsize = 0;
        public int gopsize
        {
            get
            {
                return _gopsize;
            }
            set
            {
                _gopsize = value;
            }
        }

        private string _fourcc_xvid = "XVID";
        public string fourcc_xvid
        {
            get
            {
                return _fourcc_xvid;
            }
            set
            {
                _fourcc_xvid = value;
            }
        }

        private string _fourcc_dv = "dvsd";
        public string fourcc_dv
        {
            get
            {
                return _fourcc_dv;
            }
            set
            {
                _fourcc_dv = value;
            }
        }

        private string _fourcc_mpeg4 = "DIVX";
        public string fourcc_mpeg4
        {
            get
            {
                return _fourcc_mpeg4;
            }
            set
            {
                _fourcc_mpeg4 = value;
            }
        }

        private string _fourcc_huff = "HFYU";
        public string fourcc_huff
        {
            get
            {
                return _fourcc_huff;
            }
            set
            {
                _fourcc_huff = value;
            }
        }

        private string _colorspace = "YV12";
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

        private string _predictor = "Plane";
        public string predictor
        {
            get
            {
                return _predictor;
            }
            set
            {
                _predictor = value;
            }
        }

        private string _fourcc_mpeg1 = "MPEG";
        public string fourcc_mpeg1
        {
            get
            {
                return _fourcc_mpeg1;
            }
            set
            {
                _fourcc_mpeg1 = value;
            }
        }

        private string _fourcc_mpeg2 = "MPEG";
        public string fourcc_mpeg2
        {
            get
            {
                return _fourcc_mpeg2;
            }
            set
            {
                _fourcc_mpeg2 = value;
            }
        }

        private string _dvpreset = "DVCAM";
        public string dvpreset
        {
            get
            {
                return _dvpreset;
            }
            set
            {
                _dvpreset = value;
            }
        }

        private string _codertype = "VLC";
        public string codertype
        {
            get
            {
                return _codertype;
            }
            set
            {
                _codertype = value;
            }
        }

        private string _contextmodel = "Small";
        public string contextmodel
        {
            get
            {
                return _contextmodel;
            }
            set
            {
                _contextmodel = value;
            }
        }

        private int _minbitrate = 0;
        public int minbitrate
        {
            get
            {
                return _minbitrate;
            }
            set
            {
                _minbitrate = value;
            }
        }

        private int _maxbitrate = 0;
        public int maxbitrate
        {
            get
            {
                return _maxbitrate;
            }
            set
            {
                _maxbitrate = value;
            }
        }

        private int _bittolerance = 0;
        public int bittolerance
        {
            get
            {
                return _bittolerance;
            }
            set
            {
                _bittolerance = value;
            }
        }


        private int _buffsize = 0;
        public int buffsize
        {
            get
            {
                return _buffsize;
            }
            set
            {
                _buffsize = value;
            }
        }

       //     
        private int _dia_size = 0;
        public int dia_size
        {
            get
            {
                return _dia_size;
            }
            set
            {
                _dia_size = value;
            }
        }  
   
   
   }
}
