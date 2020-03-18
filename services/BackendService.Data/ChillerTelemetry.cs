using System;
using System.Collections.Generic;
using System.Text;

namespace BackendService.Data
{
    public class ChillerTelemetry
    {
        public float temperature { get; set; }
        public string temperature_unit { get; set; }
        public float humidity { get; set; }
        public string humidity_unit { get; set; }
        public float pressure { get; set; }
        public string pressure_unit { get; set; }
    }
}
