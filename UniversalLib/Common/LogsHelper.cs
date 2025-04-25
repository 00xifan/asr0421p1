// Copyright (c) 2025-present Lenovo.  All rights reserverd
// Confidential and Restricted
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace UniversalLib.Common
{
    public class LogsHelper
    {
        private static readonly Lazy<LogsHelper> _instanceLock = new Lazy<LogsHelper>(() => new LogsHelper());
        private TraceSwitch _ts = new TraceSwitch("OutPutLogs", Guid.NewGuid().ToString());
        private string _registryKeyPath = @"SOFTWARE\Lenovo\Universal";
        private string createTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        public Guid LogsCurrentGuid = Guid.NewGuid();
        public Action<string> LogToUIMsgAction;
        public static LogsHelper Instance
        {
            get
            {
                return _instanceLock.Value;
            }
        }
        private readonly object _lock = new object();
        private LogsHelper()
        {
            lock (_lock)
            {
                LogsInit();
            }

        }
        private void LogsInit()
        {
            //Do you want to write a log
            var isWriteLog = GetRegistryInfo();
            LogStateChanged(isWriteLog);
        }
        private void LogStateChanged(bool isWriteLog)
        {
            if (isWriteLog)
            {
                try
                {
                    var processId = Process.GetCurrentProcess().Id;
                    string tempPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Path.Combine("Lenovo", "Universal", "Logs"));
                    var tempName = Process.GetCurrentProcess().MainModule.ModuleName;
                    tempName = tempName.Substring(0, tempName.LastIndexOf("."));
                    string tempAppNamePath = Path.Combine(tempPath, tempName);
                    if (!Directory.Exists(tempAppNamePath))
                    {
                        Directory.CreateDirectory(tempAppNamePath);
                    }
                    tempName = tempName + processId.ToString() + "time" + createTime + ".txt";

                    var logAllName = Path.Combine(tempAppNamePath, tempName);
                    FileStream traceStream;
                    try
                    {
                        traceStream = new FileStream(logAllName, FileMode.Append, FileAccess.Write);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error creating FileStream for trace file \"{0}\":" +
                  "\r\n{1}", logAllName, ex.Message);
                        return;
                    }
                    Trace.Listeners.Clear();
                    Trace.Listeners.Add(new TextWriterTraceListener(traceStream));
                    _ts.Level = TraceLevel.Info;
                    Trace.AutoFlush = true;
                }
                catch (System.Exception ex)
                {
                    _ts.Level = TraceLevel.Off;
                    Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                _ts.Level = TraceLevel.Off;
            }
        }
        private bool GetRegistryInfo()
        {
            bool result = false;
            RegistryKey hive = Registry.LocalMachine;
            RegistryKey keyToMoniotr = hive.OpenSubKey(_registryKeyPath);
            try
            {
                if (keyToMoniotr != null)
                {
                    var isWrite = keyToMoniotr.GetValue("LogFile")?.ToString();
                    if (isWrite != null && isWrite.Equals("1"))
                    {
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                keyToMoniotr?.Close();
                keyToMoniotr?.Dispose();
                hive?.Close();
                hive?.Dispose();
            }
            return result;
        }

        public void DebugWrite(string log)
        {
            if (_ts.TraceInfo)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"));
                sb.Append(":[Debug]: ");

                sb.Append(log);
                Trace.WriteLineIf(_ts.TraceInfo, sb);
            }
        }

        public void ErrorWrite(string log)
        {
            if (_ts.TraceError)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff"));
                sb.Append(":[Error]: ");
                sb.Append(log);
                Trace.WriteLineIf(_ts.TraceError, sb);
            }
        }

    }
}
