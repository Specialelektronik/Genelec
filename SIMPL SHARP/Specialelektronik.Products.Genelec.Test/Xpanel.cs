using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.UI;
using Crestron.SimplSharpPro;
using Specialelektronik.Products.Genelec.SmartIp;
using Crestron.SimplSharpPro.DeviceSupport;

namespace Specialelektronik.Products.Genelec.Test
{
    public class Xpanel
    {
        XpanelForSmartGraphics _xpanel;
        GenelecSpeaker _speaker;
        bool _pollstate;

        public Xpanel(uint ipId, GenelecSpeaker speaker)
        {
            _speaker = speaker;

            _xpanel = new XpanelForSmartGraphics(ipId, ControlSystem.Instance);
            _xpanel.SigChange += _xpanel_SigChange;

            if (_xpanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                ErrorLog.Error("Xpanel could not register: " + _xpanel.RegistrationFailureReason);

            _speaker.Events += _speaker_Events;
                
        }

        void _xpanel_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            if (args.Event == eSigEvent.UShortChange)
            {
                if (args.Sig.Number == 2)
                    _speaker.LevelPercent = args.Sig.UShortValue / 65535.0;
            }
            else if (args.Event == eSigEvent.BoolChange && args.Sig.BoolValue)
            {
                if (args.Sig.Number == 2)
                    _speaker.Mute = !_speaker.Mute;
                else if (args.Sig.Number == 3)
                    _speaker.PowerState = eGenelecSpeakerPowerState.Standby;
                else if (args.Sig.Number == 4)
                    _speaker.PowerState = eGenelecSpeakerPowerState.Active;
                else if (args.Sig.Number == 5)
                    _speaker.PowerState = eGenelecSpeakerPowerState.Boot;
                else if (args.Sig.Number >= 10 && args.Sig.Number <= 15)
                    _speaker.SetProfile((ushort)(args.Sig.Number - 10), false);
                else if (args.Sig.Number == 20)
                    _speaker.PollDeviceInfo();
                else if (args.Sig.Number == 21)
                    _speaker.PollPowerAndAudio();
                else if (args.Sig.Number == 22)
                    _pollstate = !_pollstate;
                    if (_pollstate)
                        _speaker.StartPolling();
                    else
                        _speaker.StopPolling();
                    _xpanel.BooleanInput[22].BoolValue = _pollstate;
            }
        }


        void _speaker_Events(object sender, GenelecSpeakerEventArgs e)
        {
            switch (e.EventType)
            {
                case GenelecSpeakerEventArgs.eEventType.Responding:
                    _xpanel.BooleanInput[1].BoolValue = e.BoolValue;
                    break;
                case GenelecSpeakerEventArgs.eEventType.LevelDb:
                    _xpanel.StringInput[9].StringValue = Math.Round(e.DoubleValue, 1).ToString();
                    break;
                case GenelecSpeakerEventArgs.eEventType.LevelPercent:
                    _xpanel.UShortInput[2].UShortValue = (ushort)(e.DoubleValue * ushort.MaxValue);
                    break;
                case GenelecSpeakerEventArgs.eEventType.Mute:
                    _xpanel.BooleanInput[2].BoolValue = e.BoolValue;
                    break;
                case GenelecSpeakerEventArgs.eEventType.DeviceInfo:
                    _xpanel.StringInput[1].StringValue = e.DeviceInfo.Model;
                    _xpanel.StringInput[2].StringValue = e.DeviceInfo.FirmwareId;
                    _xpanel.StringInput[3].StringValue = e.DeviceInfo.Build;
                    _xpanel.StringInput[4].StringValue = e.DeviceInfo.BaseId;
                    _xpanel.StringInput[5].StringValue = e.DeviceInfo.HardwareId;
                    _xpanel.StringInput[6].StringValue = e.DeviceInfo.Category;
                    _xpanel.StringInput[7].StringValue = e.DeviceInfo.Technology;
                    _xpanel.StringInput[8].StringValue = e.DeviceInfo.ApiVersion;
                    break;
                case GenelecSpeakerEventArgs.eEventType.PowerState:
                    _xpanel.StringInput[10].StringValue = e.PowerState.ToString();
                    _xpanel.BooleanInput[3].BoolValue = e.PowerState == eGenelecSpeakerPowerState.Standby;
                    _xpanel.BooleanInput[4].BoolValue = e.PowerState == eGenelecSpeakerPowerState.Active;
                    _xpanel.BooleanInput[5].BoolValue = e.PowerState == eGenelecSpeakerPowerState.Boot;
                    break;
                case GenelecSpeakerEventArgs.eEventType.Poe15W:
                    _xpanel.BooleanInput[6].BoolValue = e.BoolValue;
                    break;
                case GenelecSpeakerEventArgs.eEventType.AllocatedPower:
                    _xpanel.UShortInput[3].UShortValue = (ushort)(e.DoubleValue * 10);
                    break;
                case GenelecSpeakerEventArgs.eEventType.Profile:
                    for (ushort join = 10; join < 16; join++)
                        _xpanel.BooleanInput[join].BoolValue = (join - 10) == e.IntValue;
                    break;
            }
        }
    }
}