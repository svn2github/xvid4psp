using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Globalization;

namespace XviD4PSP
{
   public class OpenDialogs
    {
 
        private static System.Windows.Window _owner;
        public static System.Windows.Window owner
        {
            get
            {
                return _owner;
            }
            set
            {
                _owner = value;
            }
        }

        public static ArrayList GetFilesFromConsole(string arguments)
        {         
            OpenDialog o = new OpenDialog(arguments, owner);
            return o.files;
        }

        public static Massive OpenFile()
        {
            string infilepath = null;

            ArrayList files = GetFilesFromConsole("ov");

            if (files.Count > 0)
                infilepath = files[0].ToString();

            if (infilepath != null)
            {
                //создаём массив и забиваем в него данные
                Massive m = new Massive();
                m.infilepath = infilepath;
                m.infileslist = new string[] { infilepath };
                m.owner = owner;

                //исключаем DVD меню
                //if (Path.GetFileName(m.infilepath) == "VIDEO_TS.VOB")
                //    m.infilepath = Path.GetDirectoryName(m.infilepath) + "\\VTS_01_1.VOB";

                //ищем соседние файлы и спрашиваем добавить ли их к заданию при нахождении таковых
                if (Settings.AutoJoinMode == Settings.AutoJoinModes.Enabled ||
                    Settings.AutoJoinMode == Settings.AutoJoinModes.DVDonly &&
                    Calculate.IsValidVOBName(m.infilepath))
                    m = GetFriendFilesList(m);
                if (m != null)
                    return m;
            }

            return null;
        }

       public static string SaveDialog(Massive m)
       {
           //для форматов с выводом в папку
           if (m.format == Format.ExportFormats.BluRay)
           {

               System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
               folder.Description = Languages.Translate("Select folder for BluRay files:");
               folder.ShowNewFolderButton = true;

               if (Settings.BluRayPath != null && Directory.Exists(Settings.BluRayPath))
                   folder.SelectedPath = Settings.BluRayPath;

               if (folder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
               {
                   Settings.BluRayPath = folder.SelectedPath;

                   //проверяем есть ли файлы в папке
                   if (Calculate.GetFolderSize(folder.SelectedPath) != 0)
                   {
                       Message mess = new Message(m.owner);
                       mess.ShowMessage(Languages.Translate("Folder already have files! Do you want replace files ?")
                           , Languages.Translate("Path") + ": " + folder.SelectedPath, Message.MessageStyle.YesNo);
                       if (mess.result == Message.Result.No)
                           return null;
                   }

                   return folder.SelectedPath;
               }
               else
                   return null;
           }
           //для файловых форматов
           else
           {
               SaveFileDialog s = new SaveFileDialog();
               s.AddExtension = true;
               s.SupportMultiDottedExtensions = true;
               s.Title = Languages.Translate("Select unique name for output file:");

               s.DefaultExt = "." + Format.GetValidExtension(m);

               s.FileName = m.taskname + Format.GetValidExtension(m);

               s.Filter = Format.GetValidExtension(m).Replace(".", "").ToUpper() +
                   " " + Languages.Translate("files") + "|*" + Format.GetValidExtension(m);

               if (s.ShowDialog() == DialogResult.OK)
               {
                   string ext = Path.GetExtension(s.FileName).ToLower();

                   if (!s.DefaultExt.StartsWith("."))
                       s.DefaultExt = "." + s.DefaultExt;

                   if (ext != s.DefaultExt)
                       s.FileName += s.DefaultExt;

                   return s.FileName;
               }
               else
                   return null;
           }
       }

        public static Massive GetFriendFilesList(Massive m)
        {
            ArrayList fileslist = new ArrayList();
            fileslist.Add(m.infilepath);

            if (Path.GetExtension(m.infilepath).ToLower() == ".vob" && Calculate.IsValidVOBName(m.infilepath))
            {
                if (Path.GetFileName(m.infilepath) != "VIDEO_TS.VOB")
                {
                    string title = Calculate.GetTitleNum(m.infilepath);
                    string friendfile;
                    for (int i = 2; i <= 9; i++)
                    {
                        friendfile = Path.GetDirectoryName(m.infilepath) + "\\VTS_" + title + "_" + i.ToString() + ".VOB";
                        if (File.Exists(friendfile))
                        {
                            fileslist.Add(friendfile);
                        }
                    }
                }
            }
            else
            {
                char[] chars = Path.GetFileNameWithoutExtension(m.infilepath).ToCharArray();
                int pos = 0;
                foreach (char c in chars)
                {
                    pos += 1;
                    string cstring = c.ToString();
                    if (cstring == "1")
                    {
                        string friendfile;
                        for (int i = 2; i <= 9; i++)
                        {
                            friendfile = Path.GetDirectoryName(m.infilepath) + "\\" +
                                Path.GetFileNameWithoutExtension(m.infilepath).Remove(pos - 1, 1).Insert(pos - 1, i.ToString()) +
                                Path.GetExtension(m.infilepath);
                            if (File.Exists(friendfile) == true)
                            {
                                fileslist.Add(friendfile);
                            }
                        }
                    }
                }
            }

            //забиваем все найденные файлы
            m.infileslist = Calculate.ConvertArrayListToStringArray(fileslist);

            //диалог выбора файлов если их больше одного
            if (fileslist.Count > 1)
            {
                FilesListWindow f = new FilesListWindow(m);
                if (f.m == null)
                    m = null;
                else
                    m = f.m.Clone();
            }
            
            return m;
        }



    }
}
