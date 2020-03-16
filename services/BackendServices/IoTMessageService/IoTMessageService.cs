using Microsoft.ApplicationInsights;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Messaging.EventHubs.Producer;
using System.Diagnostics;
using Microsoft.ApplicationInsights.DataContracts;

namespace IoTMessageService
{
    public class IoTMessageService : IHostedService, IDisposable
    {
        private EventProcessorClient eventProcessorClient;

        private readonly ILogger<IoTMessageService> _logger;
        private readonly IConfiguration _config;
        private TelemetryClient _telemetryClient;

        private string ioTHubEPConnectionString;
        private string ioTHubEPConsumerGroup;

        private string storageContainerName;
        private string storageConnectionString;


        IoTMessageService(ILogger<IoTMessageService> logger, IConfiguration config, TelemetryClient tc)
        {
            _logger = logger;
            _config = config;
            _telemetryClient = tc;

            ioTHubEPConnectionString = _config.GetValue<string>("IOT_E2E_IOTHUB_DEFAULT_EP_CONNECTIONSTRING");
            ioTHubEPConsumerGroup = _config.GetValue<string>("IOT_E2E_IOTHUB_DEFAULT_EP_CONSUMER_GROUP");

            storageContainerName = _config.GetValue<string>("IOT_E2E_STORAGE_IOT_CONTAINER_NAME");
            storageConnectionString = _config.GetValue<string>("IOT_E2E_STORAGE_CONNECTIONSTRING"); 
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("IoTMessageService Background Service is starting.");
            _logger.LogInformation($"Consumer Group: {ioTHubEPConsumerGroup}");

            BlobContainerClient storageClient = new BlobContainerClient(storageConnectionString, storageContainerName);

            eventProcessorClient = new EventProcessorClient(storageClient, ioTHubEPConsumerGroup, ioTHubEPConnectionString);

            // Register handlers for processing events and handling errors
            eventProcessorClient.ProcessEventAsync += ProcessEventHandler;
            eventProcessorClient.ProcessErrorAsync += ProcessErrorHandler;
            eventProcessorClient.PartitionInitializingAsync += ProcessorInitHandler;
            eventProcessorClient.PartitionClosingAsync += ProcessorClosingHandler;

            // Start the processing
            await eventProcessorClient.StartProcessingAsync();

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("IoTMessage Service will be stopped.");
            // Stop the processing
            await eventProcessorClient.StopProcessingAsync();
        }

        public void Dispose()
        {
            if (eventProcessorClient.IsRunning)
            {
                _logger.LogWarning("IoTMessage Service will be stopped.");
                eventProcessorClient.StopProcessing();
            }
        }

        private Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            Stopwatch swatch = new Stopwatch();
            swatch.Start();

            var data = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
            _logger.LogInformation($"Message received. Partition: '{eventArgs.Partition.PartitionId}', Data: '{data}'");

            bool result = true;
            var devid = "";

            // Invoke API Service.


            swatch.Stop();

            var dependencyTelemetry = new DependencyTelemetry
            {
                Id = Guid.NewGuid().ToString(),
                Duration = swatch.Elapsed,
                Target = "",
                Success = result, 
                Name = "IoT Message Service",
                Timestamp = DateTimeOffset.UtcNow
            };

            dependencyTelemetry.Context.Operation.Id = "";
            dependencyTelemetry.Context.Cloud.RoleName = "IoTMessageService";
            dependencyTelemetry.Context.Cloud.RoleInstance = Environment.MachineName;

            dependencyTelemetry.Properties["deviceid"] = devid;

            return Task.CompletedTask;
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            _logger.LogInformation($"Error on Partition: {eventArgs.PartitionId}, Error: {eventArgs.Exception.Message}");
            return Task.CompletedTask;
        }

        private Task ProcessorInitHandler(PartitionInitializingEventArgs eventArgs)
        {
            _logger.LogInformation($"SimpleEventProcessor initialized. Partition: '{eventArgs.PartitionId}'");
            return Task.CompletedTask;
        }

        private Task ProcessorClosingHandler(PartitionClosingEventArgs eventArgs)
        {
            return Task.CompletedTask;
        }
    }
}
