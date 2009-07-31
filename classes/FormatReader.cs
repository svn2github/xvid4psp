using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Runtime.InteropServices;
using System.IO;
using System.Text.RegularExpressions;

namespace XviD4PSP
{
    static class FormatReader
    {
       //Для одиночных значений
       public static string GetFormatInfo(string format_name, string what_to_find)
       {
           try
           {
               using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\FormatSettings.ini", System.Text.Encoding.Default))
               {
                   string line;
                   while (sr.EndOfStream == false)
                   {
                       line = sr.ReadLine();
                       if (line.StartsWith("\\" + format_name + "\\" + what_to_find + "\\"))
                       {
                           line = line.Replace("\\" + format_name + "\\" + what_to_find + "\\", "").Trim(); //оставляем только нужную часть, и удаляем пробелы с обеих концов
                           return line;
                       }
                   }
                   return null;
               }
           }
           catch (Exception ex)
           {
               return null;
           }     
       }
       
       //Для множественных значений
       public static string[] GetFormatInfo2(string format_name, string what_to_find)
       {
           try
           {
               using (StreamReader sr = new StreamReader(Calculate.StartupPath + "\\FormatSettings.ini", System.Text.Encoding.Default))
               {
                   string line;
                   ArrayList temp_lines = new ArrayList();
                   string[] lines;
                   string[] separator = new string[] { "," };
                   while (sr.EndOfStream == false)
                   {
                       line = sr.ReadLine();
                       if (line.StartsWith("\\" + format_name + "\\" + what_to_find + "\\"))
                       {
                           line = line.Replace("\\" + format_name + "\\" + what_to_find + "\\", "").TrimEnd();//оставляем только нужную часть, и удаляем пробелы с обеих концов
                           lines = line.Split(separator, StringSplitOptions.None); //Делим строку на подстроки

                           for (int i = 0; i <= lines.Length - 1; i++)
                               temp_lines.Add(lines[i].Trim()); //Избавляемся от пробелов в подстроках, и пересобираем строчку

                           lines = (String[])temp_lines.ToArray(typeof(string));
                           return lines;
                       }
                   }
                   return null;
               }
           }
           catch (Exception ex)
           {
               return null;
           }     
       }      

    }
}
