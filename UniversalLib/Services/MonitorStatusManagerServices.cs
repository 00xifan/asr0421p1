using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UniversalLib.Common;
using UniversalLib.Core.Monitors;
using UniversalLib.Core.Sensors;

namespace UniversalLib.Services
{
    public interface IMonitorStatusManagerServices
    {
        IList<Monitor> Monitors { get; }
        void Init();
        void UnInit();
        Action<Orientation> CurrentScreenOrientationAction { get; set; }
        public Action MonitorInfoChangedAction { get; set; }
        Monitor RelationHidTouchModules(string hid);
        Monitor RelationHidStylusModules(string hid);
        Monitor GetMonitor(ScreenNameEnum screenNameEnum);
        Monitor GetMainMonitor();

        int GetScreenDPI();
    }
    public class MonitorStatusManagerServices : IMonitorStatusManagerServices
    {
        private readonly ISensorStatusManagerServices _sensorStatusManagerServices;
        public IList<Monitor> Monitors { get; private set; }
        public Action<Orientation> CurrentScreenOrientationAction { get; set; }
        public Action MonitorInfoChangedAction { get; set; }

        private Orientation screenOrientation = Orientation.Vertical;
        public Orientation ScreenOrientation
        {
            get { return screenOrientation; }
            private set
            {
                if (screenOrientation != value)
                {
                    screenOrientation = value;
                    CurrentScreenOrientationAction?.Invoke(ScreenOrientation);
                    LogsHelper.Instance.DebugWrite($"ScreenOrientation Changed as {screenOrientation}");
                }
            }
        }



        public MonitorStatusManagerServices(ISensorStatusManagerServices sensorStatusManagerServices)
        {
            _sensorStatusManagerServices = sensorStatusManagerServices;
        }

        private void OnFormChanged(SENSOR_FORM newForm)
        {
            oldForm = newForm;
            SetCurrentPCMode((int)newForm);
        }

        #region C#
        public void Init()
        {
            LogsHelper.Instance.DebugWrite($"[MonitorStatusManagerServices] begin Init");
            if (_CallBack == null)
            {
                _CallBack = new pNotifyCallBack(FuncCallBack);
                try
                {
                    pOBJ = GetSystemInfoLibObject();
                    InitLib(pOBJ, _CallBack);
                    _sensorStatusManagerServices.FormChangedAction += OnFormChanged;
                }
                catch (Exception ex)
                {
                LogsHelper.Instance.ErrorWrite($"[MonitorStatusManagerServices] init error ex {ex.Message} throw dll not found");
                    throw new DllNotFoundException($"{ex.Message}");
                }
            }
            LogsHelper.Instance.DebugWrite($"[MonitorStatusManagerServices] end Init");
        }

        public void UnInit()
        {
            if (pOBJ != IntPtr.Zero)
            {
                UnInitLib(pOBJ);
                DeletetSystemInfoLibObject(pOBJ);
            }
        }

        public Monitor RelationHidTouchModules(string hid)
        {
            return Monitors?.Where(x => !string.IsNullOrEmpty(x.TouchModuleID) && hid.Contains(x.TouchModuleID))?.FirstOrDefault();
        }
        public Monitor RelationHidStylusModules(string hid)
        {
            return Monitors?.Where(x => !string.IsNullOrEmpty(x.StylusModuleID) && hid.Contains(x.StylusModuleID))?.FirstOrDefault();
        }


        public Monitor GetMonitor(ScreenNameEnum screenNameEnum)
        {
            return Monitors?.Where(x => x.ScreenName.Equals(screenNameEnum))?.FirstOrDefault();
        }

        public Monitor GetMainMonitor()
        {
            return Monitors?.Where(x => x.UIRect.Left == 0 && x.UIRect.Top == 0)?.FirstOrDefault();
        }

        public int GetScreenDPI()
        {
            var mainMonitor = GetMainMonitor();
            if (mainMonitor == null) return 0;
            return mainMonitor.DPI;
        }

        private SENSOR_FORM oldForm = SENSOR_FORM.FF_INVALID;

