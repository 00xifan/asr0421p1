using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniversalLib.Common;
using UniversalLib.Core.Monitors;
using UniversalLib.Services;
using static UniversalLib.Core.Stylus.User32;
using Monitor = UniversalLib.Core.Monitors.Monitor;

namespace UniversalLib.Core.Stylus
{
    #region args
    public class StylusGestureEventArg
    {
        public readonly SnookerDirection FirstDirection;
        public SnookerDirection Direction;
        public readonly SnookerTarget Target;
        public readonly PenStatus CurrentPenStatus;
        public readonly string DeviceName;
        public Monitor currentMonitor;
        public DateTime? PenDownTime;

        public StylusGestureEventArg(SnookerDirection firstDirection, SnookerDirection direction, SnookerTarget target, PenStatus status, string deviceName, Monitor monitor)
        {
            this.FirstDirection = firstDirection;
            this.Direction = direction;
            this.Target = target;
            this.CurrentPenStatus = status;
            DeviceName = deviceName;
            this.currentMonitor = monitor;
        }

        public override string ToString()
        {
            return String.Format("Direction:{0} \r\n Target:{1} \r\n hidName:{2},PenDownTime:{3}", Direction, Target, DeviceName, PenDownTime);
        }
    }




    #endregion
    public class StylusWrapper
    {
        public const uint RIDEV_INPUTSINK = 0x00000100;
        public const uint RID_INPUT = 0x10000003;
        public const uint RIM_TYPEHID = 2;
        private readonly IMonitorStatusManagerServices _monitorStatusManagerServices;
        private LowPowerWindow _lowPowerWindow;


        public DateTime? PenDownTime;
        bool IsStatringSuspension;
        bool IsStatringPressDown;
        SnookerTarget DeltaTarget = SnookerTarget.None;

        private SnookerDirection firstDeltaDirection = SnookerDirection.None;
        private PenStatus preDeltaDirection = PenStatus.Leave;

        public event EventHandler<StylusGestureEventArg> SwipeStarted;
        public event EventHandler<StylusGestureEventArg> SwipeDelta;
        public event EventHandler<StylusGestureEventArg> SwipeCompleted;
        public event EventHandler<StylusGestureEventArg> Suspension;
        public event EventHandler<ushort> LongPressPenUpKey;
        bool isSuspension = false;


        public StylusWrapper(IMonitorStatusManagerServices monitorStatusManagerServices)
        {
            _monitorStatusManagerServices = monitorStatusManagerServices;
            _lowPowerWindow = new LowPowerWindow();
            _lowPowerWindow.PenInputRecived += _lowPowerWindow_PenInputRecived;
        }
        private static readonly Stopwatch _stopwatch = new Stopwatch();
        private static readonly object _lockObj = new object();
        private static bool _actionTriggered = false;
        public void HandlePenStatus(PenInputEvent pen)
        {
            lock (_lockObj)
            {
                LogsHelper.Instance.LogToUIMsgAction?.Invoke($"HandlePenStatus:{pen.PenStatus}");
                PenStatus currentStatus = pen.PenStatus;
                if ((currentStatus & (PenStatus.UPKEY | PenStatus.Suspension)) == (PenStatus.UPKEY | PenStatus.Suspension))
                {
                    if (!_stopwatch.IsRunning)
                    {
                        _stopwatch.Start();
                        _actionTriggered = false;
                    }
                    else
                    {
                        if (_stopwatch.ElapsedMilliseconds >= 100 && !_actionTriggered)
                        {
                            LongPressPenUpKey?.Invoke(this, pen.X);
                            _actionTriggered = true;
                        }
                    }
                }
                else
                {
                    // 重置计时器
                    if (_stopwatch.IsRunning)
                    {
                        _stopwatch.Stop();
                        _actionTriggered = false;
                    }
                }
            }
        }

