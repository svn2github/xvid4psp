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
            this.Owner = this.p = parent;

            //переводим
            button_ok.Content = Languages.Translate("OK");
            Title = Languages.Translate("Settings") + ":";
            check_show_psnr.ToolTip = Languages.Translate("Show x264 PSNR info");
            check_show_ssim.ToolTip = Languages.Translate("Show x264 SSIM info");
            check_show_arguments.Content = Languages.Translate("Show encoding arguments");
            check_show_script.Content = Languages.Translate("Show AviSynth script");
            check_delete_ff_cache.Content = Languages.Translate("Auto delete FFmpegSource cache");
            check_delete_dgindex_cache.Content = Languages.Translate("Auto delete DGIndex cache");
            check_search_temp.Content = Languages.Translate("Search the best temp folder place on program start");
            check_save_anamorph.Content = Languages.Translate("Maintain anamorphic aspect");
            check_alwaysprogressive.Content = Languages.Translate("Always encode to progressive video");
            check_auto_colormatrix.Content = Languages.Translate("Auto apply ColorMatrix for MPEG2 files");
            label_temppath.Content = Languages.Translate("Temp folder path:");
            check_window_dim.Content = Languages.Translate("Remember last window location");
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
            check_clone_deint.Content = Languages.Translate("Deinterlace");
            check_clone_fps.Content = Languages.Translate("Framerate");
            check_clone_audio.Content = Languages.Translate("Audio options");
            check_batch_pause.Content = Languages.Translate("Make a pause after 1-st opened file");
            check_use_64bit.Content = Languages.Translate("Use 64-bit x264");
            check_dont_delete_caches.Content = Languages.Translate("Don`t delete any caches and temporal files");
            check_use_trayicon.Content = Languages.Translate("Enable system tray icon");
            cmenu_audio_first.Content = Languages.Translate("Encode audio first, then video");

            check_use_64bit.ToolTip = Languages.Translate("Your OS must be 64-bit also!");
            check_batch_pause.ToolTip = Languages.Translate("So you can tune all encoding settings as needed, and then continue opening");
            check_clone_ar.ToolTip = Languages.Translate("Clone: resolution, crop on each side, added black borders, output SAR/aspect and aspect adjusting method.") +
                "\r\n" + Languages.Translate("Note: Autocrop analysis will not be performed!");
            check_clone_trim.ToolTip = Languages.Translate("Clone: trim start and trim end");
            check_clone_deint.ToolTip = Languages.Translate("Clone: source type, field order, deinterlace method.") +
                "\r\n" + Languages.Translate("Note: Autodeinterlace analysis will not be performed!");
            check_clone_fps.ToolTip = Languages.Translate("Clone: output framerate");
            check_clone_audio.ToolTip = Languages.Translate("Clone: output samplerate, samplerate converter, channels, channels converter");
            check_dont_delete_caches.ToolTip = Languages.Translate("Enable this option only if you use XviD4PSP as script creator, and then encoding it in another application.") +
                "\r\n" + Languages.Translate("Or for experiments. In any other cases this option must be disabled (unchecked)!");
            check_read_prmtrs.ToolTip = Languages.Translate("Read from the script: width, height, fps, duration and frames count.") + "\r\n" +
                Languages.Translate("Use it only if these parameters was changed manually in the script!");
            check_use_win7taskbar.Content = Languages.Translate("Enable Windows 7 taskbar progress indication");
            check_enable_backup.Content = Languages.Translate("Create a backups of the tasks list");
            check_validate_pathes.Content = Languages.Translate("Check for illegal characters in pathes");

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
            check_window_dim.IsChecked = Settings.WindowResize;                                   //запоминать параметры окна
            check_hide_comments.IsChecked = Settings.HideComments;                                //удалять комментарии из скрипта
            check_resize_first.IsChecked = Settings.ResizeFirst;                                  //ресайз перед фильтрацией
            check_read_prmtrs.IsChecked = Settings.ReadScript;                                    //считывать параметры скрипта
            check_log_to_file.IsChecked = check_logfile_tempfolder.IsEnabled = Settings.WriteLog; //записывать лог кодирования в файл..
            check_logfile_tempfolder.IsChecked = Settings.LogInTemp;                              //.. а файл поместить во временную папку
            textbox_extensions.Text = Settings.GoodFilesExtensions;                               //окно со списком допустимых расширений файлов (при пакетной обработке)
            check_batch_autoencoding.IsChecked = Settings.AutoBatchEncoding;                      //автозапуск кодирования (при пакетной обработке)
            check_dgindex_cache_in_temp.IsChecked = Settings.DGIndexInTemp;                       //помещать DGIndex-кэш в Темп-папку
            check_clone_ar.IsChecked = Settings.BatchCloneAR;                                     //Наследовать параметры Разрешения\Аспекта от предыдущего файла (при пакетной обработке)
            check_clone_trim.IsChecked = Settings.BatchCloneTrim;                                 //То-же что и выше, но для обрезки
            check_clone_deint.IsChecked = Settings.BatchCloneDeint;                               //А это для деинтерлейса
            check_clone_fps.IsChecked = Settings.BatchCloneFPS;                                   //Это для fps
            check_clone_audio.IsChecked = Settings.BatchCloneAudio;                               //Ну а это для звуковых параметров
            check_batch_pause.IsChecked = Settings.BatchPause;                                    //Пауза после первого открытого файла (чтоб выставить настройки и т.д.)
            check_use_64bit.IsChecked = Settings.Use64x264;                                       //Использовать 64-битную версию x264.exe
            cmenu_is_always_close_encoding.IsChecked = Settings.AutoClose;                        //Автозакрытие окна кодирования
            check_dont_delete_caches.IsChecked = !(check_delete_ff_cache.IsEnabled
                 = check_delete_dgindex_cache.IsEnabled = Settings.DeleteTempFiles);              //Удалять кэши и временные файлы
            check_use_trayicon.IsChecked = Settings.TrayIconIsEnabled;                            //Иконка в трее вкл\выкл
            cmenu_audio_first.IsChecked = Settings.EncodeAudioFirst;                              //Кодировать сначала звук, потом видео
            check_use_win7taskbar.IsChecked = Settings.Win7TaskbarIsEnabled;                      //Поддержка таскбара в Win7 вкл\выкл
            check_enable_backup.IsChecked = Settings.EnableBackup;                                //Разрешаем сохранять резервную копию списка заданий
            check_validate_pathes.IsChecked = Settings.ValidatePathes;                            //Проверять пути на "нехорошие" символы

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
            string key = new System.Windows.Input.KeyConverter().ConvertToString(e.Key);
            string mod = new System.Windows.Input.ModifierKeysConverter().ConvertToString(System.Windows.Input.Keyboard.Modifiers);
            textbox_combination.Text = ((mod.Length > 0) ? mod + "+" : "") + key;
            
            e.Handled = true;
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textbox_temp.Text) && textbox_temp.Text.Length > 3)
            {
                try
                {
                    if (textbox_temp.Text != Settings.TempPath)
                    {
                        string path = textbox_temp.Text;
                        while (path.Length > 3 && path.EndsWith("\\")) path = path.Remove(path.Length - 1, 1);
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        Settings.TempPath = textbox_temp.Text = path;
                        p.TempFolderFiles(); //Проверка папки на наличие в ней файлов
                    }
                }
                catch (Exception ex)
                {
                    ErrorException(Languages.Translate("Temp folder path:") + " " + ex.Message);
                    return;
                }
            }

            //Сохраняем список валидных расширений (для открытия папки)
            if (!string.IsNullOrEmpty(textbox_extensions.Text))
            {
                Settings.GoodFilesExtensions = textbox_extensions.Text.ToLower();
            }
          
            Close();
        }

        private void ErrorException(string message)
        {
            new Message(this).ShowMessage(message, Languages.Translate("Error"));
        }

        private void check_show_psnr_Click(object sender, RoutedEventArgs e)
        {
            Settings.x264_PSNR = check_show_psnr.IsChecked.Value;
        }

        private void check_show_ssim_Click(object sender, RoutedEventArgs e)
        {
            Settings.x264_SSIM = check_show_ssim.IsChecked.Value;
        }

        private void check_show_arguments_Click(object sender, RoutedEventArgs e)
        {
            Settings.ArgumentsToLog = check_show_arguments.IsChecked.Value;
        }

        private void check_show_script_Click(object sender, RoutedEventArgs e)
        {
            Settings.PrintAviSynth = check_show_script.IsChecked.Value;
        }

        private void check_dont_delete_caches_Click(object sender, RoutedEventArgs e)
        {
            check_delete_ff_cache.IsEnabled = check_delete_dgindex_cache.IsEnabled =
                Settings.DeleteTempFiles = !check_dont_delete_caches.IsChecked.Value;
        }
        
        private void check_delete_ff_cache_Click(object sender, RoutedEventArgs e)
        {
            Settings.DeleteFFCache = check_delete_ff_cache.IsChecked.Value;
        }

        private void check_delete_dgindex_cache_Click(object sender, RoutedEventArgs e)
        {
            Settings.DeleteDGIndexCache = check_delete_dgindex_cache.IsChecked.Value;
        }

        private void check_search_temp_Click(object sender, RoutedEventArgs e)
        {
            Settings.SearchTempPath = check_search_temp.IsChecked.Value;
        }

        private void button_temppath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            folder.Description = Languages.Translate("Place for temp files") + ":";
            folder.ShowNewFolderButton = true;
            folder.SelectedPath = Settings.TempPath;

            if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.TempPath = textbox_temp.Text = folder.SelectedPath;
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
            check_logfile_tempfolder.IsEnabled = Settings.WriteLog = check_log_to_file.IsChecked.Value;
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
                ErrorException(ex.Message);
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

        private void check_clone_deint_Click(object sender, RoutedEventArgs e)
        {
            Settings.BatchCloneDeint = check_clone_deint.IsChecked.Value;
        }
        
        private void check_clone_fps_Click(object sender, RoutedEventArgs e)
        {
            Settings.BatchCloneFPS = check_clone_fps.IsChecked.Value;
        }

        private void check_clone_audio_Click(object sender, RoutedEventArgs e)
        {
            Settings.BatchCloneAudio = check_clone_audio.IsChecked.Value;
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
                if (textbox_combination.Text != "")
                {
                    string Action = HotKeys.GetAction("=" + textbox_combination.Text);
                    if (Action != "" && Action != raw_action[combo_action.SelectedIndex].ToString())
                    {
                        ErrorException(Languages.Translate("Combination") + " \"" + textbox_combination.Text + "\" " + Languages.Translate("already used for") + " \"" + Languages.Translate(Action) + "\".");
                        return;
                    }
                }
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
            edit = true;
            textbox_combination.Text = "";
            button_save_hotkeys_Click(null, null);
        }

        private void check_use_trayicon_Click(object sender, RoutedEventArgs e)
        {
            p.TrayIcon.Visible = Settings.TrayIconIsEnabled = check_use_trayicon.IsChecked.Value;
        }

        private void cmenu_audio_first_Click(object sender, RoutedEventArgs e)
        {
            Settings.EncodeAudioFirst = cmenu_audio_first.IsChecked.Value;
        }

        private void check_use_win7taskbar_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Win7TaskbarIsEnabled = check_use_win7taskbar.IsChecked.Value)
            {
                if (!Win7Taskbar.InitializeWin7Taskbar())
                {
                    ErrorException(Languages.Translate("Failed to initialize Windows 7 taskbar interface.") +
                    " " + Languages.Translate("This feature will be disabled!"));
                    check_use_win7taskbar.IsChecked = Settings.Win7TaskbarIsEnabled = false;
                }
            }
            else
            {
                Win7Taskbar.UninitializeWin7Taskbar();
            }
        }

        private void check_enable_backup_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.EnableBackup = check_enable_backup.IsChecked.Value)
            {
                p.UpdateTasksBackup();
            }
            else
            {
                try { File.Delete(Settings.TempPath + "\\backup.tsks"); }
                catch { }
            }
        }

        private void check_validate_pathes_Click(object sender, RoutedEventArgs e)
        {
            Settings.ValidatePathes = check_validate_pathes.IsChecked.Value;
        }
	}
}