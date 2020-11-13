using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;                          				// For Basic SIMPL# Classes

namespace Specialelektronik.Products.Genelec.SmartIp
{
    /// <summary>
    /// Used to control Genelec speakers using the Smart IP protocol. Such as 4430.
    /// </summary>
    public class GenelecSpeaker : IDisposable
    {
        /// <summary>
        /// Trigged when any property on this device changes.
        /// </summary>
        public event EventHandler<GenelecSpeakerEventArgs> Events;

        bool _isResponding;
        /// <summary>
        /// Returns true if the device responded to the last command.
        /// </summary>
        public bool IsResponding
        {
            get { return _isResponding; }
            private set
            {
                if (_isResponding != value)
                {
                    _isResponding = value;
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.Responding, value);
                }
            }
        }

        string _ip;
        /// <summary>
        /// The IP address or Hostname of the device to control.
        /// </summary>
        public string Ip
        {
            get { return _ip; }
            set
            {
                _ip = value;
                UpdateBaseUrl();
            }
        }
        int _port = 9000;
        /// <summary>
        /// The port number to connect to. Default: 9000
        /// </summary>
        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                UpdateBaseUrl();
            }
        }

        /// <summary>
        /// The username of the device. Default: admin
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// The password of the device. Default: admin
        /// </summary>
        public string Password { get; set; }

        double _volumeLevelDbToSet;
        double _levelDb;
        /// <summary>
        /// Get or set the volume level in Db. Range -130 to 0.
        /// </summary>
        public double LevelDb
        {
            get { return _levelDb; }
            set
            {
                var newValue = value;
                if (newValue < -130)
                    newValue = -130;
                else if (newValue > 0)
                    newValue = 0;

                _volumeLevelDbToSet = newValue;
                if (!_queue.Contains(_setVolumeAction))
                    _queue.TryToEnqueue(_setVolumeAction);
            }
        }
        double _levelPercent;
        /// <summary>
        /// Get or set the volume level in percentage. Range 0.0 - 1.0
        /// </summary>
        public double LevelPercent
        {
            get { return _levelPercent; }
            set
            {
                LevelDb = value * 130 - 130;
            }
        }
        Action _setVolumeAction;

        bool _mute;
        /// <summary>
        /// Get or set the mute state.
        /// </summary>
        public bool Mute
        {
            get { return _mute; }
            set
            {
                _queue.TryToEnqueue(() => PerformSetMute(value));
            }
        }

        eGenelecSpeakerPowerState _powerState;
        /// <summary>
        /// Get or set the power state. You can only set it to Active, Standby or Boot. Boot will reboot the speaker.
        /// </summary>
        public eGenelecSpeakerPowerState PowerState
        {
            get { return _powerState; }
            set
            {
                if (value != eGenelecSpeakerPowerState.Unknown && 
                    value != eGenelecSpeakerPowerState.Sleep)
                    _queue.TryToEnqueue(() => PerformSetPowerState(value));
            }
        }

        string _lastDeviceInfoString;
        /// <summary>
        /// The last polled information about the device. Use PollDeviceInfo() to update this property.
        /// </summary>
        public GenelecDeviceInfo DeviceInfo { get; private set; }

        /// <summary>
        /// Gets the power allocated by PoE PSE (switch). Use PollPowerAndAudio() or StartPolling() to update this property.
        /// </summary>
        public double AllocatedPower { get; private set; }
        /// <summary>
        /// Returns true if PoE PD (loudspeaker) limits current consumption to 15W. Returns false if full power is needed (30W).
        /// Use PollPowerAndAudio() or StartPolling() to update this property.
        /// </summary>
        public bool Poe15W { get; private set; }

        /// <summary>
        /// Enables debugging messages to console.
        /// </summary>
        public bool Debug { get; set; }
        
        string _baseUrl;

        CTimer _pollTimer;
        /// <summary>
        /// This sets how often the device will be polled when using StartPolling(). Default: 5000 (ms)
        /// </summary>
        public int PollRateMs { get; set; }

        CrestronQueue<Action> _queue = new CrestronQueue<Action>(20);
        object _workHandle;

        /// <summary>
        /// Used to control Genelec speakers using the Smart IP protocol. Such as 4430. Uses default port 9000, username: admin and password: admin.
        /// </summary>
        /// <param name="ip">The IP address or Hostname of the device to control.</param>
        public GenelecSpeaker(string ip) : this(ip, 9000, "admin", "admin") { }
        /// <summary>
        /// Used to control Genelec speakers using the Smart IP protocol. Such as 4430. Uses default port 9000.
        /// </summary>
        /// <param name="ip">The IP address or Hostname of the device to control.</param>
        /// <param name="username">The username of the device.</param>
        /// <param name="password">The password of the device.</param>
        public GenelecSpeaker(string ip, string username, string password) : this(ip, 9000, username, password) { }
        /// <summary>
        /// Used to control Genelec speakers using the Smart IP protocol. Such as 4430.
        /// </summary>
        /// <param name="ip">The IP address or Hostname of the device to control.</param>
        /// <param name="port">The port number to connect to.</param>
        /// <param name="username">The username of the device.</param>
        /// <param name="password">The password of the device.</param>
        public GenelecSpeaker(string ip, int port, string username, string password)
        {
            Ip = ip;
            Port = port;
            Username = username;
            Password = password;

            DeviceInfo = new GenelecDeviceInfo();
            PollRateMs = 5000;

            _setVolumeAction = () => PerformSetVolumeDb(_volumeLevelDbToSet);

            _workHandle = CrestronInvoke.BeginInvoke(HandleQueue);
        }

        void UpdateBaseUrl()
        {
            if (!String.IsNullOrEmpty(_ip))
                _baseUrl = String.Format("http://{0}:{1}/", _ip, _port);
        }

        /// <summary>
        /// Starts polling for Power and Audio. The poll rate can be set with PollRateMs, but defaults to 5000 ms.
        /// This poll includes the following properties: PowerState, AllocatedPower, Poe15W, LevelDb, LevelPercent and Mute.
        /// </summary>
        public void StartPolling()
        {
            if (_pollTimer == null)
                _pollTimer = new CTimer(PollTimerExpired, 0);
            else
                _pollTimer.Reset(PollRateMs);
        }
        /// <summary>
        /// Stops polling the device.
        /// </summary>
        public void StopPolling()
        {
            if (_pollTimer != null)
                _pollTimer.Stop();
        }
        void PollTimerExpired(object o)
        {
            PollPowerAndAudio();
            
            if (_pollTimer != null && !_pollTimer.Disposed)
                _pollTimer.Reset(PollRateMs);
        }

        /// <summary>
        /// This polls the device for information about the device such as Model and Firmware.
        /// </summary>
        public void PollDeviceInfo() 
        {
            _queue.TryToEnqueue(PerformPollDeviceInfo);
        }
        void PerformPollDeviceInfo()
        {
            try
            {
                var data = SendGet("device/info");
                if (data == null || _lastDeviceInfoString == data)
                    return;

                var deviceInfo = JsonConvert.DeserializeObject<GenelecDeviceInfo>(data);

                _lastDeviceInfoString = data;
                DeviceInfo = deviceInfo;

                var ev = Events;
                if (ev != null)
                    ev(this, new GenelecSpeakerEventArgs(GenelecSpeakerEventArgs.eEventType.DeviceInfo, DeviceInfo));
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("GenelecSpeaker.PerformPollDeviceInfo() (" + Ip + ") - Exception.", ex);
            }
        }
        void PollAudio()
        {
            _queue.TryToEnqueue(PerformPollAudio);
        }
        void PerformPollAudio()
        {
            try
            {
                var data = SendGet("public/v1/audio/volume");
                if (data == null)
                    return;

                var audioVolume = JsonConvert.DeserializeObject<GenelecAudioVolume>(data);

                if (_levelDb != audioVolume.LevelDb)
                {
                    _levelDb = audioVolume.LevelDb;
                    _levelPercent = (audioVolume.LevelDb + 130) / 130.0;

                    TrigEvent(GenelecSpeakerEventArgs.eEventType.LevelDb, _levelDb);
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.LevelPercent, _levelPercent);
                }
                if (_mute != audioVolume.Mute)
                {
                    _mute = audioVolume.Mute;
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.Mute, _mute);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("GenelecSpeaker.PerformPollAudio() (" + Ip + ") - Exception.", ex);
            }
        }
        void PerformSetVolumeDb(double value) 
        {
            try
            {
                var json = new JObject(new JProperty("level", Math.Round(value, 1)));
                var success = SendPut("public/v1/audio/volume", json);

                if (success)
                {
                    _levelDb = value;
                    _levelPercent = (value + 130) / 130.0;

                    TrigEvent(GenelecSpeakerEventArgs.eEventType.LevelDb, _levelDb);
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.LevelPercent, _levelPercent);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("GenelecSpeaker.PerformSetVolumeDb() (" + Ip + ") - Exception.", ex);
            }
        }
        void PerformSetMute(bool value)
        {
            try
            {
                var json = new JObject(new JProperty("mute", value));
                var success = SendPut("public/v1/audio/volume", json);

                if (success)
                {
                    _mute = value;
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.Mute, _mute);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("GenelecSpeaker.PerformSetMute() (" + Ip + ") - Exception.", ex);
            }
        }

        void PerformSetPowerState(eGenelecSpeakerPowerState state)
        {
            try
            {
                var json = new JObject(new JProperty("state", state.ToString().ToUpper()));
                var success = SendPut("public/v1/device/pwr", json);

                if (success)
                {
                    _powerState = state;
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.PowerState, state);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("GenelecSpeaker.PerformSetPowerState() (" + Ip + ") - Exception.", ex);
            }
        }
        /// <summary>
        /// This polls the device for the following properies: PowerState, AllocatedPower, Poe15W, LevelDb, LevelPercent and Mute.
        /// This is the same poll as StartPolling() does.
        /// </summary>
        public void PollPowerAndAudio()
        {
            _queue.TryToEnqueue(PerformPollPowerAndAudio);
        }
        void PerformPollPowerAndAudio()
        {
            try
            {
                var data = SendGet("public/v1/device/pwr");
                if (data == null)
                    return;
                
                var devicePower = JsonConvert.DeserializeObject<GenelecDevicePower>(data);

                var state = GetPowerStateFromString(devicePower.State);
                if (_powerState != state)
                {
                    _powerState = state;
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.PowerState, state);
                }
                if (Poe15W != devicePower.Poe15W)
                {
                    Poe15W = devicePower.Poe15W;
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.Poe15W, Poe15W);
                }
                if (AllocatedPower != devicePower.PoeAllocatedPower)
                {
                    AllocatedPower = devicePower.PoeAllocatedPower;
                    TrigEvent(GenelecSpeakerEventArgs.eEventType.AllocatedPower, AllocatedPower);
                }
                if (state == eGenelecSpeakerPowerState.Active)
                    PollAudio();
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("GenelecSpeaker.PerformPollPower() (" + Ip + ") - Exception.", ex);
            }
        }
        eGenelecSpeakerPowerState GetPowerStateFromString(string state)
        {
            if (state == "STANDBY")
                return eGenelecSpeakerPowerState.Standby;
            if (state == "SLEEP")
                return eGenelecSpeakerPowerState.Sleep;
            if (state == "ACTIVE")
                return eGenelecSpeakerPowerState.Active;
            if (state == "BOOT")
                return eGenelecSpeakerPowerState.Boot;

            return eGenelecSpeakerPowerState.Unknown;
        }

        /// <summary>
        /// Restore profile from flash and set it as an active profile.
        /// </summary>
        /// <param name="profile">Profile number to load</param>
        /// <param name="loadOnStartup">If true; uses this profile as an active profile after power reset. If profile is not found in startup, profile 0 will be used</param>
        public void SetProfile(int profile, bool loadOnStartup)
        {
            _queue.TryToEnqueue(() => PerformSetProfile(profile, loadOnStartup));
        }
        void PerformSetProfile(int profile, bool loadOnStartup)
        {
            try
            {
                var json = new JObject();
                json.Add("id", profile);
                json.Add("startup", loadOnStartup);

                var success = SendPut("public/v1/profile/restore", json);

                if (success)
                {
                    var ev = Events;
                    if (ev != null)
                        ev(this, new GenelecSpeakerEventArgs(GenelecSpeakerEventArgs.eEventType.Profile, profile));
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Exception("GenelecSpeaker.PerformSetPowerState() (" + Ip + ") - Exception.", ex);
            }
        }

        /// <summary>
        /// Makes it possible to send custom commands to the device. This is a GET-request.
        /// </summary>
        /// <param name="url">The url to use. Example: public/v1/audio/volume</param>
        /// <returns>A json response if everything went well. Otherwise returns null.</returns>
        public string CustomGet(string url)
        {
            return  SendGet(url.TrimStart('/'));
        }
        /// <summary>
        /// Makes it possible to send custom commands to the device. This is a PUT-request.
        /// </summary>
        /// <param name="url">The url to use. Example: public/v1/audio/volume</param>
        /// <param name="body">The json payload. Example {"level":-20.5}</param>
        /// <returns>True if command was accepted.</returns>
        public bool CustomSet(string url, string body)
        {
            return SendPut(url.TrimStart('/'), body);
        }

        string SendGet(string url)
        {
            try
            {
                if (Debug)
                    CrestronConsole.PrintLine("GenelecSpeaker.SendGet() ({0}) - Sending to: {1}", Ip, url);

                string response;
                using (var client = GetClient())
                    response = client.Get(_baseUrl + url);

                if (Debug)
                    CrestronConsole.PrintLine("GenelecSpeaker.SendGet() ({0}) - Received: {1}", Ip, response);

                IsResponding = true;
                return response;
            }
            catch (HttpException ex)
            {
                if (ex.Response != null)
                {
                    IsResponding = true;
                    if (Debug)
                        CrestronConsole.PrintLine("GenelecSpeaker.SendGet() ({0}) - HttpException Code: {1}, Content: {2}", Ip, ex.Response.Code, ex.Response.ContentString);
                    return null;
                }
                else
                {
                    ErrorLog.Error("GenelecSpeaker.SendGet() ({0}) - HttpException: {1}", Ip, ex.Message);
                }
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("SocketException"))
                    ErrorLog.Error("GenelecSpeaker.SendGet() ({0}) - SocketException: {1}", Ip, ex.Message);
                else
                    ErrorLog.Exception("GenelecSpeaker.SendGet() (" + Ip + ") - Exception.", ex);
            }
            IsResponding = false;
            return null;
        }
        bool SendPut(string url, JObject json)
        {
            return SendPut(url, json.ToString(Formatting.None));
        }
        bool SendPut(string url, string body)
        {
            var request = new HttpClientRequest();
            request.Url.Parse(_baseUrl + url);
            request.RequestType = RequestType.Put;
            request.KeepAlive = false;
            request.Header.ContentType = "application/json";
            request.ContentString = body;
            try
            {
                if (Debug)
                    CrestronConsole.PrintLine("GenelecSpeaker.SendPut() ({0}) - Sending to: {1}\r\n{2}", Ip, url, body);

                HttpClientResponse response;
                using (var client = GetClient())
                    response = client.Dispatch(request);

                if (Debug)
                    CrestronConsole.PrintLine("GenelecSpeaker.SendPut() ({0}) - Response: {1}", Ip, response.Code == 200 ? "Success" : "Failed");

                IsResponding = true;
                return response.Code == 200;    
            }
            catch (HttpException ex)
            {
                if (ex.Response != null)
                {
                    IsResponding = true;
                    if (Debug)
                        CrestronConsole.PrintLine("GenelecSpeaker.SendPut() ({0}) - HttpException Code: {1}, Content: {2}", Ip, ex.Response.Code, ex.Response.ContentString);
                    return false;
                }
                else
                {
                    ErrorLog.Error("GenelecSpeaker.SendPut() ({0}) - HttpException: {1}", Ip, ex.Message);
                }
            }
            catch (Exception ex)
            {
                if (ex.ToString().Contains("SocketException"))
                    ErrorLog.Error("GenelecSpeaker.SendPut() ({0}) - SocketException: {1}", Ip, ex.Message);
                else
                    ErrorLog.Exception("GenelecSpeaker.SendPut() (" + Ip + ") - Exception.", ex);
            }
            IsResponding = false;
            return false;
        }

        HttpClient GetClient()
        {
            //We need to create a new client all the time because of Crestrons "cannot create header" bug...
            var client = new HttpClient();
            client.Accept = "application/json";
            client.KeepAlive = false; // Httpclient sometimes hangs if this is true
            client.UserName = Username;
            client.Password = Password;
            return client;
        }

        void TrigEvent(GenelecSpeakerEventArgs.eEventType type, bool value)
        {
            var ev = Events;
            if (ev != null)
                ev(this, new GenelecSpeakerEventArgs(type, value));
        }
        void TrigEvent(GenelecSpeakerEventArgs.eEventType type, double value)
        {
            var ev = Events;
            if (ev != null)
                ev(this, new GenelecSpeakerEventArgs(type, value));
        }
        void TrigEvent(GenelecSpeakerEventArgs.eEventType type, eGenelecSpeakerPowerState value)
        {
            var ev = Events;
            if (ev != null)
                ev(this, new GenelecSpeakerEventArgs(type, value));
        }

        /// <summary>
        /// Disposes the queue and the polltimer
        /// </summary>
        public void Dispose()
        {
            if (_pollTimer != null && !_pollTimer.Disposed)
                _pollTimer.Dispose();

            _queue.TryToEnqueue(null);
        }

        void HandleQueue(object o)
        {
            while (true)
            {
                try
                {
                    var action = _queue.Dequeue();
                    if (action == null)
                        return;

                    action();
                    CrestronEnvironment.Sleep(100);
                }
                catch (Exception ex)
                {
                    ErrorLog.Error("GenelecSpeaker.HandleQueue() (" + Ip + ") - Exception. " + ex);
                }
            }
        }
    }
}
