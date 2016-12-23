using PerfmonServiceLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PerfmonTestClient
{
    public class Callback : IPerfmonCallback
    {
        public void OnNotify(EventData e)
        {
            Console.WriteLine("{0} {1} {2}", e.DateTime, e.Path, e.Value);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Callback callback = new Callback();
            WSDualHttpBinding binding = new WSDualHttpBinding();
            string address = "http://localhost:8080/Perfmon/";
            DuplexChannelFactory<IPerfmonService> factory = new DuplexChannelFactory<IPerfmonService>(callback, binding, address);
            IPerfmonService service = factory.CreateChannel();

            service.Subscribe(@"\Processor(_Total)\% Processor Time");
            Console.ReadLine();
        }
    }
}
