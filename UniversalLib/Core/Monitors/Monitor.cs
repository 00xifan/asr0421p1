using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UniversalLib.Common;
using UniversalLib.Core.Sensors;
using Windows.Foundation;

namespace UniversalLib.Core.Monitors
{
    public enum ScreenNameEnum
    {
        None,
        ScreenB,
        ScreenC,
        SingleScreen,
    }
    /// <summary>
    /// Direction of Snooker.
    /// </summary>
    public enum SnookerDirection
    {
        None,
        LeftTop,
        RightTop,
        RightBottom,
        LeftBottom,
    }
    public enum Orientation
    {
        //
        // Summary:
        //     Control or layout should be horizontally oriented.
        Horizontal,
        //
        // Summary:
        //     Control or layout should be vertically oriented.
        Vertical
    }
    /// <summary>
    /// Target of Snooker.
    /// </summary>
    public enum SnookerTarget
    {
        /// <summary> Normal </summary>
        None,
        /// <summary> Inner Target </summary>
        Inner,
        /// <summary> Outer Target </summary>
        Outer,
        /// <summary> Out of Radian Range </summary>
        OutOfRadianRange,
    }
    public static class RectExtensions
    {
        // 扩展方法：按比例缩放矩形
        public static Rect Scale(this Rect rect, double scaleX, double scaleY)
        {
            if (rect.IsEmpty)
            {
                return rect;  // 如果矩形为空，直接返回原始矩形
            }

            double newX = rect.X * scaleX;
            double newY = rect.Y * scaleY;
            double newWidth = rect.Width * scaleX;
            double newHeight = rect.Height * scaleY;

            // 如果缩放因子为负，处理翻转
            if (scaleX < 0.0)
            {
                newX += newWidth; // 如果水平翻转，调整 X 坐标
                newWidth *= -1.0;  // 变更宽度为负值
            }

            if (scaleY < 0.0)
            {
                newY += newHeight; // 如果垂直翻转，调整 Y 坐标
                newHeight *= -1.0;  // 变更高度为负值
            }

            return new Rect(newX, newY, newWidth, newHeight);
        }

    }
    public sealed partial class Monitor
    {
        /// <summary> DPI. E.g. 150. </summary>
        public readonly int DPI;
        /// <summary> Scale of DPI. E.g. 1.5. </summary>
        public readonly double DPIScale;
        /// <summary> Reversed Scale of DPI. E.g. 0.6666 (100 / 150). </summary>
        public readonly double DPIScaleR;
        /// <summary> ID of device. Default C contains "SHP1500" and B contains "SHP1501". </summary>
        public readonly string DeviceId;
        /// <summary> X-Y-Width-Height of Device. E.g. Rect(0, 0, 1920, 1280). </summary>
        public readonly Rect WindowRect;
        /// <summary> X-Y-Width-Height of Device-Independent-Pixel. E.g. Rect(0, 0, 1,280, 853.33). </summary>
        public readonly Rect UIRect;

        /// <summary> Orientation of device. </summary>
        public readonly Orientation Orientation;
        /// <summary> Does the device flip? </summary>
        public readonly bool IsFlip;

        /// <summary> ID of Touch Module. </summary>
        public string TouchModuleID { get; internal set; }

        public string StylusModuleID { get; internal set; }
        /// <summary> Name of Screen. </summary>
        public ScreenNameEnum ScreenName { get; internal set; }

        /// <summary>
        /// When the screen information changes, the device attitude
        /// </summary>
        public SENSOR_FORM PreviousDeviceAttitude { get; internal set; }

        public string DeviceName { get; internal set; }