        private void _lowPowerWindow_PenInputRecived(RawPenInputEventArg arg)
        {
            PenStatus status = arg.PenInputEvent.PenStatus;
            HandlePenStatus(arg.PenInputEvent);
            double pointX = arg.PenInputEvent.X;
            double pointY = arg.PenInputEvent.Y;
            if (status == PenStatus.Suspension)
            {
                isSuspension = true;
            }
            else if (status == PenStatus.Leave)
            {
                isSuspension = false;
            }
            Monitor monitor = _monitorStatusManagerServices.RelationHidStylusModules(arg.PenInputEvent.DeviceName);

            if (monitor == null)
            {
                monitor = _monitorStatusManagerServices.GetMainMonitor();
            }
            if (monitor == null)
            {
                LogsHelper.Instance.DebugWrite("PenInputRecived GetMainMonitor is null");
                return;
            }

            SnookerDirection direction = monitor.Positioning(pointX, pointY);
            SnookerTarget target = monitor.Contains(pointX, pointY, direction);
            if (target == SnookerTarget.Inner)
            {
                LogsHelper.Instance.DebugWrite($"PenInputRecived::GetMainMonitor is {monitor.ScreenName}");
            }
            StylusGestureEventArg args = new StylusGestureEventArg(firstDeltaDirection, direction, target, status, arg.PenInputEvent.DeviceName, monitor);
            if (PenDownTime.HasValue)
            {
                args.PenDownTime = PenDownTime.Value;
            }

            if (status == PenStatus.Suspension || status == PenStatus.Leave)
            {
                Suspension?.Invoke(this, args);
                preDeltaDirection = PenStatus.Leave;
            }
            switch (status)
            {
                case PenStatus.Suspension:
                    {
                        if (this.IsStatringPressDown == false)
                        {
                            this.Statring(args);
                        }
                        else
                        {
                            this.Completed(args);
                        }
                    }
                    break;
                case PenStatus.PressDown:
                    {
                        if (!PenDownTime.HasValue)
                        {
                            PenDownTime = DateTime.Now;
                        }
                        if (this.IsStatringPressDown == false)
                        {
                            this.Started(args);
                        }
                        else
                        {
                            bool isContains = Contains(this.DeltaTarget, args.Target);
                            if (isContains)
                                this.Delta(args);
                        }
                        preDeltaDirection = PenStatus.PressDown;
                    }
                    break;
                case PenStatus.Leave:
                    {
                        this.Completed(args);
                        PenDownTime = null;
                    }
                    break;
                default:
                    this.Completed(args);
                    break;
            }
        }
        private void Statring(StylusGestureEventArg args)
        {
            switch (args.Target)
            {
                case SnookerTarget.Inner:
                    this.IsStatringSuspension = true;
                    break;
                default:
                    break;
            }
        }

        private void Started(StylusGestureEventArg args)
        {
            switch (args.Target)
            {
                case SnookerTarget.Inner:
                    {
                        if (preDeltaDirection != PenStatus.PressDown)
                        {
                            this.firstDeltaDirection = args.Direction;
                            this.IsStatringPressDown = true;
                            this.DeltaTarget = SnookerTarget.Inner;
                            this.SwipeStarted?.Invoke(this, args); // Delegate
                        }
                    }
                    break;
                default:
                    break;
            }
        }
        private void Delta(StylusGestureEventArg args)
        {
            this.DeltaTarget = args.Target;
            this.SwipeDelta?.Invoke(this, args); // Delegate
        }

        private void Completed(StylusGestureEventArg args)
        {
            if (this.IsStatringSuspension)
            {
                this.IsStatringSuspension = false;
            }
            if (this.IsStatringPressDown)
            {
                this.firstDeltaDirection = SnookerDirection.None;
                this.IsStatringPressDown = false;
                this.DeltaTarget = args.Target;
                this.SwipeCompleted?.Invoke(this, args); // Delegate
            }
        }

        private static bool Contains(SnookerTarget preview, SnookerTarget current)
        {
            switch (current)
            {
                case SnookerTarget.None:
                    switch (preview)
                    {
                        case SnookerTarget.None:
                        case SnookerTarget.Outer:
                            return true;
                        default:
                            return false;
                    }
                case SnookerTarget.Inner:
                    switch (preview)
                    {
                        case SnookerTarget.Inner:
                            return true;
                        default:
                            return false;
                    }
                case SnookerTarget.Outer:
                    switch (preview)
                    {
                        case SnookerTarget.Inner:
                        case SnookerTarget.Outer:
                            return true;
                        default:
                            return false;
                    }
                case SnookerTarget.OutOfRadianRange:
                    return false;
                default:
                    return false;
            }
        }
    }
}