using System;
using System.Collections.Generic;
using System.Text;

namespace XviD4PSP
{
   public class wmv_arguments
    {
       public wmv_arguments()
       {
       }

       public wmv_arguments Clone()
       {
           return (wmv_arguments)this.MemberwiseClone();
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

    }
}
