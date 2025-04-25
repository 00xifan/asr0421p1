// Copyright (c) 2025-present Lenovo.  All rights reserverd
// Confidential and Restricted
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
namespace UniversalLib.Core.Stylus
{
    public static class User32
    {
        public struct RawInputDeviceHandle : IEquatable<RawInputDeviceHandle>
        {
            readonly IntPtr value;

            public static RawInputDeviceHandle Zero => (RawInputDeviceHandle)IntPtr.Zero;

            RawInputDeviceHandle(IntPtr value) => this.value = value;

            public static IntPtr GetRawValue(RawInputDeviceHandle handle) => handle.value;

            public static explicit operator RawInputDeviceHandle(IntPtr value) => new RawInputDeviceHandle(value);

            public static bool operator ==(RawInputDeviceHandle a, RawInputDeviceHandle b) => a.Equals(b);

            public static bool operator !=(RawInputDeviceHandle a, RawInputDeviceHandle b) => !a.Equals(b);

            public bool Equals(RawInputDeviceHandle other) => value.Equals(other.value);

            public override bool Equals(object obj) =>
                obj is RawInputDeviceHandle other &&
                Equals(other);

            public override int GetHashCode() => value.GetHashCode();

            public override string ToString() => value.ToString();
        }

        public enum RawInputDeviceType
        {
            Mouse,
            Keyboard,
            Hid,
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RawInputHeader
        {
            readonly RawInputDeviceType dwType;
            readonly int dwSize;
            readonly RawInputDeviceHandle hDevice;
            readonly IntPtr wParam;

            public RawInputDeviceType Type => dwType;
            public int Size => dwSize;
            public RawInputDeviceHandle DeviceHandle => hDevice;
            public IntPtr WParam => wParam;

            public override string ToString() =>
                $"{{{Type}: {DeviceHandle}, WParam: {WParam}}}";
        }


        public struct RawInputHandle : IEquatable<RawInputHandle>
        {
            readonly IntPtr value;

            public static RawInputHandle Zero => (RawInputHandle)IntPtr.Zero;

            RawInputHandle(IntPtr value) => this.value = value;

            public static IntPtr GetRawValue(RawInputHandle handle) => handle.value;

            public static explicit operator RawInputHandle(IntPtr value) => new RawInputHandle(value);

            public static bool operator ==(RawInputHandle a, RawInputHandle b) => a.Equals(b);

            public static bool operator !=(RawInputHandle a, RawInputHandle b) => !a.Equals(b);

            public bool Equals(RawInputHandle other) => value.Equals(other.value);

            public override bool Equals(object obj) =>
                obj is RawInputHandle other &&
                Equals(other);

            public override int GetHashCode() => value.GetHashCode();

            public override string ToString() => value.ToString();
        }

