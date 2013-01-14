using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Collections;

namespace XviD4PSP
{
    public enum AviSynthColorspace : int
    {
        Undefined = 0,
        YV12 = -1610612728,
        RGB24 = +1342177281,
        RGB32 = +1342177282,
        YUY2 = +1610612740,
        I420 = -1610612720,
        IYUV = I420,

        //2.6
        Y8 = -536870912,
        YV16 = -1610611960,
        YV24 = -1610611957,
        YV411 = -1610611959
    }

    public enum AudioSampleType : int
    {
        Undefined = 0,
        INT8 = 1,
        INT16 = 2,
        INT24 = 4,    // Int24 is a very stupid thing to code, but it's supported by some hardware.
        INT32 = 8,
        FLOAT = 16
    };

    public enum MTMode : int
    {
        Undefined = 0,  //Ничего не делать с MT
        Disabled = 1,   //Запретить MT, вызвав SetMTMode(0) перед импортом скрипта
        AddDistr = 2,   //При MT-режимах добавлять Distributor() после импорта скрипта
        AddM1Distr = 4  //-//-, но перед Distributor() вызывать SetMTMode(1)
    };

    public class AviSynthException : ApplicationException
    {
        public AviSynthException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public AviSynthException(string message)
            : base(message)
        {
        }

        public AviSynthException()
            : base()
        {
        }

        public AviSynthException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public sealed class AviSynthScriptEnvironment : IDisposable
    {
        public AviSynthScriptEnvironment()
        {
        }

        public static string GetLastError()
        {
            return null;
        }

        public IntPtr Handle
        {
            get
            {
                return new IntPtr(0);
            }
        }

        public AviSynthClip OpenScriptFile(string filePath, AviSynthColorspace forceColorSpace, AudioSampleType forceSampleType)
        {
            return new AviSynthClip("Import", filePath, forceColorSpace, forceSampleType); //, this);
        }

        public AviSynthClip ParseScript(string script, AviSynthColorspace forceColorSpace, AudioSampleType forceSampleType)
        {
            return new AviSynthClip("Eval", script, forceColorSpace, forceSampleType); //, this);
        }

        void IDisposable.Dispose()
        {
        }
    }

    public class AviSynthClip : IDisposable
    {
        #region PInvoke related stuff
        [StructLayout(LayoutKind.Sequential)]
        struct AVSDLLVideoInfo
        {
            public MTMode mt_import;

            //Video
            public int width;
            public int height;
            public int raten;
            public int rated;
            public int num_frames;
            public int field_based;
            public int first_field;
            public AviSynthColorspace pixel_type_orig;
            public AviSynthColorspace pixel_type;

            // Audio
            public int audio_samples_per_second;
            public AudioSampleType sample_type_orig;
            public AudioSampleType sample_type;
            public int nchannels;
            public long num_audio_samples;
        }

        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_init(ref IntPtr avs, string func, string arg, ref AVSDLLVideoInfo vi);
        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_invoke(IntPtr avs, string func, string[] args, int len, ref AVSDLLVideoInfo vi, ref float func_out);
        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_destroy(ref IntPtr avs);

        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_isfuncexists(IntPtr avs, string name);
        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_getlasterror(IntPtr avs, [MarshalAs(UnmanagedType.LPStr)] StringBuilder sb, int len);
        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_getvariable_b(IntPtr avs, string name, ref bool val);
        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_getvariable_i(IntPtr avs, string name, ref int val);
        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_getvariable_f(IntPtr avs, string name, ref float val);
        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_getvariable_s(IntPtr avs, string name, [MarshalAs(UnmanagedType.LPStr)] StringBuilder sb, int len);

        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_getaframe(IntPtr avs, IntPtr buf, long sampleNo, long sampleCount);
        [DllImport("dlls//AviSynth//AvisynthWrapper", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Ansi)]
        private static extern int dimzon_avs_getvframe(IntPtr avs, IntPtr buf, int stride, int frm);
        #endregion

        private IntPtr _avs;
        private AVSDLLVideoInfo _vi;
        private static object locker = new object();

        private const int ERRMSG_LEN = 1024;      //Макс. длина текста в pstr->err
        private const int AVS_VARNFOUND = 666;    //Нет такой переменной\фукнции (avs_invoke, avs_isfuncexists, avs_getvariable_b\i\f\s)
        private const int AVS_VARNDEFINED = 999;  //Переменной не присвоено значение (avs_getvariable_b\i\f\s)
        private const int AVS_VARWRNGTYPE = -999; //Переменная оказалась другого типа (avs_getvariable_b\i\f\s)
        private const int AVS_GERROR = -1;        //Всё, что попалось в catch как AvisynthError

        #region Clip Properties

