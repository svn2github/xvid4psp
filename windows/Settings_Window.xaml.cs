using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace XviD4PSP
{
	public partial class Settings_Window
	{
        private MainWindow p;

        public Settings_Window(MainWindow parent, int set_focus_to)
		{
			this.InitializeComponent();

            p = parent;
            Owner = p;

            //переводим
            button_ok.Content = Languages.Translate("OK");
            Title = Languages.Translate("Settings") + ":";
            check_demux_audio.Content = Languages.Translate("Don`t demux audio for preview");
            check_show_psnr.Content = Languages.Translate("Show x264 PSNR info");
            check_show_ssim.Content = Languages.Translate("Show x264 SSIM info");
            check_show_arguments.Content = Languages.Translate("Show encoding arguments");
            check_show_script.Content = Languages.Translate("Show AviSynth script");
            check_delete_ff_cache.Content = Languages.Translate("Auto delete FFmpegSource cache");
            check_delete_dgindex_cache.Content = Languages.Translate("Auto delete DGIndex cache");
            check_search_temp.Content = Languages.Translate("Search best temp folder place when program start");
            check_save_anamorph.Content = Languages.Translate("Save anamorph aspect");
            check_alwaysprogressive.Content = Languages.Translate("Always encode to progressive video");
            check_auto_colormatrix.Content = Languages.Translate("Auto apply ColorMatrix for MPEG2 files");
            label_temppath.Content = Languages.Translate("Temp folder path:");
            check_window_dim.Content = Languages.Translate("Remember last window location");
            check_renew_script.Content = Languages.Translate("Renew script when audio/video encoding settings is changed");
            check_hide_comments.Content = Languages.Translate("Remove comments (#text) from the AviSynth script");
            check_resize_first.Content = Languages.Translate("Make crop/resize before filtering (otherwise - after filtering)");
            check_read_prmtrs.Content = Languages.Translate("Read parameters from the script when saving a new task");
            check_log_to_file.Content = Languages.Translate("Write encoding log to a file");
            check_logfile_tempfolder.Content = Languages.Translate("in Temp folder");
            label_extensions.Content = Languages.Translate("Only files with this extensions will be opened:");
            check_batch_autoencoding.Content = Languages.Translate("Start encoding after opening all files");

            tab_main.Header = Languages.Translate("Misc");
            tab_encoding.Header = Languages.Translate("Encoding log");
            tab_temp.Header = Languages.Translate("Temp files");
            tab_open_folder.Header = Languages.Translate("Batch encoding");

            check_demux_audio.IsChecked = Settings.DontDemuxAudio;
            check_show_psnr.IsChecked = Settings.x264_PSNR;
            check_show_ssim.IsChecked = Settings.x264_SSIM;
            check_show_arguments.IsChecked = Settings.ArgumentsToLog;
            check_show_script.IsChecked = Settings.PrintAviSynth;
            check_delete_ff_cache.IsChecked = Settings.DeleteFFCache;
            check_delete_dgindex_cache.IsChecked = Settings.DeleteDGIndexCache;
            check_search_temp.IsChecked = Settings.SearchTempPath;
            textbox_temp.Text = Settings.TempPath;
            check_save_anamorph.IsChecked = Settings.SaveAnamorph;
            check_alwaysprogressive.IsChecked = Settings.AlwaysProgressive;
            check_auto_colormatrix.IsChecked = Settings.AutoColorMatrix;
            check_window_dim.IsChecked = Settings.WindowResize; //запоминать параметры окна
            check_renew_script.IsChecked = Settings.RenewScript; //обновлять скрипт
            check_hide_comments.IsChecked = Settings.HideComments; //удалять комментарии из скрипта
            check_resize_first.IsChecked = Settings.ResizeFirst; //ресайз перед фильтрацией
            check_read_prmtrs.IsChecked = Settings.ReadScript; //считывать параметры скрипта
            check_log_to_file.IsChecked = Settings.WriteLog; //записывать лог кодирования в файл..
            check_logfile_tempfolder.IsChecked = Settings.LogInTemp; //.. а файл поместить во временную папку
            textbox_extensions.Text = Settings.GoodFilesExtensions; // окно со списком допустимых расширений файлов (при пакетной обработке)
            check_batch_autoencoding.IsChecked = Settings.AutoBatchEncoding; //автозапуск кодирования (при пакетной обработке)

            if (Settings.WriteLog)
                check_logfile_tempfolder.IsEnabled = true;
            else
                check_logfile_tempfolder.IsEnabled = false;


            SetTooltips();

            //Чтоб открыть окно на нужной вкладке
            if (set_focus_to == 2)
                tab_temp.Focus();
            else if (set_focus_to == 3)
                tab_encoding.Focus();
            else if (set_focus_to == 4)
                tab_open_folder.Focus();

            ShowDialog();
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (textbox_temp.Text != null &&
                textbox_temp.Text != "")
            {
                try
                {
                    if (textbox_temp.Text != Settings.TempPath)
                    {
                        if (!Directory.Exists(textbox_temp.Text))
                            Directory.CreateDirectory(textbox_temp.Text);
                        Settings.TempPath = textbox_temp.Text;
                        p.TempFolderFiles(); //Проверка на наличие в папке файлов
                    }
                }
                catch (Exception ex)
                {
                    ErrorExeption(ex.Message);
                }
            }

            //Сохраняем список валидных расширений (для открытия папки)
            if (textbox_extensions.Text != null)
            {
                Settings.GoodFilesExtensions = textbox_extensions.Text.ToLower();
            }
          
            Close();
        }

        private void SetTooltips()
        {
      
        }

        private void ErrorExeption(string message)
        {
            Message mes = new Message(this);
            mes.ShowMessage(message, Languages.Translate("Error"));
        }

        private void check_demux_audio_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_demux_audio.IsFocused)
            {
                Settings.DontDemuxAudio = check_demux_audio.IsChecked.Value;
            }
        }

        private void check_demux_audio_Checked(object sender, RoutedEventArgs e)
        {
            if (check_demux_audio.IsFocused)
            {
                Settings.DontDemuxAudio = check_demux_audio.IsChecked.Value;
            }
        }

        private void check_show_psnr_Checked(object sender, RoutedEventArgs e)
        {
            if (check_show_psnr.IsFocused)
            {
                Settings.x264_PSNR = check_show_psnr.IsChecked.Value;
            }
        }

        private void check_show_psnr_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_show_psnr.IsFocused)
            {
                Settings.x264_PSNR = check_show_psnr.IsChecked.Value;
            }
        }

        private void check_show_ssim_Checked(object sender, RoutedEventArgs e)
        {
            if (check_show_ssim.IsFocused)
            {
                Settings.x264_SSIM = check_show_ssim.IsChecked.Value;
            }
        }

        private void check_show_ssim_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_show_ssim.IsFocused)
            {
                Settings.x264_SSIM = check_show_ssim.IsChecked.Value;
            }
        }

        private void check_show_arguments_Checked(object sender, RoutedEventArgs e)
        {
            if (check_show_arguments.IsFocused)
            {
                Settings.ArgumentsToLog = check_show_arguments.IsChecked.Value;
            }
        }

        private void check_show_arguments_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_show_arguments.IsFocused)
            {
                Settings.ArgumentsToLog = check_show_arguments.IsChecked.Value;
            }
        }

        private void check_show_script_Checked(object sender, RoutedEventArgs e)
        {
            if (check_show_script.IsFocused)
            {
                Settings.PrintAviSynth = check_show_script.IsChecked.Value;
            }
        }

        private void check_show_script_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_show_script.IsFocused)
            {
                Settings.PrintAviSynth = check_show_script.IsChecked.Value;
            }
        }

        private void check_delete_ff_cache_Checked(object sender, RoutedEventArgs e)
        {
            if (check_delete_ff_cache.IsFocused)
            {
                Settings.DeleteFFCache = check_delete_ff_cache.IsChecked.Value;
            }       
        }

        private void check_delete_ff_cache_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_delete_ff_cache.IsFocused)
            {
                Settings.DeleteFFCache = check_delete_ff_cache.IsChecked.Value;
            }    
        }

        private void check_delete_dgindex_cache_Checked(object sender, RoutedEventArgs e)
        {
            if (check_delete_dgindex_cache.IsFocused)
            {
                Settings.DeleteDGIndexCache = check_delete_dgindex_cache.IsChecked.Value;
            }
        }

        private void check_delete_dgindex_cache_Unchecked(object sender, RoutedEventArgs e)
        {
            {
                Settings.DeleteDGIndexCache = check_delete_dgindex_cache.IsChecked.Value;
            }
        }

        private void check_search_temp_Checked(object sender, RoutedEventArgs e)
        {
            if (check_search_temp.IsFocused)
            {
                Settings.SearchTempPath = check_search_temp.IsChecked.Value;
            }
        }

        private void check_search_temp_Unchecked(object sender, RoutedEventArgs e)
        {
            if (check_search_temp.IsFocused)
            {
                Settings.SearchTempPath = check_search_temp.IsChecked.Value;
            }
        }

        private void button_temppath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            folder.Description = Languages.Translate("Select folder for temp files:");
            folder.ShowNewFolderButton = true;
            folder.SelectedPath = Settings.TempPath;

            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.TempPath = folder.SelectedPath;
                textbox_temp.Text = Settings.TempPath;
            }
            p.TempFolderFiles();
        }

        private void check_save_anamorph_Click(object sender, RoutedEventArgs e)
        {
            Settings.SaveAnamorph = check_save_anamorph.IsChecked.Value;
        }

        private void check_alwaysprogressive_Click(object sender, RoutedEventArgs e)
        {
            Settings.AlwaysProgressive = check_alwaysprogressive.IsChecked.Value;
        }
        
 
        private void check_auto_colormatrix_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoColorMatrix = check_auto_colormatrix.IsChecked.Value;
        }

        
        //Обработка чекбокса "помнить позицию окна"
        private void check_window_pos_Click(object sender, RoutedEventArgs e)
        {
            Settings.WindowResize = check_window_dim.IsChecked.Value;
        }

        //Обработка чекбокса "обновлять скрипт"
        private void check_renew_script_Click(object sender, RoutedEventArgs e)
        {
            Settings.RenewScript = check_renew_script.IsChecked.Value;
        }

        //Обработка чекбокса "удалять комментарии"
        private void check_hide_comments_Click(object sender, RoutedEventArgs e)
        {
            Settings.HideComments = check_hide_comments.IsChecked.Value;
        }

        //Обработка чекбокса "кроп перед фильтрацией"
        private void check_resize_first_Click(object sender, RoutedEventArgs e)
        {
            Settings.ResizeFirst = check_resize_first.IsChecked.Value;
        }


        //Обработка чекбокса "считывать параметры при добавлении задания"
        private void check_read_prmtrs_Click(object sender, RoutedEventArgs e)
        {
            Settings.ReadScript = check_read_prmtrs.IsChecked.Value;
        }

        //Лог кодирования в файл
        private void check_log_to_file_Click(object sender, RoutedEventArgs e)
        {
            Settings.WriteLog = check_log_to_file.IsChecked.Value;

            if (Settings.WriteLog)
                check_logfile_tempfolder.IsEnabled = true;
            else
                check_logfile_tempfolder.IsEnabled = false;
        }

        //Файл с логом кодирования во временную папку
        private void check_logfile_tempfolder_Click(object sender, RoutedEventArgs e)
        {
            Settings.LogInTemp = check_logfile_tempfolder.IsChecked.Value;
        }
        
        //кнопка "Открыть" для временной папки       
        private void button_temppath_open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
              System.Diagnostics.Process.Start("explorer.exe", Settings.TempPath);
            }
            catch (Exception ex)
            {
                ErrorExeption(ex.Message);
            }
        }

        private void check_batch_autoencoding_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoBatchEncoding = check_batch_autoencoding.IsChecked.Value;
        }
            
      
	}
}