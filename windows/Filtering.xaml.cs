using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace XviD4PSP
{
	public partial class Filtering
	{
        private static object locker = new object();
        private Process avsp = null;
        private MainWindow p;
        public Massive m;

        public Filtering(Massive mass, MainWindow parent)
		{
			this.InitializeComponent();

            m = mass.Clone();
            p = parent;
            Owner = p;

            script_box.Text = m.script;
            script_box.AcceptsReturn = true; //Разрешаем Enter
            script_box.AcceptsTab = true;    //Разрешаем Tab

            //переводим
            Title = Languages.Translate("Filtering") + " " + m.scriptpath;
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            button_refresh.Content = Languages.Translate("Apply");
            button_refresh.ToolTip = Languages.Translate("Refresh preview");
            //button_fullscreen.Content = Languages.Translate("Fullscreen");

            //Ограничиваем максимальную ширину окна до его открытия
            this.MaxWidth = Math.Min(((MainWindow)parent).ActualWidth * 1.25, SystemParameters.WorkArea.Width);
            this.SizeChanged += new SizeChangedEventHandler(Window_SizeChanged);

            ShowDialog();
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey); //GetKeyState
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //После открытия окна разрешаем установить бОльшую ширину, но только
            //если размер окна изменялся мышкой (а не изменением текста внутри него).
            //Встроенные в C# способы определения состояния мыши для этой цели не годятся.
            if (IsLoaded && (GetAsyncKeyState(1) < 0 || GetAsyncKeyState(2) < 0))
            {
                this.MaxWidth = SystemParameters.WorkArea.Width;
                this.MaxHeight = SystemParameters.WorkArea.Height;
                this.SizeChanged -= new SizeChangedEventHandler(Window_SizeChanged);
            }
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m.script = script_box.Text;
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void button_refresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            p.Refresh(script_box.Text);
            this.Focus();
        }

        private void button_fullscreen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
           p.SwitchToFullScreen();
           this.Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseAvsP();
        }

        private void CloseAvsP()
        {
            lock (locker)
            {
                if (avsp != null)
                {
                    avsp.Close();
                    avsp.Dispose();
                    avsp = null;
                }
            }
        }

        //Вызов редактора скрипта AvsP
        private void button_avsp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (avsp == null && p.avsp == null)
                {
                    //Пишем в файл текущий скрипт
                    string path = Settings.TempPath + "\\AvsP_" + m.key + ".avs";
                    File.WriteAllText(path, m.script, System.Text.Encoding.Default); ;

                    avsp = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();
                    info.FileName = Calculate.StartupPath + "\\apps\\AvsP\\AvsP.exe";
                    info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                    info.Arguments = "\"" + path + "\"";
                    avsp.Exited += new EventHandler(AvsPExited);
                    avsp.EnableRaisingEvents = true;
                    avsp.StartInfo = info;
                    avsp.Start();
                }
                else
                {
                    IntPtr wnd = (avsp != null) ? avsp.MainWindowHandle : p.avsp.MainWindowHandle;
                    SetForegroundWindow(wnd);
                }
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage("AvsP editor: " + ex.Message, ex.StackTrace, Languages.Translate("Error"));
                CloseAvsP();
            }
        }

        //Обработка завершения работы AvsP
        internal delegate void AvsPExitedDelegate(object sender, EventArgs e);
        private void AvsPExited(object sender, EventArgs e)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new AvsPExitedDelegate(AvsPExited), sender, e);
            }
            else
            {
                try
                {
                    string path = ((Process)sender).StartInfo.Arguments.Trim(new char[] { '"' });
                    CloseAvsP();

                    //После завершения работы AvsP перечитываем измененный им файл скрипта и вводим его содержимое в окно Фильтрация
                    using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default))
                        script_box.Text = sr.ReadToEnd();

                    //Удаляем файл скрипта
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    new Message(this).ShowMessage("AvsP editor: " + ex.Message, ex.StackTrace, Languages.Translate("Error"));
                }
            }
        }
	}
}