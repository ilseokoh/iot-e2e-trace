using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Devices;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace MessagingService
{
    public class MsgServiceEventProcessor : IEventProcessor
    {

        ServiceClient serviceClient;
        //RegistryManager iotRegManager;
        private string iothub_connectionString;
        private Microsoft.Azure.Devices.TransportType s_transportType;

        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetryClient;

        public MsgServiceEventProcessor(IConfiguration config, ILogger logger, TelemetryClient tc)
        {
            _logger = logger;
            _telemetryClient = tc;

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
            //iotRegManager = RegistryManager.CreateFromConnectionString(iothub_connectionString);

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
                var reqTime = DateTime.UtcNow;

                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                _logger.LogInformation($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");

                var hitmsg = JsonConvert.DeserializeObject<HitCountMessage>(data);
                var devid = eventData.Properties["iothub-connection-device-id"].ToString();

                //bool hasDevice;
                //try
                //{
                //    // Check the device exist
                //    var device = await iotRegManager.GetDeviceAsync(devid);
                //    hasDevice = (device != null ? true : false);
                //}
                //catch
                //{
                //    hasDevice = false;
                //}

                //bool ehResult = true;
                //if (hasDevice)
                //{
                //    try
                //    {
                //        // invoke direct method
                //        var method = new CloudToDeviceMethod("ControlMethod");
                //        method.SetPayloadJson(data);

                //        await serviceClient.InvokeDeviceMethodAsync(devid, method);
                //        ehResult = true;
                //    }
                //    catch (Exception ex)
                //    {
                //        _logger.LogError(ex.ToString());
                //        ehResult = false;
                //    }
                //}
                //else
                //{
                //    _logger.LogError("Device not found. No method call.");
                //}

                bool ehResult;
                try
                {
                    // invoke direct method
                    var method = new CloudToDeviceMethod("ControlMethod");
                    method.SetPayloadJson(data);

                    await serviceClient.InvokeDeviceMethodAsync(devid, method);
                    ehResult = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    ehResult = false;
                }

                var reqid = Guid.NewGuid().ToString();
                
                //var duration = reqTime.Subtract(DateTimeOffset.Parse(eventData.Properties["ruleset-request-time"].ToString()));
                var duration = reqTime.Subtract(eventData.SystemProperties.EnqueuedTimeUtc);

                var dependencyTelemetry = new DependencyTelemetry
                {
                    Id = reqid,
                    Duration = duration,
                    Target = "IoT Hub",
                    Success = ehResult,
                    Name = "MessageService processing",
                    Timestamp = reqTime
                };
                dependencyTelemetry.Context.Operation.Id = hitmsg.corellationId;
                dependencyTelemetry.Context.Operation.ParentId = (eventData.Properties.ContainsKey("ruleset-request-id") ? eventData.Properties["ruleset-request-id"]?.ToString() : "");

                dependencyTelemetry.Context.Cloud.RoleName = "MessageService";
                dependencyTelemetry.Context.Cloud.RoleInstance = Environment.MachineName;

                dependencyTelemetry.Properties["device-id"] = devid;

                _telemetryClient.TrackDependency(dependencyTelemetry);
                _telemetryClient.Flush();
            }

            await context.CheckpointAsync();
        }
    }
}