        private double TaskbarPhysicalH = 0.0;
        static private double taskbarH_100DPI = 0.0;
        internal Monitor(MonitorItem monitor)
        {
            string[] xy = monitor.Position.Split(',');
            string[] wh = monitor.Resolution.Split(',');
            Windows.Foundation.Rect wr = new Rect
            (
                Convert.ToInt32(xy[0]), Convert.ToInt32(xy[1]),
                Convert.ToInt32(wh[0]), Convert.ToInt32(wh[1])
            );
            this.WindowRect = wr;
            foreach (string item in Monitor.GetMonitorPnpDeviceId())
            {
                byte[] monitorEdid = (byte[])Microsoft.Win32.Registry.GetValue
                (
                    $@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Enum\{item}\Device Parameters",
                    "EDID",
                    new byte[] { }
                );
                if (monitorEdid == null || monitorEdid.Length == 0)
                    continue;
                if (monitorEdid.Length > 21) this.PhysicalWidth = 1000 * monitorEdid[21];
                if (monitorEdid.Length > 22) this.PhysicalHeight = 1000 * monitorEdid[22];

                double snookerLength = Math.Min(this.PhysicalWidth / 4d, this.PhysicalHeight / 4d);
                double snookerLengthInner = snookerLength / 2;
                this.SnookerLengthSquared = snookerLength * snookerLength;
                this.SnookerLengthInnerSquared = snookerLengthInner * snookerLengthInner;
            }

            double taskbarH = 0.0;
            double DPICoefficient = (double)monitor.DPI / 100;
            if (taskbarH_100DPI == 0.0)
            {    // 使用 GetSystemMetrics 获取屏幕高度和工作区高度
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);        // 屏幕高度
                int workAreaHeight = GetSystemMetrics(SM_CYFULLSCREEN);  // 工作区高度（不包括任务栏）

                taskbarH_100DPI = (this.WindowRect.Height - workAreaHeight) / DPICoefficient;
            }
            if (taskbarH_100DPI != 48)
            {
                taskbarH_100DPI = 48;
            }
            taskbarH = taskbarH_100DPI * DPICoefficient;
            if (this.WindowRect.Width > this.WindowRect.Height)
            {
                TaskbarPhysicalH = taskbarH / this.WindowRect.Height * this.PhysicalHeight;
            }
            else
            {
                TaskbarPhysicalH = taskbarH / this.WindowRect.Height * this.PhysicalWidth;
            }

            LogsHelper.Instance.DebugWrite($"Monitor.cs::Monitor taskbar height {taskbarH} physical height {TaskbarPhysicalH}");
            DeviceName = monitor.DeviceName;
            double s = monitor.DPI / 100d;
            this.DPIScale = s;

            double sr = 100d / monitor.DPI;
            this.DPIScaleR = sr;

            this.DPI = monitor.DPI;
            this.DeviceId = monitor.DeviceId;


            wr = wr.Scale(sr, sr);
            this.UIRect = wr;

            switch (monitor.Orientation)
            {
                case 0:
                    this.Orientation = Orientation.Horizontal;
                    this.IsFlip = false;
                    break;
                case 1:
                    this.Orientation = Orientation.Vertical;
                    this.IsFlip = false;
                    break;
                case 2:
                    this.Orientation = Orientation.Horizontal;
                    this.IsFlip = true;
                    break;
                case 3:
                    this.Orientation = Orientation.Vertical;
                    this.IsFlip = true;
                    break;
                default:
                    this.Orientation = Orientation.Horizontal;
                    this.IsFlip = false;
                    break;
            }
        }

        /// <summary>
        /// Get device-position that transformed on window.
        /// </summary>
        /// <returns> The product position. </returns>
        public Point GetWindowPoint(Point devicePoint)
        {
            switch (this.Orientation)
            {
                case Orientation.Horizontal:
                    if (this.IsFlip) return new Point(this.WindowRect.Width - devicePoint.X + this.WindowRect.X, this.WindowRect.Height - devicePoint.Y + this.WindowRect.Y);
                    else return new Point(devicePoint.X + this.WindowRect.X, devicePoint.Y + this.WindowRect.Y);
                case Orientation.Vertical:
                    if (this.IsFlip) return new Point(devicePoint.Y + this.WindowRect.X, this.WindowRect.Height - devicePoint.X + this.WindowRect.Y);
                    else return new Point(this.WindowRect.Width - devicePoint.Y + this.WindowRect.X, devicePoint.X + this.WindowRect.Y);
                default:
                    return new Point();
            }
        }
        /// <summary>
        /// Get device-position that transformed on UI.
        /// </summary>
        /// <returns> The product position. </returns>
        public Point GetUIPoint(Point devicePoint)
        {
            switch (this.Orientation)
            {
                case Orientation.Horizontal:
                    if (this.IsFlip) return new Point(this.UIRect.Width - devicePoint.X / this.WindowRect.Width * this.UIRect.Width + this.UIRect.X, this.UIRect.Height - devicePoint.Y / this.WindowRect.Height * this.UIRect.Height + this.UIRect.Y);
                    else return new Point(devicePoint.X / this.WindowRect.Width * this.UIRect.Width + this.UIRect.X, devicePoint.Y / this.WindowRect.Height * this.UIRect.Height + this.UIRect.Y);
                case Orientation.Vertical:
                    if (this.IsFlip) return new Point(devicePoint.Y / this.WindowRect.Width * this.UIRect.Width + this.UIRect.X, this.UIRect.Height - devicePoint.X / this.WindowRect.Height * this.UIRect.Height + this.UIRect.Y);
                    else return new Point(this.UIRect.Width - devicePoint.Y / this.WindowRect.Width * this.UIRect.Width + this.UIRect.X, devicePoint.X / this.WindowRect.Height * this.UIRect.Height + this.UIRect.Y);
                default:
                    return new Point();
            }
        }


