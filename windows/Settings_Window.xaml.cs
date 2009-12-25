using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;
using System.Windows.Input;

namespace XviD4PSP
{
	public partial class Settings_Window
	{
        private MainWindow p;
        private bool edit = false, list_loaded = false;
        private ArrayList raw_action = new ArrayList(); //Action, не переведенные

        public Settings_Window(MainWindow parent, int set_focus_to)
		{
			this.InitializeComponent();

            p = parent;
            Owner = p;

            //переводим
            button_ok.Content = Languages.Translate("OK");
            Title = Languages.Translate("Settings") + ":";
            check_demux_audio.Content = Languages.Translate("Don`t demux audio for preview");
            check_show_psnr.ToolTip = Languages.Translate("Show x264 PSNR info");
            check_show_ssim.ToolTip = Languages.Translate("Show x264 SSIM info");
            check_show_arguments.Content = Languages.Translate("Show encoding arguments");
            check_show_script.Content = Languages.Translate("Show AviSynth script");
            check_delete_ff_cache.Content = Languages.Translate("Auto delete FFmpegSource cache");
            check_delete_dgindex_cache.Content = Languages.Translate("Auto delete DGIndex cache");
            check_search_temp.Content = Languages.Translate("Search the best temp folder place on program start");
            check_save_anamorph.Content = Languages.Translate("Maintain anamorph aspect");
            check_alwaysprogressive.Content = Languages.Translate("Always encode to progressive video");
            check_auto_colormatrix.Content = Languages.Translate("Auto apply ColorMatrix for MPEG2 files");
            label_temppath.Content = Languages.Translate("Temp folder path:");
            check_window_dim.Content = Languages.Translate("Remember last window location");
            check_renew_script.Content = Languages.Translate("Renew script when audio/video encoding settings is changed");
            check_hide_comments.Content = Languages.Translate("Remove comments (#text) from the AviSynth script");
            check_resize_first.Content = Languages.Translate("Make crop/resize before filtering (otherwise - after filtering)");
            check_read_prmtrs.Content = Languages.Translate("Read parameters from the script when saving a new task");
            check_log_to_file.Content = Languages.Translate("Write encoding log to file");
            check_logfile_tempfolder.Content = Languages.Translate("in Temp folder");
            label_extensions.Content = Languages.Translate("Only files with this extensions will be opened:");
            check_batch_autoencoding.Content = Languages.Translate("Start encoding after opening all files");
            cmenu_is_always_close_encoding.Content = Languages.Translate("Autoclose encoding window if task was successfully accomplished");
            check_dgindex_cache_in_temp.Content = Languages.Translate("Create DGIndex cache in Temp folder");
            label_clone.Content = Languages.Translate("Clone from the already opened file to each other:");
            check_clone_ar.Content = Languages.Translate("Aspect/Resolution info (crop, aspect, etc)");
            check_clone_trim.Content = Languages.Translate("Trim");
            check_batch_pause.Content = Languages.Translate("Make a pause after 1-st opened file");
            check_use_64bit.Content = Languages.Translate("Use 64 bit x264");

            button_restore_hotkeys.Content = Languages.Translate("Restore default settings");
            button_edit_hotkeys.Content = Languages.Translate("Edit");
            button_save_hotkeys.Content = Languages.Translate("Save");
            label_action.Content = Languages.Translate("Action") + ":";
            label_combination.Content = Languages.Translate("Combination") + ":";

            tab_main.Header = Languages.Translate("Misc");
            tab_encoding.Header = Languages.Translate("Encoding");
            tab_temp.Header = Languages.Translate("Temp files");
            tab_open_folder.Header = Languages.Translate("Batch encoding");
            //tab_hotkeys.Header = Languages.Translate("HotKeys");

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
            textbox_extensions.Text = Settings.GoodFilesExtensions; //окно со списком допустимых расширений файлов (при пакетной обработке)
            check_batch_autoencoding.IsChecked = Settings.AutoBatchEncoding; //автозапуск кодирования (при пакетной обработке)
            check_dgindex_cache_in_temp.IsChecked = Settings.DGIndexInTemp; //помещать DGIndex-кэш в Темп-папку
            check_demux_audio.ToolTip = "Leave it unchecked to avoid some problems with sound";
            check_clone_ar.IsChecked = Settings.BatchCloneAR; //Наследовать параметры Разрешения\Аспекта от предыдущего файла (при пакетной обработке)
            check_clone_trim.IsChecked = Settings.BatchCloneTrim; //То-же что и выше, но для обрезки
            check_batch_pause.IsChecked = Settings.BatchPause; //Пауза после первого открытого файла (чтоб выставить настройки и т.д.)
            check_use_64bit.IsChecked = Settings.Use64x264; //Использовать 64-битную версию x264.exe

            //Загружаем HotKeys (плюс перевод к действиям)
            foreach (string line in HotKeys.Data)
            {
                if (line.Contains("="))
                {
                    string[] action_and_combination = line.Split(new string[] { "=" }, StringSplitOptions.None);
                    raw_action.Add(action_and_combination[0].Trim());
                    string translated_action = Languages.Translate(action_and_combination[0].Trim());
                    combo_action.Items.Add(translated_action);
                    listview_hotkeys.Items.Add(new { Column1 = translated_action, Column2 = action_and_combination[1] }); 
                }
            }
            combo_action.SelectedIndex = listview_hotkeys.SelectedIndex = 0;
            textbox_combination.Text = HotKeys.GetKeys(raw_action[combo_action.SelectedIndex].ToString());
            list_loaded = true;
            
            if (Settings.WriteLog)
                check_logfile_tempfolder.IsEnabled = true;
            else
                check_logfile_tempfolder.IsEnabled = false;

            cmenu_is_always_close_encoding.IsChecked = Settings.AutoClose;

            SetTooltips();

            //Чтоб открыть окно на нужной вкладке
            if (set_focus_to == 2) tab_temp.Focus();
            else if (set_focus_to == 3) tab_encoding.Focus();
            else if (set_focus_to == 4) tab_open_folder.Focus();
            else if (set_focus_to == 5) tab_hotkeys.Focus();

            ShowDialog();
		}

