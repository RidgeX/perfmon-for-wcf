using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace PerfmonServiceLibrary
{
    [ServiceContract(Namespace = "http://Perfmon", CallbackContract = typeof(IPerfmonCallback), SessionMode = SessionMode.Required)]
    public interface IPerfmonService
    {
        [OperationContract(IsInitiating = true)]
        void Join();

        [OperationContract(IsInitiating = false)]
        CategoryList List();

        [OperationContract(IsInitiating = false)]
        void Refresh();

        [OperationContract(IsInitiating = false)]
        bool Subscribe(string categoryName, string counterName);

        [OperationContract(IsInitiating = false)]
        void Unsubscribe(string categoryName, string counterName);

        [OperationContract(IsInitiating = false, IsTerminating = true, IsOneWay = true)]
        void Leave();
    }

    public interface IPerfmonCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnNext(EventData e);
    }

    [DataContract]
    public class EventData
    {
        [DataMember(Order = 0)]
        public Category Category { get; set; }

        [DataMember(Order = 1)]
        public DateTime Timestamp { get; set; }
    }

    [CollectionDataContract]
    public class CategoryList : List<Category> { };

    [DataContract]
    public class Category
    {
        [DataMember(Order = 0)]
        public string Name { get; set; }

        [DataMember(Order = 1)]
        public List<Counter> Counters { get; set; }
    }

    [DataContract]
    public class Counter
    {
        [DataMember(Order = 0)]
        public string Name { get; set; }

        [DataMember(Order = 1, EmitDefaultValue = false)]
        public List<Instance> Instances { get; set; }
    }

    [DataContract]
    public class Instance
    {
        [DataMember(Order = 0)]
        public string Name { get; set; }

        [DataMember(Order = 1)]
        public float Value { get; set; }
    }
}
