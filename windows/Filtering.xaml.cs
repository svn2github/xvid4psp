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
            script_box.AcceptsTab = true; //Разрешаем Tab

            //переводим
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            button_refresh.Content = Languages.Translate("Apply");
            button_refresh.ToolTip = Languages.Translate("Refresh preview");
            //button_auto.Content = Languages.Translate("Auto");
            //button_auto.ToolTip = Languages.Translate("Create auto script");
            
            Title = Languages.Translate("Filtering") + " " + m.scriptpath;

            ShowDialog();
          		  
        }

 //       public Massive EditFilters(Massive m)////
 //       {
 //         char[] chars = m.script.ToCharArray();//
 //
 //         string[] separator = new string[] { Environment.NewLine }
 //         string[] lines = m.script.Split(separator, StringSplitOptions.None);
 //
 //         foreach (string line in lines)
 //         {
 //            if (line.StartsWith("#"))
 //            {
 //                lbxFilters.Items.Add(line);
 //            }
 //         }
 //
 //
 //         ShowDialog();
 //
 //          //возвращаем массив
 //          return m;
 //       }////////


        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m.script = script_box.Text;
            //p.Refresh(script_box.Text);//ShowDialog
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

        //Вместо кнопки Auto теперь кнопка Fullscreen
      //  private void button_auto_Click(object sender, System.Windows.RoutedEventArgs e)
      //  {
      //      m = p.CreateAutoScript();
      //      script_box.Text = m.script;
      //  }
        
        private void button_fullscreen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
           p.SwitchToFullScreen();
        }
        
        //Обработка вызова редактора скрипта AvsP
        private void button_avsp_Click(object sender, RoutedEventArgs e)
        {
           // if (!File.Exists(Settings.TempPath + "\\preview.avs")) //Если файла preview.avs нет..
           //     AviSynthScripting.WriteScriptToFile(m.script, "preview"); //..то создаем его

                 // AviSynthScripting.WriteScriptToFile(m.script, "AvsPscript");

                    Process pr = new Process();
                    ProcessStartInfo info = new ProcessStartInfo();

                    if (File.Exists(Calculate.StartupPath + "\\apps\\AvsP\\AvsP.exe"))
                    {
                        try
                        {
                            info.FileName = Calculate.StartupPath + "\\apps\\AvsP\\AvsP.exe";
                            info.WorkingDirectory = Path.GetDirectoryName(info.FileName);
                            info.Arguments = Settings.TempPath + "\\AvsP.avs";
                            pr.StartInfo = info;
                            pr.Start();
                            pr.WaitForExit(); //Ждать завершения
                            
                            //После завершения работы AvsP перечитываем измененный им файл AvsP.avs и вводим его содержимое в окно Фильтрация
                           // string pre_script;
                            using (StreamReader sr = new StreamReader(Settings.TempPath + "\\AvsP.avs", System.Text.Encoding.Default))
                                script_box.Text = sr.ReadToEnd();
                             
                           // pre_script = sr.ReadToEnd();
                           // string[] separator = new string[] { Environment.NewLine };
                           // string[] lines = pre_script.Split(separator, StringSplitOptions.None);
                           // string script = "";
                           // foreach (string line in lines)
                           //     script += line + Environment.NewLine;
                           // script_box.Text = script;

                        }

                        catch (Exception ex)
                        {
                            Message mes = new Message(this);
                            mes.ShowMessage(ex.Message, "Error");
                        }   
               
                    }
                   else
                    {
                        Message mes = new Message(this);
                        mes.ShowMessage(Calculate.StartupPath + "\\apps\\AvsP\\AvsP.exe - Can`t find this file!", "Error");

                    }
        
        }






	}
}