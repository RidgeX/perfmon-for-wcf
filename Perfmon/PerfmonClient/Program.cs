using PerfmonClient.PerfmonService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace PerfmonClient
{
    public class Callback : IPerfmonServiceCallback
    {
        public void OnNotify(EventData e)
        {
            Console.WriteLine("{0} {1} {2} {3}", e.Time, e.Host, e.Path, e.Value);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Callback callback = new Callback();
            InstanceContext context = new InstanceContext(callback);
            PerfmonServiceClient client = new PerfmonServiceClient(context);

            client.Subscribe(@"\Test category\Test counter");
            Console.ReadLine();
        }
    }
}