        public enum RawInputGetBehavior : uint
        {
            Input = 0x10000003,
            Header = 0x10000005,
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct RID_DEVICE_INFO
        {
            [FieldOffset(0)]
            public int cbSize;
            [FieldOffset(4)]
            public int dwType;
            [FieldOffset(8)]
            public RID_DEVICE_INFO_MOUSE mouse;
            [FieldOffset(8)]
            public RID_DEVICE_INFO_KEYBOARD keyboard;
            [FieldOffset(8)]
            public RID_DEVICE_INFO_HID hid;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_MOUSE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwId;
            [MarshalAs(UnmanagedType.U4)]
            public int dwNumberOfButtons;
            [MarshalAs(UnmanagedType.U4)]
            public int dwSampleRate;
            [MarshalAs(UnmanagedType.U4)]
            public int fHasHorizontalWheel;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_KEYBOARD
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwType;
            [MarshalAs(UnmanagedType.U4)]
            public int dwSubType;
            [MarshalAs(UnmanagedType.U4)]
            public int dwKeyboardMode;
            [MarshalAs(UnmanagedType.U4)]
            public int dwNumberOfFunctionKeys;
            [MarshalAs(UnmanagedType.U4)]
            public int dwNumberOfIndicators;
            [MarshalAs(UnmanagedType.U4)]
            public int dwNumberOfKeysTotal;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_HID
        {
            [MarshalAs(UnmanagedType.U4)]
            public int dwVendorId;
            [MarshalAs(UnmanagedType.U4)]
            public int dwProductId;
            [MarshalAs(UnmanagedType.U4)]
            public int dwVersionNumber;
            [MarshalAs(UnmanagedType.U2)]
            public ushort usUsagePage;
            [MarshalAs(UnmanagedType.U2)]
            public ushort usUsage;
        }


        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(IntPtr hRawInput, RawInputGetBehavior uiCommand, [Out] byte[] pData, ref uint pcbSize, uint cbSizeHeader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(
    IntPtr hRawInput,
    RawInputGetBehavior uiCommand,
    out RawInputHeader pData,
    ref uint pcbSize,
    uint cbSizeHeader);

        public static uint GetRawInputDataSize(RawInputHandle rawInput)
        {
            var hRawInput = RawInputHandle.GetRawValue(rawInput);
            var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
            uint size = 0;

            GetRawInputData(hRawInput, RawInputGetBehavior.Input, null, ref size, headerSize);

            return size;
        }
        public static RawHid GetRawInputHidData(RawInputHandle rawInput, string hid, out RawInputHeader header)
        {
            var size = GetRawInputDataSize(rawInput);
            var headerSize = Marshal.SizeOf<RawInputHeader>();
            var bytes = new byte[size];
            // 获取原始输入数据
            var hRawInput = RawInputHandle.GetRawValue(rawInput);
            uint dataSize = size;
            GetRawInputData(hRawInput, RawInputGetBehavior.Input, bytes, ref dataSize, (uint)headerSize).EnsureSuccess();

            // 从字节数组中读取 RawInputHeader
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                IntPtr bytesPtr = handle.AddrOfPinnedObject();
                header = Marshal.PtrToStructure<RawInputHeader>(bytesPtr);

                // 获取 RawHid 数据指针
                IntPtr hidPtr = IntPtr.Add(bytesPtr, headerSize);
                return RawHid.FromPointer(hidPtr, hid);
            }
            finally
            {
                handle.Free();
            }
        }

        public struct RawHid
        {
            int dwSizeHid;
            int dwCount;
            byte[] rawData;
            public int ElementSize => dwSizeHid;
            public int Count => dwCount;
            public byte[] RawData => rawData;

            public string hid;


            public static RawHid FromPointer(IntPtr ptr, string hid)
            {
                var result = new RawHid();
                int offset = 0;
                // dwSizeHid
                result.dwSizeHid = Marshal.ReadInt32(ptr, offset);
                offset += sizeof(int);
                // dwCount
                result.dwCount = Marshal.ReadInt32(ptr, offset);
                offset += sizeof(int);


                int dataLength = result.ElementSize * result.Count;
                result.rawData = new byte[dataLength];
                result.hid = hid;
                // copy rawData
                IntPtr dataPtr = IntPtr.Add(ptr, offset);
                Marshal.Copy(dataPtr, result.rawData, 0, dataLength);
                return result;
            }

            public override string ToString() =>
                $"{{Count: {Count}, Size: {ElementSize}, Content: {BitConverter.ToString(RawData).Replace("-", " ")}}}";
        }



        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetRawInputDeviceInfoA", CharSet = CharSet.Ansi)]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, StringBuilder pData, ref uint pcbSize);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetRawInputDeviceInfoA")]
        public static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, ref RID_DEVICE_INFO pData, ref uint pcbSize);


        public static uint EnsureSuccess(this uint result)
        {
            if (result == unchecked((uint)-1)) throw new Win32Exception();

            return result;
        }

