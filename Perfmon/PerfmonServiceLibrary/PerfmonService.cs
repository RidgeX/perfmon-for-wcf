using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerfmonServiceLibrary
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    public class PerfmonService : IPerfmonService
    {
        private static readonly string[] allowedCategories =
        {
            "Memory",
            "Processor",
            "ServiceModelEndpoint 4.0.0.0",
            "ServiceModelOperation 4.0.0.0",
            "ServiceModelService 4.0.0.0"
        };

        private static readonly CategoryList categories;
        private static readonly Dictionary<string, CounterSample> prevSamples;
        private static readonly Dictionary<Tuple<string, string>, List<IPerfmonCallback>> subscribers;

        static PerfmonService()
        {
            categories = new CategoryList();
            prevSamples = new Dictionary<string, CounterSample>();
            subscribers = new Dictionary<Tuple<string, string>, List<IPerfmonCallback>>();
        }

        public void Join()
        {
            #if DEBUG
            MessageProperties properties = OperationContext.Current.IncomingMessageProperties;
            var endpoint = (RemoteEndpointMessageProperty) properties[RemoteEndpointMessageProperty.Name];
            Console.WriteLine("{0}:{1} has joined", endpoint.Address, endpoint.Port);
            #endif

            OperationContext.Current.Channel.Faulted += OnFault;
        }

        public void Leave()
        {
            #if DEBUG
            MessageProperties properties = OperationContext.Current.IncomingMessageProperties;
            var endpoint = (RemoteEndpointMessageProperty) properties[RemoteEndpointMessageProperty.Name];
            Console.WriteLine("{0}:{1} has left", endpoint.Address, endpoint.Port);
            #endif

            IPerfmonCallback callback = OperationContext.Current.GetCallbackChannel<IPerfmonCallback>();
            RemoveCallbacks(callback);
        }

        private void OnFault(object sender, EventArgs e)
        {
            var callback = (IPerfmonCallback) sender;
            RemoveCallbacks(callback);
        }

        public CategoryList List()
        {
            #if DEBUG
            Console.WriteLine("List()");
            #endif

            if (!categories.Any())
            {
                foreach (var category in PerformanceCounterCategory.GetCategories())
                {
                    if (!allowedCategories.Contains(category.CategoryName)) continue;

                    List<Counter> counters = new List<Counter>();

                    foreach (var counter in category.GetCounters(string.Empty))
                    {
                        counters.Add(new Counter() { Name = counter.CounterName });
                    }

                    categories.Add(new Category() { Name = category.CategoryName, Counters = counters });
                }
            }

            return categories;
        }

        public void Refresh()
        {
            categories.Clear();
        }

        public bool Subscribe(string categoryName, string counterName)
        {
            #if DEBUG
            Console.WriteLine("Subscribe({0}, {1})", categoryName, counterName);
            #endif

            if (!allowedCategories.Contains(categoryName))
            {
                return false;
            }

            try
            {
                if (!PerformanceCounterCategory.CounterExists(counterName, categoryName))
                {
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                // Category does not exist
                return false;
            }

            IPerfmonCallback callback = OperationContext.Current.GetCallbackChannel<IPerfmonCallback>();
            Tuple<string, string> tuple = Tuple.Create(categoryName, counterName);

            List<IPerfmonCallback> list;
            if (subscribers.TryGetValue(tuple, out list))
            {
                if (!list.Contains(callback))
                {
                    list.Add(callback);
                }
            }
            else
            {
                list = new List<IPerfmonCallback>() { callback };
                subscribers.Add(tuple, list);
            }

            return true;
        }

        public void Unsubscribe(string categoryName, string counterName)
        {
            #if DEBUG
            Console.WriteLine("Unsubscribe({0}, {1})", categoryName, counterName);
            #endif

            IPerfmonCallback callback = OperationContext.Current.GetCallbackChannel<IPerfmonCallback>();
            RemoveCallback(categoryName, counterName, callback);
        }

        public void RemoveCallback(string categoryName, string counterName, IPerfmonCallback callback)
        {
            Tuple<string, string> tuple = Tuple.Create(categoryName, counterName);

            List<IPerfmonCallback> list;
            if (subscribers.TryGetValue(tuple, out list))
            {
                list.Remove(callback);

                if (!list.Any())
                {
                    subscribers.Remove(tuple);
                }
            }
        }

        public void RemoveCallbacks(IPerfmonCallback callback)
        {
            foreach (var kvp in subscribers.ToList())
            {
                Tuple<string, string> tuple = kvp.Key;
                List<IPerfmonCallback> list = kvp.Value;

                list.Remove(callback);

                if (!list.Any())
                {
                    subscribers.Remove(tuple);
                }
            }
        }

        public void Update()
        {
            foreach (string categoryName in subscribers.Select(kvp => kvp.Key.Item1).Distinct())
            {
                PerformanceCounterCategory pcc = new PerformanceCounterCategory(categoryName);

                #if DEBUG
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                #endif

                InstanceDataCollectionCollection idcc = pcc.ReadCategory();

                #if DEBUG
                stopwatch.Stop();
                Console.WriteLine("{0:f4} ms\t{1}", stopwatch.Elapsed.TotalMilliseconds, categoryName);
                #endif

                foreach (InstanceDataCollection idc in idcc.Values)
                {
                    string counterName = idc.CounterName;
                    Tuple<string, string> tuple = Tuple.Create(categoryName, counterName);

                    List<IPerfmonCallback> list;
                    if (subscribers.TryGetValue(tuple, out list))
                    {
                        List<Instance> instances = new List<Instance>();
                        DateTime? timestamp = null;

                        foreach (InstanceData id in idc.Values)
                        {
                            string instanceName = (pcc.CategoryType == PerformanceCounterCategoryType.MultiInstance ? id.InstanceName : "*");
                            string path = string.Format(@"\{0}({1})\{2}", categoryName, instanceName, counterName);

                            CounterSample sample = id.Sample;

                            if (timestamp == null)
                            {
                                timestamp = DateTime.FromFileTime(sample.TimeStamp100nSec);
                            }

                            CounterSample prevSample;
                            if (prevSamples.TryGetValue(path, out prevSample))
                            {
                                float value = CounterSample.Calculate(prevSample, sample);
                                instances.Add(new Instance() { Name = instanceName, Value = value });
                            }
                            prevSamples[path] = sample;
                        }

                        if (instances.Count == 0) continue;

                        Counter counter = new Counter() { Name = counterName, Instances = instances };
                        Category category = new Category() { Name = categoryName, Counters = new List<Counter>() { counter } };
                        EventData e = new EventData() { Category = category, Timestamp = timestamp.Value };

                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            Parallel.ForEach(list, subscriber =>
                            {
                                try
                                {
                                    subscriber.OnNext(e);
                                }
                                catch (CommunicationException) { }
                            });
                        });
                    }
                }
            }
        }
    }
}
