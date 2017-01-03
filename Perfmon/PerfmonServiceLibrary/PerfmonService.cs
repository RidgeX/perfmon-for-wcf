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
        private static readonly HashSet<string> activeCategories;
        private static readonly object _lock;
        private static readonly Dictionary<string, CounterSample> prevSamples;
        private static readonly Dictionary<string, List<IPerfmonCallback>> subscribers;

        static PerfmonService()
        {
            activeCategories = new HashSet<string>();
            _lock = new object();
            prevSamples = new Dictionary<string, CounterSample>();
            subscribers = new Dictionary<string, List<IPerfmonCallback>>();
        }

        public CategoryList List()
        {
            CategoryList categories = new CategoryList();

            foreach (var category in PerformanceCounterCategory.GetCategories())
            {
                List<Counter> counters = new List<Counter>();

                foreach (var counter in category.GetCounters(string.Empty))
                {
                    counters.Add(new Counter() { Name = counter.CounterName });
                }

                categories.Add(new Category() { Name = category.CategoryName, Counters = counters });
            }

            return categories;
        }

        public bool Subscribe(string categoryName, string counterName)
        {
            try
            {
                if (!PerformanceCounterCategory.CounterExists(counterName, categoryName))
                {
                    // Counter does not exist
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                // Category does not exist
                return false;
            }

            IPerfmonCallback callback = OperationContext.Current.GetCallbackChannel<IPerfmonCallback>();
            string key = string.Format(@"\{0}\{1}", categoryName, counterName);

            lock (_lock)
            {
                List<IPerfmonCallback> list;
                if (subscribers.TryGetValue(key, out list))
                {
                    if (!list.Contains(callback))
                    {
                        list.Add(callback);
                        activeCategories.Add(categoryName);
                    }
                }
                else
                {
                    list = new List<IPerfmonCallback>();
                    list.Add(callback);
                    subscribers.Add(key, list);
                    activeCategories.Add(categoryName);
                }
            }

            return true;
        }

        public void Unsubscribe(string categoryName, string counterName)
        {
            IPerfmonCallback callback = OperationContext.Current.GetCallbackChannel<IPerfmonCallback>();
            string key = string.Format(@"\{0}\{1}", categoryName, counterName);

            lock (_lock)
            {
                List<IPerfmonCallback> list;
                if (subscribers.TryGetValue(key, out list))
                {
                    list.Remove(callback);

                    if (!list.Any())
                    {
                        activeCategories.Remove(categoryName);
                        subscribers.Remove(key);
                    }
                }
            }
        }

        public void Update()
        {
            lock (_lock)
            {
                foreach (string categoryName in activeCategories)
                {
                    PerformanceCounterCategory pcc = new PerformanceCounterCategory(categoryName);
                    InstanceDataCollectionCollection idcc = pcc.ReadCategory();

                    foreach (InstanceDataCollection idc in idcc.Values)
                    {
                        string counterName = idc.CounterName;
                        string key = string.Format(@"\{0}\{1}", categoryName, counterName);

                        List<IPerfmonCallback> list;
                        if (subscribers.TryGetValue(key, out list))
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
                                    subscriber.OnNext(e);
                                });
                            });
                        }
                    }
                }
            }
        }
    }
}
