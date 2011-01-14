using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    [Serializable]
    public class flac_arguments
    {
        public flac_arguments()
        {
        }

        public flac_arguments Clone()
        {
            return (flac_arguments)this.MemberwiseClone();
        }

        private int _level = 5;
        public int level
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

        private int _use_lpc = 1;
        public int use_lpc
        {
            get
            {
                return _use_lpc;
            }
            set
            {
                _use_lpc = value;
            }
        }

        private int _lpc_precision = 15;
        public int lpc_precision
        {
            get
            {
                return _lpc_precision;
            }
            set
            {
                _lpc_precision = value;
            }
        }
    }
}
