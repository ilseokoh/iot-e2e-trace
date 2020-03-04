using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RuleSetService
{
    public class RuleSetService : IHostedService, IDisposable
    {
        private EventProcessorHost eventProcessorHost;

        private const string EventHubConnectionString = "";
        private const string EventHubName = "";
        private const string StorageContainerName = "";
        private const string StorageAccountName = "";
        private const string StorageAccountKey = "";

        private static readonly string StorageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccountName, StorageAccountKey);

        private readonly ILogger<RuleSetService> _logger;

        public RuleSetService(ILogger<RuleSetService> logger)
        {
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RuleSetService Background Service is starting.");
            _logger.LogInformation("RulSet Service ... Registering EventProcessor...");

            eventProcessorHost = new EventProcessorHost(
                EventHubName,
                PartitionReceiver.DefaultConsumerGroupName,
                EventHubConnectionString,
                StorageConnectionString,
                StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages
            await eventProcessorHost.RegisterEventProcessorAsync<IoTEventProcessor>();

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Disposes of the Event Processor Host
            await eventProcessorHost.UnregisterEventProcessorAsync();
        }

        public void Dispose()
        {
        }
    }
}
