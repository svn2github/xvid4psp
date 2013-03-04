using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;
using System.Threading;
using System.Diagnostics;

namespace XviD4PSP
{
    public partial class FilesListWindow
    {
        public Massive m;

        public FilesListWindow()
        {
            this.InitializeComponent();
        }

        public FilesListWindow(Massive mass)
        {
            this.InitializeComponent();
            this.Owner = App.Current.MainWindow;
            this.m = mass.Clone();

            //забиваем список в форму
            list_files.Items.Clear();
            foreach (string file in m.infileslist)
                list_files.Items.Add(file);
            //выделяем последний файл
            list_files.SelectedItem = m.infileslist[m.infileslist.Length - 1];

            button_ok.Content = Languages.Translate("OK");
            button_cancel.Content = Languages.Translate("Cancel");
            Title = Languages.Translate("Add or remove friend files") + ":";
            btMoveUp.ToolTip = cmenu_up.Header = Languages.Translate("Move up");
            btMoveDown.ToolTip = cmenu_down.Header = Languages.Translate("Move down");
            btAdd.ToolTip = cmenu_add.Header = Languages.Translate("Add file");
            btRemove.ToolTip = cmenu_remove.Header = Languages.Translate("Remove file");

            //выдаём диалог
            ShowDialog();
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            Calculate.CheckWindowPos(this, false);
        }

        private void list_files_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Delete || e.Key == Key.OemMinus || e.Key == Key.Subtract) && list_files.SelectedItem != null) btRemove_Click(null, null);
            else if (e.Key == Key.Insert || e.Key == Key.OemPlus || e.Key == Key.Add) btAdd_Click(null, null);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m != null &&
                list_files.Items.Count > 0)
            {
                m.infileslist = new string[list_files.Items.Count];
                list_files.Items.CopyTo(m.infileslist, 0);
                m.infilepath = m.infileslist[0];
            }
            else
                m = null;
        }

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m = null;
            Close();
        }

        private void btRemove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_files.SelectedItems.Count > 0)
            {
                int n = list_files.Items.IndexOf(list_files.SelectedItems[0]);
                while (list_files.SelectedItems.Count > 0)
                {
                    foreach (string file in list_files.SelectedItems)
                    {
                        list_files.Items.Remove(file);
                        break;
                    }
                }

                //выделяем последний активный
                if (n > list_files.Items.Count - 1)
                    list_files.SelectedIndex = n - 1;
                else
                    list_files.SelectedIndex = n;
            }
        }

        private void btAdd_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string ext = Path.GetExtension(m.infileslist[0]);
            ArrayList infiles = OpenDialogs.GetFilesFromConsole("ff " + ext);

            if (infiles.Count > 0)
            {
                foreach (string file in infiles)
                    list_files.Items.Add(file);
                list_files.SelectedIndex = list_files.Items.Count - 1;
            }
        }

        private void btMoveUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_files.SelectedItems.Count > 0)
            {
                //группируем объекты
                int n = list_files.Items.IndexOf(list_files.SelectedItems[0]);
                foreach (object o in list_files.SelectedItems)
                {
                    object hole = list_files.Items[n];
                    if (hole != o)
                    {
                        list_files.Items.Remove(hole);
                        int _scount = list_files.SelectedItems.Count;
                        int _l = list_files.Items.IndexOf(list_files.SelectedItems[_scount - 1]);
                        list_files.Items.Insert(_l + 1, hole);
                    }
                    n++;
                }

                //логика пузырька :)
                int scount = list_files.SelectedItems.Count;
                int f = list_files.Items.IndexOf(list_files.SelectedItems[0]);
                int l = list_files.Items.IndexOf(list_files.SelectedItems[scount - 1]);

                //если есть что убирать
                if (f > 0)
                {
                    object o = list_files.Items[f - 1];
                    list_files.Items.Remove(o);
                    if (scount + 1 > list_files.Items.Count - 1)
                        list_files.Items.Add(o);
                    else
                        list_files.Items.Insert(l, o);
                }
                else
                    cmenu_files.IsOpen = false;
            }
        }

        private void btMoveDown_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (list_files.SelectedItems.Count > 0)
            {
                //группируем объекты
                int n = list_files.Items.IndexOf(list_files.SelectedItems[0]);
                foreach (object o in list_files.SelectedItems)
                {
                    object hole = list_files.Items[n];
                    if (hole != o)
                    {
                        list_files.Items.Remove(hole);
                        list_files.Items.Insert(n - 1, hole);
                    }
                    n++;
                }

                //логика пузырька :)
                int scount = list_files.SelectedItems.Count;
                int f = list_files.Items.IndexOf(list_files.SelectedItems[0]);
                int l = list_files.Items.IndexOf(list_files.SelectedItems[scount - 1]);

                //если есть что убирать
                if (l < list_files.Items.Count - 1)
                {
                    object o = list_files.Items[l + 1];
                    list_files.Items.Remove(o);
                    list_files.Items.Insert(f, o);
                }
                else
                    cmenu_files.IsOpen = false;
            }
        }
    }
}