using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace XviD4PSP
{
    #region Structures

    /// <summary>
    /// The TOKEN_PRIVILEGES structure contains information about 
    /// a set of privileges for an access token.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TokenPrivileges
    {
        /// <summary>
        /// Specifies the number of entries in the Privileges array. 
        /// </summary>
        internal int Count;

        /// <summary>
        /// Specifies an array of LUID_AND_ATTRIBUTES structures. 
        /// Each structure contains the LUID and attributes of a privilege.
        /// To get the name of the privilege associated with a LUID,
        /// call the LookupPrivilegeName function,passing the address of 
        /// the LUID as the value of the lpLuid parameter.
        /// The attributes of a privilege can be a combination of 
        /// the following values. 
        /// </summary>
        internal long Luid;

        /// <summary>
        /// specifies the privilege.
        /// </summary>
        internal int Attribute;
    }

    /// <summary>
    /// Contains information about the power status of the system.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemPowerStatus
    {
        internal byte ACLineStatus;
        internal byte BatteryFlag;
        internal byte BatteryLifePercent;
        internal byte Reserved;
        internal int BatteryLifeTime;
        internal int BatteryFullLifeTime;
    }

    #endregion

    /// <summary>
    /// Represents the native code.
    /// </summary>
    internal class PowerManagementNativeMethods
    {
        #region Constants

        #region  Shutdown types
        /// <summary>
        /// Shuts down the system to a point at which it is safe to 
        /// turn off the power. All file buffers have been flushed to disk, 
        /// and all running processes have stopped.  
        /// </summary>
        internal const int ExitWindowShutdown = 0x00000001;

        /// <summary>
        /// Shuts down the system and then restarts the system. 
        /// </summary>
        internal const int ExitWindowReboot = 0x00000002;

        /// <summary>
        /// This flag has no effect if terminal services is enabled. 
        /// Otherwise, the system does not send 
        /// the WM_QUERYENDSESSION and WM_ENDSESSION messages. 
        /// This can cause applications to lose data.
        /// Therefore, you should only use this flag in an emergency.
        /// </summary>
        internal const int ExitWindowForce = 0x00000004;

        /// <summary>
        /// Forces processes to terminate if they do not respond to 
        /// the WM_QUERYENDSESSION 
        /// or WM_ENDSESSION message within the timeout interval.
        /// </summary>
        internal const int ExitWindowForceIfHung = 0x00000010;

        /// <summary>
        /// Shuts down all processes running in the logon session of 
        /// the process that called the ExitWindowsEx function. 
        /// Then it logs the user off. 
        /// </summary>
        internal const int ExitWindowLogOff = 0;

        /// <summary>
        /// Shuts down the system and turns off the power. 
        /// The system must support the power-off feature. 
        /// </summary>
        internal const int ExitWindowPowerOff = 0x00000008;

        #endregion

        /// <summary>
        /// Specifies that the privilege is enabled.
        /// </summary>
        internal const int SEPrivilegeEnabled = 0x00000002;

        /// <summary>
        /// Required to query an access token.
        /// </summary>
        internal const int TokenQuery = 0x00000008;

        /// <summary>
        /// Required to enable or disable the privileges in an access token.
        /// </summary>
        internal const int TokenAdjustPrivileges = 0x00000020;

        /// <summary>
        /// Shutdown privilege name.
        /// </summary>
        internal const string SEShutdownName = "SeShutdownPrivilege";

        #region Shutdown reasons

        /// <summary>
        /// Represents other major reason for shutdown.
        /// </summary>
        internal const uint ShutdownReasonMajorOther = 0x00000000;

        /// <summary>
        /// Represents other minor reason for shutdown.
        /// </summary>
        internal const uint ShutdownReasonMinorOther = 0x00000000;

        /// <summary>
        /// Major application as shutdown reason.
        /// </summary>
        internal const uint ShutdownReasonMajorApplication = 0x00040000;

        /// <summary>
        /// Maintenence as shutdown reason.
        /// </summary>
        internal const uint ShutdownReasonMinorMaintenance = 0x00000001;

        #endregion

        /// <summary>
        /// Maximum value of Shutdown/Reboot timer.
        /// </summary>
        internal const int MaximumTimerValue = 315360000;

        /// <summary>
        /// Maximum length of Shutdown/Reboot message.
        /// </summary>
        internal const int MaximumMessageLength = 512;

        #region Power setting Guids
        /// <summary>
        /// Guid for critical battery action setting.
        /// </summary>
        internal const string BatteryCriticalActionGuid = "637ea02f-bbcb-4015-8e2c-a1c7b9c0b546";
        /// <summary>
        /// Guid for critical battery level setting.
        /// </summary>
        internal const string BatteryCriticalLevelGuid = "9a66d8d7-4ff7-4ef9-b5a2-5a326ca2a469";
        /// <summary>
        /// Guid for low battery action setting.
        /// </summary>
        internal const string BatteryLowActionGuid = "d8742dcb-3e6a-4b3c-b3fe-374623cdcf06";
        /// <summary>
        /// Guid for low battery level setting.
        /// </summary>
        internal const string BatteryLowLevelGuid = "8183ba9a-e910-48da-8769-14ae6dc1170a";
        /// <summary>
        /// Guid for low battery notification setting.
        /// </summary>
        internal const string BatteryLowNotificationGuid = "bcded951-187b-4d05-bccc-f7e51960c258";
        /// <summary>
        /// Guid for adaptive display setting.
        /// </summary>
        internal const string DisplayAdaptiveGuid = "90959d22-d6a1-49b9-af93-bce885ad335b";
        /// <summary>
        /// Guid for display brightness setting.
        /// </summary>
        internal const string DisplayBrightnessGuid = "aded5e82-b909-4619-9949-f5d71dac0bcb";
        /// <summary>
        /// Guid for display off setting.
        /// </summary>
        internal const string DisplayOffAfterGuid = "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e";
        /// <summary>
        /// Guid for hard disk off setting.
        /// </summary>
        internal const string HardDiskOffAfterGuid = "6738e2c4-e8a5-4a42-b16a-e040e769756e";
        /// <summary>
        /// Guid for multimedia sharing setting.
        /// </summary>
        internal const string MMWhenSharingGuid = "03680956-93bc-4294-bba6-4e0f09bb717f";
        /// <summary>
        /// Guid for pci express power management setting.
        /// </summary>
        internal const string PciExpressPowerManagementGuid = "ee12f906-d277-404b-b6da-e5fa1a576df5";
        /// <summary>
        /// Guid for power button action setting.
        /// </summary>
        internal const string PowerButtonActionGuid = "7648efa3-dd9c-4e3e-b566-50f929386280";
        /// <summary>
        /// Guid for power button lid close setting.
        /// </summary>
        internal const string PowerButtonLidCloseActionGuid = "5ca83367-6e45-459f-a27b-476b1d01c936";
        /// <summary>
        /// Guid for sleep button setting.
        /// </summary>
        internal const string PowerButtonSleepActionGuid = "96996bc0-ad50-47ec-923b-6f41874dd9eb";
        /// <summary>
        /// Guid for start menu power button action setting.
        /// </summary>
        internal const string PowerButtonStartMenuActionGuid = "a7066653-8d6c-40a8-910e-a1f54b84c7e5";
        /// <summary>
        /// Guid for processor critical state setting.
        /// </summary>
        internal const string ProcessorCStateSettingGuid = "68f262a7-f621-4069-b9a5-4874169be23c";
        /// <summary>
        /// Guid for processor maximum state setting.
        /// </summary>
        internal const string ProcessorMaximumStateGuid = "bc5038f7-23e0-4960-96da-33abaf5935ec";
        /// <summary>
        /// Guid for processor minimum state setting.
        /// </summary>
        internal const string ProcessorMinimumStateGuid = "893dee8e-2bef-41e0-89c6-b55d0929964c";
        /// <summary>
        /// Guid for processor perfect state setting.
        /// </summary>
        internal const string ProcessorPerfStateSettingGuid = "bbdc3814-18e9-4463-8a55-d197327c45c0";
        /// <summary>
        /// Guid for search and indexing power saving mode setting.
        /// </summary>
        internal const string SearchPowerSavingModeGuid = "c1dd9fd6-ff5b-4270-8ab6-d48f1c40506a";
        /// <summary>
        /// Guid for sleep setting.
        /// </summary>
        internal const string SleepAfterGuid = "29f6c1db-86da-48c5-9fdb-f2b67b1f44da";
        /// <summary>
        /// Guid for allow away setting.
        /// </summary>
        internal const string SleepAllowAwayGuid = "25dfa149-5dd1-4736-b5ab-e8a37b5b8187";
        /// <summary>
        /// Guid for allow hybrid sleep setting.
        /// </summary>
        internal const string SleepAllowHybridSleepGuid = "94ac6d29-73ce-41a6-809f-6363ba21b47e";
        /// <summary>
        /// Guid for prevent sleep setting.
        /// </summary>
        internal const string SleepAllowPreventGuid = "b7a27025-e569-46c2-a504-2b96cad225a1";
        /// <summary>
        /// Guid for allow standby setting.
        /// </summary>
        internal const string SleepAllowStandByGuid = "abfc2519-3608-4c2a-94ea-171b0ed546ab";
        /// <summary>
        /// Guid for auto wake setting.
        /// </summary>
        internal const string SleepAutoWakeGuid = "bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d";
        /// <summary>
        /// Guid for hibernate setting.
        /// </summary>
        internal const string SleepHibernateAfter = "9d7815a6-7ee4-497e-8888-515a05f02364";
        /// <summary>
        /// Guid for sleep idleness setting.
        /// </summary>
        internal const string SleepRequiredIdlenessGuid = "81cd32e0-7833-44f3-8737-7081f38d1f70";
        /// <summary>
        /// Guid usb selective suspend setting.
        /// </summary>
        internal const string UsbSelectiveSuspendGuid = "48e6b7a6-50f5-4782-a5d4-53bb8f07e226";
        /// <summary>
        /// Guid for wireless adapter power mode setting.
        /// </summary>
        internal const string WirelessAdapterPowerModeGuid = "12bbebe6-58d6-4636-95bb-3217ef867c1a";

        #endregion

        #endregion

        #region DLL Imports
        /// <summary>
        /// Retrieves the DC index of the specified power setting.
        /// </summary>
        /// <param name="rootPowerKey">
        /// This parameter is reserved for future use and
        /// must be set to NULL.</param>
        /// <param name="schemeGuid">
        /// Represents the power scheme to examine.</param>
        /// <param name="subgroupOfPowerSettingsGuid">
        /// Represents the subgroup of power settings.</param>
        /// <param name="powerSettingGuid">
        /// Represents the specific power setting that is being used.</param>
        /// <param name="valueIndex">
        /// Address of a DWORD that will receive the DC value index.</param>
        /// <returns>Returns ERROR_SUCCESS (zero) if the 
        /// call was successful,and a non-zero value if the call failed.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true)]
        internal static extern UInt32 PowerReadDCValueIndex(IntPtr rootPowerKey,
            IntPtr schemeGuid, IntPtr subgroupOfPowerSettingsGuid,
            IntPtr powerSettingGuid, ref UInt32 valueIndex);

        /// <summary>
        /// Retrieves the AC index of the specified power setting.
        /// </summary>
        /// <param name="rootPowerKey">
        /// This parameter is reserved for future use and
        /// must be set to NULL.</param>
        /// <param name="schemeGuid">
        /// Represents the power scheme to examine.</param>
        /// <param name="subgroupOfPowerSettingsGuid">
        /// Represents the subgroup of power settings.</param>
        /// <param name="powerSettingGuid">
        /// Represents the specific power setting that is being used.</param>
        /// <param name="valueIndex">
        /// Address of a DWORD that will receive the AC value index.</param>
        /// <returns>Returns ERROR_SUCCESS (zero) if the 
        /// call was successful,and a non-zero value if the call failed.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true)]
        internal static extern UInt32 PowerReadACValueIndex(IntPtr rootPowerKey,
            IntPtr schemeGuid, IntPtr subgroupOfPowerSettingsGuid,
            IntPtr powerSettingGuid, ref UInt32 valueIndex);

        /// <summary>
        /// Retrieves the description for the specified power setting, 
        /// subgroup, or scheme.
        /// </summary>
        /// <param name="rootPowerKey">
        /// This parameter is reserved for future use and
        /// must be set to NULL.
        /// </param>
        /// <param name="schemeGuid">
        /// Represents the power scheme to examine.
        /// </param>
        /// <param name="subgroupOfPowerSettingsGuid">
        /// Represents the subgroup of power settings. 
        /// </param>
        /// <param name="powerSettingGuid">
        /// Represents the specific power setting that is being used.
        /// </param>
        /// <param name="buffer">
        /// Address of a buffer that will receive the description.
        /// </param>
        /// <param name="bufferSize">
        /// Address of a DWORD that contains the size of the buffer 
        /// pointed to by the Buffer parameter.
        /// </param>
        /// <returns>Returns ERROR_SUCCESS (zero) if the 
        /// call was successful,and a non-zero value if the call failed.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true)]
        internal static extern UInt32 PowerReadDescription(IntPtr rootPowerKey,
            IntPtr schemeGuid, IntPtr subgroupOfPowerSettingsGuid,
            IntPtr powerSettingGuid, IntPtr buffer, ref UInt32 bufferSize);

        /// <summary>
        /// Enumerates the specified elements in a power scheme.
        /// This function is normally called in a loop incrementing 
        /// the Index parameter to retrieve subkeys until they've all 
        /// been enumerated.
        /// </summary>
        /// <param name="rootPowerKey">
        /// This parameter is reserved for future use and must be set to NULL.
        /// </param>
        /// <param name="schemeGuid">
        /// Represents the power scheme that is being enumerated. 
        /// If this parameter is NULL, an enumeration of the power 
        /// policies is returned.
        /// </param>
        /// <param name="subgroupOfPowerSettingsGuid">
        /// Represents the subgroup of power settings to be enumerated. 
        /// If NULL, an enumeration of settings under the 
        /// PolicyGuid key is returned.
        /// </param>
        /// <param name="accessFlags">
        /// Set of flags that specifies what will be enumerated
        /// </param>
        /// <param name="index">
        /// Zero-based index of the scheme, subgroup, or setting that is being 
        /// enumerated. 
        /// </param>
        /// <param name="buffer">
        /// Address of a buffer, or NULL to retrieve the size of 
        /// the buffer required. 
        /// </param>
        /// <param name="bufferSize">
        /// Address of a DWORD that on entry contains the size of the
        /// buffer pointed to by the Buffer parameter. 
        /// </param>
        /// <returns>Returns ERROR_SUCCESS (zero) if the call 
        /// was successful,and a non-zero value if the call failed.
        /// </returns>        
        [DllImport("powrprof.dll", SetLastError = true)]
        internal static extern UInt32 PowerEnumerate(IntPtr rootPowerKey,
            IntPtr schemeGuid, IntPtr subgroupOfPowerSettingsGuid,
            PowerDataAccessor accessFlags, UInt32 index,
            IntPtr buffer, ref UInt32 bufferSize);

        /// <summary>
        /// Retrieves the friendly name for the specified power setting,
        /// subgroup, or scheme. 
        /// </summary>
        /// <param name="rootPowerKey">
        /// This parameter is reserved for future use and
        /// must be set to NULL.
        /// </param>
        /// <param name="schemeGuid">
        /// Represents the power scheme to examine.
        /// </param>
        /// <param name="subgroupOfPowerSettingsGuid">
        /// Represents the subgroup of power settings. Use NO_SUBGROUP_GUID 
        /// to refer to the default power scheme.
        /// </param>
        /// <param name="powerSettingGuid">
        /// Represents the specific power setting that is being used.
        /// </param>
        /// <param name="buffer">
        /// Address of a buffer that will receive the friendly name.
        /// </param>
        /// <param name="bufferSize">
        /// Address of a DWORD that contains the size of the buffer 
        /// pointed to by the Buffer parameter.
        /// </param>
        /// <returns>Returns ERROR_SUCCESS (zero) if the 
        /// call was successful,and a non-zero value if the call failed.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true)]
        internal static extern UInt32 PowerReadFriendlyName(IntPtr rootPowerKey,
            IntPtr schemeGuid, IntPtr subgroupOfPowerSettingsGuid,
            IntPtr powerSettingGuid, IntPtr buffer, ref UInt32 bufferSize);

        /// <summary>
        /// Determines whether the computer supports hibernation.
        /// </summary>
        /// <returns>If the computer supports hibernation (power state S4) 
        /// and the file Hiberfil.sys is present on the system, 
        /// the function returns TRUE.Otherwise, the function returns FALSE.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true,
            EntryPoint = "IsPwrHibernateAllowed")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsPowerHibernateAllowed();

        /// <summary>
        /// The SetSuspendState function suspends the system by 
        /// shutting power down.Depending on the Hibernate parameter,
        /// the system either enters a suspend (sleep) state or 
        /// hibernation(S4).
        /// If the ForceFlag parameter is TRUE,the system suspends 
        /// operation immediately; if it is FALSE, 
        /// the system requests permission from all applications 
        /// and device drivers before doing so.
        /// </summary>
        /// <param name="hibernate">
        /// Specifies the state of the system. If TRUE,the system 
        /// hibernates.If FALSE, the system is suspended.
        /// </param>
        /// <param name="forceCritical">
        /// Forced suspension. If TRUE, the function 
        /// broadcasts a PBT_APMSUSPEND event to each application and driver, 
        /// then immediately suspends operation.
        /// If FALSE, the function broadcasts a PBT_APMQUERYSUSPEND event 
        /// to each application to request permission to suspend operation.
        /// </param>
        /// <param name="disableWakeEvent">
        /// If TRUE, the system disables all wake events. If FALSE, 
        /// any system wake events remain enabled.
        /// </param>
        /// <returns>
        /// If the function succeeds,the return value is nonzero.
        /// If the function fails,the return value is zero. 
        /// To get extended error information, call Marshal.GetLastWin32Error.
        /// </returns>
        [DllImport("powrprof.dll", EntryPoint = "SetSuspendState",
            CharSet = CharSet.Ansi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetSuspendState(
            [MarshalAs(UnmanagedType.Bool)]bool hibernate,
            [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
            [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);

        /// <summary>
        /// Determines whether the computer supports the sleep states.
        /// </summary>
        /// <returns>
        /// If the computer supports the sleep states (S1, S2, and S3), 
        /// the function returns TRUE. Otherwise, the function returns FALSE.
        /// </returns>
        [DllImport("powrprof.dll", EntryPoint = "IsPwrHibernateAllowed",
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsPowerSuspendAllowed();

        /// <summary>
        /// Determines whether the computer supports the soft off power state.
        /// </summary>
        /// <returns>
        /// If the computer supports soft off (power state S5),
        /// the function returns TRUE.Otherwise, the function returns FALSE.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true,
            EntryPoint = "IsPwrShutdownAllowed")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsPowerShutdownAllowed();

        /// <summary>
        /// Determines the current state of the computer.
        /// </summary>
        /// <returns>
        /// If the system was restored to the working state automatically 
        /// and the user is not active, the function returns TRUE. 
        /// Otherwise, the function returns FALSE.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsSystemResumeAutomatic();

        /// <summary>
        /// This function returns a pseudohandle for the current process.
        /// </summary>
        /// <returns>
        /// The return value is a pseudohandle to the current process.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// The OpenProcessToken function opens the access token 
        /// associated with a process.
        /// </summary>
        /// <param name="currentProcessHandle">
        /// A handle to the process whose access token is opened.
        /// </param>
        /// <param name="desiredAccess">
        /// Specifies an access mask that specifies the requested 
        /// types of access to the access token. 
        /// </param>
        /// <param name="tokenHandle">
        /// A pointer to a handle that identifies the newly 
        /// opened access token when the function returns.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero. 
        /// </returns>
        [DllImport("advapi32.dll", ExactSpelling = true,
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr currentProcessHandle,
            UInt32 desiredAccess, ref IntPtr tokenHandle);

        /// <summary>
        /// The LookupPrivilegeValue function retrieves 
        /// the locally unique identifier (LUID) used on a specified system
        /// to locally represent the specified privilege name.
        /// </summary>
        /// <param name="systemName">
        /// A pointer to a null-terminated string that specifies the name 
        /// of the system on which the privilege name is retrieved. 
        /// If a null string is specified, the function attempts to find 
        /// the privilege name on the local system. 
        /// </param>
        /// <param name="privilegeName">
        /// A pointer to a null-terminated string thatspecifies the name
        /// of the privilege, as defined in the Winnt.h header file. 
        /// </param>
        /// <param name="privilegeLuid">
        /// A pointer to a variable that receives the LUID by which the 
        /// privilege is known on the system specified by the lpSystemName 
        /// parameter. 
        /// </param>
        /// <returns>If the function succeeds, the function returns nonzero.
        /// </returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(string systemName,
            string privilegeName, ref long privilegeLuid);

        /// <summary>
        /// The AdjustTokenPrivileges function enables or disables 
        /// privileges in the specified access token.
        /// </summary>
        /// <param name="tokenHandle">
        /// A handle to the access token that contains the privileges 
        /// to be modified.
        /// </param>
        /// <param name="disableAllPrivileges">
        /// Specifies whether the function disables all of 
        /// the token's privileges.
        /// </param>
        /// <param name="newState">
        /// A pointer to a TOKEN_PRIVILEGES structure that specifies 
        /// an array of privileges and their attributes. 
        /// </param>
        /// <param name="bufferLength">
        /// Specifies the size, in bytes, of the buffer pointed 
        /// to by the PreviousState parameter. 
        /// </param>
        /// <param name="previousState">
        /// A pointer to a buffer that the function fills with a 
        /// TOKEN_PRIVILEGES structure that contains the previous 
        /// state of any privileges that the function modifies.
        /// </param>
        /// <param name="returnLength">
        /// A pointer to a variable that receives the required size,
        /// in bytes, of the buffer pointed to by the PreviousState 
        /// parameter.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// </returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(IntPtr tokenHandle,
            [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
            ref TokenPrivileges newState, UInt32 bufferLength,
            IntPtr previousState, IntPtr returnLength);

        /// <summary>
        /// Logs off the interactive user, shuts down the system, 
        /// or shuts down and 
        /// restarts the system. 
        /// </summary>
        /// <param name="shutdownType">
        /// The shutdown type.
        /// </param>
        /// <param name="reason">
        /// The reason for initiating the shutdown.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ExitWindowsEx(
            UInt32 shutdownType, UInt32 reason);

        /// <summary>
        /// Retrieves the power status of the system. The status 
        /// indicates whether the
        /// system is running on AC or DC power, whether the battery
        /// is currently charging, and how much battery life remains.
        /// </summary>
        /// <param name="sysPowerStatus">
        /// A pointer to a SYSTEM_POWER_STATUS structure that receives status 
        /// iformation.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// </returns>
        [DllImport("Kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetSystemPowerStatus(
            out SystemPowerStatus sysPowerStatus);

        /// <summary>
        /// Locks the workstation's display. 
        /// </summary>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// </returns>
        [DllImport("user32.dll", SetLastError = true,
            EntryPoint = "LockWorkStation")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LockWorkstation();

        /// <summary>
        /// Initiates a shutdown and optional restart of the specified 
        /// computer.
        /// </summary>
        /// <param name="machineName">
        /// The network name of the computer to be shut down. 
        /// If lpMachineName is NULL or an empty string, the function 
        /// shuts down the local computer. 
        /// </param>
        /// <param name="message">
        /// The message to be displayed in the shutdown dialog box. 
        /// </param>
        /// <param name="timeout">
        /// The length of time that the shutdown dialog box should be 
        /// displayed, in seconds. 
        /// </param>
        /// <param name="forceAppsClosed">
        /// If this parameter is TRUE, applications with unsaved changes 
        /// are to be forcibly closed.
        /// </param>
        /// <param name="rebootAfterShutdown">
        /// If this parameter is TRUE, the computer is to restart 
        /// immediately after shutting down.
        /// </param>
        /// <returns>
        /// If the function succeeds, the return value is nonzero.
        /// </returns>
        [DllImport("Advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool InitiateSystemShutdown(
            [MarshalAs(UnmanagedType.LPStr)] string machineName,
            [MarshalAs(UnmanagedType.LPStr)] string message,
            UInt32 timeout,
            [MarshalAs(UnmanagedType.Bool)] bool forceAppsClosed,
            [MarshalAs(UnmanagedType.Bool)] bool rebootAfterShutdown);

        /// <summary>
        ///Retrieves the active power scheme and returns a GUID that 
        /// identifies the scheme.
        /// </summary>
        /// <param name="userRootPowerKey">
        /// This parameter is reserved for future use and must be set to NULL. 
        /// </param>
        /// <param name="activePolicyGuid">
        /// Address of a pointer that will receive a pointer to 
        /// a GUID structure.
        /// </param>
        /// <returns>
        /// Returns ERROR_SUCCESS (zero) if the call was 
        /// successful, and a non-zero value if the call failed.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PowerGetActiveScheme(IntPtr userRootPowerKey,
            out IntPtr activePolicyGuid);

        /// <summary>
        /// Sets the active power scheme for the current user.
        /// </summary>
        /// <param name="userRootPowerKey">
        /// This parameter is reserved for future use
        /// and must be set to NULL.
        /// </param>
        /// <param name="powerSchemeGuid">
        /// Represents the power scheme to activate.
        /// </param>
        /// <returns>
        /// Returns ERROR_SUCCESS (zero) if the call was successful,
        /// and a non-zero value if the call failed.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PowerSetActiveScheme(IntPtr userRootPowerKey,
            IntPtr powerSchemeGuid);

        /// <summary>
        ///Deletes a specified scheme from the database.
        /// </summary>
        /// <param name="rootPowerKey">
        /// This parameter is reserved for future use and must be set to NULL.
        /// </param>
        /// <param name="schemeGuid"> 
        /// Represents the power scheme that is being deleted.
        /// </param>
        /// <returns>
        /// Returns ERROR_SUCCESS (zero) if the call was successful, 
        /// and a non-zero value if the call failed.
        /// </returns>
        [DllImport("powrprof.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PowerDeleteScheme(
            IntPtr rootPowerKey,
            IntPtr schemeGuid);

        #endregion

        #region Enumeration

        internal enum PowerDataAccessor
        {
            /// <summary>
            /// Used with PowerSettingAccessCheck to check for group policy
            /// overrides for AC power settings. 
            /// </summary>
            AccessACPowerSettingIndex = 0,   // 0x0

            /// <summary>
            /// Used with PowerSettingAccessCheck to check for group policy 
            /// overrides for DC power settings. 
            /// </summary>
            AccessDCPowerSettingIndex = 1,// 0x1

            /// <summary>
            /// Used to enumerate power schemes with PowerEnumerate and with
            /// PowerSettingAccessCheck to check for restricted access to 
            /// specific power schemes. 
            /// </summary>
            AccessScheme = 16,// 0x10

            /// <summary>
            /// Used to enumerate subgroups with PowerEnumerate. 
            /// </summary>
            AccessSubgroup = 17,// 0x11

            /// <summary>
            /// Used to enumerate individual power settings with 
            /// PowerEnumerate. 
            /// </summary>
            AccessIndividualSetting = 18,// 0x12

            /// <summary>
            /// Used with PowerSettingAccessCheck to check for 
            /// group policy overrides for active power schemes. 
            /// </summary>
            AccessActiveScheme = 19, // 0x13

            /// <summary>
            /// Used with PowerSettingAccessCheck to check for 
            /// restricted access for creating power schemes. 
            /// </summary>
            AccessCreateScheme = 20 // 0x14

        };
        #endregion
    }
}
