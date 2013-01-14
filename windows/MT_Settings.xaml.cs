using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;

namespace XviD4PSP
{
    public partial class MT_Settings
    {
        public Massive m;
        private string old_raw_script;
        public bool NeedUpdate = false;
        private string _def = Languages.Translate("Default") + ": ";

        public MT_Settings(Massive mass, System.Windows.Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;

            if (mass != null)
            {
                m = mass.Clone();

                //Скрипт для определения изменений
                m = AviSynthScripting.CreateAutoAviSynthScript(m);
                old_raw_script = m.script;
            }

            //переводим
            MT.Header = Title = Languages.Translate("MT settings");
            label_memorymax.Content = Languages.Translate("Value for SetMemoryMax") + ":";
            label_mtmode_before.Content = Languages.Translate("Start SetMTMode with") + ":";
            label_mtmode_after.Content = Languages.Translate("End SetMTMode with") + ":";
            label_mtmode_threads.Content = Languages.Translate("Threads number") + ":";
            label_mt_internal.Content = Languages.Translate("MT internal handling") + ":";
            button_ok.Content = Languages.Translate("OK");

            if (SysInfo.AVSIsMT)
            {
                string info = Languages.Translate("Please refer to MUXER documentation for more info").Replace("MUXER", "AviSynth") + ".\r\n\r\n";
                num_memorymax.ToolTip = Languages.Translate("Sets the size of the frame buffer cache, in Mb (0 - Auto)") + ".\r\n" + info + _def + "0";
                num_mtmode_before.ToolTip = Languages.Translate("MT mode before video importing function (0 - don't add SetMTMode)") + ".\r\n" +
                    Languages.Translate("Recommended value: 3, 5 or 6") + "\r\n\r\n" + info + _def + "0";
                num_mtmode_after.ToolTip = Languages.Translate("MT mode after video importing function (0 - don't add SetMTMode)") + ".\r\n" +
                    Languages.Translate("Recommended value: 2") + "\r\n\r\n" + info + _def + "0";
                num_mtmode_threads.ToolTip = Languages.Translate("Sets the number of threads for MT (0 - Auto)") + ".\r\n" + info + _def + "0";

                if (Settings.MTSettings_Warning)
                {
                    Message mes = new Message(this);
                    mes.ShowMessage(Languages.Translate("This settings is for advanced users only.") + "\r\n" +
                        Languages.Translate("Incorrect values may leads to hangs or crashes!") + "\r\n" +
                        Languages.Translate("Use it at your own risk!"), Languages.Translate("Warning"), Message.MessageStyle.Ok);
                    Settings.MTSettings_Warning = false;
                }
            }
            else
            {
                grid_main.IsEnabled = false;
                grid_main.ToolTip = Languages.Translate("You need a multithreaded (MT) version of AviSynth to use this settings") + ".\r\n" +
                    Languages.Translate("Your current version is") + ": " + SysInfo.AVSVersionString;
            }

            combo_mt_internal.Items.Add(new ComboBoxItem() { Content = "Undefined", Tag = MTMode.Undefined, ToolTip = Languages.Translate("Don't care about multithreading") });
            combo_mt_internal.Items.Add(new ComboBoxItem() { Content = "Deactivate", Tag = MTMode.Disabled, ToolTip = Languages.Translate("Deactivate multithreading") });
            combo_mt_internal.Items.Add(new ComboBoxItem() { Content = "Distributor", Tag = MTMode.AddDistr, ToolTip = Languages.Translate("Add Distributor() at the end of the script") });

            num_memorymax.Value = Settings.SetMemoryMax;
            num_mtmode_before.Value = Settings.SetMTMode_1;
            num_mtmode_after.Value = Settings.SetMTMode_2;
            num_mtmode_threads.Value = Settings.SetMTMode_Threads;
            combo_mt_internal.SelectedValue = Settings.MTMode_Internal;

            ShowDialog();
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            if (m != null)
            {
                //Новый скрипт
                m = AviSynthScripting.CreateAutoAviSynthScript(m);

                //Проверяем, изменился ли скрипт
                NeedUpdate = (old_raw_script != m.script);
            }

            Close();
        }

        private void num_memorymax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_memorymax.IsAction)
            {
                Settings.SetMemoryMax = Convert.ToInt32(num_memorymax.Value);
            }
        }

        private void num_mtmode_before_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_mtmode_before.IsAction)
            {
                Settings.SetMTMode_1 = Convert.ToInt32(num_mtmode_before.Value);
            }
        }

        private void num_mtmode_after_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_mtmode_after.IsAction)
            {
                Settings.SetMTMode_2 = Convert.ToInt32(num_mtmode_after.Value);
            }
        }

        private void num_mtmode_threads_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (num_mtmode_threads.IsAction)
            {
                Settings.SetMTMode_Threads = Convert.ToInt32(num_mtmode_threads.Value);
            }
        }

        private void combo_mt_internal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_mt_internal.SelectedItem != null)
            {
                ComboBoxItem item = (ComboBoxItem)combo_mt_internal.SelectedItem;

                if (combo_mt_internal.IsDropDownOpen || combo_mt_internal.IsSelectionBoxHighlighted)
                {
                    Settings.MTMode_Internal = (MTMode)item.Tag;
                }

                combo_mt_internal.ToolTip = item.Content + " - " + item.ToolTip + ".\r\n\r\n" +
                    Languages.Translate("How XviD4PSP will handle multithreaded scripts in all its internal operations (not affect video encoding!).") + "\r\n" +
                    Languages.Translate("When MT isn't used, recommended value is Undefined.") + "\r\n" +
                    Languages.Translate("Distributor is needed for efficient MT processing, but Deactivating may increase the stability.") + "\r\n\r\n" +
                    _def + MTMode.Undefined.ToString();
            }
        }
    }
}