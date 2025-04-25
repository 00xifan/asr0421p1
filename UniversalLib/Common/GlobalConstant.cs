// Copyright (c) 2025-present Lenovo.  All rights reserverd
// Confidential and Restricted
using Microsoft.Win32;
using System;
using System.Management;
namespace UniversalLib.Common
{
    public class GlobalConstant
    {
        public enum MachineType
        {
            None,
            DualDisplayDevice,
            SingleDisplayDevice,
        }

        /// <summary>
        /// Is the current machine a single screen device or a YB9 dual screen device
        /// </summary>
        public MachineType CurrentMachineType = MachineType.None;


        private static readonly Lazy<GlobalConstant> _instanceLock = new Lazy<GlobalConstant>(() => new GlobalConstant());
        public static GlobalConstant Instance
        {
            get
            {
                return _instanceLock.Value;
            }
        }
        public GlobalConstant()
        {

        }


        #region get machine type

        private const string YB9_14IAH10_3D = "83KJ";
        private const string YB9_14IMU9_2D = "83FF";
        private const string YB9_14IRU8_1D = "82YQ";

        private const string ScreenB1Or2DId = "LEN8390";
        private const string ScreenC1Or2DId = "LEN8391";

        private const string ScreenB3DId = "LEN8A11";
        private const string ScreenC3DId = "LEN8A12";

        public string CurrentScreenBId = string.Empty;
        public string CurrentScreenCId = string.Empty;


        public const string DualDisplayBTouchID = @"\\?\HID#VID_17EF&PID_6161&MI_03&Col01";
        public const string DualDisplayBStylusID = @"\\?\HID#VID_17EF&PID_6161&MI_03&Col03";

        public const string DualDisplayCTouchID = @"\\?\HID#VID_17EF&PID_6161&MI_03&Col02";
        public const string DualDisplayCStylusID = @"\\?\HID#VID_17EF&PID_6161&MI_03&Col04";

        public static string SingleDisplayTouchID = string.Empty;
        public static string SingleDisplayStylusID = string.Empty;

        public void SystemMachineType()
        {
            if (CurrentMachineType != MachineType.None)
            {
                return;
            }
            string subKeyPath = @"HARDWARE\DESCRIPTION\System\BIOS";
            string mode = string.Empty;
            using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(subKeyPath))
            {
                if (registryKey != null)
                {
                    // 读取 SystemProductName 的值
                    object productName = registryKey.GetValue("SystemProductName");

                    if (productName != null)
                    {
                        mode = productName.ToString();
                    }
                }
            }
            switch (mode)
            {
                case YB9_14IAH10_3D:
                    CurrentScreenBId = ScreenB3DId;
                    CurrentScreenCId = ScreenC3DId;
                    CurrentMachineType = MachineType.DualDisplayDevice;
                    break;
                case YB9_14IMU9_2D:
                    CurrentScreenBId = ScreenB1Or2DId;
                    CurrentScreenCId = ScreenC1Or2DId;
                    CurrentMachineType = MachineType.DualDisplayDevice;
                    break;
                case YB9_14IRU8_1D:
                    CurrentScreenBId = ScreenB1Or2DId;
                    CurrentScreenCId = ScreenC1Or2DId;
                    CurrentMachineType = MachineType.DualDisplayDevice;
                    break;
                default:
                    CurrentMachineType = MachineType.SingleDisplayDevice;
                    break;
            }
        }

        private string GetWmiProperty(string wmiProperty, string wmiClass)
        {
            string result = string.Empty;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT {wmiProperty} FROM {wmiClass}");
            foreach (ManagementObject obj in searcher.Get())
            {
                result = obj[wmiProperty]?.ToString();
            }
            return result;
        }
        #endregion

    }
}
