using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections;
using System.Globalization;
using System.Windows.Interop;

namespace XviD4PSP
{
    public enum SourceType
    {
        UNKNOWN, NOT_ENOUGH_SECTIONS,
        PROGRESSIVE, INTERLACED, FILM, DECIMATING,
        HYBRID_FILM_INTERLACED, HYBRID_PROGRESSIVE_FILM, HYBRID_PROGRESSIVE_INTERLACED
    };

    public enum FieldOrder
    {
        UNKNOWN, VARIABLE, TFF, BFF
    };

    public enum DeinterlaceType
    {
        Disabled, TFM, Yadif, YadifModEDI, TDeint, TDeintEDI, TomsMoComp, LeakKernelDeint, FieldDeinterlace, QTGMC, QTGMC_2, MCBob, NNEDI,
        YadifModEDI2, SmoothDeinterlace, TIVTC, TIVTC_TDeintEDI, TIVTC_YadifModEDI, TDecimate, TDecimate_23, TDecimate_24, TDecimate_25
    };

    public enum Detecting { Interlace, Fields };

    //На основе http://forum.doom9.org/showthread.php?p=758642 и MeGUI
    //Описание алгоритма http://avisynth.org/mediawiki/Interlace_detection
    public partial class SourceDetector
    {
        private static object locker = new object();
        private BackgroundWorker worker = null;
        private AviSynthReader reader = null;
        private IntPtr Handle = IntPtr.Zero;
        private string ErrorText = null;
        private string StackTrace = null;
        private string Script = null;
        private int num_closes = 0;
        private bool IsAborted = false;
        public bool IsErrors = false;
        public Massive m;

        SourceType source_type = SourceType.UNKNOWN;
        FieldOrder field_order = FieldOrder.UNKNOWN;
        bool majorityFilm;

        //Счетчики
        int numTC = 0, numProg = 0, numInt = 0, numUseless = 0;
        int sectionCountA = 0, sectionCountB = 0;

        //Настройки
        double AnalysePercent = Settings.SD_Analyze;    //Анализировать (% от всего видео), 1
        double HybridPercent = Settings.SD_Hybrid_Int;  //Порог для Hybrid`ных типов интерлейса, 5
        double FOPercent = Settings.SD_Hybrid_FO;       //Порог для Hybrid FieldOrder (переменный порядок полей), 10
        double DecimationThreshold = 2;                 //Порог для Decimate, 2
        double MinimumUsefulSections = 13.5;            //Минимальное кол-во секций, пригодных для анализа (% от общего кол-ва секций)
        bool UsePortions = Settings.SD_Portions_FO;     //Поиск интерлейсных частей (порций) и анализ полей только в них (для гибридного интерлейса)
        int MaxPortions = 15;                           //Макс. допустимое кол-во порций (анализ полей только в интерлейсных секциях), 5

        public SourceDetector(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();

            progress_total.Maximum = 100;
            Title = Languages.Translate("Detecting interlace") + "...";
            label_info.Content = Languages.Translate("Please wait... Work in progress...");
            this.ContentRendered += new EventHandler(Window_ContentRendered);

            //BackgroundWorker
            CreateBackgroundWorker();
            worker.RunWorkerAsync();

            ShowDialog();
        }

        void Window_ContentRendered(object sender, EventArgs e)
        {
            if (Handle == IntPtr.Zero)
                Win7Taskbar.SetProgressIndeterminate(this, ref Handle);
        }

        private void CreateBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                //Начало анализа
                RunAnalyzer(0, -1, null);

