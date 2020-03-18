using System;
using System.Collections.Generic;
using System.Text;

namespace dis_tracing_function
{
    public class Record
    {
        public string time;
        public string resourceId;
        public string operationName;
        public string durationMs;
        public string correlationId;
        public string properties;
        public string level;
    }

    public class D2CProperties
    {
        public string messageSize;
        public string deviceId;
        public string callerLocalTimeUtc;
        public string calleeLocalTimeUtc;
    }

    public class IngressProperties
    {
        public string isRoutingEnabled;
        public string parentSpanId;
    }

    public class EgressProperties
    {
        public string endpointType;
        public string endpointName;
        public string parentSpanId;
    }

    public class EventHubMessage
    {
        public Record[] records;
    }
}
