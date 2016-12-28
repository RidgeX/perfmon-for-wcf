using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace PerfmonServiceLibrary
{
    [ServiceContract(Namespace = "http://Perfmon", CallbackContract = typeof(IPerfmonCallback))]
    public interface IPerfmonService
    {
        [OperationContract]
        void Subscribe(string path);

        [OperationContract]
        void Unsubscribe(string path);
    }

    public interface IPerfmonCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnNext(EventData e);
    }

    [DataContract]
    public class EventData
    {
        [DataMember]
        public Category Category { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }

    [DataContract]
    public class Category
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<Counter> Counters { get; set; }
    }

    [DataContract]
    public class Counter
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<Instance> Instances { get; set; }
    }

    [DataContract]
    public class Instance
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public float Value { get; set; }
    }
}
