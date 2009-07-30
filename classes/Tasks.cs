using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;

namespace XviD4PSP
{
    public class Task
    {
        private string _id;
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        private string _thm;
        public string THM
        {
            get { return _thm; }
            set { _thm = value; }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _info;
        public string Info
        {
            get { return _info; }
            set { _info = value; }
        }

        private Massive _mass;
        public Massive Mass
        {
            get { return _mass; }
            set { _mass = value; }
        }

        public Task(string thm, string status, Massive mass)
        {
            _thm = thm;
            _status = status;
            _name = Path.GetFileName(mass.outfilepath);

            _info = "";

            if (mass.outvcodec != "Copy" &&
                mass.outvcodec != "Disabled")
            {
                //Переопределение некоторых параметров видео на основе текущего скрипта (т.к. он мог быть изменен вручную)
                if (Settings.ReadScript == true) //если в настройках это разрешено
                {
                    AviSynthReader reader = new AviSynthReader();
                    reader.ParseScript(mass.script);
                    mass.outresw = reader.Width;
                    mass.outresh = reader.Height;
                    mass.outframes = reader.FrameCount;
                    reader.Close();
                    reader = null;
                    mass.outduration = TimeSpan.FromSeconds((double)mass.outframes / Calculate.ConvertStringToDouble(mass.outframerate));
                    mass.outfilesize = Calculate.GetEncodingSize(mass);
                    
                    //Переопределяем SAR в случае анаморфного кодирования
                    if (mass.sar != null && mass.sar != "1:1")
                    {
                        mass = Calculate.CalculateSAR(mass);
                    }
                }

                _info += mass.outresw + "x" + mass.outresh;
                _info += "  " + mass.outframerate;
                if (mass.encodingmode == Settings.EncodingModes.Quality ||
                    mass.encodingmode == Settings.EncodingModes.Quantizer ||
                    mass.encodingmode == Settings.EncodingModes.TwoPassQuality ||
                    mass.encodingmode == Settings.EncodingModes.ThreePassQuality)
                    _info += "  Q" + Calculate.ConvertDoubleToPointString((double)mass.outvbitrate, 1);
                else if (mass.encodingmode == Settings.EncodingModes.TwoPassSize ||
                    mass.encodingmode == Settings.EncodingModes.ThreePassSize ||
                    mass.encodingmode == Settings.EncodingModes.OnePassSize)
                    _info += "  " + mass.outvbitrate + "mb";
                else
                    _info += "  " + mass.outvbitrate + "kbps";
            }
            else if (mass.outvcodec == "Copy")
            {
                _info += mass.inresw + "x" + mass.inresh;
                _info += "  " + mass.inframerate;
                _info += "  " + mass.invbitrate + "kbps";
            }

            if (mass.outaudiostreams.Count > 0)
            {
                AudioStream outstream = (AudioStream)mass.outaudiostreams[mass.outaudiostream];

                if (outstream.codec != "Copy")
                {
                    _info += "  " + outstream.samplerate + "Hz";
                    _info += "  " + Calculate.ExplainChannels(outstream.channels);
                    _info += "  " + outstream.bitrate + "kbps";

                    if (mass.volume != "Disabled")
                        _info += "  " + mass.volume;
                }
                else
                {
                    if (mass.inaudiostreams.Count > 0)
                    {
                        AudioStream instream = (AudioStream)mass.inaudiostreams[mass.inaudiostream];
                        _info += "  " + instream.samplerate + "Hz";
                        _info += "  " + Calculate.ExplainChannels(instream.channels);
                        _info += "  " + instream.bitrate + "kbps";
                    }
                }
            }

            if (mass.outfilesize != Languages.Translate("Unknown"))
                _info += "  " + mass.outfilesize.Replace(" ", "");

            _mass = mass.Clone();
            _id = mass.key;
        }
    }



    public class myTask :
        ObservableCollection<Task>
    {
        public myTask(string thm, string status, Massive mass)
        {
            Add(new Task(thm, status, mass));
        }
    }
}
