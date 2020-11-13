using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace Specialelektronik.Products.Genelec.SmartIp
{
    /// <summary>
    /// Event args for the base GenelecSpeaker class
    /// </summary>
    public class GenelecSpeakerEventArgs : EventArgs
    {
        /// <summary>
        /// The available property changes that can be trigged with this event
        /// </summary>
        public enum eEventType
        {
            /// <summary>
            /// BoolValue
            /// </summary>
            Responding,
            /// <summary>
            /// DoubleValue
            /// </summary>
            LevelDb,
            /// <summary>
            /// DoubleValue
            /// </summary>
            LevelPercent,
            /// <summary>
            /// BoolValue
            /// </summary>
            Mute,
            /// <summary>
            /// DeviceInfo
            /// </summary>
            DeviceInfo,
            /// <summary>
            /// PowerState
            /// </summary>
            PowerState,
            /// <summary>
            /// BoolValue
            /// </summary>
            Poe15W,
            /// <summary>
            /// DoubleValue
            /// </summary>
            AllocatedPower,
            /// <summary>
            /// IntValue
            /// </summary>
            Profile
        }
        /// <summary>
        /// The property that changed
        /// </summary>
        public eEventType EventType { get; private set; }

        /// <summary>
        /// Contains the new value of the property for event types Responding, Mute and Poe15W.
        /// </summary>
        public bool BoolValue { get; private set; }
        /// <summary>
        /// Contains the new value of the property for event type Profile.
        /// </summary>
        public int IntValue { get; private set; }
        /// <summary>
        /// Contains the new value of the property for event types LevelDb, LevelPercent and AllocatedPower.
        /// </summary>
        public double DoubleValue { get; private set; }
        /// <summary>
        /// Contains the new value of the property for event type DeviceInfo.
        /// </summary>
        public GenelecDeviceInfo DeviceInfo { get; private set; }
        /// <summary>
        /// Contains the new value of the property for event type PowerState.
        /// </summary>
        public eGenelecSpeakerPowerState PowerState { get; private set; }

        /// <summary></summary>
        internal GenelecSpeakerEventArgs(eEventType type, bool value)
        {
            EventType = type;
            BoolValue = value;
        }
        /// <summary></summary>
        internal GenelecSpeakerEventArgs(eEventType type, int value)
        {
            EventType = type;
            IntValue = value;
        }
        /// <summary></summary>
        internal GenelecSpeakerEventArgs(eEventType type, double value)
        {
            EventType = type;
            DoubleValue = value;
        }
        /// <summary></summary>
        internal GenelecSpeakerEventArgs(eEventType type, GenelecDeviceInfo value)
        {
            EventType = type;
            DeviceInfo = value;
        }
        /// <summary></summary>
        internal GenelecSpeakerEventArgs(eEventType type, eGenelecSpeakerPowerState value)
        {
            EventType = type;
            PowerState = value;
        }
    }
}