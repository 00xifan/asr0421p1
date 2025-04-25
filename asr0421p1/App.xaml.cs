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
using UniversalLib.Core.Monitors;
using UniversalLib.Core.Sensors;
using UniversalLib.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
            _serviceProvider = serviceCollection.BuildServiceProvider();

            _sensorStatusManagerServices = GetService<ISensorStatusManagerServices>();
            _sensorStatusManagerServices.Init();

            _monitorStatusManagerServices = GetService<IMonitorStatusManagerServices>();
            _monitorStatusManagerServices.Init();

            _sensorStatusManagerServices.FormChangedAction += Sensor_FromChangedActioned;
        }

        private void Sensor_FromChangedActioned(SENSOR_FORM fORM)
        {
            if (fORM == SENSOR_FORM.FF_TENT)
            {
                ShowWindowsOnTentMode();
             
            }
            else
            {
                ShowSingleWindow();
            }
        }

        private void ShowWindowsOnTentMode()
        {
            var monitorB = _monitorStatusManagerServices.GetMonitor(ScreenNameEnum.ScreenB);
            if (monitorB != null)
            {
                m_windowB = CreateAndActivateWindow();
            }

            var monitorC = _monitorStatusManagerServices.GetMonitor(ScreenNameEnum.ScreenC);
            if (monitorC != null)
            {
                m_windowC = CreateAndActivateWindow();
            }
        }

        private Window CreateAndActivateWindow()
        {
            var window = new ASRWindow();
            window.Activate();
            return window;
        }


        private void ShowSingleWindow()
        {
            m_window = CreateAndActivateWindow();
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
            SENSOR_FORM currentFormStatus = _sensorStatusManagerServices.GetComputerMode();
           
            if (currentFormStatus == SENSOR_FORM.FF_TENT)
            {
                ShowWindowsOnTentMode();
            }
            else
            {
                ShowSingleWindow();
            }
        }

        private Window? m_window;
        private Window? m_windowB;
        private Window? m_windowC;


    }
}
