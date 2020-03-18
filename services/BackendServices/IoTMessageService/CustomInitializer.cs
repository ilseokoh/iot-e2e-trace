using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace IoTMessageService
{
    public class CustomInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = "IoTMessageService";
            telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
        }
    }
}
