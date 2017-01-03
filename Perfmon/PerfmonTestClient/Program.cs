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
        public void OnNext(EventData e)
        {
            Category category = e.Category;
            DateTime timestamp = e.Timestamp;

            Console.Clear();

            foreach (Counter counter in category.Counters)
            {
                foreach (Instance instance in counter.Instances)
                {
                    Console.WriteLine("{0} \\{1}({2})\\{3} {4}", timestamp, category.Name, instance.Name, counter.Name, instance.Value);
                }
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Callback callback = new Callback();
            NetTcpBinding binding = new NetTcpBinding();
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.Security.Mode = SecurityMode.None;
            string address = "net.tcp://localhost:8080/Perfmon/";
            DuplexChannelFactory<IPerfmonService> factory = new DuplexChannelFactory<IPerfmonService>(callback, binding, address);
            IPerfmonService service = factory.CreateChannel();

            CategoryList categories = service.List();
            categories.Sort((a, b) => a.Name.CompareTo(b.Name));

            /*
            foreach (Category category in categories)
            {
                Console.WriteLine(category.Name);
            }
            */

            service.Subscribe("Processor", "% Processor Time");
            //service.Subscribe("Memory", "Available MBytes");
            Console.ReadLine();

            ((IClientChannel) service).Close();
        }
    }
}
