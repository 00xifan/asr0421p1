using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UniversalLib.Common;
using UniversalLib.Core.Sensors;

namespace UniversalLib.Services
{
    public interface ISensorStatusManagerServices
    {
        void Init();
        int AngleNow { get; }
        Action<int, int> AngleChangedAction { get; set; }
        SENSOR_FORM CurrentFormStatus { get; }
        Action<SENSOR_FORM> FormChangedAction { get; set; }

        SENSOR_FORM GetComputerMode();
    }
    public class SensorStatusManagerServices : ISensorStatusManagerServices
    {
        public int angleOld = -1;
        private int angleNow = -1;
        public int AngleNow
        {
            get { return angleNow; }
            private set
            {
                if (angleNow != value)
                {
                    angleOld = angleNow;
                    angleNow = value;
                    AngleChangedAction?.Invoke(angleOld, angleNow);
                }
            }
        }

        /// <summary>
        /// 角度变化事件。第一个参数为旧角度，第二个参数为新角度。
        /// </summary>
        public Action<int, int> AngleChangedAction { get; set; }

        private SENSOR_FORM m_pcForm = SENSOR_FORM.FF_INVALID;
        public SENSOR_FORM CurrentFormStatus
        {
            get { return m_pcForm; }
            private set
            {
                if (m_pcForm != value)
                {
                    m_pcForm = value;
                    FormChangedAction?.Invoke(m_pcForm);
                    LogsHelper.Instance.DebugWrite($"CurrentFormStatus Changed as {m_pcForm}");
                }
            }
        }

        public Action<SENSOR_FORM> FormChangedAction { get; set; }



        internal void SetCurrentFormStatus(SENSOR_FORM form)
        {
            CurrentFormStatus = form;
        }

        /// <summary>
        /// Non dual screens will not initialize,Will return the default PC value
        /// </summary>
        /// <exception cref="DllNotFoundException">not libstaticmanager dll</exception>
        public void Init()
        {
            LogsHelper.Instance.DebugWrite($"[SensorStatusManagerServices] begin Init");
            GlobalConstant.Instance.SystemMachineType();
            if (GlobalConstant.Instance.CurrentMachineType == GlobalConstant.MachineType.SingleDisplayDevice)
            {
                LogsHelper.Instance.DebugWrite($"[SensorStatusManagerServices] CurrentMachineType as  SingleDisplayDevice Init default Current formStatus PC");
                CurrentFormStatus = SENSOR_FORM.FF_PC;
                LogsHelper.Instance.DebugWrite($"[SensorStatusManagerServices] end Init");
                return;
            }


            if (_ModelChangedCallBack == null)
            {
                _ModelChangedCallBack = new pNotifyCallBack(ModelChangedCallBack);
                try
                {
                    bool result = initLESM((ulong)LibStatusType.SENSOR_EVENT);
                    LogsHelper.Instance.DebugWrite($"[SensorStatusManagerServices] initLESM  result {result}");
                    result = register_callback(_ModelChangedCallBack);
                    startLESM();
                    var status = GetComputerMode();
                    if (CurrentFormStatus != status)
                    {
                        CurrentFormStatus = status;
                    }
                    LogsHelper.Instance.DebugWrite($"Init CurrentFormStatus as {CurrentFormStatus}");
                }
                catch (Exception ex)
                {
                    LogsHelper.Instance.ErrorWrite($"[SensorStatusManagerServices] init error ex {ex.Message} throw dll not found");
                    throw new DllNotFoundException($"{ex.Message}");
                }
            }
            LogsHelper.Instance.DebugWrite($"[SensorStatusManagerServices] end Init");
        }

        public void UnInit()
        {
            try
            {
                stopLESM();
                uninitLESM();
            }
            catch (Exception ex)
            {
                LogsHelper.Instance.ErrorWrite($"[SensorStatusManagerServices] uninit error ex {ex.Message} throw dll not found");
                throw new DllNotFoundException($"{ex.Message}");
            }
        }

        private void ModelChangedCallBack(LE_DEVICESTATUS_ID _MsdId, IntPtr wParam, IntPtr lParam)
        {
            var status = (SENSOR_FORM)wParam;
            if (CurrentFormStatus != status)
            {
                CurrentFormStatus = status;
            }
            AngleNow = (int)lParam;
        }

        public SENSOR_FORM GetComputerMode()
        {
            SENSOR_FORM ret = SENSOR_FORM.FF_INVALID;
            try
            {
                IntPtr getSucc = IntPtr.Zero;
                ret = (SENSOR_FORM)get_value(LE_DEVICESTATUS_ID.SENSOR_PCMODE_ACQUIRE, getSucc);
            }
            catch (Exception ex)
            {
                LogsHelper.Instance.ErrorWrite($"[SensorStatusManagerServices] GetComputerMode error ex {ex.Message}");
            }
            return ret;
        }


        #region C++ 引入
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void pNotifyCallBack(LE_DEVICESTATUS_ID _Type, IntPtr wParam, IntPtr lParam);
        private pNotifyCallBack _ModelChangedCallBack;

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("LibStatusManager.dll", CharSet = CharSet.Unicode, EntryPoint = "initLESM", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool initLESM(ulong eventType = 15);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("LibStatusManager.dll", CharSet = CharSet.Unicode, EntryPoint = "startLESM", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern void startLESM();

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("LibStatusManager.dll", CharSet = CharSet.Unicode, EntryPoint = "stopLESM", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern void stopLESM();

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("LibStatusManager.dll", CharSet = CharSet.Unicode, EntryPoint = "uninitLESM", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern void uninitLESM();

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("LibStatusManager.dll", CharSet = CharSet.Unicode, EntryPoint = "register_callback", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern bool register_callback([MarshalAs(UnmanagedType.FunctionPtr)] pNotifyCallBack cb);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("LibStatusManager.dll", CharSet = CharSet.Unicode, EntryPoint = "get_value", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern int get_value(LE_DEVICESTATUS_ID id, IntPtr pResult);

        #endregion
    }
}
