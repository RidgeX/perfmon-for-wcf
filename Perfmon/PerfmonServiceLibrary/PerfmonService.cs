using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerfmonServiceLibrary
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class PerfmonService : IPerfmonService
    {
        private static readonly object _lock;
        private static readonly Dictionary<string, CounterSample> prevSamples;
        private static readonly Dictionary<string, List<IPerfmonCallback>> subscribers;

        static PerfmonService()
        {
            _lock = new object();
            prevSamples = new Dictionary<string, CounterSample>();
            subscribers = new Dictionary<string, List<IPerfmonCallback>>();
        }

        public void Notify(EventData e)
        {
            lock (_lock)
            {
                List<IPerfmonCallback> list;
                if (subscribers.TryGetValue(e.Path, out list))
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Parallel.ForEach(list, subscriber =>
                        {
                            subscriber.OnNotify(e);
                        });
                    });
                }
            }
        }

        public void Subscribe(string path)
        {
            IPerfmonCallback callback = OperationContext.Current.GetCallbackChannel<IPerfmonCallback>();

            lock (_lock)
            {
                List<IPerfmonCallback> list;
                if (subscribers.TryGetValue(path, out list))
                {
                    if (!list.Contains(callback))
                    {
                        list.Add(callback);
                    }
                }
                else
                {
                    list = new List<IPerfmonCallback>();
                    list.Add(callback);
                    subscribers.Add(path, list);
                }
            }
        }

        public void Unsubscribe(string path)
        {
            IPerfmonCallback callback = OperationContext.Current.GetCallbackChannel<IPerfmonCallback>();

            lock (_lock)
            {
                List<IPerfmonCallback> list;
                if (subscribers.TryGetValue(path, out list))
                {
                    list.Remove(callback);
                }
            }
        }

        public void Update()
        {
            PerformanceCounterCategory pcc = new PerformanceCounterCategory("Processor");
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
                    Notify(e);
                }
            }
        }
    }
}
