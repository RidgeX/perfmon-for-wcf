using PerfmonServiceLibrary;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PerfmonServiceWindows
{
    public class Service : ServiceBase
    {
        private PerfmonService service;
        private ServiceHost serviceHost;
        private Timer timer;

        public Service()
        {
            base.ServiceName = "PerfmonService";
        }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            if (Environment.UserInteractive)
            {
                string param = string.Concat(args);

                switch (param)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;

                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                }
            }
            else
            {
                ServiceBase.Run(new Service());
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception) e.ExceptionObject;

            try
            {
                File.AppendAllText("error.log", ex.Message + Environment.NewLine);
            }
            catch (Exception) { }
        }

        protected override void OnStart(string[] args)
        {
            service = new PerfmonService();

            if (serviceHost != null)
            {
                serviceHost.Close();
            }

            serviceHost = new ServiceHost(service);
            serviceHost.Open();

            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
            }

            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += (s, e) => service.Update();
            timer.Start();
        }

        protected override void OnStop()
        {
            if (timer != null)
            {
                timer.Stop();
                timer.Dispose();
                timer = null;
            }

            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }
        }
    }
}
