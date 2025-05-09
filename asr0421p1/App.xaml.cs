using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using asr0421p1.ASR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using UniversalLib.Common;
using UniversalLib.Core.Monitors;
using UniversalLib.Core.Sensors;
using UniversalLib.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace asr0421p1
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        private IMonitorStatusManagerServices _monitorStatusManagerServices;
        private ISensorStatusManagerServices _sensorStatusManagerServices;
        private IStylusGestureServices _stylusGestureServices;

   
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            InitUnityService();

        }

        private void InitUnityService()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ISensorStatusManagerServices, SensorStatusManagerServices>();
            serviceCollection.AddSingleton<IMonitorStatusManagerServices, MonitorStatusManagerServices>();

            serviceCollection.AddSingleton<IStylusGestureServices, StylusGestureServices>();
            _serviceProvider = serviceCollection.BuildServiceProvider();

            _sensorStatusManagerServices = GetService<ISensorStatusManagerServices>();
            _sensorStatusManagerServices.Init();

            _monitorStatusManagerServices = GetService<IMonitorStatusManagerServices>();
            _monitorStatusManagerServices.Init();
            _stylusGestureServices = GetService<IStylusGestureServices>();
            _stylusGestureServices.Init();
        }



        public T GetService<T>() where T : notnull
        {
            return _serviceProvider.GetRequiredService<T>();
        }
        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            GlobalConstant.Instance.SystemMachineType();
            // 总是创建主窗口
            m_TentwindowC = new ASRWindow(ScreenNameEnum.ScreenC);
            //m_TentwindowC.Statuslock= new TextBlock() {Text="Lenovo" };



            // 双屏模式下创建B屏窗口但默认隐藏
            if (GlobalConstant.Instance.CurrentMachineType == GlobalConstant.MachineType.DualDisplayDevice)
            {
                m_TentwindowC.Activate();
                m_TentwindowB = new ASRWindow(ScreenNameEnum.ScreenB);
                //m_TentwindowB.Statuslock = new TextBlock() { Text = "Geo" };
            }

        }


        public static ASRWindow m_TentwindowB { get; private set; }
        public static ASRWindow m_TentwindowC { get; private set; }

    }
}
