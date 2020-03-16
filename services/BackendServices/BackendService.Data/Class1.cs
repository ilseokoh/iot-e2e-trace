using System;
using Newtonsoft.Json;

namespace BackendService.Data
{
    

    public class Class1
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
