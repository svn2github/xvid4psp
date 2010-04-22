using System;
using System.Drawing;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace XviD4PSP
{
    #region Structures

    /// <summary>
    /// Represents the status of the battery.
    /// </summary>
    public struct BatteryStatus
    {
        #region Private Members

        /// <summary>
        /// If true, at least one battery is present in the system.
        /// </summary>
        private bool batteryPresent;

        /// <summary>
        /// If this member is TRUE, a battery is currently charging. 
        /// </summary>
        private bool charging;

        /// <summary>
        /// The battery charge status.
        /// </summary>
        private BatteryFlag batteryFlag;

        /// <summary>
        /// The percentage of full battery charge remaining. 
        /// This member can be a value in the range 0 to 100, 
        /// or 255 if status is unknown.
        /// </summary>
        private byte batteryLifePercent;

        #endregion

        #region Properties

        /// <summary>
        /// If true, at least one battery is present in the system.
        /// </summary>
        public bool BatteryPresent
        {
            get
            {
                return batteryPresent;
            }
            set
            {
                batteryPresent = value;
            }
        }

        /// <summary>
        /// If this member is TRUE, a battery is currently charging. 
        /// </summary>
        public bool Charging
        {
            get
            {
                return charging;
            }
            set
            {
                charging = value;
            }
        }

        /// <summary>
        /// The battery charge status.
        /// </summary>
        public BatteryFlag BatteryFlag
        {
            get
            {
                return batteryFlag;
            }
            set
            {
                batteryFlag = value;
            }
        }

        /// <summary>
        /// The percentage of full battery charge remaining. 
        /// This member can be a value in the range 0 to 100, 
        /// or 255 if status is unknown.
        /// </summary>
        public byte BatteryLifePercent
        {
            get
            {
                return batteryLifePercent;
            }
            set
            {
                batteryLifePercent = value;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents System battery status.
    /// </summary>
    public struct SystemBatteryState
    {
        #region Private Members

        /// <summary>
        /// If this member is TRUE, the system battery charger is currently 
        /// operating on external power. 
        /// </summary>
        private bool alternateCurrentOnline;

        /// <summary>
        /// If this member is TRUE, at least one battery is present in the 
        /// system. 
        /// </summary>
        private bool batteryPresent;

        /// <summary>
        /// If this member is TRUE, a battery is currently charging. 
        /// </summary>
        private bool charging;

        /// <summary>
        /// If this member is TRUE, a battery is currently discharging. 
        /// </summary>
        private bool discharging;

        ///// <summary>
        ///// Reserved. 
        ///// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private byte[] spare;

        /// <summary>
        /// The theoretical capacity of the battery when new, in mWh.
        /// </summary>
        private UInt32 maxCapacity;

        /// <summary>
        /// The estimated remaining capacity of the battery, in mWh.
        /// </summary>
        private UInt32 remainingCapacity;

        /// <summary>
        /// The current rate of discharge of the battery, in mW. 
        /// A nonzero, positive rate indicates charging; 
        /// a negative rate indicates discharging. 
        /// </summary>
        private UInt32 rate;

        /// <summary>
        /// The estimated time remaining on the battery, in seconds. 
        /// </summary>
        private UInt32 estimatedTime;

        /// <summary>
        /// The manufacturer's suggestion of a capacity, in mWh,
        /// at which a low battery alert should occur.
        /// </summary>
        private UInt32 defaultAlert1;

        /// <summary>
        /// The manufacturer's suggestion of a capacity, in mWh,
        /// at which a warning battery alert should occur.
        /// </summary>
        private UInt32 defaultAlert2;

        public SystemBatteryState(byte[] spare) : this()
        {
            this.spare = spare;
        }

        #endregion

        #region Properties

        /// <summary>
        /// If this member is TRUE, the system battery charger is currently 
        /// operating on external power. 
        /// </summary>
        public bool AlternateCurrentOnline
        {
            get
            {
                return alternateCurrentOnline;
            }
            set
            {
                alternateCurrentOnline = value;
            }
        }

        /// <summary>
        /// If this member is TRUE, at least one battery is present in the 
        /// system. 
        /// </summary>
        public bool BatteryPresent
        {
            get
            {
                return batteryPresent;
            }
            set
            {
                batteryPresent = value;
            }
        }

        /// <summary>
        /// If this member is TRUE, a battery is currently charging. 
        /// </summary>
        public bool Charging
        {
            get
            {
                return charging;
            }
            set
            {
                charging = value;
            }
        }

        /// <summary>
        /// If this member is TRUE, a battery is currently discharging. 
        /// </summary>
        public bool Discharging
        {
            get
            {
                return discharging;
            }
            set
            {
                discharging = value;
            }
        }

        /// <summary>
        /// Reserved. 
        /// </summary>
        public ArrayList Spare
        {
            get
            {
                ArrayList spareArrayList = new ArrayList();
                for (int index = 0; index < spare.Length; index++)
                {
                    spareArrayList.Add(spare[index]);
                }

                return spareArrayList;
            }
        }

        /// <summary>
        /// The theoretical capacity of the battery when new, in mWh.
        /// </summary>
        public UInt32 MaxCapacity
        {
            get
            {
                return maxCapacity;
            }
            set
            {
                maxCapacity = value;
            }
        }

        /// <summary>
        /// The estimated remaining capacity of the battery, in mWh.
        /// </summary>
        public UInt32 RemainingCapacity
        {
            get
            {
                return remainingCapacity;
            }
            set
            {
                remainingCapacity = value;
            }
        }

        /// <summary>
        /// The current rate of discharge of the battery, in mW. 
        /// A nonzero, positive rate indicates charging; 
        /// a negative rate indicates discharging. 
        /// </summary>
        public UInt32 Rate
        {
            get
            {
                return rate;
            }
            set
            {
                rate = value;
            }
        }

        /// <summary>
        /// The estimated time remaining on the battery, in seconds. 
        /// </summary>
        public UInt32 EstimatedTime
        {
            get
            {
                return estimatedTime;
            }
            set
            {
                estimatedTime = value;
            }
        }

        /// <summary>
        /// The manufacturer's suggestion of a capacity, in mWh,
        /// at which a low battery alert should occur.
        /// </summary>
        public UInt32 DefaultAlert1
        {
            get
            {
                return defaultAlert1;
            }
            set
            {
                defaultAlert1 = value;
            }
        }

        /// <summary>
        /// The manufacturer's suggestion of a capacity, in mWh,
        /// at which a warning battery alert should occur.
        /// </summary>
        public UInt32 DefaultAlert2
        {
            get
            {
                return defaultAlert2;
            }
            set
            {
                defaultAlert2 = value;
            }
        }

        #endregion
    };

    #endregion

    #region  Enumerations

    /// <summary>
    /// Represents the battery charge status.
    /// </summary>
    [Flags]
    public enum BatteryFlag : byte
    {
        /// <summary>
        /// The battery is not being charged and 
        /// the battery capacity is between low and high(between 33 to 66 %).
        /// </summary>
        BetweenLowAndHigh = 0,

        /// <summary>
        /// The battery capacity is at more than 66 percent.
        /// </summary>
        High = 1,

        /// <summary>
        /// The battery capacity is at less than 33 percent.
        /// </summary>
        Low = 2,

        /// <summary>
        /// The battery capacity is at less than five percent.
        /// </summary>
        Critical = 4,

        /// <summary>
        /// Charging.
        /// </summary>
        Charging = 8,

        /// <summary>
        /// No system battery available.
        /// </summary>
        NoSystemBattery = 128,

        /// <summary>
        /// Unable to read the battery flag information.
        /// </summary>
        UnknownStatus = 255
    }

    /// <summary>
    ///  Represents power information to be set or retrieved.
    /// </summary>
    public enum PowerInformationLevel
    {
        /// <summary>
        /// For No Information.
        /// </summary>
        None = 0,

        /// <summary>
        /// Information of System Battery State.
        /// </summary>
        SystemBatteryState = 5
    }

    #endregion

    #region PowerManager class

    /// <summary>
    /// Provides wrapper over power management native APIs.
    /// </summary>     
    [ToolboxItem(true), ToolboxBitmap(typeof(PowerManager))]
    public class PowerManager : Component
    {
        #region Public Properties

        /// <summary>
        /// Determines whether the computer supports hibernation.
        /// </summary>
        /// <returns>If the computer supports hibernation (power state S4) 
        /// and the file Hiberfil.sys is present on the system, 
        /// the function returns TRUE.Otherwise, the function returns FALSE.
        /// </returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool IsPowerHibernateAllowed
        {
            get
            {
                try
                {
                    return !(PowerManagementNativeMethods.IsPowerHibernateAllowed());
                }
                catch (Exception exception)
                {
                    throw new PowerManagerException(exception.Message, exception);
                }
            }
        }

        /// <summary>
        /// Determines whether the computer supports the sleep states.
        /// </summary>
        /// <returns>
        /// If the computer supports the sleep states (S1, S2, and S3), 
        /// the function returns TRUE. Otherwise, the function returns FALSE.
        /// </returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool IsPowerSuspendAllowed
        {
            get
            {
                try
                {
                    return PowerManagementNativeMethods.IsPowerSuspendAllowed();
                }
                catch (Exception exception)
                {
                    throw new PowerManagerException(exception.Message, exception);
                }
            }
        }

        /// <summary>
        /// Determines whether the computer supports the soft off power state.
        /// </summary>
        /// <returns>If the computer supports soft off (power state S5), 
        /// the function returns TRUE. 
        /// Otherwise, the function returns FALSE.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool IsPowerShutdownAllowed
        {
            get
            {
                try
                {
                    return PowerManagementNativeMethods.IsPowerShutdownAllowed();
                }
                catch (Exception exception)
                {
                    throw new PowerManagerException(exception.Message, exception);
                }
            }
        }

        /// <summary>
        /// Determines the current state of the computer.
        /// </summary>
        /// <returns>
        /// If the system was restored to the working state automatically 
        /// and the user is not active, the function returns TRUE. 
        /// Otherwise, the function returns FALSE.
        /// </returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool IsSystemResumeAutomatic
        {
            get
            {
                try
                {
                    return PowerManagementNativeMethods.IsSystemResumeAutomatic();
                }
                catch (Exception exception)
                {
                    throw new PowerManagerException(exception.Message, exception);
                }
            }
        }

        #endregion

        #region Public Methods

        #region Power Scheme Related Methods
        /// <summary>
        /// Retrieves the title of the active power scheme.
        /// </summary>
        /// <returns>Title of the active power scheme 
        /// if the method succeeds else returns null.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public string GetActivePowerScheme()
        {
            try
            {
                //Get the active power scheme guid.
                string schemeGuid = PowerManager.GetActivePowerSchemeGuid();

                //Get all available power scheme names and guids.
                ArrayList allSchemes = PowerManager.GetAvailablePowerSchemesAndGuid();

                for (int index = 0; index < allSchemes.Count; index++)
                {
                    //Compare the active power scheme guid with retrieved
                    //power schemes' guids and get the name of active power 
                    //scheme and return it.
                    if (((ArrayList)allSchemes[index])[0].ToString() == schemeGuid)
                        return ((ArrayList)allSchemes[index])[1].ToString();
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            return String.Empty;
        }

        /// <summary>
        /// Sets the active power scheme.
        /// </summary>
        /// <param name="powerSchemeTitle">The title of the power 
        /// scheme to be activated.</param>
        /// <returns>True, if the method succeeds.</returns>
        /// <exception cref="ArgumentNullException">
        /// The required parameter is null or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified parameter is not valid.
        /// </exception>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool SetActivePowerScheme(string powerSchemeTitle)
        {
            if (String.IsNullOrEmpty(powerSchemeTitle))
            {
                throw new ArgumentNullException
                    (PowerManagementResource.ArgumentNullMessage);
            }

            bool setActivePowerSchemeStatus = false;
            bool powerSchemeExist = false;

            try
            {
                //Get all available power scheme names and guids.
                ArrayList allSchemes = PowerManager.GetAvailablePowerSchemesAndGuid();

                for (int index = 0; index < allSchemes.Count; index++)
                {
                    //Get the guid of the schemename passed, pass it as parameter
                    //to SetActivePowerSchemeGuid function.
                    if (((ArrayList)allSchemes[index])[1].ToString() ==
                        powerSchemeTitle)
                    {
                        powerSchemeExist = true;

                        setActivePowerSchemeStatus
                            = PowerManager.SetActivePowerSchemeGuid(
                                ((ArrayList)allSchemes[index])[0].ToString());

                        break;
                    }
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!powerSchemeExist)
            {
                throw new ArgumentException(PowerManagementResource.PowerSchemeNotFound);
            }

            return setActivePowerSchemeStatus;
        }

        /// <summary>
        /// Retrieves all available power schemes.
        /// </summary>
        /// <returns>List of all the available power schemes.
        /// </returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public List<string> GetAvailablePowerSchemes()
        {
            List<string> allPowerSchemes = new List<string>();
            IntPtr ptrToPowerScheme = IntPtr.Zero;
            IntPtr friendlyName = IntPtr.Zero;
            try
            {
                uint buffSize = 100;
                uint schemeIndex = 0;
                ptrToPowerScheme =
                    Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));

                //Get the guids of all available power schemes.
                while (PowerManagementNativeMethods.PowerEnumerate(
                            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                            PowerManagementNativeMethods.PowerDataAccessor.AccessScheme,
                            schemeIndex, ptrToPowerScheme, ref buffSize) == 0)
                {
                    friendlyName = Marshal.AllocHGlobal(1000);
                    buffSize = 1000;

                    //Pass the guid retrieved in PowerEnumerate as 
                    //parameter to get the power scheme name.
                    PowerManagementNativeMethods.PowerReadFriendlyName(IntPtr.Zero,
                        ptrToPowerScheme, IntPtr.Zero, IntPtr.Zero, friendlyName, ref buffSize);

                    string name = Marshal.PtrToStringUni(friendlyName);

                    //Add retrieved power scheme name in the list.
                    allPowerSchemes.Add(name);

                    schemeIndex++;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }
            finally
            {
                Marshal.FreeHGlobal(ptrToPowerScheme);
                Marshal.FreeHGlobal(friendlyName);
            }
            return allPowerSchemes;
        }

        /// <summary>
        /// Deletes the specified power scheme.
        /// </summary>
        /// <param name="powerSchemeTitle">The title of the power scheme to 
        /// be deleted.</param>
        /// <returns>True, if the method succeeds.</returns>
        /// <exception cref="ArgumentNullException">
        /// The required parameter is null or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified parameter is not valid.
        /// </exception>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool DeletePowerScheme(string powerSchemeTitle)
        {
            if (String.IsNullOrEmpty(powerSchemeTitle))
            {
                throw new ArgumentNullException(
                    PowerManagementResource.ArgumentNullMessage);
            }

            //Check whether passed powerSchemeTitle is either one of 
            //default power schemes or active power scheme.
            if (powerSchemeTitle.Equals(PowerManagementResource.PowerSchemeBalancedTitle)
                || powerSchemeTitle.Equals(PowerManagementResource.PowerSchemePowerSaverTitle)
                || powerSchemeTitle.Equals(PowerManagementResource.PowerSchemeHighPerformanceTitle)
                || powerSchemeTitle.Equals(this.GetActivePowerScheme()))
            {
                throw new PowerManagerException(
                    PowerManagementResource.DeleteSchemeMessage);
            }

            bool deletePowerSchemeStatus = false;
            bool powerSchemeExist = false;
            IntPtr ptrToActiveScheme = IntPtr.Zero;
            try
            {
                //Get all available power scheme names and guids.
                ArrayList allSchemes = PowerManager.GetAvailablePowerSchemesAndGuid();

                Guid deleteSchemeGuid = Guid.Empty;
                for (int index = 0; index < allSchemes.Count; index++)
                {
                    //Compare the active scheme names with passed scheme name
                    //and get the guid of power scheme to be deleted.
                    if (((ArrayList)allSchemes[index])[1].ToString()
                        == powerSchemeTitle)
                    {
                        deleteSchemeGuid
                            = new Guid(((ArrayList)allSchemes[index])[0].ToString());

                        powerSchemeExist = true;
                        break;
                    }
                }

                if (deleteSchemeGuid != Guid.Empty)
                {
                    ptrToActiveScheme
                        = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));

                    Marshal.StructureToPtr(deleteSchemeGuid, ptrToActiveScheme, true);

                    //Delete the power scheme, giving powerscheme guid ad argument.
                    deletePowerSchemeStatus
                        = !(PowerManagementNativeMethods.PowerDeleteScheme(
                                IntPtr.Zero, ptrToActiveScheme));
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }
            finally
            {
                Marshal.FreeHGlobal(ptrToActiveScheme);
            }
            //Throw exception if specified power scheme does not exist.
            if (!powerSchemeExist)
            {
                throw new ArgumentException(PowerManagementResource.PowerSchemeNotFound);
            }

            return deletePowerSchemeStatus;
        }

        /// <summary>
        /// This method retrieves the AC power setting values for the specified power scheme.
        /// </summary>
        /// <param name="powerSchemeTitle">The title of the power scheme.</param>
        /// <returns>The AC power setting values for the specified power scheme.</returns>
        /// <exception cref="ArgumentNullException">
        /// The required parameter is null or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified parameter is not valid.
        /// </exception>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public PowerScheme ReadPowerSchemeACValue(string powerSchemeTitle)
        {
            return PowerManager.ReadPowerSchemeValue(powerSchemeTitle, ValueType.AC);
        }

        /// <summary>
        /// This method retrieves the DC power setting values for the specified power scheme.
        /// </summary>
        /// <param name="powerSchemeTitle">The title of the power scheme.</param>
        /// <returns>The DC power setting values for the specified power scheme.</returns>
        /// <exception cref="ArgumentNullException">
        /// The required parameter is null or empty.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified parameter is not valid.
        /// </exception>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public PowerScheme ReadPowerSchemeDCValue(string powerSchemeTitle)
        {
            return PowerManager.ReadPowerSchemeValue(powerSchemeTitle, ValueType.DC);
        }
        #endregion

        #region Battery Related Methods

        /// <summary>
        /// Gets the status of battery.
        /// </summary>
        /// <returns>Battery status.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public BatteryStatus GetBatteryStatus()
        {
            bool systemPowerStatus = false;
            BatteryStatus batteryStatus = new BatteryStatus();

            try
            {
                SystemPowerStatus powerStatus = new SystemPowerStatus();

                //Get BatteryLifePercent and BatteryFlag values
                systemPowerStatus =
                    (PowerManagementNativeMethods.GetSystemPowerStatus(out powerStatus));


                batteryStatus.BatteryLifePercent = powerStatus.BatteryLifePercent;
                batteryStatus.BatteryFlag = (BatteryFlag)powerStatus.BatteryFlag;

                if ((powerStatus.BatteryFlag & (byte)BatteryFlag.Charging) ==
                    (byte)BatteryFlag.Charging)
                {
                    batteryStatus.Charging = true;
                }
                else
                {
                    batteryStatus.Charging = false;
                }

                if ((powerStatus.BatteryFlag & (byte)BatteryFlag.NoSystemBattery) ==
                   (byte)BatteryFlag.NoSystemBattery)
                {
                    batteryStatus.BatteryPresent = false;
                }
                else
                {
                    batteryStatus.BatteryPresent = true;
                }
            }
            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }
            if (!systemPowerStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return batteryStatus;
        }

        #endregion

        #region Windows Security Related Methods

        #region Reboot

        /// <summary>
        /// Shuts down the system and then restarts the system.
        /// </summary>
        /// <param name="force">If True, then it forces processes to terminate.
        /// If false then sends message to processes and it forces processes 
        /// to terminate if they do not respond to the message within 
        /// the timeout interval.</param>
        /// <returns>True, if reboot has been initiated.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool RebootComputer(bool force)
        {
            bool rebootComputerStatus = false;
            bool hasPrivilege = false;
            try
            {
                //Set privileges for this application for rebooting pc.
                if (PowerManager.SetPrivilege(PowerManagementNativeMethods.SEShutdownName))
                {
                    if (force)
                    {
                        //Force all applications to close and reboot the system .
                        rebootComputerStatus = PowerManagementNativeMethods.ExitWindowsEx(
                            (PowerManagementNativeMethods.ExitWindowReboot
                                + PowerManagementNativeMethods.ExitWindowForceIfHung
                                + PowerManagementNativeMethods.ExitWindowForce),
                            (PowerManagementNativeMethods.ShutdownReasonMajorApplication |
                                PowerManagementNativeMethods.ShutdownReasonMinorMaintenance));
                    }

                    else
                    {
                        //Sends message to processes and it forces processes to terminate
                        //if they do not respond to the message within the timeout interval.
                        rebootComputerStatus = PowerManagementNativeMethods.ExitWindowsEx(
                            PowerManagementNativeMethods.ExitWindowReboot,
                            (PowerManagementNativeMethods.ShutdownReasonMajorApplication |
                                PowerManagementNativeMethods.ShutdownReasonMinorMaintenance));
                    }

                    hasPrivilege = true;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!hasPrivilege)
            {
                throw new PowerManagerException(PowerManagementResource.NoShutdownPrivilege);
            }
            else if (!rebootComputerStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return rebootComputerStatus;
        }

        /// <summary>
        /// Shuts down the system and then restarts the system.
        /// </summary>
        /// <param name="rebootMessage">The message to be displayed in the 
        /// shutdown dialog box.Maximum length is 511 characters.</param>
        /// <param name="secondsTimer">The length of time that the shutdown
        /// dialog box should be displayed.MAX Shutdown TimeOut == 10 Years in seconds. 
        /// ((10*365*24*60*60)-1 seconds).
        /// </param>
        /// <param name="force">If this parameter is TRUE, applications 
        /// with unsaved changes are to be forcibly closed.
        /// Note that this can result in data loss.
        /// </param>
        /// <returns>True, if reboot has been initiated.</returns>
        /// <exception cref="ArgumentException">
        /// The specified parameter is not valid.
        /// </exception>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool RebootComputer(string rebootMessage,
            UInt32 secondsTimer, bool force)
        {
            //Check for valid parameter values.
            if (!String.IsNullOrEmpty(rebootMessage))
            {
                if (rebootMessage.Length >=
                    PowerManagementNativeMethods.MaximumMessageLength)
                {
                    throw new ArgumentException(
                        PowerManagementResource.RebootMessageLength);
                }
            }

            if (secondsTimer >= PowerManagementNativeMethods.MaximumTimerValue)
            {
                throw new ArgumentException(
                    PowerManagementResource.MaximumRebootTimerValue);
            }

            bool rebootComputerStatus = false;
            bool hasPrivilege = false;
            try
            {
                //Set privileges for this application for rebooting pc.
                if (PowerManager.SetPrivilege(PowerManagementNativeMethods.SEShutdownName))
                {
                    //Show specified message and reboot PC after specified time.
                    rebootComputerStatus =
                        PowerManagementNativeMethods.InitiateSystemShutdown(
                            null, rebootMessage, secondsTimer, force, true);

                    hasPrivilege = true;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!hasPrivilege)
            {
                throw new PowerManagerException(PowerManagementResource.NoShutdownPrivilege);
            }
            else if (!rebootComputerStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return rebootComputerStatus;
        }

        #endregion

        #region ShutDown

        /// <summary>
        /// Shuts down the system to a point at which it is safe to turn off 
        /// the power.
        /// </summary>
        /// <param name="force">If True, then it forces processes to terminate. 
        /// If false then sends message to processes and it forces processes 
        /// to terminate if they do not respond to the message within the 
        /// timeout interval.</param>
        /// <returns>True, if shutdown has been initiated.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool ShutdownComputer(bool force)
        {
            bool shutdownComputerStatus = false;
            bool hasPrivilege = false;
            try
            {
                //Check whether the computer supports the soft off power state and 
                //Set privileges for this application for rebooting pc.
                if (this.IsPowerShutdownAllowed
                    && PowerManager.SetPrivilege(PowerManagementNativeMethods.SEShutdownName))
                {
                    if (force)
                    {
                        //Force all applications to close and shutdown the system.
                        shutdownComputerStatus =
                            PowerManagementNativeMethods.ExitWindowsEx(
                                (PowerManagementNativeMethods.ExitWindowShutdown
                                    + PowerManagementNativeMethods.ExitWindowForceIfHung
                                    + PowerManagementNativeMethods.ExitWindowForce),
                                (PowerManagementNativeMethods.ShutdownReasonMajorApplication
                                    | PowerManagementNativeMethods.ShutdownReasonMinorMaintenance));
                    }

                    else
                    {
                        //Sends message to processes and it forces processes to terminate
                        //if they do not respond to the message within the timeout interval.
                        shutdownComputerStatus = PowerManagementNativeMethods.ExitWindowsEx(
                            PowerManagementNativeMethods.ExitWindowShutdown,
                            (PowerManagementNativeMethods.ShutdownReasonMajorApplication
                                | PowerManagementNativeMethods.ShutdownReasonMinorMaintenance));
                    }

                    hasPrivilege = true;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!hasPrivilege)
            {
                throw new PowerManagerException(PowerManagementResource.NoShutdownPrivilege);
            }
            else if (!shutdownComputerStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return shutdownComputerStatus;
        }

        /// <summary>
        /// Shuts down the system to a point at which it is safe to turn off 
        /// the power.
        /// </summary>
        /// <param name="shutdownMessage">The message to be displayed 
        /// in the shutdown dialog box.Maximum length is 511 characters.</param>
        /// <param name="secondsTimer">The length of time that the shutdown 
        /// dialog box should be displayed, in seconds.
        /// MAX Shutdown TimeOut == 10 Years in seconds (((10*365*24*60*60)-1 seconds).
        /// </param>
        /// <param name="force">If this parameter is TRUE, applications 
        /// with unsaved changes are to be forcibly closed.
        /// Note that this can result in data loss.
        /// </param>
        /// <returns>True, if shutdown has been initiated.</returns>
        /// <exception cref="ArgumentException">
        /// The specified parameter is not valid.
        /// </exception>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool ShutdownComputer(string shutdownMessage,
            UInt32 secondsTimer, bool force)
        {
            //Check for valid parameter values.
            if (!String.IsNullOrEmpty(shutdownMessage))
            {
                if (shutdownMessage.Length >=
                    PowerManagementNativeMethods.MaximumMessageLength)
                {
                    throw new ArgumentException(
                        PowerManagementResource.ShutdownMessageLength);
                }
            }

            if (secondsTimer >= PowerManagementNativeMethods.MaximumTimerValue)
            {
                throw new ArgumentException(
                    PowerManagementResource.MaximumShutdownTimerValue);
            }

            bool shutdownComputerStatus = false;
            bool hasPrivilege = false;
            try
            {
                //Determines whether the computer supports the soft off power 
                //state and set privileges for this application for rebooting pc.
                if (this.IsPowerShutdownAllowed
                    && PowerManager.SetPrivilege(PowerManagementNativeMethods.SEShutdownName))
                {
                    //Show specified message and shutdown PC after specified time.
                    shutdownComputerStatus =
                        (PowerManagementNativeMethods.InitiateSystemShutdown(
                            null, shutdownMessage, secondsTimer, force, false)
                        != false);

                    hasPrivilege = true;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!hasPrivilege)
            {
                throw new PowerManagerException(PowerManagementResource.NoShutdownPrivilege);
            }
            else if (!shutdownComputerStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return shutdownComputerStatus;
        }

        #endregion

        /// <summary>
        /// Shuts down the system and turns off the power. 
        /// The system must support the power-off feature.
        /// </summary>
        /// <param name="force">If True, then it forces processes to terminate.
        /// If false then sends message to processes and it forces processes 
        /// to terminate if they do not respond to the message within 
        /// the timeout interval.</param>
        /// <returns>True, if the shutdown has been initiated</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool PowerOffComputer(bool force)
        {
            bool powerOffComputerStatus = false;
            bool hasPrivilege = false;
            try
            {
                //Check whether the computer supports the soft off 
                //power state and set privileges for this application for rebooting pc.
                if (this.IsPowerShutdownAllowed
                    && PowerManager.SetPrivilege(
                        PowerManagementNativeMethods.SEShutdownName))
                {
                    if (force)
                    {
                        //Force all applications to close and poweroff the system. 
                        powerOffComputerStatus =
                            PowerManagementNativeMethods.ExitWindowsEx(
                                (PowerManagementNativeMethods.ExitWindowPowerOff
                                    + PowerManagementNativeMethods.ExitWindowForceIfHung
                                    + PowerManagementNativeMethods.ExitWindowForce),
                                (PowerManagementNativeMethods.ShutdownReasonMajorApplication |
                                    PowerManagementNativeMethods.ShutdownReasonMinorMaintenance));
                    }

                    else
                    {
                        //Sends message to processes and it forces processes to terminate
                        // if they do not respond to the message within the timeout interval.
                        powerOffComputerStatus =
                            PowerManagementNativeMethods.ExitWindowsEx(
                                PowerManagementNativeMethods.ExitWindowPowerOff,
                                (PowerManagementNativeMethods.ShutdownReasonMajorApplication |
                                    PowerManagementNativeMethods.ShutdownReasonMinorMaintenance));
                    }

                    hasPrivilege = true;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!hasPrivilege)
            {
                throw new PowerManagerException(PowerManagementResource.NoShutdownPrivilege);
            }
            else if (!powerOffComputerStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return powerOffComputerStatus;
        }

        /// <summary>
        /// Locks the workstation's display.
        /// This simulates the pressing of Windows+L key combination.
        /// </summary>
        /// <returns>True, if the Locking process has been initiated.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool LockWorkStation()
        {
            bool lockWorkStationStatus = false;
            try
            {
                //Lock workstation
                if (PowerManagementNativeMethods.LockWorkstation())
                {
                    lockWorkStationStatus = true;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!lockWorkStationStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return lockWorkStationStatus;
        }

        /// <summary>
        /// Hibernates the system.
        /// </summary>
        /// <param name="force">If this parameter is TRUE,the system suspends 
        /// operation immediately; 
        /// if it is FALSE, the system broadcasts an event to each application 
        /// to request permission to suspend operation.</param>
        /// <returns>True, if the function succeeds.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is raised if the computer does 
        /// not support hibernation (power state S4) and 
        /// the file Hiberfil.sys is not present on the system.
        /// </exception>
        public bool HibernateComputer(bool force)
        {
            bool hibernateComputerStatus = false;

            //Check whether the computer supports hibernation.
            if (this.IsPowerHibernateAllowed)
                throw new InvalidOperationException(PowerManagementResource.NoHibernationSupport);

            try
            {
                //Hibernates PC.
                hibernateComputerStatus
                    = PowerManagementNativeMethods.SetSuspendState(true, force, false);
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!hibernateComputerStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return hibernateComputerStatus;
        }

        /// <summary>
        /// Hibernates the system.
        /// </summary>
        /// <returns>On successful execution returns true and the 
        /// system broadcasts an event to each application to request
        /// permission to suspend operation.
        /// </returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// This exception is raised if the computer does 
        /// not support hibernation (power state S4) and 
        /// the file Hiberfil.sys is not present on the system.
        /// </exception>
        public bool HibernateComputer()
        {
            return this.HibernateComputer(false);
        }

        /// <summary>
        /// Puts the system into suspend (sleep) state.
        /// </summary>
        /// <param name="force">If this parameter is TRUE, the system suspends 
        /// operation immediately; if it is FALSE, the system broadcasts an 
        /// event to each application to request permission to suspend 
        /// operation.
        /// </param>
        /// <param name="disableWakeEvents">If this parameter is TRUE, 
        /// the system disables all wake events. If the parameter is FALSE, 
        /// any system wake events remain enabled.</param>
        /// <returns>True, if the function succeeds.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool StandbyComputer(bool force, bool disableWakeEvents)
        {
            bool standbyComputerStatus = false;
            try
            {
                ////Check whether the computer supports the sleep states.
                //if (this.IsPowerSuspendAllowed)
                //{
                    //Standby PC
                    standbyComputerStatus = PowerManagementNativeMethods.SetSuspendState(
                        false, force, disableWakeEvents);
                //}
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!standbyComputerStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return standbyComputerStatus;
        }

        /// <summary>
        /// Logs off the current user.
        /// </summary>
        /// <param name="force">If True, then it forces processes to terminate.
        /// If false then sends message to processes and it forces processes to 
        /// terminate if they do not respond to the message within the timeout 
        /// interval.</param>
        /// <returns>True, if log off has been initiated.</returns>
        /// <exception cref="PowerManagerException">
        /// Call to native API has raised an error.
        /// </exception>
        public bool LogOffCurrentUser(bool force)
        {
            bool logOffCurrentUserStatus = false;
            try
            {
                if (force)
                {
                    //Forces processes to terminate and Logoff current user.
                    logOffCurrentUserStatus = PowerManagementNativeMethods.ExitWindowsEx(
                        PowerManagementNativeMethods.ExitWindowForce, 0);
                }
                else
                {
                    //Sends message to processes and  forces processes to terminate
                    //if they do not respond to the message within the timeout interval 
                    //and logoff current user.
                    logOffCurrentUserStatus = PowerManagementNativeMethods.ExitWindowsEx(
                        PowerManagementNativeMethods.ExitWindowLogOff, 0);
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }

            if (!logOffCurrentUserStatus)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                    + Marshal.GetLastWin32Error());
            }

            return logOffCurrentUserStatus;
        }

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Retrieves the active power scheme and returns a GUID that 
        /// identifies the scheme.
        /// </summary>
        /// <returns>If the method succeeds then returns GUID of the 
        /// active power scheme else it returns null.</returns>
        private static string GetActivePowerSchemeGuid()
        {
            IntPtr ptrToActivePowerScheme
                = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));

            //Get guid of active power scheme
            if (!PowerManagementNativeMethods.PowerGetActiveScheme(
                IntPtr.Zero, out ptrToActivePowerScheme))
            {
                Guid guidActiveScheme
                    = (Guid)Marshal.PtrToStructure(ptrToActivePowerScheme, typeof(Guid));

                Marshal.FreeHGlobal(ptrToActivePowerScheme);
                return guidActiveScheme.ToString();
            }
            Marshal.FreeHGlobal(ptrToActivePowerScheme);
            return null;
        }

        /// <summary>
        /// Sets the active power scheme for the current user.
        /// </summary>
        /// <param name="guid">Guid of the power scheme to activate.</param>
        /// <returns>True, if the method succeeds.</returns>
        private static bool SetActivePowerSchemeGuid(string guid)
        {
            IntPtr ptrToActiveScheme = IntPtr.Zero;
            try
            {
                Guid activeSchemeGuid = new Guid(guid);

                ptrToActiveScheme
                    = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));

                Marshal.StructureToPtr(activeSchemeGuid, ptrToActiveScheme, true);

                //Set active power scheme
                if (!PowerManagementNativeMethods.PowerSetActiveScheme(
                    IntPtr.Zero, ptrToActiveScheme))
                {
                    return true;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(ptrToActiveScheme);
            }
            return false;
        }

        /// <summary>
        /// Sets the application privileges.
        /// </summary>
        /// <param name="privilege">Privilege to be given to the current application.</param>
        /// <returns>True, if the method succeeds.</returns>
        private static bool SetPrivilege(string privilege)
        {
            bool success;
            TokenPrivileges tokenPrivileges;
            IntPtr tokenHandle = IntPtr.Zero;

            //Get handle to current process.
            IntPtr currentProcessHandle =
                PowerManagementNativeMethods.GetCurrentProcess();

            // Get a token for this process. 
            success = PowerManagementNativeMethods.OpenProcessToken(
                currentProcessHandle,
                (PowerManagementNativeMethods.TokenAdjustPrivileges
                    | PowerManagementNativeMethods.TokenQuery), ref tokenHandle);

            //Set properties of token.
            tokenPrivileges.Count = 1;
            tokenPrivileges.Luid = 0;

            tokenPrivileges.Attribute = PowerManagementNativeMethods.SEPrivilegeEnabled;

            //Get the LUID for specified privilege name.
            success = PowerManagementNativeMethods.LookupPrivilegeValue(
                null, privilege, ref tokenPrivileges.Luid);

            //Enable specified privilege in the specified access token.
            success = PowerManagementNativeMethods.AdjustTokenPrivileges(
                tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero);

            return success;
        }

        /// <summary>
        /// Retrieves the title and guid of the currently present 
        /// power schemes.
        /// </summary>
        /// <returns>ArrayList containing the title and Guid of the 
        /// power schemes.
        /// </returns>
        private static ArrayList GetAvailablePowerSchemesAndGuid()
        {
            ArrayList allSchemesAndGuid = new ArrayList();
            IntPtr ptrToPowerScheme = IntPtr.Zero;
            IntPtr friendlyName = IntPtr.Zero;
            try
            {
                uint buffSize = 100;
                uint schemeIndex = 0;

                ptrToPowerScheme =
                    Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));

                //Get the guids of all available power schemes.
                while (PowerManagementNativeMethods.PowerEnumerate(
                            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                            PowerManagementNativeMethods.PowerDataAccessor.AccessScheme,
                            schemeIndex, ptrToPowerScheme, ref buffSize) == 0)
                {
                    friendlyName = Marshal.AllocHGlobal(1000);

                    Guid schemeGuid = (Guid)Marshal.PtrToStructure(
                        ptrToPowerScheme, typeof(Guid));

                    buffSize = 1000;

                    //Pass the guid retrieved in PowerEnumerate as parameter 
                    //to get the power scheme name.
                    PowerManagementNativeMethods.PowerReadFriendlyName(
                        IntPtr.Zero, ptrToPowerScheme, IntPtr.Zero, IntPtr.Zero,
                        friendlyName, ref buffSize);

                    string schemeName = Marshal.PtrToStringUni(friendlyName);

                    allSchemesAndGuid.Add(new ArrayList());

                    //Add retrieved power scheme name in the arraylist
                    ((ArrayList)allSchemesAndGuid[(int)schemeIndex]).Add(schemeGuid);

                    //Add retrieved power scheme Guid in the arraylist
                    ((ArrayList)allSchemesAndGuid[(int)schemeIndex]).Add(schemeName);

                    schemeIndex++;
                }
            }

            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }
            finally
            {
                Marshal.FreeHGlobal(ptrToPowerScheme);
                Marshal.FreeHGlobal(friendlyName);
            }
            return allSchemesAndGuid;
        }

        #region AC/DC methods

        /// <summary>
        /// This method retrieves the AC/DC power setting values for the 
        /// specified power scheme.
        /// </summary>
        /// <param name="powerSchemeTitle">Name of the power scheme.</param>
        /// <param name="type">Type of the settings to be retrieved 
        /// (Possible values are AC and DC).</param>
        /// <returns>Power setting values.</returns>
        private static PowerScheme ReadPowerSchemeValue(string powerSchemeTitle, ValueType type)
        {
            if (String.IsNullOrEmpty(powerSchemeTitle))
            {
                throw new ArgumentNullException("powerSchemeTitle");
            }

            ArrayList allSchemes = PowerManager.GetAvailablePowerSchemesAndGuid();
            bool flagFound = false;
            int index = 0;
            //Check whether specified power scheme is present
            for (index = 0; index < allSchemes.Count; index++)
            {
                if (((ArrayList)allSchemes[index])[1].ToString() == powerSchemeTitle)
                {
                    flagFound = true;
                    break;
                }
            }
            if (flagFound == false)
            {
                //Throw exception if specified power scheme does not exist
                throw new ArgumentException(PowerManagementResource.PowerSchemeNotFound);
            }

            PowerScheme powerScheme = new PowerScheme();
            powerScheme.Name = ((ArrayList)allSchemes[index])[1].ToString();
            powerScheme.Guid = ((ArrayList)allSchemes[index])[0].ToString();
            powerScheme.Description = GetDescription(powerScheme.Guid, null, null);

            //Get Sub Group details
            powerScheme.SubGroups.AddRange(PowerManager.GetSubGroups(powerScheme.Guid));

            for (int i = 0; i < powerScheme.SubGroups.Count; i++)
            {
                powerScheme.SubGroups[i].PowerSettings.AddRange(
                    PowerManager.GetPowerSettings(powerScheme.Guid, powerScheme.SubGroups[i].Guid, type));
            }
            return powerScheme;
        }

        /// <summary>
        /// Gets the information of power settings available under specifed sub group.
        /// </summary>
        /// <param name="powerSchemeGuid">Guid of the power scheme.</param>
        /// <param name="subGroupGuid">Guid of the setting sub group.</param>
        /// <param name="type">Type of the value to be retrieved (Possible values AC or DC).</param>
        /// <returns></returns>
        private static List<PowerSetting> GetPowerSettings(string powerSchemeGuid, string subGroupGuid, ValueType type)
        {
            if (String.IsNullOrEmpty(powerSchemeGuid) || String.IsNullOrEmpty(subGroupGuid))
                throw new ArgumentNullException(
                    String.Format(CultureInfo.InvariantCulture, PowerManagementResource.ErrorSpecifyBothArguments,
                            "powerSchemeGuid", "subGroupGuid"));

            if (!PowerManager.ValidateGuid(powerSchemeGuid))
                throw new ArgumentException(
                    String.Format(CultureInfo.InvariantCulture, PowerManagementResource.ErrorInvalidGuid,
                            "powerSchemeGuid"));

            if (!PowerManager.ValidateGuid(subGroupGuid))
                throw new ArgumentException(
                    String.Format(CultureInfo.InvariantCulture, PowerManagementResource.ErrorInvalidGuid,
                            "subGroupGuid"));

            IntPtr friendlyName = IntPtr.Zero;
            IntPtr settingGuidPtr = IntPtr.Zero;
            IntPtr subGroupGuidPtr = IntPtr.Zero;
            IntPtr powerSchemeGuidPtr = IntPtr.Zero;

            try
            {
                List<PowerSetting> powerSettings = new List<PowerSetting>();
                friendlyName = Marshal.AllocHGlobal(1000);
                settingGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                subGroupGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                powerSchemeGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));

                uint bufferSize = 1000;

                Guid guid = new Guid(subGroupGuid);
                Marshal.StructureToPtr(guid, subGroupGuidPtr, true);

                guid = new Guid(powerSchemeGuid);
                Marshal.StructureToPtr(guid, powerSchemeGuidPtr, true);

                uint schemeIndex = 0;
                //Enumerate settings
                while (PowerManagementNativeMethods.PowerEnumerate(
                            IntPtr.Zero, powerSchemeGuidPtr, subGroupGuidPtr,
                            PowerManagementNativeMethods.PowerDataAccessor.AccessIndividualSetting,
                            schemeIndex, settingGuidPtr, ref bufferSize) == 0)
                {

                    Guid settingGuid = (Guid)Marshal.PtrToStructure(settingGuidPtr, typeof(Guid));

                    bufferSize = 1000;

                    //Get the power setting name.
                    PowerManagementNativeMethods.PowerReadFriendlyName(
                        IntPtr.Zero, powerSchemeGuidPtr, subGroupGuidPtr, settingGuidPtr,
                        friendlyName, ref bufferSize);

                    string settingName = Marshal.PtrToStringUni(friendlyName);

                    PowerSetting setting = new PowerSetting();
                    setting.Name = settingName;
                    setting.Guid = settingGuid.ToString();
                    setting.Description = GetDescription(powerSchemeGuid, subGroupGuid, setting.Guid);
                    setting.Value = GetValue(powerSchemeGuid, subGroupGuid, setting.Guid, type);
                    powerSettings.Add(setting);
                    schemeIndex++;
                }
                return powerSettings;
            }
            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }
            finally
            {
                Marshal.FreeHGlobal(friendlyName);
                Marshal.FreeHGlobal(powerSchemeGuidPtr);
                Marshal.FreeHGlobal(subGroupGuidPtr);
                Marshal.FreeHGlobal(settingGuidPtr);
            }
        }

        /// <summary>
        /// Validate a Guid.
        /// </summary>
        /// <param name="guid">Guid to be validated.</param>
        /// <returns>True, if guid is value else false.</returns>
        private static bool ValidateGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid))
                return false;

            Regex isGuid = new Regex(PowerManagementResource.RegularExpressionGuid,
                                RegexOptions.Compiled);

            if (guid.StartsWith("{") != guid.EndsWith("}"))
                return false;

            if (isGuid.IsMatch(guid))
                return true;

            return false;
        }

        /// <summary>
        /// Gets the information of sub groups available under specifed power scheme.
        /// </summary>
        /// <param name="powerSchemeGuid">Guid of the power scheme.</param>
        /// <returns>List of sub groups.</returns>
        private static List<SettingSubGroup> GetSubGroups(string powerSchemeGuid)
        {
            if (String.IsNullOrEmpty(powerSchemeGuid))
                throw new ArgumentNullException("powerSchemeGuid");

            if (!PowerManager.ValidateGuid(powerSchemeGuid))
                throw new ArgumentException(
                    String.Format(CultureInfo.InvariantCulture, PowerManagementResource.ErrorInvalidGuid,
                            "powerSchemeGuid"));

            IntPtr subGroupGuidPtr = IntPtr.Zero;
            IntPtr powerSchemeGuidPtr = IntPtr.Zero;
            IntPtr friendlyName = IntPtr.Zero;

            try
            {
                List<SettingSubGroup> subGroups = new List<SettingSubGroup>();
                string[] subGroupGuids = { 
                                PowerManagementResource.SubgroupBatteryGuid,
                                PowerManagementResource.SubgroupDiskGuid,
                                PowerManagementResource.SubgroupDisplayGuid,
                                PowerManagementResource.SubgroupMultimediaGuid,
                                PowerManagementResource.SubgroupPciExpressGuid,
                                PowerManagementResource.SubgroupPowerButtonGuid,
                                PowerManagementResource.SubgroupProcessorGuid,
                                PowerManagementResource.SubgroupSearchIndexingGuid,
                                PowerManagementResource.SubgroupSleepGuid,
                                PowerManagementResource.SubgroupUsbGuid,
                                PowerManagementResource.SubgroupWirelessAdapterGuid,
                            };

                subGroupGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                powerSchemeGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                friendlyName = Marshal.AllocHGlobal(1000);

                Guid g = new Guid(powerSchemeGuid);
                Marshal.StructureToPtr(g, powerSchemeGuidPtr, true);

                //Enumerate sub groups
                foreach (string guid in subGroupGuids)
                {
                    Guid subGroupGuid = new Guid(guid);
                    Marshal.StructureToPtr(subGroupGuid, subGroupGuidPtr, true);

                    uint buffSize = 1000;

                    //Get the setting sub group name.
                    PowerManagementNativeMethods.PowerReadFriendlyName(
                        IntPtr.Zero, powerSchemeGuidPtr, subGroupGuidPtr, IntPtr.Zero,
                        friendlyName, ref buffSize);

                    string subGroupName = Marshal.PtrToStringUni(friendlyName);

                    SettingSubGroup subGroup = new SettingSubGroup();
                    subGroup.Name = subGroupName;
                    subGroup.Guid = subGroupGuid.ToString();
                    subGroup.Description = GetDescription(powerSchemeGuid, subGroup.Guid, null);
                    subGroups.Add(subGroup);
                }
                return subGroups;
            }
            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }
            finally
            {
                Marshal.FreeHGlobal(powerSchemeGuidPtr);
                Marshal.FreeHGlobal(subGroupGuidPtr);
                Marshal.FreeHGlobal(friendlyName);
            }
        }

        /// <summary>
        /// Gets the value of the specified power setting.
        /// </summary>
        /// <param name="powerSchemeGuid">Guid of the power scheme.</param>
        /// <param name="subGroupGuid">Guid of the setting sub group.</param>
        /// <param name="settingGuid">Guid of the power setting.</param>
        /// <param name="valueType">Type of the value to be retrieved (Possible values AC or DC).</param>
        /// <returns>Value of the specified power setting.</returns>
        private static string GetValue(string powerSchemeGuid, string subGroupGuid, string settingGuid, ValueType valueType)
        {
            if (String.IsNullOrEmpty(powerSchemeGuid))
                throw new ArgumentNullException("powerSchemeGuid");

            if (String.IsNullOrEmpty(powerSchemeGuid))
                throw new ArgumentNullException("subGroupGuid");

            if (String.IsNullOrEmpty(powerSchemeGuid))
                throw new ArgumentNullException("settingGuid");

            if (!PowerManager.ValidateGuid(powerSchemeGuid))
                throw new ArgumentException(
                    String.Format(CultureInfo.InvariantCulture, PowerManagementResource.ErrorInvalidGuid,
                            "powerSchemeGuid"));

            if (!PowerManager.ValidateGuid(subGroupGuid))
                throw new ArgumentException(
                    String.Format(CultureInfo.InvariantCulture, PowerManagementResource.ErrorInvalidGuid,
                            "subGroupGuid"));

            if (!PowerManager.ValidateGuid(settingGuid))
                throw new ArgumentException(
                    String.Format(CultureInfo.InvariantCulture, PowerManagementResource.ErrorInvalidGuid,
                            "settingGuid"));

            string value = String.Empty;
            uint returnCode = 0;
            uint indexValue = 0;
            IntPtr powerSchemeGuidPtr = IntPtr.Zero;
            IntPtr subGroupGuidPtr = IntPtr.Zero;
            IntPtr settingGuidPtr = IntPtr.Zero;
            try
            {
                powerSchemeGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                subGroupGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                settingGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));

                Guid guid = new Guid(powerSchemeGuid);
                Marshal.StructureToPtr(guid, powerSchemeGuidPtr, true);

                guid = new Guid(subGroupGuid);
                Marshal.StructureToPtr(guid, subGroupGuidPtr, true);

                guid = new Guid(settingGuid);
                Marshal.StructureToPtr(guid, settingGuidPtr, true);

                if (valueType == ValueType.AC)
                {
                    returnCode = PowerManagementNativeMethods.PowerReadACValueIndex(
                        IntPtr.Zero, powerSchemeGuidPtr, subGroupGuidPtr, settingGuidPtr, ref indexValue);
                }
                else
                {
                    returnCode = PowerManagementNativeMethods.PowerReadDCValueIndex(
                        IntPtr.Zero, powerSchemeGuidPtr, subGroupGuidPtr, settingGuidPtr, ref indexValue);
                }
            }
            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }
            finally
            {
                Marshal.FreeHGlobal(powerSchemeGuidPtr);
                Marshal.FreeHGlobal(subGroupGuidPtr);
                Marshal.FreeHGlobal(settingGuidPtr);
            }
            if (returnCode != 0)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                + returnCode.ToString(CultureInfo.InvariantCulture));
            }
            value = PowerManager.GetValueString(settingGuid, indexValue);
            return value;
        }

        /// <summary>
        /// Converts the specified index value to its equivalent readable form.
        /// </summary>
        /// <param name="settingGuid">Guid of the power setting.</param>
        /// <param name="value">Index value of the setting.</param>
        /// <returns>Readable value in string format.</returns>
        private static string GetValueString(string settingGuid, uint value)
        {
            string valueString = String.Empty;
            string[] pChoices = {   PowerManagementResource.PowerSchemePowerSaverTitle, 
                                    PowerManagementResource.ChoiceAutomatic,
                                    PowerManagementResource.PowerSchemeHighPerformanceTitle };

            string[] searchChoices = {  PowerManagementResource.PowerSchemePowerSaverTitle, 
                                        PowerManagementResource.PowerSchemeBalancedTitle, 
                                        PowerManagementResource.PowerSchemeHighPerformanceTitle };

            string[] mmChoices = {  PowerManagementResource.ChoiceDoNothing,
                                    PowerManagementResource.ChoicePreventIdle, 
                                    PowerManagementResource.ChoiceUseAwayMode };

            string[] pciChoices = { PowerManagementResource.ChoiceNone, 
                                    PowerManagementResource.ChoiceModeratePowerSaving, 
                                    PowerManagementResource.ChoiceMaximumPowerSaving };

            string[] waChoices = {  PowerManagementResource.ChoiceMaximumPerformance,
                                    PowerManagementResource.ChoiceLowPowerSaving,
                                    PowerManagementResource.ChoiceMediumPowerSaving,
                                    PowerManagementResource.ChoiceMaximumPowerSaving };
            switch (settingGuid)
            {
                case PowerManagementNativeMethods.BatteryCriticalActionGuid:
                case PowerManagementNativeMethods.BatteryLowActionGuid:
                case PowerManagementNativeMethods.PowerButtonLidCloseActionGuid:
                case PowerManagementNativeMethods.PowerButtonActionGuid:
                case PowerManagementNativeMethods.PowerButtonSleepActionGuid:
                    valueString = ((BatteryAction)value).ToString();
                    break;

                case PowerManagementNativeMethods.PowerButtonStartMenuActionGuid:
                    valueString = ((BatteryAction)(value + 1)).ToString();
                    break;

                case PowerManagementNativeMethods.BatteryLowLevelGuid:
                case PowerManagementNativeMethods.BatteryCriticalLevelGuid:
                case PowerManagementNativeMethods.DisplayBrightnessGuid:
                case PowerManagementNativeMethods.ProcessorMinimumStateGuid:
                case PowerManagementNativeMethods.ProcessorMaximumStateGuid:
                case PowerManagementNativeMethods.SleepRequiredIdlenessGuid:
                    valueString = value.ToString(CultureInfo.InvariantCulture)
                        + PowerManagementResource.PostfixPercentage;
                    break;

                case PowerManagementNativeMethods.BatteryLowNotificationGuid:
                case PowerManagementNativeMethods.DisplayAdaptiveGuid:
                case PowerManagementNativeMethods.SleepAllowAwayGuid:
                case PowerManagementNativeMethods.SleepAllowHybridSleepGuid:
                case PowerManagementNativeMethods.SleepAllowStandByGuid:
                case PowerManagementNativeMethods.SleepAllowPreventGuid:
                case PowerManagementNativeMethods.UsbSelectiveSuspendGuid:
                case PowerManagementNativeMethods.SleepAutoWakeGuid:
                    if (value == 1)
                        valueString = PowerManagementResource.ChoiceEnabled;
                    else
                        valueString = PowerManagementResource.ChoiceDisabled;
                    break;

                case PowerManagementNativeMethods.HardDiskOffAfterGuid:
                case PowerManagementNativeMethods.DisplayOffAfterGuid:
                case PowerManagementNativeMethods.SleepAfterGuid:
                case PowerManagementNativeMethods.SleepHibernateAfter:
                    if (value == 0)
                        valueString = PowerManagementResource.ChoiceNever;
                    else
                        valueString = value.ToString(CultureInfo.InvariantCulture)
                            + PowerManagementResource.PostfixSeconds;
                    break;

                case PowerManagementNativeMethods.MMWhenSharingGuid:
                    if (value < mmChoices.Length)
                        valueString = mmChoices[value];
                    break;
                case PowerManagementNativeMethods.PciExpressPowerManagementGuid:
                    if (value < pciChoices.Length)
                        valueString = pciChoices[value];
                    break;

                case PowerManagementNativeMethods.ProcessorCStateSettingGuid:
                    if (value / 2 < pChoices.Length)
                        valueString = pChoices[value / 2];
                    break;

                case PowerManagementNativeMethods.ProcessorPerfStateSettingGuid:
                    if (value / 2 < pChoices.Length)
                        valueString = pChoices[value / 2];
                    if (value % 2 == 0)
                        valueString += PowerManagementResource.PostfixAcSpecific;
                    else
                        valueString += PowerManagementResource.PostfixDcSpecific;
                    break;

                case PowerManagementNativeMethods.SearchPowerSavingModeGuid:
                    if (value < searchChoices.Length)
                        valueString = searchChoices[value];
                    break;

                case PowerManagementNativeMethods.WirelessAdapterPowerModeGuid:
                    if (value < waChoices.Length)
                        valueString = waChoices[value];
                    break;

                default: valueString = value.ToString(CultureInfo.InvariantCulture);
                    break;
            }
            return valueString;
        }

        /// <summary>
        /// Gets the description of the specifed power scheme , setting sub group or setting.
        /// </summary>
        /// <param name="schemeGuid">Guid of the power scheme.</param>
        /// <param name="subGroupGuid">Guid of the setting sub group.</param>
        /// <param name="settingGuid">Guid of the power setting.</param>
        /// <returns>Description of the specified power scheme , setting sub group or setting.</returns>
        private static string GetDescription(string schemeGuid, string subGroupGuid, string settingGuid)
        {
            uint returnCode = 0;
            uint bufferSize = 1000;

            IntPtr description = IntPtr.Zero;
            IntPtr powerSchemeGuidPtr = IntPtr.Zero;
            IntPtr subGroupGuidPtr = IntPtr.Zero;
            IntPtr settingGuidPtr = IntPtr.Zero;

            try
            {
                if (String.IsNullOrEmpty(schemeGuid))
                    return null;

                Guid guid = new Guid();

                if (!String.IsNullOrEmpty(schemeGuid))
                {
                    powerSchemeGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                    guid = new Guid(schemeGuid);
                    Marshal.StructureToPtr(guid, powerSchemeGuidPtr, true);

                    if (!String.IsNullOrEmpty(subGroupGuid))
                    {
                        guid = new Guid(subGroupGuid);
                        subGroupGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                        Marshal.StructureToPtr(guid, subGroupGuidPtr, true);
                        if (!String.IsNullOrEmpty(settingGuid))
                        {
                            guid = new Guid(settingGuid);
                            settingGuidPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                            Marshal.StructureToPtr(guid, settingGuidPtr, true);
                        }
                    }
                }

                description = Marshal.AllocHGlobal(1000);

                returnCode = PowerManagementNativeMethods.PowerReadDescription(
                    IntPtr.Zero, powerSchemeGuidPtr, subGroupGuidPtr, settingGuidPtr,
                        description, ref bufferSize);

                if (returnCode == 0)
                {
                    if (bufferSize > 0)
                        return Marshal.PtrToStringUni(description);
                    else
                        return String.Empty;
                }
            }
            catch (Exception exception)
            {
                throw new PowerManagerException(exception.Message, exception);
            }
            finally
            {
                Marshal.FreeHGlobal(description);
                Marshal.FreeHGlobal(powerSchemeGuidPtr);
                Marshal.FreeHGlobal(subGroupGuidPtr);
                Marshal.FreeHGlobal(settingGuidPtr);
            }
            if (returnCode != 0)
            {
                throw new PowerManagerException(PowerManagementResource.Win32ErrorCodeMessage
                + returnCode.ToString(CultureInfo.InvariantCulture));
            }
            return String.Empty;
        }
        #endregion

        #endregion

        #region Private Enumerations
        /// <summary>
        /// Types of power setting values.
        /// </summary>
        private enum ValueType
        {
            /// <summary>
            /// Value for AC.
            /// </summary>
            AC,
            /// <summary>
            /// Value of DC.
            /// </summary>
            DC
        }

        /// <summary>
        /// Represents actions for battery power settings of power schemes.
        /// </summary>
        private enum BatteryAction
        {
            /// <summary>
            /// Do nothing.
            /// </summary>
            DoNothing = 0,
            /// <summary>
            /// Suspend the system.
            /// </summary>
            Sleep = 1,
            /// <summary>
            /// Hibernate the system.
            /// </summary>
            Hibernate = 2,
            /// <summary>
            /// Shut down the system.
            /// </summary>
            Shutdown = 3,
        }

        #endregion
    }
    #endregion

    /// <summary>
    /// Represents the power setting values of a power scheme.
    /// </summary>
    public class PowerScheme
    {
        #region Private Members
        /// <summary>
        /// Friendly name of the power scheme.
        /// </summary>
        private string name;
        /// <summary>
        /// Description of the power scheme.
        /// </summary>
        private string description;
        /// <summary>
        /// Guid of the power scheme.
        /// </summary>
        private string guid;
        /// <summary>
        /// List of sub groups of power settings within the power scheme.
        /// </summary>
        private List<SettingSubGroup> subGroups = new List<SettingSubGroup>();
        #endregion

        #region Public Properties
        /// <summary>
        /// Friendly name of the power scheme.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
        /// <summary>
        /// Description of the power scheme.
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }
        /// <summary>
        /// Guid of the power scheme.
        /// </summary>
        public string Guid
        {
            get { return this.guid; }
            set { this.guid = value; }
        }
        /// <summary>
        /// List of sub groups of power settings within the power scheme.
        /// </summary>
        public List<SettingSubGroup> SubGroups
        {
            get { return this.subGroups; }
        }
        #endregion
    }

    /// <summary>
    /// Represents a power setting sub group of a power scheme.
    /// </summary>
    public class SettingSubGroup
    {
        #region Private Members
        /// <summary>
        /// Friendly name of the sub group.
        /// </summary>
        private string name;
        /// <summary>
        /// Description of the setting sub group.
        /// </summary>
        private string description;
        /// <summary>
        /// Guid of the setting sub group.
        /// </summary>
        private string guid;
        /// <summary>
        /// List of power settings within the sub group.
        /// </summary>
        private List<PowerSetting> powerSettings = new List<PowerSetting>();
        #endregion

        #region Public Properties
        /// <summary>
        /// Friendly name of the sub group.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
        /// <summary>
        /// Description of the setting sub group.
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }
        /// <summary>
        /// Guid of the setting sub group.
        /// </summary>
        public string Guid
        {
            get { return this.guid; }
            set { this.guid = value; }
        }
        /// <summary>
        /// List of power settings within the sub group.
        /// </summary>
        public List<PowerSetting> PowerSettings
        {
            get { return this.powerSettings; }
        }
        #endregion
    }

    /// <summary>
    /// Represents a power setting of a power scheme.
    /// </summary>
    public class PowerSetting
    {
        #region Private Members
        /// <summary>
        /// Friendly name of the power setting.
        /// </summary>
        private string name;
        /// <summary>
        /// Description of the power setting.
        /// </summary>
        private string description;
        /// <summary>
        /// Guid of the power setting.
        /// </summary>
        private string guid;
        /// <summary>
        /// Value of the power setting.
        /// </summary>
        private string value;
        #endregion

        #region Public Properties
        /// <summary>
        /// Friendly name of the power setting.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.name = value; }
        }
        /// <summary>
        /// Description of the power setting.
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }
        /// <summary>
        /// Guid of the power setting.
        /// </summary>
        public string Guid
        {
            get { return this.guid; }
            set { this.guid = value; }
        }
        /// <summary>
        /// Value of the power setting.
        /// </summary>
        public string Value
        {
            get { return this.value; }
            set { this.value = value; }
        }
        #endregion
    }
}
