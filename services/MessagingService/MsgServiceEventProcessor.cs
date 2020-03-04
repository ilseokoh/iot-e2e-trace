using Microsoft.Azure.Devices;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
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
        private string iothub_connectionString;
        private Microsoft.Azure.Devices.TransportType s_transportType;

        private readonly ILogger _logger;

        public MsgServiceEventProcessor(IConfiguration config, ILogger logger)
        {
            _logger = logger;

            iothub_connectionString = config.GetValue<string>("IOT_E2E_IOTHUB_SERVICE_CONNECTIONSTRING"); ;
            s_transportType = Microsoft.Azure.Devices.TransportType.Amqp;
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            _logger.LogInformation($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");

            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            serviceClient = ServiceClient.CreateFromConnectionString(iothub_connectionString, s_transportType);

            _logger.LogInformation($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            _logger.LogInformation($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                _logger.LogInformation($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");

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
                    _logger.LogError(ex.ToString());
                }
            }
        }
    }
}
