using System;
using System.Text;
using Crestron.SimplSharp;
using Specialelektronik.Products.Genelec.SmartIp;                          				// For Basic SIMPL# Classes

namespace Genelec_Speaker_CSharp
{
    public delegate void EmptyDelegate();
    public delegate void ShortDelegate(short value);
    public delegate void UShortDelegate(ushort value);
    public delegate void StringDelegate(SimplSharpString value);

    public class GenelecSpeakerWrapper
    {
        public UShortDelegate SetRespondingFb { get; set; }
        public UShortDelegate SetPowerActiveFb { get; set; }
        public UShortDelegate SetPowerStandbyFb { get; set; }
        public UShortDelegate SetPower15WFb { get; set; }
        public UShortDelegate SetMuteFb { get; set; }
        public ShortDelegate SetLevelDbFb { get; set; }
        public StringDelegate SetLevelDbTextFb { get; set; }
        public UShortDelegate SetLevelPercentFb { get; set; }
        public UShortDelegate SetAllocatedPowerFb { get; set; }
        public StringDelegate SetModelFb { get; set; }
        public StringDelegate SetFirmwareIdFb { get; set; }
        public StringDelegate SetBuildFb { get; set; }
        public StringDelegate SetBaseIdFb { get; set; }
        public StringDelegate SetHardwareIdFb { get; set; }
        public StringDelegate SetCategoryFb { get; set; }
        public StringDelegate SetTechnologyFb { get; set; }
        public StringDelegate SetApiVersionFb { get; set; }

        public string Ip { set { _device.Ip = value; } }
        public ushort Port { set { _device.Port = value; } }
        public ushort Debug { set { _device.Debug = value > 0; } }
        public ushort Mute { set { _device.Mute = value > 0; } }
        public short LevelDb { set { _device.LevelDb = value / 10.0; } }
        public ushort LevelPercent { set { _device.LevelPercent = value / 65535.0; } }

        GenelecSpeaker _device;

        public GenelecSpeakerWrapper()
        {
            _device = new GenelecSpeaker("");
            _device.Events += _device_Events;
        }

        public void PollDeviceInfo()
        {
            _device.PollDeviceInfo();
        }
        public void PollPowerAndAudio(ushort value)
        {
            if (value > 0)
                _device.StartPolling();
            else
                _device.StopPolling();
        }
        
        public void PowerActivate()
        {
            _device.PowerState = eGenelecSpeakerPowerState.Active;
        }
        public void PowerStandby()
        {
            _device.PowerState = eGenelecSpeakerPowerState.Standby;
        }
        public void PowerBoot()
        {
            _device.PowerState = eGenelecSpeakerPowerState.Boot;
        }

        public void MuteToggle()
        {
            _device.Mute = !_device.Mute;
        }

        public void RecallProfile(ushort number, ushort startup)
        {
            _device.SetProfile(number, startup > 0);
        }

        public string CustomGet(string url)
        {
            return _device.CustomGet(url);
        }
        public ushort CustomSet(string url, string json)
        {
            return (ushort)(_device.CustomSet(url, json) ? 1 : 0);
        }

        void _device_Events(object sender, GenelecSpeakerEventArgs e)
        {
            switch (e.EventType)
            {
                case GenelecSpeakerEventArgs.eEventType.Responding:
                    SetRespondingFb(Convert.ToUInt16(e.BoolValue));
                    break;
                case GenelecSpeakerEventArgs.eEventType.LevelDb:
                    SetLevelDbFb((short)(e.DoubleValue * 10));
                    SetLevelDbTextFb(Math.Round(e.DoubleValue, 1).ToString());
                    break;
                case GenelecSpeakerEventArgs.eEventType.LevelPercent:
                    SetLevelPercentFb((ushort)(e.DoubleValue * 65535));
                    break;
                case GenelecSpeakerEventArgs.eEventType.Mute:
                    SetMuteFb(Convert.ToUInt16(e.BoolValue));
                    break;
                case GenelecSpeakerEventArgs.eEventType.DeviceInfo:
                    SetModelFb(e.DeviceInfo.Model);
                    SetFirmwareIdFb(e.DeviceInfo.FirmwareId);
                    SetBuildFb(e.DeviceInfo.Build);
                    SetBaseIdFb(e.DeviceInfo.BaseId);
                    SetHardwareIdFb(e.DeviceInfo.HardwareId);
                    SetCategoryFb(e.DeviceInfo.Category);
                    SetTechnologyFb(e.DeviceInfo.Technology);
                    SetApiVersionFb(e.DeviceInfo.ApiVersion);
                    break;
                case GenelecSpeakerEventArgs.eEventType.PowerState:
                    SetPowerActiveFb(Convert.ToUInt16(e.PowerState == eGenelecSpeakerPowerState.Active));
                    SetPowerStandbyFb(Convert.ToUInt16(e.PowerState == eGenelecSpeakerPowerState.Standby));
                    break;
                case GenelecSpeakerEventArgs.eEventType.Poe15W:
                    SetPower15WFb(Convert.ToUInt16(e.BoolValue));
                    break;
                case GenelecSpeakerEventArgs.eEventType.AllocatedPower:
                    SetAllocatedPowerFb((ushort)(e.DoubleValue * 10));
                    break;
                case GenelecSpeakerEventArgs.eEventType.Profile:
                    break;
            }
        }
    }
}
