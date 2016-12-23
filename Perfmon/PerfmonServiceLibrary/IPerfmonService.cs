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
        public DateTime DateTime { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public double Value { get; set; }
    }
}
