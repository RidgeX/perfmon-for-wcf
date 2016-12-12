using PerfmonServiceLibrary;
using System;
using System.Collections.Generic;
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

                Random random = new Random();
                bool running = true;

                while (running)
                {
                    service.Notify(new EventData()
                    {
                        Time = DateTime.Now,
                        Host = "localhost",
                        Path = @"\Test category\Test counter",
                        Value = random.NextDouble()
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