        public bool HasVideo
        {
            get
            {
                return VideoWidth > 0 && VideoHeight > 0;
            }
        }

        public int VideoWidth
        {
            get
            {
                return _vi.width;
            }
        }

        public int VideoHeight
        {
            get
            {
                return _vi.height;
            }
        }

        public int raten
        {
            get
            {
                return _vi.raten;
            }
        }

        public int rated
        {
            get
            {
                return _vi.rated;
            }
        }

        public int field_based
        {
            get
            {
                //0-false, 1-true
                return _vi.field_based;
            }
        }

        public int first_field
        {
            get
            {
                //0-?, 1-TFF, 2-BFF
                return _vi.first_field;
            }
        }

        public int num_frames
        {
            get
            {
                return _vi.num_frames;
            }
        }

        // Audio
        public int AudioSampleRate
        {
            get
            {
                return _vi.audio_samples_per_second;
            }
        }

        public long SamplesCount
        {
            get
            {
                return _vi.num_audio_samples;
            }
        }

        public AudioSampleType SampleType
        {
            get
            {
                return _vi.sample_type;
            }
        }

        public short ChannelsCount
        {
            get
            {
                return (short)_vi.nchannels;
            }
        }

        public AviSynthColorspace PixelType
        {
            get
            {
                return _vi.pixel_type;
            }
        }

        public AviSynthColorspace OriginalColorspace
        {
            get
            {
                return _vi.pixel_type_orig;
            }
        }

        public AudioSampleType OriginalSampleType
        {
            get
            {
                return _vi.sample_type_orig;
            }
        }

        #endregion

        //Полноценное открытие: инициализация + Invoke + GetVideoInfo (func обязательно должна возвращать Clip с видео!)
        public AviSynthClip(string func, string arg, AviSynthColorspace forceColorSpace, AudioSampleType forceSampleType) //, AviSynthScriptEnvironment env)
        {
            _avs = new IntPtr(0);
            _vi = new AVSDLLVideoInfo();
            _vi.mt_import = (SysInfo.AVSIsMT) ? Settings.MTMode_Internal : MTMode.Undefined;

            //Эти два поля работают и на вход, и на выход. Для "dimzon_avs_init" через них можно задать требуемые на выходе PixelType и SampleType.
            //При нуле (Undefined) никаких преобразований не будет. На выходе из "dimzon_avs_init" поля будут содержать то, что получилось в итоге.
            //В "dimzon_avs_invoke" работают только на выход.
            _vi.pixel_type = forceColorSpace;
            _vi.sample_type = forceSampleType;

            //Эти поля содержат инфу об исходных PixelType и SampleType, т.е. до того, как они были изменены в "dimzon_avs_init".
            //В "dimzon_avs_invoke" обновляются только в случае их равенства нулю.
            _vi.pixel_type_orig = AviSynthColorspace.Undefined;
            _vi.sample_type_orig = AudioSampleType.Undefined;
  
            if (0 != dimzon_avs_init(ref _avs, func, arg, ref _vi))
            {
                string err = GetLastError();
                cleanup(false);
                throw new AviSynthException(err);
            }
        }

        //Только инициализация
        public AviSynthClip()
        {
            _avs = new IntPtr(0);
            _vi = new AVSDLLVideoInfo();

            //Просто получаем Handle и выходим
            if (0 != dimzon_avs_init(ref _avs, null, null, ref _vi))
            {
                string err = GetLastError();
                cleanup(false);
                throw new AviSynthException(err);
            }
        }

        public float Invoke(string function, string[] args, float default_value)
        {
            #region Description for Invoke
            //Для функций, которые не принимают\не требуют параметров: args = new string[] { } или null
            //Для функций, которым требуется входной клип: args = new string[] { "last" }
            //Для передачи функции доп. параметров: args = new string[] { "last", "Rec709", "10", "98.6" }
            //Для передачи параметров не-клип функции: new string[] { "12345", "Orange", "true" }
            //Всего в args можно передать до 10-ти параметров (включая last), лишнее обрезается.
            //Всё передается как string, "расшифровка и восстановление" типов в AviSynthWrapper`е - по косвенным признакам.
            //Но можно использовать и Eval (хотя работает он иногда странновато): Invoke("Eval", new string[] { "SetMTMode(5, 4)" }, 0);
            //Возвращаемое значение преобразуется в float и выводится через func_out в виде:
            //Invoke вернул   Clip: MinValue (и для оставшегося неперечисленного)
            //Invoke вернул   Bool: 0 - false, MaxValue - true
            //Invoke вернул    Int: (float)value
            //Invoke вернул  Float: value
            //Invoke вернул String: MaxValue (текст в pstr->err)
            #endregion

            float func_out = float.MinValue;
            if (0 != dimzon_avs_invoke(_avs, function, args, ((args != null) ? args.Length : 0), ref _vi, ref func_out))
            {
                string err = GetLastError();
                cleanup(false);
                throw new AviSynthException(err);
            }
            return (func_out != float.MinValue) ? func_out : default_value;
        }

