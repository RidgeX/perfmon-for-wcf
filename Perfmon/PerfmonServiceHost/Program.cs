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

                PerformanceCounterCategory pcc = new PerformanceCounterCategory("Processor");
                Dictionary<string, CounterSample> prevSamples = new Dictionary<string, CounterSample>();
                bool running = true;

                while (running)
                {
                    InstanceDataCollectionCollection idcc = pcc.ReadCategory();

                    foreach (InstanceDataCollection idc in idcc.Values)
                    {
                        if (!idc.CounterName.Equals("% Processor Time")) continue;

                        foreach (InstanceData id in idc.Values)
                        {
                            string path = string.Format(@"\{0}({1})\{2}", pcc.CategoryName, id.InstanceName, idc.CounterName);

                            CounterSample prevSample;
                            if (!prevSamples.TryGetValue(path, out prevSample))
                            {
                                prevSample = CounterSample.Empty;
                            }
                            CounterSample sample = id.Sample;
                            float value = CounterSample.Calculate(prevSample, sample);
                            prevSamples[path] = sample;

                            DateTime dateTime = DateTime.FromFileTime(sample.TimeStamp100nSec);
                            EventData e = new EventData() { DateTime = dateTime, Path = path, Value = value };
                            service.Notify(e);
                        }
                    }

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
