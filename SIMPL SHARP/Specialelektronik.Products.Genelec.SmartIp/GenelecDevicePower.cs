using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace Specialelektronik.Products.Genelec.SmartIp
{
    class GenelecDevicePower
    {
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("poeAllocatedPwr")]
        public double PoeAllocatedPower { get; set; }

        [JsonProperty("poePd15W")]
        public bool Poe15W { get; set; } 
    }
}