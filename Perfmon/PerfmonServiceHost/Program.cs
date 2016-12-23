using PerfmonServiceLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerfmonServiceHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            PerfmonService service = new PerfmonService();
            ServiceHost selfHost = new ServiceHost(service);

            try
            {
                selfHost.Open();

                PerformanceCounter counter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                bool running = true;

                while (running)
                {
                    service.Notify(new EventData()
                    {
                        DateTime = DateTime.Now,
                        Path = @"\Processor(_Total)\% Processor Time",
                        Value = counter.NextValue()
                    });
                    Thread.Sleep(1000);
                }

                selfHost.Close();
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("An exception occurred: {0}", ce.Message);
                selfHost.Abort();
            }
        }
    }
}
