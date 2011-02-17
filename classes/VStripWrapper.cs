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
        private static extern int ifoOpen(string v_Filename, int v_Flags);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern bool ifoClose(int v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern string ifoGetVideoDesc(int v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int ifoGetNumAudio(int v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int ifoGetNumSubPic(int v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern string ifoGetAudioDesc(int v_IFO_handle, int v_Ptr);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern string ifoGetSubPicDesc(int v_IFO_handle, int v_Ptr);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int ifoGetNumPGCI(int v_IFO_handle);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int ifoGetPGCIInfo(int v_IFO_handle, int v_Ptr, ref TitleTime v_len);

        [DllImport("dlls//VStrip//vStrip.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern bool ifoGetPGCICells(int v_IFO_handle, int v_Ptr, ref t_vs_vobcellid v_cells);

        public struct TitleTime
        {
            public byte s1;
            public byte s2;
            public byte s3;
            public byte s4;
        }

        public struct t_vs_vobcellid
        {
            public int start_lba, end_lba;
            public int vob_id;
            public byte cell_id;
            public byte angle;
            public byte chapter;
            public TitleTime time;
        }

        public struct TIFOProgramChain
        {
            public int NumCells;
            public double Length;
            public t_vs_vobcellid Cells;
        }

        private int Handle;

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
            return ifoGetVideoDesc(Handle);
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
            return ifoGetAudioDesc(Handle, AudioTrack);
        }

        public string GetSubPicsDesc(int SubPic)
        {
            return ifoGetSubPicDesc(Handle, SubPic);
        }

        public int CountPGCI()
        {
            return ifoGetNumPGCI(Handle);
        }

        public TimeSpan Duration()
        {
            TitleTime tt = new TitleTime();
            TimeSpan duration = TimeSpan.Zero;
            TimeSpan c_duration = TimeSpan.Zero;

            for (int i = 0; i < ifoGetNumPGCI(Handle); i++)
            {
                ifoGetPGCIInfo(Handle, i, ref tt);
                c_duration = TimeSpan.FromHours(tt.s1) +
                TimeSpan.FromMinutes(tt.s2) +
                TimeSpan.FromSeconds(tt.s3) +
                TimeSpan.FromMilliseconds(tt.s4);

                //Отбрасываем мусор
                if (c_duration.TotalMinutes > 1)
                    duration += c_duration;
            }

            return duration;
        }

        public string System()
        {
            string info = GetVideoDesc();
            string[] separator = new string[] { " " };
            string[] s = info.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            return s[2];
        }

        public string Width()
        {
            string info = GetVideoDesc();
            string[] separator = new string[] { " " };
            string[] s = info.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            string res = s[1];

            separator = new string[] { "x" };
            s = res.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            return s[0];
        }

        public string Height()
        {
            string info = GetVideoDesc();
            string[] separator = new string[] { " " };
            string[] s = info.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            string res = s[1];

            separator = new string[] { "x" };
            s = res.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            return s[1];
        }
    }
}
