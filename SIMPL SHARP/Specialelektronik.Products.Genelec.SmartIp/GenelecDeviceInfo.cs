using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace Specialelektronik.Products.Genelec.SmartIp
{
    /// <summary>
    /// Contains information about the speaker, such as model and firmware.
    /// </summary>
    public class GenelecDeviceInfo
    {
        /// <summary>
        /// Firmware identification number in format model_base-major.minor.revbuild_date_and_time. Example: 44x0-1.1.11-202007021238
        /// </summary>
        [JsonProperty("fwId")]
        public string FirmwareId { get; set; }

        /// <summary>
        /// Committed GIT revision number. -modif means that uncommitted source code is used when creating firmware. Example: c5ca14
        /// </summary>
        [JsonProperty("build")]
        public string Build { get; set; }

        /// <summary>
        /// Platform software version number in format major.minor.rev. Example: 1.0.0
        /// </summary>
        [JsonProperty("baseId")]
        public string BaseId { get; set; }

        /// <summary>
        /// Hardware version string.
        /// </summary>
        [JsonProperty("hwId")]
        public string HardwareId { get; set; }

        /// <summary>
        /// Compability information for upgrading firmware
        /// </summary>
        [JsonProperty("upgradeId")]
        public int UpgradeId { get; set; }

        /// <summary>
        /// Device model name. Example: 4430
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; set; }

        /// <summary>
        /// Category. Example: SAM_2WAY
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>
        /// Technology. Example: SAM_IP
        /// </summary>
        [JsonProperty("technology")]
        public string Technology { get; set; }

        /// <summary>
        /// API version. Example: v1
        /// </summary>
        [JsonProperty("apiVer")]
        public string ApiVersion { get; set; }

        /// <summary>
        /// New firmware is running and waiting for confirmation from user. Bootloader reverts backup firmware during next reboot if confirmation is not done.
        /// </summary>
        [JsonProperty("confirmFwUpdate")]
        public bool ConfirmFirmwareUpdate { get; set; }
    }
}