using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UniversalLib.Core.Mouse;
using Windows.Foundation;
using Windows.Graphics;
using System.Threading;
using static UniversalLib.Core.Mouse.RawInputMouseReceiverWindow;
using System.Diagnostics;
using UniversalLib.Common;

namespace UniversalLib.Services
{
    public interface IMouseMonitorServices
    {
        void Init();
        event EventHandler<Point> MouseDown;
        event EventHandler<Point> MouseUp;
        event EventHandler<Point> MouseMove;
        event EventHandler<Point> MousePause;
        event EventHandler<Point> MouseLongPress;
        event EventHandler<Point> MouseRightLongPress;
        event EventHandler<short> MouseWheelDeltaStart;
        event EventHandler<short> MouseWheelDeltaEnd;
    }

    public class MouseMonitorServices : IMouseMonitorServices
    {
        public event EventHandler<Point> MouseDown;
        public event EventHandler<Point> MouseUp;
        public event EventHandler<Point> MouseMove;
        public event EventHandler<Point> MousePause;
        public event EventHandler<Point> MouseLongPress;
        public event EventHandler<Point> MouseRightLongPress;

        public event EventHandler<short> MouseWheelDeltaStart;
        public event EventHandler<short> MouseWheelDeltaEnd;
        private RawInputMouseReceiverWindow _mouseReceiverWindow;

        private Point _downPoint;
        private Point _rightDownPoint;
        private Point _position;
        private Win32Helper.POINT  _lpPoint;

        private Timer _mousePauseTimer;
        private readonly object _pauseTimerLock = new object();
        private const int PauseThreshold = 200; // 毫秒

        private bool _isLeftButtonDown = false;
        private bool _isRightButtonDown = false;

        private Timer _mouseLongPressTimer;
        private readonly object _longPressTimerLock = new object();
        private const int LongPressThreshold = 2000; // 长按阈值，单位毫秒
        private const int LongRightPressThreshold = 100; // 毫秒
        private readonly object _longRightPressTimerLock = new object();
        private Stopwatch _longPressStopwatch = new Stopwatch();
        private bool _longPressTriggered = false;
        private bool _longRightPressTriggered = false;

        #region wheelDelta
        short wheelDelta;
        private bool _isMouseWheelDeltaStart = false;
        private Timer _mouseWheelDeltaTimer;
        private readonly object _mouseWheelDeltaTimerLock = new object();
        private const int _mouseWheelDeltaThreshold = 500; // 长按阈值，单位毫秒
        #endregion
        public void Init()
        {
            _mouseReceiverWindow = new RawInputMouseReceiverWindow();
            _mouseReceiverWindow.Input += MouseInput;
            // 初始化计时器，但暂不开始计时
            _mousePauseTimer = new Timer(OnMousePauseElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _mouseLongPressTimer = new Timer(OnMouseLongPressElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _mouseWheelDeltaTimer = new Timer(OnMouseWheelDeltaElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        #region  mouse wheel delta
        private void OnMouseWheelDeltaElapsed(object state)
        {
            _isMouseWheelDeltaStart = false;
            MouseWheelDeltaEnd?.Invoke(this, wheelDelta);
        }

        private void RestartMouseWheelDeltaTimer()
        {
            lock (_mouseWheelDeltaTimerLock)
            {
                _mouseWheelDeltaTimer.Change(_mouseWheelDeltaThreshold, Timeout.Infinite);
            }
        }
        #endregion
        private void RestartPauseTimer()
        {
            lock (_pauseTimerLock)
            {
                // 重置计时器的触发时间
                _mousePauseTimer.Change(PauseThreshold, Timeout.Infinite);
            }
        }
        private bool isMousePauseMove = false;
        private void RestartLongPressTimer()
        {
            lock (_longPressTimerLock)
            {
                _mouseLongPressTimer.Change(LongPressThreshold, Timeout.Infinite);
            }
        }

        //
        private void MouseInput(Rawmouse rawmouse)
        {
            if (rawmouse.usButtonFlags == 0)
            {
                if (rawmouse.lLastX != 0 || rawmouse.lLastY != 0)
                {
                    RestartPauseTimer();
                    GetCursorPosition();
                    //MouseMove?.Invoke(this, _position);
                    double distance = Math.Sqrt(Math.Pow(_position.X - _downPoint.X, 2) + Math.Pow(_position.Y - _downPoint.Y, 2));

                    if (distance > 20)
                    {
                        isMousePauseMove = true;
                    }

                    if (_isRightButtonDown)
                    {
                        double rightDistance = Math.Sqrt(Math.Pow(_position.X - _rightDownPoint.X, 2) + Math.Pow(_position.Y - _rightDownPoint.Y, 2));
                        if (rightDistance > 5)
                        {
                            OnMouseRightLongPressElapsed();
                        }
                    }
                }
            }
            else
            {
                if ((rawmouse.usButtonFlags & 0x0001) != 0)
                {      // 鼠标左键按下
                    GetCursorPosition();
                    _downPoint.X = _position.X;
                    _downPoint.Y = _position.Y;
                    _isLeftButtonDown = true;
                    _longPressTriggered = false;
                    _longPressStopwatch.Restart();
                    MouseDown?.Invoke(this, _position);
                    RestartLongPressTimer();
                }
                if ((rawmouse.usButtonFlags & 0x0002) != 0)
                {
                    //鼠标左键抬起
                    GetCursorPosition();
                    _isLeftButtonDown = false;
                    _longPressStopwatch.Stop();
                    if (!_longPressTriggered)
                    {
                        // 未触发长按，可以处理单击事件
                    }
                    isMousePauseMove = false;
                    MouseUp?.Invoke(this, _position);
                }
                if ((rawmouse.usButtonFlags & 0x0004) != 0)
                {
                    GetCursorPosition();
                    _rightDownPoint.X = _position.X;
                    _rightDownPoint.Y = _position.Y;
                    //右键按下
                    _isRightButtonDown = true;
                    _longRightPressTriggered = false;
                }
                if ((rawmouse.usButtonFlags & 0x0008) != 0)
                {
                    //右键抬起
                    _isRightButtonDown = false;
                }
                // roll event
                if ((rawmouse.usButtonFlags & 0x0400) != 0)
                {
                    wheelDelta = rawmouse.usButtonData;
                    if (!_isMouseWheelDeltaStart)
                    {
                        MouseWheelDeltaStart?.Invoke(this, wheelDelta);
                        _isMouseWheelDeltaStart = true;
                    }
                    RestartMouseWheelDeltaTimer();
                }
            }
        }

        private void CheckLongPress()
        {
            if (_isLeftButtonDown && !_longPressTriggered && isMousePauseMove && _longPressStopwatch.ElapsedMilliseconds >= LongPressThreshold)
            {
                _longPressTriggered = true;
                // 切换到 UI 线程
                MouseLongPress?.Invoke(this, _position);
            }
        }



        private void OnMousePauseElapsed(object state)
        {
            MousePause?.Invoke(this, _position);
        }
        private void OnMouseRightLongPressElapsed()
        {
            if (_isRightButtonDown && !_longRightPressTriggered)
            {
                _longRightPressTriggered = true;
                MouseRightLongPress?.Invoke(this, _position);
            }
        }

        private void OnMouseLongPressElapsed(object state)
        {
            CheckLongPress();
        }


        private void GetCursorPosition()
        {
            Win32Helper.GetCursorPos(out _lpPoint);
            _position.X = _lpPoint.X;
            _position.Y = _lpPoint.Y;
        }

    }
}