                if (!IsErrors)
                {
                    if (!IsAborted || IsAborted && source_type != SourceType.UNKNOWN)
                    {
                        m.interlace = source_type;
                        m.interlace_results = "Film:                 " + numTC + "\r\nInterlaced:        " + numInt + "\r\nProgressive:      " + numProg +
                           "\r\nUseless:            " + numUseless + "\r\nTotal:               " + (numTC + numInt + numProg + numUseless);
                    }
                    if (!IsAborted || IsAborted && field_order != FieldOrder.UNKNOWN)
                    {
                        m.fieldOrder = field_order;
                        if (sectionCountA != 0 || sectionCountB != 0)
                            m.interlace_results += "\r\n\r\nTFF:                 " + sectionCountA + "\r\nBFF:                 " + sectionCountB;
                    }

                    m = Format.GetOutInterlace(m);
                    m = Calculate.UpdateOutFrames(m);
                }
            }
            catch (Exception ex)
            {
                if (!IsAborted && num_closes == 0)
                {
                    IsErrors = true;
                    ErrorText = "SourceDetector: " + ex.Message;
                    StackTrace = ex.StackTrace;
                }
            }
        }

        private void RunAnalyzer(Detecting detecting, int frameCount, string trimLine)
        {
            if (IsAborted || IsErrors) return;
            string script = AviSynthScripting.GetInfoScript(m, AviSynthScripting.ScriptMode.Interlace);

            int numFrames = 0;
            if (frameCount > 0) numFrames = frameCount;
            else
            {
                try
                {
                    reader = new AviSynthReader();
                    reader.ParseScript(script);
                    numFrames = reader.FrameCount;
                }
                catch (Exception ex)
                {
                    if (!IsAborted && num_closes == 0)
                    {
                        IsErrors = true;
                        ErrorText = "SourceDetector (RunAnalyzer): " + ex.Message;
                        StackTrace = ex.StackTrace;
                        Script = script;
                    }
                }
                finally
                {
                    CloseReader(true);
                }
            }

            if (IsAborted || IsErrors) return;

            //Еще настройки
            int SelectLength = 5;                              //Длина выборки (5 кадров)
            int MinAnalyseSections = Settings.SD_Min_Sections; //Мин. кол-во секций для анализа, 150

            if (detecting == Detecting.Fields)
            {
                //Тут в описании неточность, в оригинале выход скрипта имеет в два раза больше кадров из-за
                //loop(2), а не из-за SeparateFields. Вместо loop(2) лучше использовать один из уже готовых
                //клипов с удвоенным кол-вом кадров: atff или abff (без разницы) - декодеру не придется
                //дважды проходить по одному и тому-же месту (loop(2) - дошли до конца, идем в начало и
                //начинаем новый проход через весь клип). А если еще вместо DifferenceFromPrevious
                //использовать DifferenceToNext, то скорость вырастет еще больше!
                //SelectLength надо тоже установить равным 10-ти, иначе в одной выборке для FieldOrder
                //будут группы кадров (полей) из двух разных участков видео.
                //-----
                // Field order script. For this, we separatefields, so we have twice as many frames anyway
                // It saves time, and costs nothing to halve the minimum sections to analyse for this example
                //minAnalyseSections = minAnalyseSections / 2 + 1; // We add one to prevent getting 0;

                int NewLength = 10; //Длина выборки будет 10 кадров, а мин. кол-во секций пропорционально уменьшаем
                MinAnalyseSections = (int)Math.Max(MinAnalyseSections / (NewLength / (double)SelectLength), 1);
                SelectLength = NewLength;
            }

            // Check if we need to modify the SelectRangeEvery parameters:
            int SelectEvery = (int)((100.0 * (double)SelectLength) / AnalysePercent);
            if (((double)SelectLength * (double)numFrames / (double)SelectEvery) < (int)MinAnalyseSections * SelectLength)
            {
                if (numFrames >= MinAnalyseSections * SelectLength) // If there are actually enough frames
                {
                    SelectEvery = (int)(((double)numFrames / ((double)MinAnalyseSections * (double)SelectLength)) * (double)SelectLength);
                }
                else
                    // if there aren't enough frames, analyse everything -- that's got to be good enough
                    SelectEvery = SelectLength;
            }

            //Имя лог-файла
            string logFileName = Settings.TempPath + "\\detecting_" + detecting.ToString().ToLower() + ".log";
            File.Delete(logFileName);

            //Прогон скрипта
            if (detecting == Detecting.Fields) SetFieldPhase();
            PlayScript(AviSynthScripting.GetSourceDetectionScript(detecting, script, trimLine, logFileName, SelectEvery, SelectLength));

            if (IsAborted || IsErrors) return;

            //Определение интерлейса\полей (чтение и анализ лог-файлов)
            if (detecting == Detecting.Interlace)
                AnalyseInterlace(logFileName, SelectEvery, SelectLength, numFrames);
            else if (detecting == Detecting.Fields)
                AnalyseFields(logFileName, SelectLength);
        }

        private void PlayScript(string script)
        {
            try
            {
                reader = new AviSynthReader();
                reader.ParseScript(script);
                int total = reader.FrameCount;

                for (int i = 0; i < total && !IsAborted; i++)
                {
                    reader.ReadFrameDummy(i);
                    worker.ReportProgress(((i + 1) * 100) / total);
                }
            }
            catch (Exception ex)
            {
                if (!IsAborted && num_closes == 0)
                {
                    IsErrors = true;
                    ErrorText = "SourceDetector (PlayScript): " + ex.Message;
                    StackTrace = ex.StackTrace;
                    Script = script;
                }
            }
            finally
            {
                CloseReader(true);
            }
        }

        private void AnalyseInterlace(string logFileName, int selectEvery, int selectLength, int inputFrames)
        {
            #region variable declaration
            StreamReader instream = new StreamReader(logFileName);
            bool[,] data = new bool[5, 2];
            int count = 0;
            int sectionCount = 0;

            // Decimation data
            int totalCombed = 0;
            int[] sectionsWithMovingFrames = new int[6];

            // interlaced portions
            ArrayList[] portions = new ArrayList[2];
            portions[0] = new ArrayList();
            portions[1] = new ArrayList();

            int[] portionLength = new int[2];
            int[] nextPortionIndex = new int[2];
            bool[] inPortion = new bool[2];
            int[] numPortions = new int[2];
            int[] portionStatus = new int[2];

            #endregion
            #region loop
            string line = instream.ReadLine();
            while (line != null && !IsAborted && !IsErrors)
            {
                string[] contents = line.Split(new char[] { '-' });
                data[count, 0] = (contents[0].Equals("true"));
                data[count, 1] = (contents[1].Equals("true"));
                count++;

                if (count == 5)
                {
                    sectionCount++;
                    int numComb = 0;
                    int numMoving = 0;
                    int combA = -1, combB = -1;
                    for (int i = 0; i < 5; i++)
                    {
                        if (data[i, 0])
                        {
                            numComb++;
                            if (combA == -1)
                                combA = i;
                            else
                                combB = i;
                        }
                        if (data[i, 1])
                            numMoving++;
                    }
                    totalCombed += numComb;
                    sectionsWithMovingFrames[numMoving]++;
                    if (numMoving < 5)
                    {
                        numUseless++;
                        portionStatus[0] = 1;
                        portionStatus[1] = 1;
                    }
                    else if (numComb == 2 && ((combB - combA == 1) || (combB - combA == 4)))
                    {
                        numTC++;
                        portionStatus[0] = 0;
                        portionStatus[1] = 2;
                    }
                    else if (numComb > 0)
                    {
                        numInt++;
                        portionStatus[0] = 2;
                        portionStatus[1] = 0;
                    }
                    else
                    {
                        numProg++;
                        portionStatus[0] = 0;
                        portionStatus[1] = 0;
                    }

                    #region portions
                    // Manage film and interlaced portions
                    for (int i = 0; i < 2; i++)
                    {
                        if (portionStatus[i] == 0) // Stop any portions we are in.
                        {
                            if (inPortion[i])
                            {
                                ((int[])portions[i][nextPortionIndex[i]])[1] = sectionCount;
                                #region useless comments
                                /* if (portionLength[i] == 1) // This should help reduce random fluctuations, by removing length 1 portions
 * I've now changed my mind about random fluctuations. I believe they are good, because they occur when Decomb is on the verge of making
 * a wrong decision. Instead of continuing with this decision, which would then regard this section of the film as progressive, leaving combing
 * this now has the effect of dramatically increasing the number of portions, forcing the whole thing to be deinterlaced, which is better,
 * as it leaves no residual combing.
 * 
 * Edit again: i've left this section commented out, but the other section which removes length 1 progressive sections, I've left in, as it is
 * safer to deinterlace progressive stuff than vice versa.
                                {
                                    portions[i].RemoveAt(nextPortionIndex[i]);
                                    nextPortionIndex[i]--;
                                    numPortions[i]--;
                                }
*/
                                #endregion
                                nextPortionIndex[i]++;
                                inPortion[i] = false;
                            }
                            portionLength[i] = 0;
                        }
                        else if (portionStatus[i] == 1) // Continue all portions, but don't start a new one.
                        {
                            portionLength[i]++;
                        }
                        else if (portionStatus[i] == 2) // Start a new portion, or continue an old one.
                        {
                            if (inPortion[i])
                                portionLength[i]++;
                            else
                            {
                                int startIndex = sectionCount - portionLength[i];
                                int lastEndIndex = -2;
                                if (nextPortionIndex[i] > 0)
                                    lastEndIndex = ((int[])portions[i][nextPortionIndex[i] - 1])[1];
                                if (startIndex - lastEndIndex > 1) // If the last portion ended more than 1 section ago. This culls trivial portions
                                {
                                    portions[i].Add(new int[2]);
                                    ((int[])portions[i][nextPortionIndex[i]])[0] = startIndex;
                                    portionLength[i]++;
                                    numPortions[i]++;
                                }
                                else
                                {
                                    nextPortionIndex[i]--;
                                }
                                inPortion[i] = true;
                            }
                        }
                    }
                    #endregion
                    count = 0;
                }
                line = instream.ReadLine();
            }
            #endregion
            #region final counting
            instream.Close();

            int[] array = new int[] { numInt, numProg, numTC };
            Array.Sort(array); //array[0] - min, array[2] - max

            if (numInt + numProg + numTC < (int)Math.Max(((double)sectionCount * MinimumUsefulSections / 100.0), 1))
            {
                if (!checkDecimate(sectionsWithMovingFrames))
                {
                    //Source does not have enough data. This either comes from an internal error or an unexpected source type
                    //source_type = SourceType.NOT_ENOUGH_SECTIONS;
                    source_type = SourceType.UNKNOWN;
                }
                return;
            }

            #region plain
            //if (array[1] < (double)(array[0] + array[1] + array[2]) / 100.0 * HybridPercent)
            if (array[1] < (double)(array[0] + array[2]) / 100.0 * HybridPercent)
            {
                if (array[2] == numProg)
                {
                    //Source is declared completely progressive
                    source_type = SourceType.PROGRESSIVE;
                    checkDecimate(sectionsWithMovingFrames);
                }
                else if (array[2] == numInt)
                {
                    //Source is declared completely interlaced
                    source_type = SourceType.INTERLACED;
                    RunAnalyzer(Detecting.Fields, -1, null); //field order script
                }
                else
                {
                    //Source is declared completely telecined
                    source_type = SourceType.FILM;
                    RunAnalyzer(Detecting.Fields, -1, null); //field order script
                }
            }
            #endregion
            #region hybrid
            else
            {
                if (array[0] == numProg) // We have a hybrid film/ntsc. This is the most common
                {
                    //Source is declared hybrid film/ntsc. Majority is:
                    if (array[2] == numTC)
                    {
                        //film
                        majorityFilm = true;
                    }
                    else
                    {
                        //ntsc (interlaced)
                        majorityFilm = false;
                    }
                    source_type = SourceType.HYBRID_FILM_INTERLACED;
                    RunAnalyzer(Detecting.Fields, -1, null); //field order script
                }
                else if (array[0] == numInt)
                {
                    //if (array[0] > (double)(array[0] + array[1] + array[2]) / 100.0 * HybridPercent) // There is also a section of interlaced
                    if (array[0] > (double)(array[1] + array[2]) / 100.0 * HybridPercent)
                    {
                        //Sourc is declared hybrid film/ntsc. Majority is film.
                        source_type = SourceType.HYBRID_FILM_INTERLACED;
                        majorityFilm = true;
                        RunAnalyzer(Detecting.Fields, -1, null); //field order script
                    }
                    else
                    {
                        //Source is declared hybrid film/progressive
                        majorityFilm = (array[2] == numTC);
                        source_type = SourceType.HYBRID_PROGRESSIVE_FILM;
                        int frameCount = -1;
                        string trimLine = null;
                        if (UsePortions && numPortions[1] <= MaxPortions)
                        {
                            //Анализ полей только в интерлейсных частях
                            trimLine = findPortions(portions[1], selectEvery, selectLength, numPortions[1], sectionCount, inputFrames, out frameCount);
                        }
                        RunAnalyzer(Detecting.Fields, frameCount, trimLine); //field order script
                    }
                }
                else if (array[0] == numTC)
                {
                    //Скорее всего видео - кривой трансфер, для него numTC определились ошибочно, игнорируем их
                    if (m.inframerate != null && !m.inframerate.StartsWith("29") && !m.inframerate.StartsWith("30")) array[0] = 0;
                    //if (array[0] > (double)(array[0] + array[1] + array[2]) / 100.0 * HybridPercent) // There is also a section of film
                    if (array[0] > (double)(array[1] + array[2]) / 100.0 * HybridPercent)
                    {
                        //Source is declared hybrid film/ntsc. Majority is ntsc (interlaced).
                        source_type = SourceType.HYBRID_FILM_INTERLACED;
                        majorityFilm = false;
                        RunAnalyzer(Detecting.Fields, -1, null); //field order script
                    }
                    else
                    {
                        //Source is declared hybrid progressive/interlaced.
                        source_type = SourceType.HYBRID_PROGRESSIVE_INTERLACED;

                        int frameCount = -1;
                        string trimLine = null;
                        if (UsePortions && numPortions[0] <= MaxPortions)
                        {
                            //Анализ полей только в интерлейсных частях
                            trimLine = findPortions(portions[0], selectEvery, selectLength, numPortions[0], sectionCount, inputFrames, out frameCount);
                        }
                        RunAnalyzer(Detecting.Fields, frameCount, trimLine); //field order script
                    }
                }
            }
            #endregion
            #endregion
        }

        private void AnalyseFields(string filename, int SelectLength)
        {
            StreamReader instream = new StreamReader(filename);
            int count = 0, countA = 0, countB = 0;
            double valueA, valueB;

            string line = instream.ReadLine();
            while (line != null && !IsAborted && !IsErrors)
            {
                count++;
                string[] contents = line.Split(new char[] { '-' });
                valueA = Double.Parse(contents[0], new CultureInfo("en-US"));
                valueB = Double.Parse(contents[1], new CultureInfo("en-US"));

                if (valueA > valueB) countA++;
                else if (valueB > valueA) countB++;

                if (count == SelectLength) //10
                {
                    //Секция = SelectLength, а не просто 10.
                    //------
                    // Truly interlaced sections should always make one of the counts be 5 and the other 0.
                    // Progressive sections will be randomly distributed between localCountA and localCountB,
                    // so this algorithm successfully ignores those sections.
                    // Film sections will always have two frames which show the actual field order, and the other
                    // frames will show an arbitrary field order. This algorithm (luckily) seems to work very well
                    // with film sections as well. Using this thresholding as opposed to just comparing countB to countA
                    // produces _much_ more heavily-sided results.
                    if (countA > countB && countB == 0) sectionCountA++;
                    if (countB > countA && countA == 0) sectionCountB++;
                    countA = countB = count = 0;
                }
                line = instream.ReadLine();
            }
            instream.Close();

            if (sectionCountA == 0 && sectionCountB == 0)
            {
                field_order = FieldOrder.UNKNOWN;
            }
            else if ((Math.Min(sectionCountA, sectionCountB) * 100.0 / (Math.Max(sectionCountA, sectionCountB) *
               Math.Pow(FOPercent, 1.0 / 20))) >= FOPercent || sectionCountA == sectionCountB)
            {
                field_order = FieldOrder.VARIABLE;
            }
            else if (sectionCountA > sectionCountB)
            {
                field_order = FieldOrder.TFF;
            }
            else if (sectionCountA < sectionCountB)
            {
                field_order = FieldOrder.BFF;
            }
        }

        private bool checkDecimate(int[] data)
        {
            int[] dataCopy = new int[6];
            Array.Copy(data, dataCopy, 6);
            Array.Sort(dataCopy);

            int numMovingFrames = -1;

            for (int i = 0; i < data.Length; i++)
            {
                if (dataCopy[5] == data[i])
                    numMovingFrames = i;
            }

            if (dataCopy[5] > (double)dataCopy[4] * DecimationThreshold && numMovingFrames != 5 && numMovingFrames != 0)
            // If there are 5 moving frames, then it needs no decimation
            // If there are 0 moving frames, then we have a problem.
            {
                source_type = SourceType.DECIMATING;
                return true;
            }
            return false;
        }

        private string findPortions(ArrayList portions, int selectEvery, int selectLength, int numPortions, int sectionCount, int inputFrames, out int frameCount)
        {
            frameCount = 0;
            string trimLine = "";
            int lastEndFrame = -1;
            for (int i = 0; i < numPortions; i++)
            {
                int portionStart = ((int[])portions[i])[0];
                int portionEnd = ((int[])portions[i])[1];
                int startFrame = Math.Max(0, (portionStart) * selectEvery);
                if (portionEnd == 0) portionEnd = sectionCount;
                int endFrame = Math.Min(inputFrames - 1, (portionEnd + 1) * selectEvery);
                frameCount += endFrame - startFrame;
                trimLine += string.Format("trim({0},{1}) ++ ", startFrame, endFrame);
                lastEndFrame = endFrame;
            }

            return trimLine.TrimEnd(new char[] { ' ', '+' });
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (((BackgroundWorker)sender).WorkerReportsProgress)
            {
                if (progress_total.IsIndeterminate)
                {
                    progress_total.IsIndeterminate = false;
                    label_info.Content = Languages.Translate("Detecting interlace") + "...";
                }

                progress_total.Value = e.ProgressPercentage;
                Title = "(" + e.ProgressPercentage.ToString("0") + "%)";

                //Прогресс в Taskbar
                //if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressValue(Handle, Convert.ToUInt64(e.ProgressPercentage), 100);
            }
        }

        internal delegate void FieldPhaseDelegate();
        private void SetFieldPhase()
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new FieldPhaseDelegate(SetFieldPhase));
            else
            {
                progress_total.Value = 0;
                label_info.Content = Languages.Translate("Detecting fields order") + "...";
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                ErrorException("SourceDetector (Unhandled): " + ((Exception)e.Error).Message, ((Exception)e.Error).StackTrace);
                m = null;
            }
            else
            {
                //Добавляем скрипт в StackTrace
                if (!string.IsNullOrEmpty(Script))
                    StackTrace += Calculate.WrapScript(Script, 150);

                if (IsErrors && !IsAborted)
                    ErrorException(ErrorText, StackTrace);

                if (IsErrors)
                    m = null;

                if (!IsErrors || IsAborted)
                {
                    //удаляем мусор
                    SafeDelete(Settings.TempPath + "\\detecting_interlace.log");
                    SafeDelete(Settings.TempPath + "\\detecting_fields.log");
                }
            }

            Close();
        }

        private void SafeDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception ex)
            {
                ErrorException("SafeDelete: " + ex.Message, ex.StackTrace);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool cancel_closing = false;

            if (worker != null)
            {
                if (worker.IsBusy && num_closes < 5)
                {
                    //Отмена
                    IsAborted = true;
                    cancel_closing = true;
                    worker.CancelAsync();
                    num_closes += 1;
                    m = null;
                }
                else
                {
                    worker.Dispose();
                    worker = null;
                }
            }

            //Отменяем закрытие окна
            if (cancel_closing)
            {
                //CloseReader(false);

                worker.WorkerReportsProgress = false;
                label_info.Content = Languages.Translate("Aborting... Please wait...");
                Win7Taskbar.SetProgressState(Handle, TBPF.INDETERMINATE);
                progress_total.IsIndeterminate = true;
                e.Cancel = true;
            }
            else
                CloseReader(true);
        }

        private void CloseReader(bool _null)
        {
            lock (locker)
            {
                if (reader != null)
                {
                    reader.Close();
                    if (_null) reader = null;
                }
            }
        }

        internal delegate void ErrorExceptionDelegate(string data, string info);
        private void ErrorException(string data, string info)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ErrorExceptionDelegate(ErrorException), data, info);
            else
            {
                if (worker != null) worker.WorkerReportsProgress = false;
                if (Handle == IntPtr.Zero) Handle = new WindowInteropHelper(this).Handle;
                Win7Taskbar.SetProgressTaskComplete(Handle, TBPF.ERROR);

                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
    }
}