        private void FuncCallBack(NotifyType _Type, IntPtr _JsonOut)
        {
            var jsonStr = Marshal.PtrToStringUni(_JsonOut);
            LogsHelper.Instance.DebugWrite($"Monitor FuncCallBack {jsonStr}");
            if (string.IsNullOrEmpty(jsonStr))
            {
                return;
            }
            if (_Type == NotifyType.Notify_MointorInfo)
            {
                oldForm = _sensorStatusManagerServices.CurrentFormStatus;

                var data = JsonConvert.DeserializeObject<MonitorModel>(jsonStr);
                if (data != null && data.Monitor != null)
                {
                    Monitors = data.Monitor.Select(m => new Monitor(m)).ToList();
                    RelationMonitorModules();
                    LogsHelper.Instance.DebugWrite("MonitorInfoChanged begin invoke");
                    MonitorInfoChangedAction?.Invoke();
                    LogsHelper.Instance.DebugWrite("MonitorInfoChanged end invoke");
                }
            }
        }
         
        private void RelationMonitorModules()
        {
            LogsHelper.Instance.DebugWrite($"begin RelationMonitorModules");
            GlobalConstant.Instance.SystemMachineType();
            if (GlobalConstant.Instance.CurrentMachineType == GlobalConstant.MachineType.DualDisplayDevice)
            {
                Monitor monitorB = Monitors?.Where(x => x.DeviceId.Contains(GlobalConstant.Instance.CurrentScreenBId))?.FirstOrDefault();
                Monitor monitorC = Monitors?.Where(x => x.DeviceId.Contains(GlobalConstant.Instance.CurrentScreenCId))?.FirstOrDefault();

                if (monitorB != null)
                {
                    SetScreenOrientation(monitorB.Orientation);
                    monitorB.ScreenName = ScreenNameEnum.ScreenB;
                    monitorB.TouchModuleID = GlobalConstant.DualDisplayBTouchID;
                    monitorB.StylusModuleID = GlobalConstant.DualDisplayBStylusID;
                }
                if (monitorC != null)
                {
                    SetScreenOrientation(monitorC.Orientation);
                    monitorC.ScreenName = ScreenNameEnum.ScreenC;
                    monitorC.TouchModuleID = GlobalConstant.DualDisplayCTouchID;
                    monitorC.StylusModuleID = GlobalConstant.DualDisplayCStylusID;
                }
            }
            //Single screen does not require HID mapping. When HID reports, simply associate the screen.

            LogsHelper.Instance.DebugWrite($"end RelationMonitorModules");
        }


        private void SetScreenOrientation(Orientation opientation)
        {
            if (ScreenOrientation != opientation)
            {
                ScreenOrientation = opientation;
            }
        }
        private void SetCurrentPCMode(int mode)
        {
            FnSetPCMode(pOBJ, mode);
        }


        #endregion


        #region  C++
        private const string LibStatusManager_PATH = @"LibStatusManager.dll";
        private IntPtr pOBJ = IntPtr.Zero;
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        protected delegate void pNotifyCallBack(NotifyType _Type, IntPtr _JsonOut);
        protected static pNotifyCallBack _CallBack;

        protected enum NotifyType
        {
            Notify_MointorInfo,
            Notify_DisplayModeCheck
        }

        public enum ScreenCover
        {
            ScreenCover_B = 0,
            ScreenCover_C
        }

        [DllImport(LibStatusManager_PATH, CharSet = CharSet.Unicode, EntryPoint = "GetSystemInfoLibObject", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern IntPtr GetSystemInfoLibObject();

        [DllImport(LibStatusManager_PATH, CharSet = CharSet.Unicode, EntryPoint = "DeletetSystemInfoLibObject", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern void DeletetSystemInfoLibObject(IntPtr pObj);

        [DllImport(LibStatusManager_PATH, CharSet = CharSet.Unicode, EntryPoint = "InitLib", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern void InitLib(IntPtr pObj, [MarshalAs(UnmanagedType.FunctionPtr)] pNotifyCallBack cb);

        [DllImport(LibStatusManager_PATH, CharSet = CharSet.Unicode, EntryPoint = "UnInitLib", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern void UnInitLib(IntPtr pObj);


        [DllImport("LibStatusManager.dll", CharSet = CharSet.Unicode, EntryPoint = "FnSetPCMode", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern void FnSetPCMode(IntPtr pObj, int mode);

        #endregion



    }
}
