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

namespace XviD4PSP
{
	public partial class Filtering
	{
        public Massive m;
        private MainWindow p;

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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsLoaded && e.WidthChanged && e.HeightChanged)
            {               
                //После открытия окна разрешаем установить бОльшую ширину
                this.MaxWidth = SystemParameters.WorkArea.Width;
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
        }

        private void button_fullscreen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
           p.SwitchToFullScreen();
        }

        //Обработка вызова редактора скрипта AvsP
        private void button_avsp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process pr = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = Calculate.StartupPath + "\\apps\\AvsP\\AvsP.exe";
                info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                info.Arguments = Settings.TempPath + "\\AvsP.avs";
                pr.StartInfo = info;
                pr.Start();
                pr.WaitForExit(); //Ждать завершения

                //После завершения работы AvsP перечитываем измененный им файл AvsP.avs и вводим его содержимое в окно Фильтрация
                using (StreamReader sr = new StreamReader(Settings.TempPath + "\\AvsP.avs", System.Text.Encoding.Default))
                    script_box.Text = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                new Message(this).ShowMessage("AvsP editor: " + ex.Message, Languages.Translate("Error"));
            }
        }
	}
}