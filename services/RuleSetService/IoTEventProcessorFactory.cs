using Microsoft.ApplicationInsights;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace RuleSetService
{
    public class IoTEventProcessorFactory : IEventProcessorFactory
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetryClient;

        public IoTEventProcessorFactory(IConfiguration config, ILogger logger, TelemetryClient tc)
        {
            _config = config;
            _logger = logger;
            _telemetryClient = tc;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new IoTEventProcessor(_config, _logger, _telemetryClient);
        }
    }
}
