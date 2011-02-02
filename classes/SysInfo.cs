using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Collections;
using System.Data;
using System.Runtime.InteropServices;
using System.Globalization;
using Microsoft.Win32;

namespace XviD4PSP
{
    public class SysInfo
    {
        //Struct to retrieve system info
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SYSTEM_INFO
        {
            public uint dwOemId;
            public uint dwPageSize;
            public uint lpMinimumApplicationAddress;
            public uint lpMaximumApplicationAddress;
            public uint dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public uint dwProcessorLevel;
            public uint dwProcessorRevision;
        }

        //Struct to retrieve memory status (over 4Gb)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        //To get system information
        [DllImport("kernel32")]
        static extern void GetSystemInfo(ref SYSTEM_INFO pSI);

        //To get Memory status (over 4Gb)
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static int ProcessorsCount()
        {
            SYSTEM_INFO pSI = new SYSTEM_INFO();
            GetSystemInfo(ref pSI);
            return (int)pSI.dwNumberOfProcessors;
        }

        public static string GetCPUInfo()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0", false))
                {
                    string result = "Unknown";
                    if (key == null) return result;

                    object value = key.GetValue("ProcessorNameString");
                    if (value == null) return result;
                    else result = value.ToString();

                    value = key.GetValue("~MHz");
                    if (value == null) return result;
                    else return result + " (~" + value.ToString() + ")";
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetTotalRAM()
        {
            try
            {
                ulong installedMemory;
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    installedMemory = memStatus.ullTotalPhys;
                    return (installedMemory / 1048576).ToString() + "Mb";
                }
            }
            catch { }

            return "Unknown";
        }

        public static string GetOSName()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false))
                {
                    string result = "Unknown";
                    if (key == null) return result;

                    //Имя ОС
                    object value = key.GetValue("ProductName");
                    if (value == null) return result;
                    else return value.ToString();

                    //Service Pack
                    //value = key.GetValue("CSDVersion");
                    //if (value == null) return result;
                    //else return (result + " " + value.ToString());
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetOSArchitecture()
        {
            try
            {
                string env = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                if (!String.IsNullOrEmpty(env) && env.Contains("64")) return " (x64)";
                else
                {
                    env = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
                    if (!String.IsNullOrEmpty(env) && env.Contains("64")) return " (x64)";
                }

                return " (x32)";
            }
            catch
            {
                return "";
            }
        }

        public static string GetOSNameFull()
        {
            return GetOSName() + (Environment.OSVersion.ServicePack.Length > 0 ? " " + Environment.OSVersion.ServicePack : "") + GetOSArchitecture();
        }

        public static string GetFrameworkVersion()
        {
            try
            {
                string service_pack = "";
                double version = 0.0, current_version = 0.0;
                CultureInfo culture = new CultureInfo("en-US");
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP", false))
                {
                    if (key == null) return "";
                    foreach (string sub_key in key.GetSubKeyNames())
                    {
                        if (double.TryParse(Calculate.GetRegexValue(@"^v([345]\.?\d?)", sub_key), NumberStyles.Any, culture, out current_version))
                        {
                            if (current_version > version)
                            {
                                //Version
                                version = current_version;

                                //Service Pack
                                object sp = Registry.GetValue(key + "\\" + sub_key, "SP", null);
                                if (sp != null) { service_pack = (sp.ToString() != "0") ? " Service Pack " + sp.ToString() : ""; }
                                else if (current_version >= 4)
                                {
                                    sp = Registry.GetValue(key + "\\" + sub_key + "\\Full", "Servicing", null);
                                    if (sp != null) { service_pack = (sp.ToString() != "0") ? " Service Pack " + sp.ToString() : ""; }
                                    else
                                    {
                                        sp = Registry.GetValue(key + "\\" + sub_key + "\\Client", "Servicing", null);
                                        if (sp != null) { service_pack = (sp.ToString() != "0") ? " Service Pack " + sp.ToString() : ""; }
                                        else service_pack = "";
                                    }
                                }
                                else
                                    service_pack = "";
                            }
                        }
                    }
                }
                return (version > 0) ? (" (v" + version.ToString("F1", culture) + service_pack + ")") : "";
            }
            catch
            {
                return "";
            }
        }

        public static string RetrievedAviSynthVersion = "";
        public static bool RetrieveAviSynthVersion()
        {
#if DEBUG
            AviSynthVersion = "Unknown (DEBUG mode)";
#else
            AviSynthReader reader = new AviSynthReader();

            try
            {
                reader.ParseScript("Assert(false, VersionString())");
            }
            catch (Exception ex)
            {
                if (ex.Message == "Cannot load avisynth.dll") return false;
                if (ex is AviSynthException) RetrievedAviSynthVersion = ex.Message;
                else return false;
            }
            finally
            {
                reader.Close();
                reader = null;
            }
#endif
            return true;
        }
    }
}
