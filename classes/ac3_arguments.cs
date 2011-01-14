using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
    [Serializable]
    public class ac3_arguments
    {
        public ac3_arguments()
        {
        }

        public ac3_arguments Clone()
        {
            return (ac3_arguments)this.MemberwiseClone();
        }

        private int _dnorm = 31;
        public int dnorm
        {
            get
            {
                return _dnorm;
            }
            set
            {
                _dnorm = value;
            }
        }

        private int _bandwidth = -1;
        public int bandwidth
        {
            get
            {
                return _bandwidth;
            }
            set
            {
                _bandwidth = value;
            }
        }
    }
}
