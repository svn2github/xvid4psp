using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

using System.Collections;
using System.Data;
using System.Runtime.InteropServices; 

namespace XviD4PSP
{
   public class SysInfo
    {
        //Struct to retrive system info
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public uint dwOemId;
            public uint dwPageSize;
            public uint lpMinimumApplicationAddress;
            public uint lpMaximumApplicationAddress;
            public uint dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public uint dwProcessorLevel;
            public uint dwProcessorRevision;
        }
        //struct to retrive memory status
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUS
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public uint dwTotalPhys;
            public uint dwAvailPhys;
            public uint dwTotalPageFile;
            public uint dwAvailPageFile;
            public uint dwTotalVirtual;
            public uint dwAvailVirtual;
        }

        //To get system information
        [DllImport("kernel32")]
        static extern void GetSystemInfo(ref SYSTEM_INFO pSI);

        //To get Memory status
        [DllImport("kernel32")]
        static extern void GlobalMemoryStatus(ref MEMORYSTATUS buf);


        //Cnstants used for processor types
        public const int PROCESSOR_INTEL_386 = 386;
        public const int PROCESSOR_INTEL_486 = 486;
        public const int PROCESSOR_INTEL_PENTIUM = 586;
        public const int PROCESSOR_MIPS_R4000 = 4000;
        public const int PROCESSOR_ALPHA_21064 = 21064;

        public int ProcessorsCount()
        {
            SYSTEM_INFO pSI = new SYSTEM_INFO();
            GetSystemInfo(ref pSI);
            return (int)pSI.dwNumberOfProcessors;
        }

        public void GetAllInfo()
        {
            try
            {
                //SYSTEM_INFO pSI = new SYSTEM_INFO();
                //GetSystemInfo(ref pSI);
                //string CPUType;
                //switch (pSI.dwProcessorType)
                //{

                //    case PROCESSOR_INTEL_386:
                //        CPUType = "Intel 386";
                //        break;
                //    case PROCESSOR_INTEL_486:
                //        CPUType = "Intel 486";
                //        break;
                //    case PROCESSOR_INTEL_PENTIUM:
                //        CPUType = "Intel Pentium";
                //        break;
                //    case PROCESSOR_MIPS_R4000:
                //        CPUType = "MIPS R4000";
                //        break;
                //    case PROCESSOR_ALPHA_21064:
                //        CPUType = "DEC Alpha 21064";
                //        break;
                //    default:
                //        CPUType = "(unknown)";
                //}

                //listBox1.InsertItem(0, "Active Processor Mask :		" + pSI.dwActiveProcessorMask.ToString());
                //listBox1.InsertItem(1, "Allocation Granularity :		" + pSI.dwAllocationGranularity.ToString());
                //listBox1.InsertItem(2, "Number Of Processors :		" + pSI.dwNumberOfProcessors.ToString());
                //listBox1.InsertItem(3, "OEM ID :				" + pSI.dwOemId.ToString());
                //listBox1.InsertItem(4, "Page Size :			" + pSI.dwPageSize.ToString());
                //// Processor Level (Req filtering to get level)
                //listBox1.InsertItem(5, "Processor Level Value :		" + pSI.dwProcessorLevel.ToString());
                //listBox1.InsertItem(6, "Processor Revision :		" + pSI.dwProcessorRevision.ToString());
                //listBox1.InsertItem(7, "CPU type :			" + CPUType);
                //listBox1.InsertItem(8, "Maximum Application Address :	" + pSI.lpMaximumApplicationAddress.ToString());
                //listBox1.InsertItem(9, "Minimum Application Address :	" + pSI.lpMinimumApplicationAddress.ToString());

                ///**************	To retrive info from GlobalMemoryStatus ****************/

                //MEMORYSTATUS memSt = new MEMORYSTATUS();
                //GlobalMemoryStatus(ref memSt);

                //listBox1.InsertItem(10, "Available Page File :		" + (memSt.dwAvailPageFile / 1024).ToString());
                //listBox1.InsertItem(11, "Available Physical Memory :		" + (memSt.dwAvailPhys / 1024).ToString());
                //listBox1.InsertItem(12, "Available Virtual Memory :		" + (memSt.dwAvailVirtual / 1024).ToString());
                //listBox1.InsertItem(13, "Size of structur :			" + memSt.dwLength.ToString());
                //listBox1.InsertItem(14, "Memory In Use :			" + memSt.dwMemoryLoad.ToString());
                //listBox1.InsertItem(15, "Total Page Size :			" + (memSt.dwTotalPageFile / 1024).ToString());
                //listBox1.InsertItem(16, "Total Physical Memory :		" + (memSt.dwTotalPhys / 1024).ToString());
                //listBox1.InsertItem(17, "Total Virtual Memory :		" + (memSt.dwTotalVirtual / 1024).ToString());

            }
            catch (Exception)
            {
  
            }
        }




    }
}
