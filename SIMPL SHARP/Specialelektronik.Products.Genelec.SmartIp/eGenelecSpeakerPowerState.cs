using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace Specialelektronik.Products.Genelec.SmartIp
{
    /// <summary>
    /// Possible power states
    /// </summary>
    public enum eGenelecSpeakerPowerState
    {
        /// <summary>
        /// Power state is unknown. This mode cannot be set.
        /// </summary>
        Unknown,
        /// <summary>
        /// Speaker is in standby.
        /// </summary>
        Standby,
        /// <summary>
        /// Speaker is in sleep mode. This mode cannot be set.
        /// </summary>
        Sleep,
        /// <summary>
        /// Speaker is active.
        /// </summary>
        Active,
        /// <summary>
        /// This mode can only be set. This will reboot the speaker.
        /// </summary>
        Boot
    }
}