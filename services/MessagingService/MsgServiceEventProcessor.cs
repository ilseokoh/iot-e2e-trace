using Microsoft.Azure.Devices;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MessagingService
{
    public class MsgServiceEventProcessor : IEventProcessor
    {

        ServiceClient serviceClient;
        private const string s_connectionString = "";
        private static Microsoft.Azure.Devices.TransportType s_transportType = Microsoft.Azure.Devices.TransportType.Amqp;

        public MsgServiceEventProcessor()
        {
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");

            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString, s_transportType);

            Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");

                var devid = eventData.Properties["iothub-connection-device-id"].ToString();

                try
                {
                    // invoke direct method
                    var method = new CloudToDeviceMethod("ControlMethod", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
                    method.SetPayloadJson(data);

                    await serviceClient.InvokeDeviceMethodAsync(devid, method);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }
    }
}
