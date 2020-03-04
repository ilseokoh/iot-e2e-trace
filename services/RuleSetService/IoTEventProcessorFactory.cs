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

        public IoTEventProcessorFactory(IConfiguration config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new IoTEventProcessor(_config, _logger);
        }
    }
}
