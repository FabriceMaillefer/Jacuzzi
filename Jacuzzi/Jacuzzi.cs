using Modbus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Woopsa;

namespace Jacuzzi
{
    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible)]
    public class MesureTemperature
    {
        public string x { get; set; }
        public double y { get; set; }
    }

    public class JacuzziParameters
    {
        public uint TempsActivation { get; set; }
        public uint TempsDesactivation { get; set; }
        public int IntervalSecondsMesureTemperature { get; set; }
        public int HistoriqueCountMax { get; set; }
        public List<MesureTemperature> HistoriqueTemperatureEau { get; set; } = new List<MesureTemperature>();
        public List<MesureTemperature> HistoriqueTemperatureAir { get; set; } = new List<MesureTemperature>();

        public List<MesureTemperature> HistoriquePompe { get; set; } = new List<MesureTemperature>();

        public static void Serialize(JacuzziParameters param)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(JacuzziParameters));
            using (StreamWriter writer = new StreamWriter("Jacuzzi.xml"))
            {
                serializer.Serialize(writer, param);
            }
        }

        public static JacuzziParameters DeSerializeOrCreate()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(JacuzziParameters));
                using (StreamReader reader = new StreamReader("Jacuzzi.xml"))
                {
                    return serializer.Deserialize(reader) as JacuzziParameters;
                }
            }
            catch
            {
                JacuzziParameters param = new JacuzziParameters();

                param.HistoriqueCountMax = 144;
                param.IntervalSecondsMesureTemperature = 600;
                param.TempsActivation = 10;
                param.TempsDesactivation = 20;

                return param;
            }
        }
    }

    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible | WoopsaVisibility.IEnumerableObject)]
    //[WoopsaVisibility(WoopsaVisibility.All)]
    public class Jacuzzi
    {
        private const string DateTimeFormat = "yyyy'-'MM'-'dd HH':'mm':'ss";

        public Jacuzzi(JacuzziParameters parameters, string host)
        {
            _parameters = parameters;

            Client = new ModbusClientTcp();
            Host = host;

            /*TempsActivation = 5;
            TempsDesactivation = 15;*/

            _pompeTimer = new DownTimer(TimeSpan.FromMinutes(TempsActivation));

            PompeMode = true;
            PompeManuel = false;

            Projecteur = false;
            LumiereSol = false;

            WaterMain = false;
            WaterRefill = false;

            /*HistoriqueCountMax = 144;
            IntervalSecondsMesureTemperature = 600;*/

            _measureTimer = new DownTimer(TimeSpan.Zero);

            /*HistoriqueTemperatureEau = new List<MesureTemperature>(HistoriqueCountMax);
            HistoriqueTemperatureAir = new List<MesureTemperature>(HistoriqueCountMax);*/
            CleanHistorique();
        }

        public void CleanHistorique()
        {
            DateTime TimeToKeep = DateTime.Now - TimeSpan.FromHours(4);

            HistoriqueTemperatureAir.RemoveAll(_ => DateTime.ParseExact(_.x, DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture) < TimeToKeep);
            HistoriqueTemperatureEau.RemoveAll(_ => DateTime.ParseExact(_.x, DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture) < TimeToKeep);
            HistoriquePompe.RemoveAll(_ => DateTime.ParseExact(_.x, DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture) < TimeToKeep);
        }

        private JacuzziParameters _parameters;

        #region Process
        public void CyclicUpdate()
        {
            ManualInputCheck();

            // Auto désactivation du remplissage
            if (WaterLevel)
                WaterRefill = false;

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

                HistoriqueTemperatureAir.Add(new MesureTemperature() { x = DateTime.Now.ToString(DateTimeFormat), y = TemperatureAir });
                HistoriqueTemperatureEau.Add(new MesureTemperature() { x = DateTime.Now.ToString(DateTimeFormat), y = TemperatureEau });

                CleanHistorique();

                /*
                while (HistoriqueTemperatureAir.Count >= HistoriqueCountMax)
                {
                    HistoriqueTemperatureAir.RemoveAt(0);
                }

                while (HistoriqueTemperatureEau.Count >= HistoriqueCountMax)
                {
                    HistoriqueTemperatureEau.RemoveAt(0);
                }*/

                JacuzziParameters.Serialize(_parameters);
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

            _pompeTimer.Restart();

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
        public bool ButtonWaterRefill => Client.ReadSingleCoil(6);

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
            if (buttonWaterMain && !_lastButtonWaterMain) // Detection de trigger
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
        public bool LumiereSol
        {
            get
            {
                return Client.ReadSingleCoil(0x200 + 5);
            }
            private set
            {
                Client.WriteSingleCoil(0 + 5, value);
            }
        }
        // public bool PompeManuel { get; private set; }
        public bool PompeManuel
        {
            get
            {
                return Client.ReadSingleCoil(0x200 + 1);
            }
            private set
            {
                HistoriquePompe.Add(new MesureTemperature()
                {
                    x = DateTime.Now.ToString(DateTimeFormat),
                    y = value ? 1 : 0,
                });

                CleanHistorique();

                Client.WriteSingleCoil(0 + 1, value);
            }
        }
        public bool Chauffage
        {
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
                if (!value)
                    WaterRefill = false;
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
                if (value)
                    WaterMain = true;
                Client.WriteSingleCoil(0 + 3, value);
            }
        }

        public bool WaterLevel => Client.ReadSingleCoil(8);

        public uint TempsActivation { get => _parameters.TempsActivation; set { _parameters.TempsActivation = value; } }
        public uint TempsDesactivation { get => _parameters.TempsDesactivation; set { _parameters.TempsDesactivation = value; } }

        public List<MesureTemperature> HistoriqueTemperatureEau => _parameters.HistoriqueTemperatureEau;
        public List<MesureTemperature> HistoriqueTemperatureAir => _parameters.HistoriqueTemperatureAir;
        public List<MesureTemperature> HistoriquePompe => _parameters.HistoriquePompe;

        public int IntervalSecondsMesureTemperature { get => _parameters.IntervalSecondsMesureTemperature; set { _parameters.IntervalSecondsMesureTemperature = value; } }
        public int HistoriqueCountMax { get => _parameters.HistoriqueCountMax; set { _parameters.HistoriqueCountMax = value; } }

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

        public string HistoriquePompeSerialized
        {
            get
            {
                return WoopsaJsonData.CreateFromDeserializedData(HistoriquePompe).Serialize();
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
                    _client.Connect(Host, 502, 5000);
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