        public string Invoke(string function, string[] args, string default_value)
        {
            float func_out = float.MinValue;
            if (0 != dimzon_avs_invoke(_avs, function, args, ((args != null) ? args.Length : 0), ref _vi, ref func_out))
            {
                string err = GetLastError();
                cleanup(false);
                throw new AviSynthException(err);
            }
            return (func_out == float.MaxValue) ? GetLastError() : default_value;
        }

        private void cleanup(bool disposing)
        {
            if (_avs != IntPtr.Zero)
            {
                lock (locker)
                {
                    if (_avs != IntPtr.Zero)
                    {
                        //Позаимствовано из MeGUI (для уменьшения вылетов из-за DGMultiSource).
                        //Видимо сто лет как не актуально, но пусть уж остаётся..
                        System.Threading.Thread.Sleep(100);

                        try
                        {
                            dimzon_avs_destroy(ref _avs);
                        }
                        catch (DllNotFoundException) { }

                        _avs = new IntPtr(0);
                        if (disposing)
                            GC.SuppressFinalize(this);
                    }
                }
            }
        }

        ~AviSynthClip()
        {
            cleanup(false);
        }

        void IDisposable.Dispose()
        {
            cleanup(true);
        }

        public bool IsFuncExists(string name)
        {
            int res = dimzon_avs_isfuncexists(_avs, name);
            if (res < 0) throw new AviSynthException(GetLastError());
            return (res == 0);
        }

        private string GetLastError()
        {
            StringBuilder sb = new StringBuilder(ERRMSG_LEN);
            sb.Length = dimzon_avs_getlasterror(_avs, sb, ERRMSG_LEN);
            return sb.ToString();
        }

        public bool GetVarBoolean(string variableName, bool defaultValue)
        {
            int res = 0;
            bool v = false;
            res = dimzon_avs_getvariable_b(_avs, variableName, ref v);
            if (res < 0) throw new AviSynthException(GetLastError());
            return (res == 0) ? v : defaultValue;
        }

        public int GetVarInteger(string variableName, int defaultValue)
        {
            int v = 0, res = 0;
            res = dimzon_avs_getvariable_i(_avs, variableName, ref v);
            if (res < 0) throw new AviSynthException(GetLastError());
            return (res == 0) ? v : defaultValue;
        }

        public float GetVarFloat(string variableName, float defaultValue)
        {
            int res = 0;
            float v = 0;
            res = dimzon_avs_getvariable_f(_avs, variableName, ref v);
            if (res < 0) throw new AviSynthException(GetLastError());
            return (res == 0) ? v : defaultValue;
        }

        public string GetVarString(string variableName, string defaultValue)
        {
            StringBuilder sb = new StringBuilder(ERRMSG_LEN);
            int res = dimzon_avs_getvariable_s(_avs, variableName, sb, ERRMSG_LEN);
            if (res < 0) throw new AviSynthException(GetLastError());
            return (res == 0) ? sb.ToString() : defaultValue;
        }

        public void ReadAudio(IntPtr addr, long offset, int count)
        {
            if (0 != dimzon_avs_getaframe(_avs, addr, offset, count))
                throw new AviSynthException(GetLastError());
        }

        public void ReadAudio(byte buffer, long offset, int count)
        {
            GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                ReadAudio(h.AddrOfPinnedObject(), offset, count);
            }
            finally
            {
                h.Free();
            }
        }

        public void ReadFrame(IntPtr addr, int stride, int frame)
        {
            if (0 != dimzon_avs_getvframe(_avs, addr, stride, frame))
                throw new AviSynthException(GetLastError());
        }

        public short BitsPerSample
        {
            get
            {
                return (short)(BytesPerSample * 8);
            }
        }

        public short BytesPerSample
        {
            get
            {
                switch (SampleType)
                {
                    case AudioSampleType.INT8:
                        return 1;
                    case AudioSampleType.INT16:
                        return 2;
                    case AudioSampleType.INT24:
                        return 3;
                    case AudioSampleType.INT32:
                        return 4;
                    case AudioSampleType.FLOAT:
                        return 4;
                    default:
                        throw new ArgumentException(SampleType.ToString());
                }
            }
        }

        public int AvgBytesPerSec
        {
            get
            {
                return AudioSampleRate * ChannelsCount * BytesPerSample;
            }
        }

        public long AudioSizeInBytes
        {
            get
            {
                return SamplesCount * ChannelsCount * BytesPerSample;
            }
        }
    }
}
