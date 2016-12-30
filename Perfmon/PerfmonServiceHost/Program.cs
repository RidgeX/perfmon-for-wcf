using PerfmonServiceLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerfmonServiceHost
{
    public class DebugInspector : IClientMessageInspector, IDispatchMessageInspector
    {
        // Request from client
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            Console.WriteLine(request);
            return null;
        }

        // Reply to client
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            Console.WriteLine(reply);
        }

        // Call to client
        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            Console.WriteLine(request);
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState) { }
    }

    public class DebugBehavior : IEndpointBehavior
    {
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime) { }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            DebugInspector debugInspector = new DebugInspector();
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(debugInspector);
            endpointDispatcher.DispatchRuntime.CallbackClientRuntime.ClientMessageInspectors.Add(debugInspector);
        }

        public void Validate(ServiceEndpoint endpoint) { }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            PerfmonService service = new PerfmonService();
            ServiceHost selfHost = new ServiceHost(service);

            try
            {
                /*
                ServiceEndpoint endpoint = selfHost.Description.Endpoints[0];
                endpoint.EndpointBehaviors.Add(new DebugBehavior());
                */

                selfHost.Open();

                var timer = new System.Timers.Timer();
                timer.Interval = 1000;
                timer.Elapsed += (s, e) => service.Update();
                timer.Start();

                Console.WriteLine("Press <Enter> to terminate service.");
                Console.ReadLine();

                timer.Stop();

                Console.WriteLine("Closing connections, please wait...");
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
