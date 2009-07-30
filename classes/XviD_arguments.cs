using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
   public class XviD_arguments
    {
       public XviD_arguments()
       {
       }

       public XviD_arguments Clone()
       {
           return (XviD_arguments)this.MemberwiseClone();
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

        private bool _grey = false;
        public bool grey
        {
            get
            {
                return _grey;
            }
            set
            {
                _grey = value;
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

        private bool _limimasking = false;
        public bool limimasking
        {
            get
            {
                return _limimasking;
            }
            set
            {
                _limimasking = value;
            }
        }

    }
}
