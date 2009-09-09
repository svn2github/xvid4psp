using System;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            //Launcher.exe 30 "С:\Program Files\Winnydows\XviD4PSP5" - кол-во попыток по 1сек, путь до XviD4PSP 
            //Launcher ждет завершения работы предыдущей копии XviD4PSP (если таковая имеется), после чего запускает новую копию. 
            //По-умолчанию дается одна попытка, если такой процесс уже запущен - то лаунчер просто завершает свою работу..
            try
            {
                int attempts = 1; //По умолчанию одна попытка..
                if (args.Length > 0) //.. но можно изменить через коммандную строку при запуске
                    attempts = Convert.ToInt32(args[0]);
                while (attempts >= 1)
                {
                    Process[] procs = Process.GetProcessesByName("XviD4PSP");
                    if (procs.Length > 0) //Если есть процессы с таким именем..
                    {
                        Thread.Sleep(1000); //..ждем и уменьшаем на единицу счетчик попыток  
                        attempts -= 1;
                    }
                    else
                    {
                        if (args.Length >= 2)
                            Process.Start(args[1] + "\\XviD4PSP.exe");
                        else
                            Process.Start("XviD4PSP.exe");
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // 
            }
        }
    }
}
