using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PerfmonServiceWindows
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller process;
        private ServiceInstaller service;

        public ProjectInstaller()
        {
            process = new ServiceProcessInstaller();
            service = new ServiceInstaller();

            process.Account = ServiceAccount.LocalSystem;

            service.ServiceName = "PerfmonService";
            service.DisplayName = "Performance Monitor for WCF Service";
            service.StartType = ServiceStartMode.Manual;

            base.Installers.Add(process);
            base.Installers.Add(service);
        }
    }
}
