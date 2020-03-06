using Microsoft.ApplicationInsights;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace MessagingService
{
    public class MsgServiceEventProcessorFactory : IEventProcessorFactory
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly TelemetryClient _telemetryClient;

        public MsgServiceEventProcessorFactory(IConfiguration config, ILogger logger, TelemetryClient tc)
        {
            _config = config;
            _logger = logger;
            _telemetryClient = tc;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new MsgServiceEventProcessor(_config, _logger, _telemetryClient);
        }
    }
}
