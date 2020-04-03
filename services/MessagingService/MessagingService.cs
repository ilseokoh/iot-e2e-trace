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

namespace MessagingService
{
    public class MessagingService : IHostedService, IDisposable
    {
        private EventProcessorHost eventProcessorHost;

        private readonly ILogger<MessagingService> _logger;
        private readonly IConfiguration _config;
        private TelemetryClient _telemetryClient;

        private string EventHubConnectionString;
        private string MsgSvcEventHubName;
        private string EventHubConsumerGroup;
        private string StorageContainerName;
        private string StorageAccountName;
        private string StorageAccountKey;

        private string StorageConnectionString;

        public MessagingService(ILogger<MessagingService> logger, IConfiguration config, TelemetryClient tc)
        {
            _logger = logger;
            _config = config;
            _telemetryClient = tc;

            EventHubConnectionString = _config.GetValue<string>("IOT_E2E_EH_CONNECTIONSTRING");
            MsgSvcEventHubName = _config.GetValue<string>("IOT_E2E_EH_MSG_SVC_NAME");
            EventHubConsumerGroup = _config.GetValue<string>("IOT_E2E_EH_CONSUMER_GROUP");

            StorageContainerName = _config.GetValue<string>("IOT_E2E_STORAGE_MSG_SVC_CONTAINER_NAME");
            StorageAccountName = _config.GetValue<string>("IOT_E2E_STORAGE_ACCOUNT_NAME");
            StorageAccountKey = _config.GetValue<string>("IOT_E2E_STORAGE_ACCOUNT_KEY");

            StorageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", StorageAccountName, StorageAccountKey);

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Message Service ... Registering EventProcessor...");
            _logger.LogInformation($"Consumer Group: {EventHubConsumerGroup}");

            eventProcessorHost = new EventProcessorHost(
                MsgSvcEventHubName,
                EventHubConsumerGroup,
                EventHubConnectionString,
                StorageConnectionString,
                StorageContainerName);

            eventProcessorHost.PartitionManagerOptions = new PartitionManagerOptions()
            {
                // Interval for which the lease is taken on Azure Blob representing an EventHub partition. 
                // If the lease is not renewed within this interval, 
                // it will cause it to expire and ownership of the partition will move to another EventProcessorHost instance.
                LeaseDuration = TimeSpan.FromSeconds(60),

                //Renew interval for all leases for partitions currently held by EventProcessorHost instance.
                RenewInterval = TimeSpan.FromSeconds(50),  // default 10sec

                // So RenewInterval is smaller than Leaseduration
            };

            // Registers the Event Processor Host and starts receiving messages
            //await eventProcessorHost.RegisterEventProcessorAsync<MsgServiceEventProcessor>();
            await eventProcessorHost.RegisterEventProcessorFactoryAsync(new MsgServiceEventProcessorFactory(_config, _logger, _telemetryClient));
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
