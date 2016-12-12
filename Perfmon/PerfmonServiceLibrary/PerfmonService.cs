using System;
using System.Collections.Generic;
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
        private static readonly Dictionary<string, List<IPerfmonCallback>> subscribers;

        static PerfmonService()
        {
            _lock = new object();
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
    }
}
