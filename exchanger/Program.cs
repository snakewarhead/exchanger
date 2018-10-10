using exchanger.Controller;
using exchanger.Util;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Threading;

namespace exchanger
{
    static class Program
    {
        private static ExchangeController exchangeController;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (!Utils.IsSupportedRuntimeVersion())
            {
                MessageBox.Show("需要.NET框架, 请升级到 4.6.2 以上",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Process.Start(
                    "http://dotnetsocial.cloudapp.net/GetDotnet?tfm=.NETFramework,Version=v4.6.2");
                return;
            }

            Utils.ReleaseMemory(true);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            // handle UI exceptions
            Application.ThreadException += Application_ThreadException;
            // handle non-UI exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.ApplicationExit += Application_ApplicationExit;
            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Directory.SetCurrentDirectory(Application.StartupPath);
            Logging.OpenLogFile();

            exchangeController = new ExchangeController();
            exchangeController.Start();

            Application.Run();
        }

        private static int exited = 0;
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref exited) == 1)
            {
                string errMsg = e.ExceptionObject.ToString();
                Logging.Error(errMsg);
                MessageBox.Show(
                    $"Unexpected error{Environment.NewLine}{errMsg}",
                    "non-UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (Interlocked.Increment(ref exited) == 1)
            {
                string errorMsg = $"Exception Detail: {Environment.NewLine}{e.Exception}";
                Logging.Error(errorMsg);
                MessageBox.Show(
                    $"Unexpected error{Environment.NewLine}{errorMsg}",
                    "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private static void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Logging.Info("os wake up");
                    if (exchangeController != null)
                    {
                        System.Threading.Tasks.Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(10 * 1000);
                            try
                            {
                                exchangeController.Start();
                                Logging.Info("controller started");
                            }
                            catch (Exception ex)
                            {
                                Logging.LogUsefulException(ex);
                            }
                        });
                    }
                    break;
                case PowerModes.Suspend:
                    if (exchangeController != null)
                    {
                        exchangeController.Stop();
                        Logging.Info("controller stopped");
                    }
                    Logging.Info("os suspend");
                    break;
            }
        }

        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            // detach static event handlers
            Application.ApplicationExit -= Application_ApplicationExit;
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            Application.ThreadException -= Application_ThreadException;
            if (exchangeController != null)
            {
                exchangeController.Stop();
                exchangeController = null;
            }
        }

    }
}
