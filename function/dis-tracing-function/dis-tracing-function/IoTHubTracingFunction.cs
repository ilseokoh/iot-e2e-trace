using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// This code is based on https://github.com/Azure-Samples/e2e-diagnostic-eventhub-ai-function/blob/master/e2e-diagnostics/run.csx

namespace dis_tracing_function
{
    public class IoTHubTracingFunction
    {
        const string DefaultRoleInstance = "default";
        const string DefaultIoTHubRoleName = "IoT Hub";
        const string DefaultDeviceRoleName = "Devices";

        private readonly TelemetryClient telemetry;

        public IoTHubTracingFunction(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetry = new TelemetryClient(telemetryConfiguration);
        }


        [FunctionName("iothub-dist-tracing")]
        public void Run([EventHubTrigger("insights-logs-e2ediagnostics", Connection = "E2E_DIAGNOSTICS_EVENTHUB_ENDPOINT", ConsumerGroup = "$Default")] EventData[] events, ILogger log)
        {
            foreach (EventData eventData in events)
            {
                string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                EventHubMessage ehm = null;
                try
                {
                    ehm = JsonConvert.DeserializeObject<EventHubMessage>(messageBody);
                }
                catch (JsonSerializationException e)
                {
                    log.LogError($"Cannot parse Event Hub messages: {e.Message}");
                }
                catch (Exception e)
                {
                    log.LogError($"Unknown error when parse Event Hub messages: {e.Message}");
                }

                if (ehm == null)
                {
                    return;
                }

                foreach (Record record in ehm.records)
                {
                    log.LogInformation($"Get Record: {record.operationName}");
                    var hasError = record.level == "Error";
                    if (record.operationName == "DiagnosticIoTHubD2C")
                    {
                        try
                        {
                            var properties = JsonConvert.DeserializeObject<D2CProperties>(record.properties);
                            if (properties != null)
                            {
                                var deviceId = properties.deviceId;
                                var callerLocalTimeUtc = DateTimeToMilliseconds(DateTimeOffset.Parse(properties.callerLocalTimeUtc).UtcDateTime);
                                var calleeLocalTimeUtc = DateTimeToMilliseconds(DateTimeOffset.Parse(properties.calleeLocalTimeUtc).UtcDateTime);
                                var d2cLatency = (int)(calleeLocalTimeUtc - callerLocalTimeUtc);

                                SendD2CLog(deviceId, d2cLatency, record.time, record.correlationId, properties, hasError);
                            }
                            else
                            {
                                log.LogError($"D2C log properties is null: {record.properties}");
                            }
                        }
                        catch (JsonSerializationException e)
                        {
                            log.LogError($"Cannot parse D2C log properties: {e.Message}");
                        }
                        catch (Exception e)
                        {
                            log.LogError($"Send D2C log to AI failed: {e.Message}");
                        }
                    }
                    else if (record.operationName == "DiagnosticIoTHubIngress")
                    {
                        try
                        {
                            var properties = JsonConvert.DeserializeObject<IngressProperties>(record.properties);
                            if (properties != null)
                            {
                                SendIngressLog(Convert.ToInt32(record.durationMs), properties.parentSpanId, record.time, record.correlationId, properties, hasError);
                            }
                            else
                            {
                                log.LogError($"Ingress log properties is null: {record.properties}");
                            }
                        }
                        catch (JsonSerializationException e)
                        {
                            log.LogError($"Cannot parse Ingress log properties: {e.Message}");
                        }
                        catch (Exception e)
                        {
                            log.LogError($"Send Ingress log to AI failed: {e.Message}");
                        }
                    }
                }
            }
        }

        public long DateTimeToMilliseconds(DateTime time)
        {
            return (long)(time - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public string GetOperationId(string correlationId)
        {
            var ids = correlationId.Split('-');
            return ids[1];
        }

        public string GetSpanId(string correlationId)
        {
            var ids = correlationId.Split('-');
            return ids[2];
        }

        public void SendD2CLog(string deviceId, int d2cLatency, string time, string correlationId, D2CProperties properties, bool hasError = false)
        {
            var reqid = GetSpanId(correlationId);
            var dependencyTelemetry = new DependencyTelemetry
            {
                Id = reqid,
                Target = DefaultIoTHubRoleName,
                Duration = new TimeSpan(0, 0, 0, 0, d2cLatency),
                Success = !hasError,
                Name = "D2C Latency: " + deviceId
            };

            dependencyTelemetry.Properties["calleeLocalTimeUtc"] = properties.calleeLocalTimeUtc;
            dependencyTelemetry.Properties["callerLocalTimeUtc"] = properties.callerLocalTimeUtc;
            dependencyTelemetry.Properties["deviceId"] = properties.deviceId;
            dependencyTelemetry.Properties["messageSize"] = properties.messageSize;

            if (!DateTimeOffset.TryParse(time, out var timestamp))
            {
                timestamp = DateTimeOffset.Now;
                dependencyTelemetry.Timestamp = timestamp;
                dependencyTelemetry.Properties["originalTimestamp"] = time;
            }
            else
            {
                dependencyTelemetry.Timestamp = timestamp;
            }

            telemetry.Context.Cloud.RoleName = DefaultDeviceRoleName;
            telemetry.Context.Cloud.RoleInstance = deviceId;
            telemetry.Context.Operation.Id = GetOperationId(correlationId);
            telemetry.Context.Operation.ParentId = reqid;

            telemetry.TrackDependency(dependencyTelemetry);
            telemetry.Flush();
        }

        public void SendIngressLog(int ingressLatency, string parentId, string time, string correlationId, IngressProperties properties, bool hasError = false)
        {
            var reqid = GetSpanId(correlationId);
            var requestTelemetry = new RequestTelemetry
            {
                Id = reqid,
                Duration = new TimeSpan(0, 0, 0, 0, ingressLatency),
                Success = !hasError,
                Name = "Ingress Latency"
            };

            requestTelemetry.Properties["isRoutingEnabled"] = properties.isRoutingEnabled;
            requestTelemetry.Properties["parentSpanId"] = properties.parentSpanId;

            if (!DateTimeOffset.TryParse(time, out var timestamp))
            {
                timestamp = DateTimeOffset.Now;
                requestTelemetry.Timestamp = timestamp;
                requestTelemetry.Properties["originalTimestamp"] = time;
            }
            else
            {
                requestTelemetry.Timestamp = timestamp;
            }

            telemetry.Context.Cloud.RoleName = DefaultIoTHubRoleName;
            telemetry.Context.Cloud.RoleInstance = DefaultRoleInstance;
            telemetry.Context.Operation.ParentId = parentId;
            telemetry.Context.Operation.Id = GetOperationId(correlationId);

            telemetry.TrackRequest(requestTelemetry);
            telemetry.Flush();
        }
    }
}