        //@Static
        /// <summary>
        /// Get the rectangle of the vector transformation.
        /// </summary>
        /// <param name="vector"> The vector. E.g. Rect(0, 0, 1, 1). </param>
        /// <param name="destination"> The destination. E.g. Rect(0, 0, 1,280, 853.33). </param>
        /// <returns></returns>
        public static Rect GetRect(Rect vector, Rect destination) => new Rect
        {
            X = vector.X * destination.Width + destination.X,
            Y = vector.Y * destination.Height + destination.Y,
            Width = vector.Width * destination.Width,
            Height = vector.Height * destination.Height,
        };
        /// <summary>
        /// Get the cropped rectangle
        /// </summary>
        /// <param name="destination"> The destination. E.g. Rect(0, 0, 1,280, 853.33). </param>
        /// <param name="padding"> The padding. E.g. Thickness(0, 0, 0, 74). </param>
        /// <returns></returns>
        //public static Rect Padding(Rect destination,  Thickness padding) => new Rect
        //{
        //    X = destination.X + padding.Left,
        //    Y = destination.Y + padding.Top,
        //    Width = destination.Width - padding.Left - padding.Right,
        //    Height = destination.Height - padding.Top - padding.Bottom,
        //};

    }
    public sealed partial class Monitor
    {

        /// <summary> Width for Physical. E.g. 29 Centimeter. </summary>
        public readonly int PhysicalWidth;
        /// <summary> Height for Physical. E.g. 18 Centimeter. </summary>
        public readonly int PhysicalHeight;

        /// <summary>
        /// 1/4 zhengfangx mianji 
        /// </summary>
        private readonly double SnookerLengthSquared;
        /// <summary>
        /// 1/8 zhengfangxing mianji
        /// </summary>
        private readonly double SnookerLengthInnerSquared;


        public SnookerDirection Positioning(double pointX, double pointY)
        {
            bool isLeft = pointX <= this.PhysicalWidth / 2;
            bool isTop = pointY <= this.PhysicalHeight / 2;

            if (isLeft && isTop) return SnookerDirection.LeftTop;
            else if (isLeft == false && isTop) return SnookerDirection.RightTop;
            else if (isLeft == false && isTop == false) return SnookerDirection.RightBottom;
            else return SnookerDirection.LeftBottom;
        }

        /// <summary>
        /// Gets the target by point and direction.
        /// 
        /// </summary>
        public SnookerTarget Contains(double pointX, double pointY, SnookerDirection direction)
        {

            switch (direction)
            {
                case SnookerDirection.LeftTop:
                    return this.Contains(pointX, pointY, pointX - 0, pointY - 0, 0.785398163397448); // LeftTopRadianCenter (45) * Math.PI / 180
                case SnookerDirection.RightTop:
                    return this.Contains(pointX, pointY, pointX - this.PhysicalWidth, pointY - 0, 2.35619449019234); // RightTopRadianCenter (45 + 90) * Math.PI / 180
                case SnookerDirection.RightBottom:
                    return this.Contains(pointX, pointY, pointX - this.PhysicalWidth, pointY - this.PhysicalHeight, 3.92699081698724); // RightBottomRadianCenter (45 + 90 + 90) * Math.PI / 180
                default:
                    return this.Contains(pointX, pointY, pointX - 0, pointY - this.PhysicalHeight, 5.49778714378214); // LeftBottomRadianCenter (45 + 90 + 90 + 90) * Math.PI / 180
            }
        }
        private SnookerTarget Contains(double pointX, double pointY, double vectorX, double vectorY, double radianCenter)
        {
            double lengthSquared = Monitor.GetLengthSquared(vectorX, vectorY);
            if (lengthSquared < this.SnookerLengthInnerSquared)
            {
                //Horizontal screen
                if (this.WindowRect.Width > this.WindowRect.Height)
                {
                    //It takes effect in the taskbar
                    if (this.PhysicalHeight - pointY < TaskbarPhysicalH ||
                        pointY < TaskbarPhysicalH)
                    {
                        return SnookerTarget.Inner;
                    }
                }
                else //Vertical screen
                {
                    //It takes effect in the taskbar
                    if (this.PhysicalWidth - pointX < TaskbarPhysicalH ||
                        pointX < TaskbarPhysicalH)
                    {
                        return SnookerTarget.Inner;
                    }
                }
                return SnookerTarget.OutOfRadianRange;
            }
            else if (lengthSquared < this.SnookerLengthSquared)
            {
                //Horizontal screen
                if (this.WindowRect.Width > this.WindowRect.Height)
                {
                    if (pointY < this.PhysicalHeight / 2)    //Top Pos
                    {
                        //It takes effect out the taskbar
                        if (pointY > TaskbarPhysicalH)
                        {
                            return SnookerTarget.Outer;
                        }
                    }
                    else                                   //Bottom Pos
                    {
                        //It takes effect out the taskbar
                        if (this.PhysicalHeight - pointY > TaskbarPhysicalH)
                        {
                            return SnookerTarget.Outer;
                        }
                    }
                }
                else //Vertical screen
                {
                    if (pointX < this.PhysicalHeight / 2)    //Top Pos
                    {
                        //It takes effect out the taskbar
                        if (pointX > TaskbarPhysicalH)
                        {
                            return SnookerTarget.Outer;
                        }
                    }
                    else                                   //Bottom Pos
                    {
                        //It takes effect out the taskbar
                        if (this.PhysicalWidth - pointX > TaskbarPhysicalH)
                        {
                            return SnookerTarget.Outer;
                        }
                    }
                }
                return SnookerTarget.OutOfRadianRange;
            }
            else
            {
                return SnookerTarget.None;
            }
        }

        //@Static
        public static double GetRadian(double vectorX, double vectorY) => (Math.Atan2(vectorY, vectorX) + Math.PI + Math.PI) % (Math.PI + Math.PI);
        public static double GetLengthSquared(double vectorX, double vectorY) => vectorX * vectorX + vectorY * vectorY;


        private const int DIGCF_PRESENT = 0x00000002;
        private const int DIGCF_ALLCLASSES = 0x00000004;

        // SetupAPI DLL 
        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SetupDiGetClassDevs(IntPtr classGuid, string enumerator, IntPtr hwndParent, uint flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetupDiGetDeviceInstanceId(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, StringBuilder deviceInstanceId, int deviceInstanceIdSize, out int requiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        static extern int SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }
        public static IEnumerable<string> GetMonitorPnpDeviceId()
        {
            List<string> ids = new List<string>();

            IntPtr deviceInfoSet = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);

            if (deviceInfoSet == IntPtr.Zero)
            {
                LogsHelper.Instance.DebugWrite("Error: Unable to get device info set.");
                return ids;
            }

            try
            {
                SP_DEVINFO_DATA deviceInfoData = new SP_DEVINFO_DATA();
                deviceInfoData.cbSize = (uint)Marshal.SizeOf(deviceInfoData);

                uint deviceIndex = 0;
                while (SetupDiEnumDeviceInfo(deviceInfoSet, deviceIndex, ref deviceInfoData))
                {
                    StringBuilder deviceInstanceId = new StringBuilder(256);
                    int requiredSize;
                    if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, deviceInstanceId, deviceInstanceId.Capacity, out requiredSize))
                    {
                        // Get device PNP ID
                        string pnpDeviceId = deviceInstanceId.ToString();
                        LogsHelper.Instance.DebugWrite($"Device PNP ID: {pnpDeviceId}");

                        if (pnpDeviceId == null) continue;
                        ids.Add(pnpDeviceId.ToString());
                    }
                    deviceIndex++;
                }
            }
            catch (Exception ex)
            {
                LogsHelper.Instance.ErrorWrite($"GetMonitorPnpDeviceId.Exception:{ex.Message}");
            }
            finally
            {
                SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return ids;
        }

        // 定义常量，用于 GetSystemMetrics 函数
        private const int SM_CYSCREEN = 1;       // 屏幕高度（以像素为单位）
        private const int SM_CYFULLSCREEN = 17;  // 工作区高度（以像素为单位）

        // 导入 GetSystemMetrics 函数
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }
}