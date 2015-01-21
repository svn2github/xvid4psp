using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace XviD4PSP
{
    public class VStripWrapper
    {
        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern IntPtr ifoOpen(string v_Filename, int v_Flags);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern bool ifoClose(IntPtr v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern IntPtr ifoGetVideoDesc(IntPtr v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int ifoGetNumAudio(IntPtr v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int ifoGetNumSubPic(IntPtr v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern IntPtr ifoGetAudioDesc(IntPtr v_IFO_handle, int v_Ptr);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern IntPtr ifoGetSubPicDesc(IntPtr v_IFO_handle, int v_Ptr);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int ifoGetNumPGCI(IntPtr v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int ifoGetPGCIInfo(IntPtr v_IFO_handle, int v_Ptr, ref TitleTime v_len);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern bool ifoGetPGCICells(IntPtr v_IFO_handle, int v_Ptr, IntPtr v_cells);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TitleTime
        {
            public byte hours;
            public byte minutes;
            public byte seconds;
            public byte frames;  //The two high bits are the frame rate (VStrip.dll)
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct t_vs_vobcellid
        {
            public uint start_lba;
            public uint end_lba;
            public UInt16 vob_id;
            public byte cell_id;
            public byte angle;
            public byte chapter;
            public TitleTime time;
        }

        private struct SimpleCell
        {
            public uint start;
            public uint end;
            public TitleTime time;
        }

        private IntPtr Handle;

        public void Open(string file)
        {
            Handle = ifoOpen(file, 0);
        }

        public void Close()
        {
            ifoClose(Handle);
        }

        public string GetVideoDesc()
        {
            return Marshal.PtrToStringAnsi(ifoGetVideoDesc(Handle));
        }

        public int CountAudio()
        {
            return ifoGetNumAudio(Handle);
        }

        public int CountSubPics()
        {
            return ifoGetNumSubPic(Handle);
        }

        public string GetAudioDesc(int AudioTrack)
        {
            return Marshal.PtrToStringAnsi(ifoGetAudioDesc(Handle, AudioTrack));
        }

        public string GetSubPicsDesc(int SubPic)
        {
            return Marshal.PtrToStringAnsi(ifoGetSubPicDesc(Handle, SubPic));
        }

        public int CountPGCI()
        {
            return ifoGetNumPGCI(Handle);
        }

        public TimeSpan Duration()
        {
            TitleTime tt = new TitleTime();
            TimeSpan duration = TimeSpan.Zero;
            List<SimpleCell> SortedCells = new List<SimpleCell>();

            int pgc_num = ifoGetNumPGCI(Handle);
            for (int i = 0; i < pgc_num; i++)
            {
                int num_cells = ifoGetPGCIInfo(Handle, i, ref tt);
                IntPtr ptr_cells = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(t_vs_vobcellid)) * num_cells);
                if (ptr_cells != IntPtr.Zero)
                {
                    try
                    {
                        if (ifoGetPGCICells(Handle, i, ptr_cells))
                        {
                            IntPtr ptr_cell = ptr_cells;
                            for (int k = 0; k < num_cells; k++)
                            {
                                t_vs_vobcellid cell = (t_vs_vobcellid)Marshal.PtrToStructure(ptr_cell, typeof(t_vs_vobcellid));
                                ptr_cell = new IntPtr(ptr_cell.ToInt64() + Marshal.SizeOf(typeof(t_vs_vobcellid))); //ptr += offset

                                //Сохраняем инфу от cell, если у нас такой еще нет (берутся только start\end_lba и time)
                                SimpleCell obj = new SimpleCell() { start = cell.start_lba, end = cell.end_lba, time = cell.time };
                                if (!SortedCells.Contains(obj))
                                    SortedCells.Add(obj);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr_cells);
                    }
                }
            }

            foreach (SimpleCell cell in SortedCells)
            {
                #region frames bits
                /*
                1 0 0 1 0 0 1 0  (73)
                1 0 0 1 0 0 0 0  (9, & 0x3f) 25fps
                0 0 0 0 0 0 1 0  (64, & ~0x3f) 25fps

                0 1 0 1 0 0 0 0  (10, & 0x3f) 29fps
                0 0 0 0 0 0 1 1  (192, & ~0x3f) 29fps
                */
                #endregion

                duration += TimeSpan.FromHours(cell.time.hours) +
                    TimeSpan.FromMinutes(cell.time.minutes) +
                    TimeSpan.FromSeconds(cell.time.seconds) +
                    TimeSpan.FromMilliseconds((cell.time.frames & 0x3f) * //Перевод кадров в ms
                    (((cell.time.frames & (1 << 7)) != 0) ? 1000.0 / (30000.0 / 1001.0) : 40.0));
            }

            return duration;
        }

        public string GetVideoInfo()
        {
            //"MPEG2 720x576 PAL 4:3  "
            //"MPEG2 720x576 PAL 16:9 letbox "
            //"MPEG2 720x480 NTSC 16:9 letbox " //+ "pan&scan "
            string info = GetVideoDesc();
            if (info.Length > 20)
            {
                string[] res = info.Split(new char[] { ' ' });
                if (res.Length > 3) return res[1] + " " + res[3] + " " + res[2];
            }

            return "Unknown";
        }
    }
}
