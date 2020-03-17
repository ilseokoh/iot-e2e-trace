using System;
using Newtonsoft.Json;

namespace BackendService.Data
{
    
    public class ChillerMessage
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty(PropertyName = "temperature")]
        public float Temperature { get; set; }

        [JsonProperty(PropertyName = "humidity")]
        public float Humidity { get; set; }

        [JsonProperty(PropertyName = "pressure")]
        public float Pressure { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public DateTimeOffset TimeStamp { get; set; }
    }
}