        public static RawInputHeader GetRawInputDataHeader(RawInputHandle rawInput)
        {
            var hRawInput = RawInputHandle.GetRawValue(rawInput);
            var headerSize = (uint)Marshal.SizeOf<RawInputHeader>();
            var size = headerSize;

            GetRawInputData(hRawInput, RawInputGetBehavior.Header, out var header, ref size, headerSize).EnsureSuccess();

            return header;
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(RawInputDevice[] pRawInputDevice, uint numberDevices, uint size);


        [StructLayout(LayoutKind.Sequential)]
        public struct RawInputDevice
        {
            public HidUsagePage UsagePage;
            public HidUsage Usage;
            public RawInputDeviceFlags Flags;
            public IntPtr Target;

            public override string ToString()
            {
                return string.Format("{0}/{1}, flags: {2}, target: {3}", UsagePage, Usage, Flags, Target);
            }

        }




        public enum HidUsagePage : ushort
        {
            UNDEFINED = 0x00,   // Unknown usage page
            GENERIC = 0x01,     // Generic desktop controls
            SIMULATION = 0x02,  // Simulation controls
            VR = 0x03,          // Virtual reality controls
            SPORT = 0x04,       // Sports controls
            GAME = 0x05,        // Games controls
            KEYBOARD = 0x07,    // Keyboard controls
            DIGITIZERS = 0x0D,    // Digitizers
        }
        public enum HidUsage : ushort
        {
            Undefined = 0x00,       // Unknown usage
            Pointer = 0x01,         // Pointer
            Mouse = 0x02,           // Mouse
            Joystick = 0x04,        // Joystick
            Gamepad = 0x05,         // Game Pad
            Keyboard = 0x06,        // Keyboard
            Keypad = 0x07,          // Keypad
            SystemControl = 0x80,   // Muilt-axis Controller
            Tablet = 0x80,          // Tablet PC controls
            Consumer = 0x0C,        // Consumer

            // Pen usage
            Pen = 0x02,             // Pen
            PreferredColor = 0x5C,  // Preferred Color
            InRange = 0x32,         // In Range
        }

        [Flags]
        public enum RawInputDeviceFlags
        {
            NONE = 0,                   // No flags
            REMOVE = 0x00000001,        // Removes the top level collection from the inclusion list. This tells the operating system to stop reading from a device which matches the top level collection. 
            EXCLUDE = 0x00000010,       // Specifies the top level collections to exclude when reading a complete usage page. This flag only affects a TLC whose usage page is already specified with PageOnly.
            PAGEONLY = 0x00000020,      // Specifies all devices whose top level collection is from the specified UsagePage. Note that Usage must be zero. To exclude a particular top level collection, use Exclude.
            NOLEGACY = 0x00000030,      // Prevents any devices specified by UsagePage or Usage from generating legacy messages. This is only for the mouse and keyboard.
            INPUTSINK = 0x00000100,     // Enables the caller to receive the input even when the caller is not in the foreground. Note that WindowHandle must be specified.
            CAPTUREMOUSE = 0x00000200,  // Mouse button click does not activate the other window.
            NOHOTKEYS = 0x00000200,     // Application-defined keyboard device hotkeys are not handled. However, the system hotkeys; for example, ALT+TAB and CTRL+ALT+DEL, are still handled. By default, all keyboard hotkeys are handled. NoHotKeys can be specified even if NoLegacy is not specified and WindowHandle is NULL.
            APPKEYS = 0x00000400,       // Application keys are handled.  NoLegacy must be specified.  Keyboard only.

            // Enables the caller to receive input in the background only if the foreground application does not process it. 
            // In other words, if the foreground application is not registered for raw input, then the background application that is registered will receive the input.
            EXINPUTSINK = 0x00001000,
            DEVNOTIFY = 0x00002000
        }




        #region Device Info Header

        static StringBuilder devBufer;
        public static RawHid GetDeviceHid(IntPtr lParam)
        {
            var header = GetRawInputDataHeader((RawInputHandle)lParam);
            int tempHader = Convert.ToInt32(header.DeviceHandle.ToString());
            IntPtr ptr = new IntPtr(tempHader);
            var deviceHandle = ptr; // a handle obtained from WM_TOUCH message.
            if (devBufer == null) devBufer = new StringBuilder(4096 * 2);
            devBufer.Clear();

            uint returnedDataSize = (uint)devBufer.Capacity;
            var firstCall = GetRawInputDeviceInfo(deviceHandle, 0x20000007, devBufer, ref returnedDataSize);

            var firstError = Marshal.GetLastWin32Error();
            var firtsDataSize = returnedDataSize;
            var devName = devBufer.ToString();
            if (string.IsNullOrEmpty(devName)) devName = "No name retrieved";

            var devInfo = new RID_DEVICE_INFO();
            var structureSize = Marshal.SizeOf<RID_DEVICE_INFO>();
            devInfo.cbSize = structureSize;
            returnedDataSize = (uint)structureSize;
            var secondCall = GetRawInputDeviceInfo(deviceHandle, 0x2000000B, ref devInfo, ref returnedDataSize);
            var secondError = Marshal.GetLastWin32Error();
            string hidData = "ERROR: hid data retrieval failed";
            if (devInfo.dwType == 2)
            {
                hidData = devInfo.hid.ToString();
            }
            RawHid rawHid = new RawHid();
            rawHid.hid = devBufer.ToString();

            switch (header.Type)
            {
                case RawInputDeviceType.Hid:
                    return GetRawInputHidData((RawInputHandle)lParam, rawHid.hid, out _);
                default: break;
            }
            return rawHid;
        }
        #endregion

        [Flags]
        public enum PenStatus : byte
        {
            Leave = 0000000000,
            NOKEYTOUCH = 0b00000001, // 1
            UPKEY = 0b00000010, // 2
            DOWNKEYTOUCH = 0b00000100, // 4
            DOWNKEY = 0b00001000, // 8
            Suspension = 0b00100000, // 32
            PressDown = 0b00100001,//33
        }

        public class PenInputEvent
        {
            public string DeviceName;
            public string DeviceType;
            public IntPtr DeviceHandle;     // Handle to the device that send the input
            public string Name;

            public PenStatus PenStatus;
            public ushort X;
            public ushort Y;
            public ushort XTilt;
            public ushort YTilt;
            public ushort Twist;
            public ushort Pressure;
            public ushort BatteryStrength;
            public ushort DigitizerStatus;

            private string _source;
            public string Source
            {
                get => _source;
                set => _source = string.Format("Pen_{0}", value.PadLeft(2, '0'));
            }
            #region 解析stylus 坐标等数据
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct InputData
            {
                public Rawinputheader header;           // 64 bit header size: 24  32 bit the header size: 16
                public RawData data;                    // Creating the rest in a struct allows the header size to align correctly for 32/64 bit
            }
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct Rawinputheader
            {
                public uint dwType;                     // Type of raw input (RIM_TYPEHID 2, RIM_TYPEKEYBOARD 1, RIM_TYPEMOUSE 0)
                public uint dwSize;                     // Size in bytes of the entire input packet of data. This includes RAWINPUT plus possible extra input reports in the RAWHID variable length array. 
                public IntPtr hDevice;                  // A handle to the device generating the raw input data. 
                public IntPtr wParam;                   // RIM_INPUT 0 if input occurred while application was in the foreground else RIM_INPUTSINK 1 if it was not.

                public override string ToString()
                {
                    return string.Format("RawInputHeader\n dwType : {0}\n dwSize : {1}\n hDevice : {2}\n wParam : {3}", dwType, dwSize, hDevice, wParam);
                }
            }
            [StructLayout(LayoutKind.Explicit, Pack = 1)]
            public struct RawData
            {
                [FieldOffset(0)]
                internal Rawmouse mouse;
                [FieldOffset(0)]
                internal Rawkeyboard keyboard;
                [FieldOffset(0)]
                internal Rawhid hid;
                [FieldOffset(0)]
                internal RawHidpen pen;


            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Rawmouse
            {
                public ushort usFlags;
                public ushort usButtonFlags;
                public ushort usButtonData;
                public uint ulRawButtons;
                public int lLastX;
                public int lLastY;
                public uint ulExtraInformation;

                public override string ToString()
                {

                    return $"lLastX {lLastX},lLastY {lLastY},usButtonFlags {usButtonFlags}";
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Rawkeyboard
            {
                public ushort Makecode;                 // Scan code from the key depression
                public ushort Flags;                    // One or more of RI_KEY_MAKE, RI_KEY_BREAK, RI_KEY_E0, RI_KEY_E1
                private readonly ushort Reserved;       // Always 0    
                public ushort VKey;                     // Virtual Key Code
                public uint Message;                    // Corresponding Windows message for exmaple (WM_KEYDOWN, WM_SYASKEYDOWN etc)
                public uint ExtraInformation;           // The device-specific addition information for the event (seems to always be zero for keyboards)

                public override string ToString()
                {
                    return string.Format("Rawkeyboard\n Makecode: {0}\n Makecode(hex) : {0:X}\n Flags: {1}\n Reserved: {2}\n VKeyName: {3}\n Message: {4}\n ExtraInformation {5}\n",
                                                        Makecode, Flags, Reserved, VKey, Message, ExtraInformation);
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Rawhid
            {
                public uint dwSizHid;
                public uint dwCount;
                public byte bRawData;

                public override string ToString()
                {
                    return string.Format("Rawhib\n dwSizeHid : {0}\n dwCount : {1}\n bRawData : {2}\n", dwSizHid, dwCount, bRawData);
                }
            }
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct RawHidpen
            {
                [MarshalAs(UnmanagedType.U4)]
                public uint dwSizHid;
                [MarshalAs(UnmanagedType.U4)]
                public uint dwCount;
                public HidPenData data;
            }
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct HidPenData
            {
                [MarshalAs(UnmanagedType.U1)]
                public byte reportID;
                [MarshalAs(UnmanagedType.U1)]
                public byte status;
                [MarshalAs(UnmanagedType.U2)]
                public UInt16 x;
                [MarshalAs(UnmanagedType.U2)]
                public UInt16 y;
                [MarshalAs(UnmanagedType.U2)]
                public UInt16 pressure;
                [MarshalAs(UnmanagedType.U2)]
                public UInt16 xTilt;
                [MarshalAs(UnmanagedType.U2)]
                public UInt16 yTilt;
                [MarshalAs(UnmanagedType.U2)]
                public UInt16 twist;
                [MarshalAs(UnmanagedType.U2)]
                public UInt16 scanTime;
                [MarshalAs(UnmanagedType.U1)]
                public byte batteryStrength;
                [MarshalAs(UnmanagedType.U1)]
                public byte digitizerStatus;
                public override string ToString()
                {
                    return string.Format("HidPenData status: {0} twist : {0:X} batteryStrength: {1} digitizerStatus: {2}", status, twist, batteryStrength, digitizerStatus);
                }
            }

            #endregion

            public static PenInputEvent Create(HidPenData data, string deviceName)
            { 
                return new PenInputEvent
                {
                    DeviceName = deviceName,
                    PenStatus = (PenStatus)data.status,
                    X = data.x,
                    Y = data.y,
                    XTilt = data.xTilt,
                    YTilt = data.yTilt,
                    Twist = data.twist,
                    Pressure = data.pressure,
                    DigitizerStatus = data.digitizerStatus,
                    BatteryStrength = data.batteryStrength
                };
            }

            public string ToString(double monitorPhysicalWidth, double monitorPhysicalHeight)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"X: {this.X / monitorPhysicalWidth}");
                sb.AppendLine($"Y: {this.Y / monitorPhysicalHeight}");
                sb.AppendLine($"Pressure: {this.Pressure / 4096d}");
                return sb.ToString();
            }
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Device");
                sb.AppendLine($"DeviceName: {this.DeviceName}");
                sb.AppendLine($"DeviceType: {this.DeviceType}");
                sb.AppendLine($"DeviceHandle: {this.DeviceHandle}");
                sb.AppendLine($"Name: {this.Name}");

                sb.AppendLine($"PenStatus: {this.PenStatus}");
                sb.AppendLine($"X: {this.X}");
                sb.AppendLine($"Y: {this.Y}");
                sb.AppendLine($"XTilt: {this.XTilt}");
                sb.AppendLine($"YTilt: {this.YTilt}");
                sb.AppendLine($"Twist: {this.Twist}");
                sb.AppendLine($"Pressure: {this.Pressure}");
                sb.AppendLine($"DigitizerStatus: {this.DigitizerStatus}");
                sb.AppendLine($"BatteryStrength: {this.BatteryStrength}");

                sb.AppendLine($"Source: {this.Source}");
                return sb.ToString();
            }
        }

        public class RawPenInputEventArg : EventArgs
        {
            public readonly PenInputEvent PenInputEvent;
            public RawPenInputEventArg(PenInputEvent arg)
            {
                this.PenInputEvent = arg;
            }
        }

    }
}
