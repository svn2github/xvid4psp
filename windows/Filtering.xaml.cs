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
        private string old_filtering = "";
        private string old_script = "";
        private Process avsp = null;
        private MainWindow p;
        public Massive m;

        public Filtering(Massive mass, MainWindow parent)
		{
			this.InitializeComponent();
            this.Owner = this.p = parent;

            if (mass != null)
            {
                m = mass.Clone();
                script_box.Text = m.script;

                button_refresh.Content = Languages.Translate("Apply");
                button_refresh.ToolTip = Languages.Translate("Refresh preview");
                button_fullscreen.ToolTip = Languages.Translate("Fullscreen mode");
                button_Avsp.ToolTip = Languages.Translate("AvsP editor");
            }
            else
            {
                old_filtering = Settings.Filtering;

                grid_profiles.Visibility = Visibility.Visible;
                button_refresh.Visibility = button_fullscreen.Visibility = button_Avsp.Visibility = Visibility.Collapsed;
                text_profile.Content = Languages.Translate("Profile:");
                button_add.ToolTip = Languages.Translate("Add profile");
                button_remove.ToolTip = Languages.Translate("Remove profile");

                LoadProfiles();
                LoadPreset();
            }

            //переводим
            Title = Languages.Translate("Filtering");
            button_ok.Content = Languages.Translate("OK");
            button_ok.ToolTip = Languages.Translate("Save changes");
            button_cancel.Content = Languages.Translate("Cancel");
            button_cancel.ToolTip = Languages.Translate("Cancel");

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
            if (m != null)
                m.script = script_box.Text;
            else if (old_script != script_box.Text)
                SavePreset(Settings.Filtering);

            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (m == null)
                Settings.Filtering = old_filtering;

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
                ErrorException("AvsP editor: " + ex.Message, ex.StackTrace);
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
                    ErrorException("AvsP editor: " + ex.Message, ex.StackTrace);
                }
            }
        }

        private void LoadProfiles()
        {
            //Загружаем список фильтров
            combo_profile.Items.Clear();
            combo_profile.Items.Add("Disabled");
            try
            {
                foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\filtering"))
                    combo_profile.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            catch (Exception) { }

            combo_profile.SelectedItem = Settings.Filtering;
        }

        private void LoadPreset()
        {
            try
            {
                string preset = Settings.Filtering;
                if (preset != "Disabled")
                {
                    string path = Calculate.StartupPath + "\\presets\\filtering\\" + preset + ".avs";
                    using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default))
                        script_box.Text = old_script = sr.ReadToEnd();
                }
                else
                {
                    old_script = "";
                    script_box.Clear();
                }
            }
            catch (Exception) { }
        }

        private void SavePreset(string preset)
        {
            string old_preset = "";
            try
            {
                old_preset = Settings.Filtering;
                if (preset == "Disabled")
                {
                    button_add_Click(null, null);
                    return;
                }

                Settings.Filtering = preset;
                File.WriteAllText(Calculate.StartupPath + "\\presets\\filtering\\" + preset + ".avs", script_box.Text, System.Text.Encoding.Default);
                old_script = script_box.Text;
            }
            catch (Exception ex)
            {
                ErrorException(Languages.Translate("Can`t save profile") + ": " + ex.Message, ex.StackTrace);
                Settings.Filtering = old_preset;
            }
        }

        private void combo_profile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((combo_profile.IsDropDownOpen || combo_profile.IsSelectionBoxHighlighted) && combo_profile.SelectedItem != null)
            {
                Settings.Filtering = combo_profile.SelectedItem.ToString();
                LoadPreset();
            }
        }

        private void button_add_Click(object sender, RoutedEventArgs e)
        {
            NewProfile newp = new NewProfile("Custom", "", NewProfile.ProfileType.Filtering, this);
            if (newp.profile != null)
            {
                SavePreset(newp.profile);
                LoadProfiles();
            }
        }

        private void button_remove_Click(object sender, RoutedEventArgs e)
        {
            string preset = Settings.Filtering;
            if (preset == "Disabled")
                return;

            if (combo_profile.Items.Count > 1)
            {
                Message mess = new Message(this);
                mess.ShowMessage(Languages.Translate("Do you realy want to remove profile") + " \"" + preset + "\"?",
                    Languages.Translate("Question"), Message.MessageStyle.YesNo);

                if (mess.result == Message.Result.Yes)
                {
                    int last_num = combo_profile.SelectedIndex;
                    string profile_path = Calculate.StartupPath + "\\presets\\filtering\\" + preset + ".avs";

                    try
                    {
                        File.Delete(profile_path);
                    }
                    catch (Exception ex)
                    {
                        ErrorException(Languages.Translate("Can`t delete profile") + ": " + ex.Message, ex.StackTrace);
                        return;
                    }

                    //загружаем список пресетов
                    combo_profile.Items.Clear();
                    combo_profile.Items.Add("Disabled");
                    try
                    {
                        foreach (string file in Directory.GetFiles(Calculate.StartupPath + "\\presets\\filtering"))
                            combo_profile.Items.Add(Path.GetFileNameWithoutExtension(file));
                    }
                    catch (Exception) { }

                    //прописываем текущий пресет
                    if (last_num == 0)
                    {
                        //Самый первый пресет
                        Settings.Filtering = combo_profile.Items[0].ToString();
                    }
                    else
                    {
                        //Предыдущий (перед удалённым) пресет
                        Settings.Filtering = combo_profile.Items[last_num - 1].ToString();
                    }
                    combo_profile.SelectedItem = Settings.Filtering;
                    combo_profile.UpdateLayout();

                    LoadPreset();
                }
            }
            else
            {
                new Message(this).ShowMessage(Languages.Translate("Not allowed removing the last profile!"), Languages.Translate("Warning"), Message.MessageStyle.Ok);
            }
        }

        internal delegate void ErrorExceptionDelegate(string data, string info);
        private void ErrorException(string data, string info)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new ErrorExceptionDelegate(ErrorException), data, info);
            else
            {
                Message mes = new Message(this.IsLoaded ? this : Owner);
                mes.ShowMessage(data, info, Languages.Translate("Error"), Message.MessageStyle.Ok);
            }
        }
	}
}