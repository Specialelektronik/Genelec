using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace Specialelektronik.Products.Genelec.SmartIp
{
    class GenelecAudioVolume
    {
        [JsonProperty("level")]
        public double LevelDb { get; private set; }

        [JsonProperty("mute")]
        public bool Mute { get; private set; }
    }
}