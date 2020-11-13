using System;
using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.DeviceSupport;
using Specialelektronik.Products.Genelec.SmartIp;         	// For Generic Device Support

namespace Specialelektronik.Products.Genelec.Test
{
    public class ControlSystem : CrestronControlSystem
    {
        public static ControlSystem Instance { get; private set; }

        Xpanel _xpanel;
        GenelecSpeaker _speaker;

        public ControlSystem()
            : base()
        {
            try
            {
                Instance = this;
                Thread.MaxNumberOfUserThreads = 20;

                CrestronEnvironment.ProgramStatusEventHandler += ControlSystem_ControllerProgramEventHandler;
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("Exception in the constructor.", ex);
            }
        }

        public override void InitializeSystem()
        {
            try
            {
                _speaker = new GenelecSpeaker("192.168.10.128");
                _speaker.Debug = true;
                _xpanel = new Xpanel(0x03, _speaker);

                _speaker.StartPolling();
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("Exception in InitializeSystem.", ex);
            }
        }

        void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Stopping):
                    _speaker.Dispose();
                    break;
            }

        }
    }
}