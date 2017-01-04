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
                list = new List<IPerfmonCallback>();
                list.Add(callback);
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
            RemoveClient(categoryName, counterName, callback);
        }

        public void RemoveClient(string categoryName, string counterName, IPerfmonCallback callback)
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

                            CounterSample prevSample;
                            if (!prevSamples.TryGetValue(path, out prevSample))
                            {
                                prevSample = CounterSample.Empty;
                            }
                            CounterSample sample = id.Sample;
                            float value = CounterSample.Calculate(prevSample, sample);
                            prevSamples[path] = sample;

                            if (timestamp == null)
                            {
                                timestamp = DateTime.FromFileTime(sample.TimeStamp100nSec);
                            }

                            instances.Add(new Instance() { Name = instanceName, Value = value });
                        }

                        if (instances.Count == 0) continue;

                        Counter counter = new Counter() { Name = counterName, Instances = instances };
                        Category category = new Category() { Name = categoryName, Counters = new List<Counter>() { counter } };
                        EventData e = new EventData() { Category = category, Timestamp = timestamp.Value };

                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            Parallel.ForEach(list, subscriber =>
                            {
                                var channel = (IClientChannel) subscriber;

                                if (channel.State == CommunicationState.Opened)
                                {
                                    subscriber.OnNext(e);
                                }
                                else if (channel.State == CommunicationState.Closed || channel.State == CommunicationState.Faulted)
                                {
                                    RemoveClient(categoryName, counterName, subscriber);
                                }
                            });
                        });
                    }
                }
            }
        }
    }
}
