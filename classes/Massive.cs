using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace XviD4PSP
{
    public class Massive
    {
        public enum InterlaceModes { Progressive = 1, Interlaced }

        public Massive()
        {
            this.x264options = new x264_arguments();
            this.XviD_options = new XviD_arguments();
            this.ffmpeg_options = new ffmpeg_arguments();
            this.wmv_options = new wmv_arguments();
            this.aac_options = new aac_arguments();
            this.mp3_options = new mp3_arguments();
            this.ac3_options = new ac3_arguments();
            this.flac_options = new flac_arguments();

            this.vpasses = new ArrayList();
            this.inaudiostreams = new ArrayList();
            this.outaudiostreams = new ArrayList();
        }

        public Massive Clone()
        {
            Massive temp = (Massive)this.MemberwiseClone();
            temp.x264options = this.x264options.Clone();
            temp.XviD_options = this.XviD_options.Clone();
            temp.wmv_options = this.wmv_options.Clone();
            temp.ffmpeg_options = this.ffmpeg_options.Clone();
            temp.aac_options = this.aac_options.Clone();
            temp.mp3_options = this.mp3_options.Clone();
            temp.ac3_options = this.ac3_options.Clone();
            temp.flac_options = this.flac_options.Clone();
            temp.vpasses = (ArrayList)this.vpasses.Clone();
            temp.inaudiostreams = (ArrayList)this.inaudiostreams.Clone();
            temp.outaudiostreams = (ArrayList)this.outaudiostreams.Clone();
            //Это решит проблему с неправильным клонированием AudioStreams, но создаст новые проблемы
            //for (int i = 0; this.inaudiostreams.Count > 0 && i < this.inaudiostreams.Count; i++)
            //{
            //    temp.inaudiostreams[i] = ((AudioStream)this.inaudiostreams[i]).Clone();
            //}
            //for (int i = 0; this.outaudiostreams.Count > 0 && i < this.outaudiostreams.Count; i++)
            //{
            //    temp.outaudiostreams[i] = ((AudioStream)this.outaudiostreams[i]).Clone();
            //}

            return temp;
        }

        private System.Windows.Window _owner;
        public System.Windows.Window owner
        {
            get
            {
                return _owner;
            }
            set
            {
                _owner = value;
            }
        }

        private string _key;
        public string key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }

        private string _taskname;
        public string taskname
        {
            get
            {
                return _taskname;
            }
            set
            {
                _taskname = value;
            }
        }

        private string _infilepath;
        public string infilepath
        {
            get
            {
                return _infilepath;
            }
            set
            {
                _infilepath = value;
            }
        }

        private string _infilepath_source;
        public string infilepath_source
        {
            get
            {
                return _infilepath_source;
            }
            set
            {
                _infilepath_source = value;
            }
        }

        private string[] _infileslist;
        public string[] infileslist
        {
            get
            {
                return _infileslist;
            }
            set
            {
                _infileslist = value;
            }
        }

        private string _dvdname;
        public string dvdname
        {
            get
            {
                return _dvdname;
            }
            set
            {
                _dvdname = value;
            }
        }

        private string _indexfile;
        public string indexfile
        {
            get
            {
                return _indexfile;
            }
            set
            {
                _indexfile = value;
            }
        }

        private string _oldindexfile;
        public string oldindexfile
        {
            get
            {
                return _oldindexfile;
            }
            set
            {
                _oldindexfile = value;
            }
        }

        private string _outfilepath;
        public string outfilepath
        {
            get
            {
                return _outfilepath;
            }
            set
            {
                _outfilepath = value;
            }
        }

        private string _outvideofile;
        public string outvideofile
        {
            get
            {
                return _outvideofile;
            }
            set
            {
                _outvideofile = value;
            }
        }

        private string _invcodec;
        public string invcodec
        {
            get
            {
                return _invcodec;
            }
            set
            {
                _invcodec = value;
            }
        }

        private string _invcodecshort;
        public string invcodecshort
        {
            get
            {
                return _invcodecshort;
            }
            set
            {
                _invcodecshort = value;
            }
        }

        private string _sar;
        public string sar
        {
            get
            {
                return _sar;
            }
            set
            {
                _sar = value;
            }
        }

        private string _outvcodec;
        public string outvcodec
        {
            get
            {
                return _outvcodec;
            }
            set
            {
                _outvcodec = value;
            }
        }

        private ArrayList _vpasses;
        public ArrayList vpasses
        {
            get
            {
                return _vpasses;
            }
            set
            {
                _vpasses = value;
            }
        }

        private AviSynthScripting.Decoders _vdecoder;
        public AviSynthScripting.Decoders vdecoder
        {
            get
            {
                return _vdecoder;
            }
            set
            {
                _vdecoder = value;
            }
        }

        private AviSynthScripting.SamplerateModifers _sampleratemodifer = Settings.SamplerateModifer;
        public AviSynthScripting.SamplerateModifers sampleratemodifer
        {
            get
            {
                return _sampleratemodifer;
            }
            set
            {
                _sampleratemodifer = value;
            }
        }

        private string _volumeaccurate = Settings.VolumeAccurate;
        public string volumeaccurate
        {
            get
            {
                return _volumeaccurate;
            }
            set
            {
                _volumeaccurate = value;
            }
        }

        private string _volume;
        public string volume
        {
            get
            {
                return _volume;
            }
            set
            {
                _volume = value;
            }
        }

        private int _inresw = 0;
        public int inresw
        {
            get
            {
                return _inresw;
            }
            set
            {
                _inresw = value;
            }
        }

        private int _inresh = 0;
        public int inresh
        {
            get
            {
                return _inresh;
            }
            set
            {
                _inresh = value;
            }
        }

        private double _inaspect;
        public double inaspect
        {
            get
            {
                return _inaspect;
            }
            set
            {
                _inaspect = value;
            }
        }

        private double _outaspect;
        public double outaspect
        {
            get
            {
                return _outaspect;
            }
            set
            {
                _outaspect = value;
            }
        }

        private double _pixelaspect = 1.0;
        public double pixelaspect
        {
            get
            {
                return _pixelaspect;
            }
            set
            {
                _pixelaspect = value;
            }
        }

        private bool _isanamorphic = false;
        public bool IsAnamorphic
        {
            get
            {
                return _isanamorphic;
            }
            set
            {
                _isanamorphic = value;
            }
        }

        private AspectResolution.AspectFixes _aspectfix = AspectResolution.AspectFixes.Disabled;
        public AspectResolution.AspectFixes aspectfix
        {
            get
            {
                return _aspectfix;
            }
            set
            {
                _aspectfix = value;
            }
        }

        private int _inaudiostream = 0;
        public int inaudiostream
        {
            get
            {
                return _inaudiostream;
            }
            set
            {
                _inaudiostream = value;
            }
        }

        private int _invideostream_mkvid = 0;
        public int invideostream_mkvid
        {
            get
            {
                return _invideostream_mkvid;
            }
            set
            {
                _invideostream_mkvid = value;
            }
        }

        private int _invideostream_ffid = 0;
        public int invideostream_ffid
        {
            get
            {
                return _invideostream_ffid;
            }
            set
            {
                _invideostream_ffid = value;
            }
        }

        private int _intextstreams;
        public int intextstreams
        {
            get
            {
                return _intextstreams;
            }
            set
            {
                _intextstreams = value;
            }
        }

        private TimeSpan _induration;
        public TimeSpan induration
        {
            get
            {
                return _induration;
            }
            set
            {
                _induration = value;
            }
        }

        private TimeSpan _outduration;
        public TimeSpan outduration
        {
            get
            {
                return _outduration;
            }
            set
            {
                _outduration = value;
            }
        }

        private int _inframes;
        public int inframes
        {
            get
            {
                return _inframes;
            }
            set
            {
                _inframes = value;
            }
        }

        private int _outframes;
        public int outframes
        {
            get
            {
                return _outframes;
            }
            set
            {
                _outframes = value;
            }
        }

        private int _invbitrate;
        public int invbitrate
        {
            get
            {
                return _invbitrate;
            }
            set
            {
                _invbitrate = value;
            }
        }

        private decimal _outvbitrate;
        public decimal outvbitrate
        {
            get
            {
                return _outvbitrate;
            }
            set
            {
                _outvbitrate = value;
            }
        }

        private string _standart;
        public string standart
        {
            get
            {
                return _standart;
            }
            set
            {
                _standart = value;
            }
        }

        private int _outresw;
        public int outresw
        {
            get
            {
                return _outresw;
            }
            set
            {
                _outresw = value;
            }
        }

        private int _outresh;
        public int outresh
        {
            get
            {
                return _outresh;
            }
            set
            {
                _outresh = value;
            }
        }

        private Format.ExportFormats _format;
        public Format.ExportFormats format
        {
            get
            {
                return _format;
            }
            set
            {
                _format = value;
            }
        }

        private string _subtitlepath;
        public string subtitlepath
        {
            get
            {
                return _subtitlepath;
            }
            set
            {
                _subtitlepath = value;
            }
        }

        private SourceType _interlace = SourceType.UNKNOWN;
        public SourceType interlace
        {
            get
            {
                return _interlace;
            }
            set
            {
                _interlace = value;
            }
        }

        private FieldOrder _fieldOrder = FieldOrder.UNKNOWN;
        public FieldOrder fieldOrder
        {
            get
            {
                return _fieldOrder;
            }
            set
            {
                _fieldOrder = value;
            }
        }

        private bool _IsPullDown = false;
        public bool IsPullDown
        {
            get
            {
                return _IsPullDown;
            }
            set
            {
                _IsPullDown = value;
            }
        }

        private DeinterlaceType _deinterlace = DeinterlaceType.Disabled;
        public DeinterlaceType deinterlace
        {
            get
            {
                return _deinterlace;
            }
            set
            {
                _deinterlace = value;
            }
        }

        private string _filtering;
        public string filtering
        {
            get
            {
                return _filtering;
            }
            set
            {
                _filtering = value;
            }
        }

        private string _sbc;
        public string sbc
        {
            get
            {
                return _sbc;
            }
            set
            {
                _sbc = value;
            }
        }

        private string _vencoding;
        public string vencoding
        {
            get
            {
                return _vencoding;
            }
            set
            {
                _vencoding = value;
            }
        }

        private string _script;
        public string script
        {
            get
            {
                return _script;
            }
            set
            {
                _script = value;
            }
        }

        private string _scriptpath;
        public string scriptpath
        {
            get
            {
                return _scriptpath;
            }
            set
            {
                _scriptpath = value;
            }
        }

        private int _cropl = 0;
        public int cropl
        {
            get
            {
                return _cropl;
            }
            set
            {
                _cropl = value;
            }
        }

        private int _cropr = 0;
        public int cropr
        {
            get
            {
                return _cropr;
            }
            set
            {
                _cropr = value;
            }
        }

        private int _cropb = 0;
        public int cropb
        {
            get
            {
                return _cropb;
            }
            set
            {
                _cropb = value;
            }
        }

        private int _cropt = 0;
        public int cropt
        {
            get
            {
                return _cropt;
            }
            set
            {
                _cropt = value;
            }
        }

        private int _cropl_copy = 0;
        public int cropl_copy
        {
            get
            {
                return _cropl_copy;
            }
            set
            {
                _cropl_copy = value;
            }
        }

        private int _cropr_copy = 0;
        public int cropr_copy
        {
            get
            {
                return _cropr_copy;
            }
            set
            {
                _cropr_copy = value;
            }
        }

        private int _cropb_copy = 0;
        public int cropb_copy
        {
            get
            {
                return _cropb_copy;
            }
            set
            {
                _cropb_copy = value;
            }
        }

        private int _cropt_copy = 0;
        public int cropt_copy
        {
            get
            {
                return _cropt_copy;
            }
            set
            {
                _cropt_copy = value;
            }
        }

        private int _blackw = 0;
        public int blackw
        {
            get
            {
                return _blackw;
            }
            set
            {
                _blackw = value;
            }
        }

        private int _blackh = 0;
        public int blackh
        {
            get
            {
                return _blackh;
            }
            set
            {
                _blackh = value;
            }
        }

        private AviSynthScripting.Resizers _resizefilter;
        public AviSynthScripting.Resizers resizefilter
        {
            get
            {
                return _resizefilter;
            }
            set
            {
                _resizefilter = value;
            }
        }

        private Settings.EncodingModes _encodingmode;
        public Settings.EncodingModes encodingmode
        {
            get
            {
                return _encodingmode;
            }
            set
            {
                _encodingmode = value;
            }
        }


        private string _inframerate;
        public string inframerate
        {
            get
            {
                return _inframerate;
            }
            set
            {
                _inframerate = value;
            }
        }

        private AviSynthScripting.FramerateModifers _frameratemodifer = Settings.FramerateModifer;
        public AviSynthScripting.FramerateModifers frameratemodifer
        {
            get
            {
                return _frameratemodifer;
            }
            set
            {
                _frameratemodifer = value;
            }
        }

        private bool _iscolormatrix = false;
        public bool iscolormatrix
        {
            get
            {
                return _iscolormatrix;
            }
            set
            {
                _iscolormatrix = value;
            }
        }

        private int _hue = 0;
        public int hue
        {
            get
            {
                return _hue;
            }
            set
            {
                _hue = value;
            }
        }

        private double _saturation = 1.0;
        public double saturation
        {
            get
            {
                return _saturation;
            }
            set
            {
                _saturation = value;
            }
        }

        private int _brightness = 0;
        public int brightness
        {
            get
            {
                return _brightness;
            }
            set
            {
                _brightness = value;
            }
        }

        private double _contrast = 1.00; //1.00
        public double contrast
        {
            get
            {
                return _contrast;
            }
            set
            {
                _contrast = value;
            }
        }

        private string _outframerate;
        public string outframerate
        {
            get
            {
                return _outframerate;
            }
            set
            {
                _outframerate = value;
            }
        }

        private string _infilesize;
        public string infilesize
        {
            get
            {
                return _infilesize;
            }
            set
            {
                _infilesize = value;
            }
        }

        private int _infilesizeint;
        public int infilesizeint
        {
            get
            {
                return _infilesizeint;
            }
            set
            {
                _infilesizeint = value;
            }
        }

        private string _outfilesize;
        public string outfilesize
        {
            get
            {
                return _outfilesize;
            }
            set
            {
                _outfilesize = value;
            }
        }

        private x264_arguments _x264options;
        public x264_arguments x264options
        {
            get
            {
                return _x264options;
            }
            set
            {
                _x264options = value;
            }
        }

        private XviD_arguments _XviD_options;
        public XviD_arguments XviD_options
        {
            get
            {
                return _XviD_options;
            }
            set
            {
                _XviD_options = value;
            }
        }

        private wmv_arguments _wmv_options;
        public wmv_arguments wmv_options
        {
            get
            {
                return _wmv_options;
            }
            set
            {
                _wmv_options = value;
            }
        }

        private ffmpeg_arguments _ffmpeg_options;
        public ffmpeg_arguments ffmpeg_options
        {
            get
            {
                return _ffmpeg_options;
            }
            set
            {
                _ffmpeg_options = value;
            }
        }

        private aac_arguments _aac_options;
        public aac_arguments aac_options
        {
            get
            {
                return _aac_options;
            }
            set
            {
                _aac_options = value;
            }
        }

        private mp3_arguments _mp3_options;
        public mp3_arguments mp3_options
        {
            get
            {
                return _mp3_options;
            }
            set
            {
                _mp3_options = value;
            }
        }

        private ac3_arguments _ac3_options;
        public ac3_arguments ac3_options
        {
            get
            {
                return _ac3_options;
            }
            set
            {
                _ac3_options = value;
            }
        }

        private flac_arguments _flac_options;
        public flac_arguments flac_options
        {
            get
            {
                return _flac_options;
            }
            set
            {
                _flac_options = value;
            }
        }

        private bool _isconvertfps = true;
        public bool isconvertfps
        {
            get
            {
                return _isconvertfps;
            }
            set
            {
                _isconvertfps = value;
            }
        }

        //private bool _isforcefps = true;
        //public bool isforcefps
        //{
        //    get
        //    {
        //        return _isforcefps;
        //    }
        //    set
        //    {
        //        _isforcefps = value;
        //    }
        //}

        private string _outvinfo;
        public string outvinfo
        {
            get
            {
                return _outvinfo;
            }
            set
            {
                _outvinfo = value;
            }
        }

        private string _outainfo;
        public string outainfo
        {
            get
            {
                return _outainfo;
            }
            set
            {
                _outainfo = value;
            }
        }

        private int _thmframe = 0;
        public int thmframe
        {
            get
            {
                return _thmframe;
            }
            set
            {
                _thmframe = value;
            }
        }

        private ArrayList _inaudiostreams;
        public ArrayList inaudiostreams
        {
            get
            {
                return _inaudiostreams;
            }
            set
            {
                _inaudiostreams = value;
            }
        }

        private ArrayList _outaudiostreams;
        public ArrayList outaudiostreams
        {
            get
            {
                return _outaudiostreams;
            }
            set
            {
                _outaudiostreams = value;
            }
        }

        private int _outaudiostream = 0;
        public int outaudiostream
        {
            get
            {
                return _outaudiostream;
            }
            set
            {
                _outaudiostream = value;
            }
        }

        private bool _isvideo = true;
        public bool isvideo
        {
            get
            {
                return _isvideo;
            }
            set
            {
                _isvideo = value;
            }
        }

        private bool _dontmuxstreams = false;
        public bool dontmuxstreams
        {
            get
            {
                return _dontmuxstreams;
            }
            set
            {
                _dontmuxstreams = value;
            }
        }

        //private bool _isanamorph = false;
        //public bool isanamorph
        //{
        //    get
        //    {
        //        return _isanamorph;
        //    }
        //    set
        //    {
        //        _isanamorph = value;
        //    }
        //}

        private string _split;
        public string split
        {
            get
            {
                return _split;
            }
            set
            {
                _split = value;
            }
        }

        private string _bluray_type = Settings.BluRayType;
        public string bluray_type
        {
            get
            {
                return _bluray_type;
            }
            set
            {
                _bluray_type = value;
            }
        }

        //По умолчанию Гистограмма отключена
        private string _levels = "Disabled";
        public string levels
        {
            get
            {
                return _levels;
            }
            set
            {
                _levels = value;
            }
        }

        //Пользовательская строчка для mkvmerge
        private string _mkvstring;
        public string mkvstring
        {   
            get
            {
              return _mkvstring;
           }
           set
           {
              _mkvstring = value;
           }

        }

        //Трим - начало
        private int _trim_start = 0;
        public int trim_start
        {
            get
            {
                return _trim_start;
            }
            set
            {
                _trim_start = value;
            }
        }
        
        //Трим - конец
        private int _trim_end = 0;
        public int trim_end
        {
            get
            {
                return _trim_end;
            }
            set
            {
                _trim_end = value;
            }
        }

        //Тест-скрипт (нарезка)
        private bool _testscript = false;
        public bool testscript
        {
            get
            {
                return _testscript;
            }
            set
            {
                _testscript = value;
            }
        }
    }
}