        //Нажатия кнопок для HotKeys
        private void Settings_KeyDown(object sender, KeyEventArgs e)
        {
            string PressedKeys = "";
            if (Keyboard.Modifiers == ModifierKeys.Control) PressedKeys = "Ctrl+";
            if (Keyboard.Modifiers == ModifierKeys.Shift) PressedKeys = "Shift+";
            if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt)) PressedKeys = "Ctrl+Alt+";
            PressedKeys += e.Key.ToString();
            textbox_combination.Text = PressedKeys;
            e.Handled = true;
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
            new Message(this).ShowMessage(message, Languages.Translate("Error"));
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

        private void cmenu_is_always_close_encoding_Click(object sender, RoutedEventArgs e)
        {
            Settings.AutoClose = cmenu_is_always_close_encoding.IsChecked.Value;
        }

        private void check_dgindex_cache_in_temp_Click(object sender, RoutedEventArgs e)
        {
            Settings.DGIndexInTemp = check_dgindex_cache_in_temp.IsChecked.Value;
        }

        private void check_clone_ar_Click(object sender, RoutedEventArgs e)
        {
            Settings.BatchCloneAR = check_clone_ar.IsChecked.Value;
        }

        private void check_clone_trim_Click(object sender, RoutedEventArgs e)
        {
            Settings.BatchCloneTrim = check_clone_trim.IsChecked.Value;
        }
        
        private void check_batch_pause_Click(object sender, RoutedEventArgs e)
        {
            Settings.BatchPause = check_batch_pause.IsChecked.Value;
        }

        private void check_use_64bit_Click(object sender, RoutedEventArgs e)
        {
            Settings.Use64x264 = check_use_64bit.IsChecked.Value;
        }

