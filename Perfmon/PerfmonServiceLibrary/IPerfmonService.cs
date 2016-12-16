﻿using System;
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
        void Notify(EventData e);

        [OperationContract]
        void Subscribe(string path);

        [OperationContract]
        void Unsubscribe(string path);
    }

    public interface IPerfmonCallback
    {
        [OperationContract(IsOneWay = true)]
        void OnNotify(EventData e);
    }

    [DataContract]
    public class EventData
    {
        [DataMember]
        public DateTime Time { get; set; }

        [DataMember]
        public string Host { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public double Value { get; set; }
    }
}