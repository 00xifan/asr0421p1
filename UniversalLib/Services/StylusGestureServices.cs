using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniversalLib.Core.Monitors;
using UniversalLib.Core.Stylus;

namespace UniversalLib.Services
{
    public interface IStylusGestureServices
    {
        void Init();
        event EventHandler<StylusGestureEventArg> ShowNote;

        event EventHandler<StylusGestureEventArg> Suspension;
        event EventHandler<ushort> LongPressPenUpKey;
    }

    public class StylusGestureServices : IStylusGestureServices
    {
        private readonly IMonitorStatusManagerServices _monitorStatusManagerServices;
        private StylusWrapper _stylusWrapper;
        public event EventHandler<StylusGestureEventArg> GestureCompleted;
        /// <summary>
        /// The gesture operation is in progress.
        /// </summary>
        public event EventHandler<StylusGestureEventArg> GestureDelta;

        public event EventHandler<StylusGestureEventArg> ShowNote;

        public event EventHandler<StylusGestureEventArg> Suspension;

        public event EventHandler<ushort> LongPressPenUpKey;

        public StylusGestureServices(IMonitorStatusManagerServices monitorStatusManagerServices)
        {
            _monitorStatusManagerServices = monitorStatusManagerServices;
        }

        public void Init()
        {
            _stylusWrapper = new StylusWrapper(_monitorStatusManagerServices);
            _stylusWrapper.SwipeDelta += _stylusWrapper_SwipeDelta;
            _stylusWrapper.SwipeCompleted += _stylusWrapper_SwipeCompleted;
            _stylusWrapper.Suspension += _stylusWrapper_Suspension;
            _stylusWrapper.LongPressPenUpKey += _stylusWrapper_LongPressPenUpKey;
        }

        private void _stylusWrapper_LongPressPenUpKey(object sender, ushort e)
        {
            LongPressPenUpKey.Invoke(this, e);
        }

        private void _stylusWrapper_Suspension(object sender, StylusGestureEventArg e)
        {
            Suspension?.Invoke(this, e);
        }

        private void _stylusWrapper_SwipeCompleted(object sender, StylusGestureEventArg e)
        {
            Monitor currentMonitor = _monitorStatusManagerServices.GetMainMonitor();
            if (currentMonitor != null)
            {
                e.currentMonitor = currentMonitor;
                this.GestureCompleted?.Invoke(this, e);
                if (e.Target != SnookerTarget.Inner && e.Target != SnookerTarget.OutOfRadianRange)
                {
                    if (e.FirstDirection != e.Direction)
                    {
                        e.Direction = e.FirstDirection;
                    }

                    if (e.PenDownTime != null)
                    {
                        long t = DateTime.Now.Ticks - e.PenDownTime.Value.Ticks;
                        if (t <= 300 * 10000)//millsecond*10000=ticks
                        {
                            return;
                        }
                    }
                    ShowNote?.Invoke(this, e);
                }
            }
        }
        private void _stylusWrapper_SwipeDelta(object sender, StylusGestureEventArg e)
        {
            var currentMonitor = _monitorStatusManagerServices.RelationHidStylusModules(e.DeviceName);
            if (currentMonitor != null)
            {
                this.GestureDelta?.Invoke(this, e);
            }
        }
    }
}
