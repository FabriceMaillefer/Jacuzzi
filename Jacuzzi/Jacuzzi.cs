using Modbus;
using System;
using System.Collections.Generic;
using Woopsa;

namespace Jacuzzi
{
    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible)]
    public class MesureTemperature
    {
        public MesureTemperature(DateTime Time, double value)
        {
            x = Time.ToString("yyyy'-'MM'-'dd HH':'mm':'ss");
            y = value;
        }

        public string x { get; set; }
        public double y { get; set; }
    }

    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject)]
    //[WoopsaVisibility(WoopsaVisibility.All)]
    public class Jacuzzi
    {
        public Jacuzzi(string host)
        {
            Client = new ModbusClientTcp();
            Host = host;

            TempsActivation = 5;
            TempsDesactivation = 15;

            _pompeTimer = new DownTimer(TimeSpan.FromMinutes(TempsActivation));

            PompeMode = true;
            PompeManuel = false;

            Projecteur = false;
            LumiereSol = false;

            WaterMain = false;
            WaterRefill = false;

            HistoriqueCountMax = 144;
            IntervalSecondsMesureTemperature = 600;
            _measureTimer = new DownTimer(TimeSpan.FromSeconds(IntervalSecondsMesureTemperature));

            HistoriqueTemperatureEau = new List<MesureTemperature>(HistoriqueCountMax);
            HistoriqueTemperatureAir = new List<MesureTemperature>(HistoriqueCountMax);

        }


        #region Process
        public void CyclicUpdate()
        {
            ManualInputCheck();

            UpdatePompe();

            // Historique des mesures
            UpdateHistoriqueTemperature();
        }

        public void UpdatePompe()
        {
            // Gestion timer pompe
            if (PompeMode) // Mode auto
            {
                if (_pompeTimer.Elapsed)
                {
                    bool pompeManuel = PompeManuel;

                    pompeManuel = !pompeManuel;

                    PompeManuel = pompeManuel;

                    if (pompeManuel)
                        _pompeTimer.SetTimeout(TimeSpan.FromMinutes(TempsActivation));
                    else
                        _pompeTimer.SetTimeout(TimeSpan.FromMinutes(TempsDesactivation));

                    _pompeTimer.Restart();
                }
            }
            else
            {
                _pompeTimer.SetTimeout(TimeSpan.FromMinutes(TempsActivation));
            }
        }

        public void UpdateHistoriqueTemperature()
        {
            if (_measureTimer.Elapsed)
            {
                _measureTimer.SetTimeout(TimeSpan.FromSeconds(IntervalSecondsMesureTemperature));
                _measureTimer.Restart();

                HistoriqueTemperatureAir.Add(new MesureTemperature(DateTime.Now, TemperatureAir));
                HistoriqueTemperatureEau.Add(new MesureTemperature(DateTime.Now, TemperatureEau));

                while (HistoriqueTemperatureAir.Count >= HistoriqueCountMax)
                {
                    HistoriqueTemperatureAir.RemoveAt(0);
                }

                while (HistoriqueTemperatureEau.Count >= HistoriqueCountMax)
                {
                    HistoriqueTemperatureEau.RemoveAt(0);
                }
            }
        }
        #endregion

        #region Session Control

        public void Extinction(int privilegeCode)
        {
            CheckControlAccess(privilegeCode);

            PompeMode = true;
            Projecteur = false;
            LumiereSol = false;

            WaterMain = false;
            WaterRefill = false;

            DateTimeout = DateTime.Now;
        }

        public void ToggleWaterMain(int privilegeCode)
        {
            CheckControlAccess(privilegeCode);

            WaterMain = !WaterMain;

            UpdateTimeout();
        }

        public void ToggleWaterRefill(int privilegeCode)
        {
            CheckControlAccess(privilegeCode);

            WaterRefill = !WaterRefill;

            UpdateTimeout();
        }

        public void ToggleLightMain(int privilegeCode)
        {
            CheckControlAccess(privilegeCode);

            Projecteur = !Projecteur;

            UpdateTimeout();
        }

        public void ToggleLightFloor(int privilegeCode)
        {
            CheckControlAccess(privilegeCode);

            LumiereSol = !LumiereSol;

            UpdateTimeout();
        }

        public void TogglePumpMode(int privilegeCode)
        {
            CheckControlAccess(privilegeCode);

            PompeMode = !PompeMode;

            UpdateTimeout();
        }
        public void TogglePumpManual(int privilegeCode)
        {
            CheckControlAccess(privilegeCode);

            PompeManuel = !PompeManuel;

            UpdateTimeout();
        }

        private void CheckControlAccess(int privilegeCode)
        {
            if (!CanControl(privilegeCode))
                throw new Exception("Privilege access not allowed");
        }

        public bool CanControl(int privilegeCode)
        {
            return CanGrantPrivilegeAccess || privilegeCode == _currentPrivilegeCode;
        }

        private void UpdateTimeout()
        {
            DateTimeout = DateTime.Now + PrivilageAccessDuration;
        }


        public void GrantPrivilegeAccess(string pseudo, int privilegeCode)
        {
            if (CanGrantPrivilegeAccess)
            {
                Pseudo = pseudo;
                _currentPrivilegeCode = privilegeCode;
                UpdateTimeout();
            }
            else
                throw new Exception("Cannot grant privilege access.");
        }

        public void ReleasePrivilegeAccess(int privilegeCode)
        {
            CheckControlAccess(privilegeCode);
            _currentPrivilegeCode = null;
            Pseudo = string.Empty;
        }

        public void ForceReleasePrivilegeAccess(string adminCode)
        {
            if (adminCode == "titi")
            {
                _currentPrivilegeCode = null;
                Pseudo = string.Empty;
            }
        }

        public bool CanGrantPrivilegeAccess => !_currentPrivilegeCode.HasValue || DateTime.Now > DateTimeout;
        public DateTime DateTimeout { get; private set; }

        public readonly TimeSpan PrivilageAccessDuration = TimeSpan.FromMinutes(60);
        public string Pseudo { get; private set; }
        private int? _currentPrivilegeCode;
        #endregion


        #region Manual physical Inputs
        public bool ButtonLed => Client.ReadSingleCoil(0);
        public bool ButtonProjo => Client.ReadSingleCoil(1);
        public bool ButtonPompe => Client.ReadSingleCoil(2);
        public bool ButtonChauffage => Client.ReadSingleCoil(3);

        public bool ButtonWaterMain => Client.ReadSingleCoil(4);
        public bool ButtonWaterRefill => Client.ReadSingleCoil(5);

        public void ManualInputCheck()
        {
            // Gestion bouton poussoir
            bool buttonLed = ButtonLed;
            if (buttonLed && !_lastButonLed) // Detection de trigger
                LumiereSol = !LumiereSol;
            _lastButonLed = buttonLed;

            bool buttonProjo = ButtonProjo;
            if (buttonProjo && !_lastButonProjo) // Detection de trigger
                Projecteur = !Projecteur;
            _lastButonProjo = buttonProjo;

            bool buttonPompe = ButtonPompe;
            if (buttonPompe && !_lastButonPompe) // Detection de trigger
                PompeManuel = !PompeManuel;
            _lastButonPompe = buttonPompe;

            bool buttonChauffage = ButtonChauffage;
            if (buttonChauffage && !_lastButonChauffage) // Detection de trigger
                Chauffage = !Chauffage;
            _lastButonChauffage = buttonChauffage;

            bool buttonWaterMain = ButtonWaterMain;
            if (buttonChauffage && !_lastButtonWaterMain) // Detection de trigger
                WaterMain = !WaterMain;
            _lastButtonWaterMain = buttonWaterMain;

            bool buttonWaterRefill = ButtonWaterRefill;
            if (buttonWaterRefill && !_lastButtonWaterRefill) // Detection de trigger
                WaterRefill = !WaterRefill;
            _lastButtonWaterRefill = buttonWaterRefill;
        }

        private bool _lastButonLed;
        private bool _lastButonProjo;
        private bool _lastButonPompe;
        private bool _lastButonChauffage;
        private bool _lastButtonWaterMain;
        private bool _lastButtonWaterRefill;
        #endregion

        #region Properties
        public double TemperatureEau => Client.ReadHoldingRegisters(0, 1)[0] / 10.0;
        public double TemperatureAir => Client.ReadHoldingRegisters(1, 1)[0] / 10.0;

        public bool Projecteur
        {
            get
            {
                return Client.ReadSingleCoil(0x200 + 4);
            }
            private set
            {
                Client.WriteSingleCoil(0 + 4, value);
            }
        }
        public bool LumiereSol {
            get
            {
                return Client.ReadSingleCoil(0x200+5);
            }
            private set
            {
                Client.WriteSingleCoil(0+5, value);
            }
        }
       // public bool PompeManuel { get; private set; }
        public bool PompeManuel {
            get
            {
                return Client.ReadSingleCoil(0x200+1);
            }
            private set
            {
                Client.WriteSingleCoil(0+1, value);
            }
        }
        public bool Chauffage {
            get
            {
                return Client.ReadSingleCoil(0x200);
            }
            private set
            {
                Client.WriteSingleCoil(0, value);
            }
        }
        public bool PompeMode { get; private set; }

        public bool WaterMain
        {
            get
            {
                return Client.ReadSingleCoil(0x200 + 2);
            }
            private set
            {
                Client.WriteSingleCoil(0 + 2, value);
            }
        }

        public bool WaterRefill
        {
            get
            {
                return Client.ReadSingleCoil(0x200 + 3);
            }
            private set
            {
                Client.WriteSingleCoil(0 + 3, value);
            }
        }

        public uint TempsActivation { get; set; }
        public uint TempsDesactivation { get; set; }

        public List<MesureTemperature> HistoriqueTemperatureEau { get; set; }
        public List<MesureTemperature> HistoriqueTemperatureAir { get; set; }
        public int IntervalSecondsMesureTemperature { get; set; }
        public int HistoriqueCountMax { get; set; }

        public string TempsRestantPompe => _pompeTimer.RemainingTime.ToString(@"mm\:ss");

        public string HistoriqueTemperatureEauSerialized
        {
            get
            {
                return WoopsaJsonData.CreateFromDeserializedData(HistoriqueTemperatureEau).Serialize();
            }
        }

        public string HistoriqueTemperatureAirSerialized
        {
            get
            {
                return WoopsaJsonData.CreateFromDeserializedData(HistoriqueTemperatureAir).Serialize();
            }
        }
        #endregion

        public object locker = new object();
        private DownTimer _pompeTimer;
        private DownTimer _measureTimer;


        #region Modbus
        private ModbusClientTcp Client
        {
            get
            {
                if (!_client.IsConnected)
                    _client.Connect(Host, 502, 1000);
                return _client;
            }
            set
            {
                _client = value;
            }
        }
        public string Host { get; protected set; }

        private ModbusClientTcp _client; 
        #endregion
    }
}
