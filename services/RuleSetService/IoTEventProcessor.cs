using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RuleSetService
{
    public class IoTEventProcessor : IEventProcessor
    {

        private static EventHubClient eventHubClient;
        private const string EventHubConnectionString = "";
        private const string EventHubName = "";

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
            // close the eventhub client
            eventHubClient.Close();

            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            var connectionStringBuilder = new EventHubsConnectionStringBuilder(EventHubConnectionString)
            {
                EntityPath = EventHubName
            };
            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");

                var devid = eventData.SystemProperties["iothub-connection-device-id"].ToString();

                var newevent = new EventData(Encoding.UTF8.GetBytes(data));
                newevent.Properties.Add("iothub-connection-device-id", devid);

                try
                {
                    eventHubClient.SendAsync(newevent, devid);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"{DateTime.Now} > Exception: {exception.Message}");
                }
            }

            return context.CheckpointAsync();
        }
    }
}
