using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    [Serializable]
    public class Trim
    {
        public Trim()
        {
        }

        public Trim Clone()
        {
            return (Trim)this.MemberwiseClone();
        }

        private int _start = -1;
        public int start
        {
            get
            {
                return _start;
            }
            set
            {
                _start = value;
            }
        }

        private int _end = -1;
        public int end
        {
            get
            {
                return _end;
            }
            set
            {
                _end = value;
            }
        }
    }
}
