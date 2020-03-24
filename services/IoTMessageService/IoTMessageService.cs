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
using System.Net.Http;
using BackendService.Data;
using Newtonsoft.Json;
using System.Text.Unicode;

namespace IoTMessageService
{
    public class IoTMessageService : IHostedService, IDisposable
    {
        private EventProcessorClient eventProcessorClient;

        private readonly ILogger<IoTMessageService> _logger;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _clientFactory;

        private TelemetryClient _telemetryClient;

        private string ioTHubEPConnectionString;
        private string ioTHubEPConsumerGroup;

        private string storageContainerName;
        private string storageConnectionString;

        private string apiUrl;


        public IoTMessageService(ILogger<IoTMessageService> logger, IConfiguration config, TelemetryClient tc, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _config = config;
            _telemetryClient = tc;
            _clientFactory = clientFactory;

            ioTHubEPConnectionString = _config.GetValue<string>("IOT_E2E_IOTHUB_DEFAULT_EP_CONNECTIONSTRING");
            ioTHubEPConsumerGroup = _config.GetValue<string>("IOT_E2E_IOTHUB_DEFAULT_EP_CONSUMER_GROUP");

            storageContainerName = _config.GetValue<string>("IOT_E2E_STORAGE_IOT_ROUTING_CONTAINER_NAME");
            storageConnectionString = _config.GetValue<string>("IOT_E2E_STORAGE_CONNECTIONSTRING");

            apiUrl = _config.GetValue<string>("IOT_E2E_API_SERVICE_URL");

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
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

        private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            //Activity.Current.SetIdFormat(ActivityIdFormat.W3C);
            if (eventArgs.Data.SystemProperties.ContainsKey("traceparent"))
            {
                Activity.Current.SetParentId(eventArgs.Data.SystemProperties["traceparent"].ToString());
            }

            var data = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());
            _logger.LogInformation($"Message received. Partition: '{eventArgs.Partition.PartitionId}', Data: '{data}'");

            var devid = eventArgs.Data.SystemProperties["iothub-connection-device-id"].ToString();
            // Date and time the Device-to-Cloud message was received by IoT Hub.
            var iothubTimestamp = DateTimeOffset.Parse(eventArgs.Data.SystemProperties["iothub-enqueuedtime"].ToString());
            var telemetry = JsonConvert.DeserializeObject<ChillerTelemetry>(data);

            // Invoke API Service.
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            var newmsg = new ChillerMessage()
            {
                DeviceId = devid,
                Id = Guid.NewGuid().ToString(),
                Humidity = telemetry.humidity,
                Pressure = telemetry.pressure,
                Temperature = telemetry.temperature,
                TimeStamp = iothubTimestamp
            };
            request.Content = new StringContent(JsonConvert.SerializeObject(newmsg), Encoding.UTF8, "application/json");

            var response = await _clientFactory.CreateClient().SendAsync(request);

            if (response.IsSuccessStatusCode) await eventArgs.UpdateCheckpointAsync();
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
