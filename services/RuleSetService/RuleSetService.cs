using Microsoft.ApplicationInsights;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
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

        private readonly ILogger<RuleSetService> _logger;
        private readonly IConfiguration _config;
        private TelemetryClient _telemetryClient;

        private string EventHubConnectionString;
        private string IoTRoutingEventHubName;
        private string EventHubConsumerGroup;
        private string StorageContainerName;
        private string StorageAccountName;
        private string StorageAccountKey;

        private string StorageConnectionString;

        public RuleSetService(ILogger<RuleSetService> logger, IConfiguration config, TelemetryClient tc)
        {
            _logger = logger;
            _config = config;
            _telemetryClient = tc;

            EventHubConnectionString = _config.GetValue<string>("IOT_E2E_EH_CONNECTIONSTRING");
            IoTRoutingEventHubName = _config.GetValue<string>("IOT_E2E_EH_IOT_ROUTING_NAME");
            EventHubConsumerGroup = _config.GetValue<string>("IOT_E2E_EH_CONSUMER_GROUP");

            StorageContainerName = _config.GetValue<string>("IOT_E2E_STORAGE_IOT_ROUTING_CONTAINER_NAME");
            StorageAccountName = _config.GetValue<string>("IOT_E2E_STORAGE_ACCOUNT_NAME");
            StorageAccountKey = _config.GetValue<string>("IOT_E2E_STORAGE_ACCOUNT_KEY");

            StorageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccountName, StorageAccountKey);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RuleSetService Background Service is starting.");
            _logger.LogInformation($"Consumer Group: {EventHubConsumerGroup}");

            // EPH Init
            eventProcessorHost = new EventProcessorHost(
                IoTRoutingEventHubName,
                EventHubConsumerGroup,
                EventHubConnectionString,
                StorageConnectionString,
                StorageContainerName);

            eventProcessorHost.PartitionManagerOptions = new PartitionManagerOptions()
            {
                LeaseDuration = TimeSpan.FromSeconds(60),
                RenewInterval = TimeSpan.FromSeconds(60)
            };

            // Registers the Event Processor Host and starts receiving messages
            // await eventProcessorHost.RegisterEventProcessorAsync<IoTEventProcessor>();

            // Use factory
            await eventProcessorHost.RegisterEventProcessorFactoryAsync(new IoTEventProcessorFactory(_config, _logger, _telemetryClient));
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