        private void button_restore_hotkeys_Click(object sender, RoutedEventArgs e)
        {
            Settings.HotKeys = "";
            p.SetHotKeys(); //Тут происходит обновление HotKeys.Data
            UpdateHotKeysBox();
            Menu_Changed(null, null);
            textbox_combination.Text = HotKeys.GetKeys(raw_action[combo_action.SelectedIndex].ToString());
        }

        private void button_edit_hotkeys_Click(object sender, RoutedEventArgs e)
        {
            edit = !edit;
            if (edit)
            {
                button_save_hotkeys.Foreground = Brushes.Red;
                this.PreviewKeyDown += new KeyEventHandler(Settings_KeyDown);
            }
            else
            {
                button_save_hotkeys.Foreground = Brushes.White;
                this.PreviewKeyDown -= new KeyEventHandler(Settings_KeyDown);
                textbox_combination.Text = HotKeys.GetKeys(raw_action[combo_action.SelectedIndex].ToString());
            }
        }

        private void combo_action_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_action.IsDropDownOpen)
            {
                list_loaded = false;
                textbox_combination.Text = HotKeys.GetKeys(raw_action[combo_action.SelectedIndex].ToString());
                listview_hotkeys.SelectedIndex = combo_action.SelectedIndex;
                listview_hotkeys.Focus();
                list_loaded = true;
            }
        }

        private void button_save_hotkeys_Click(object sender, RoutedEventArgs e)
        {
            if (edit)
            {
                string Action = HotKeys.GetAction("=" + textbox_combination.Text);
                if (Action != "" && Action != raw_action[combo_action.SelectedIndex].ToString())
                {
                    new Message(this).ShowMessage(Languages.Translate("Combination") + " \"" + textbox_combination.Text + "\" " + Languages.Translate("already used for") + " \"" + Languages.Translate(Action) + "\".", Languages.Translate("Error"));
                }
                else
                {
                    string output = "";
                    foreach (string line in HotKeys.Data)
                    {
                        if (line.Contains("="))
                        {
                            string[] action = line.Trim().Split(new string[] { "=" }, StringSplitOptions.None);
                            if (action[0] == raw_action[combo_action.SelectedIndex].ToString())
                            {
                                output += action[0] + "=" + textbox_combination.Text + "; ";
                            }
                            else 
                                output += line.Trim() + "; ";
                        }
                    }
                    Settings.HotKeys = output;
                    p.SetHotKeys(); //Тут происходит обновление HotKeys.Data
                    UpdateHotKeysBox();
                    Menu_Changed(null, null);
                }
            }
        }

        private void Menu_Changed(object sender, SelectionChangedEventArgs e)
        {
            edit = false;
            button_save_hotkeys.Foreground = Brushes.White;
            this.PreviewKeyDown -= new KeyEventHandler(Settings_KeyDown);
        }

        private void UpdateHotKeysBox()
        {
            list_loaded = false;
            listview_hotkeys.Items.Clear();
            foreach (string line in HotKeys.Data)
            {
                if (line.Contains("="))
                {
                    string[] action_and_combination = line.Split(new string[] { "=" }, StringSplitOptions.None);
                    string translated_action = (Languages.Translate(action_and_combination[0].Trim()));
                    listview_hotkeys.Items.Add(new { Column1 = translated_action, Column2 = action_and_combination[1] });
                }
            }
            listview_hotkeys.SelectedIndex = combo_action.SelectedIndex;
            listview_hotkeys.Focus();
            list_loaded = true;
        }

        private void listview_hotkeys_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (list_loaded && listview_hotkeys.SelectedIndex != -1)
            {
                combo_action.SelectedIndex = listview_hotkeys.SelectedIndex;
                textbox_combination.Text = HotKeys.GetKeys(raw_action[combo_action.SelectedIndex].ToString());
            }
        }

        private void listview_hotkeys_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                if (!edit) button_edit_hotkeys_Click(null, null);
                else button_save_hotkeys_Click(null, null);
        }

        private void listview_hotkeys_RightUp(object sender, MouseButtonEventArgs e)
        {
            if (edit) button_edit_hotkeys_Click(null, null);
        }
	}
}