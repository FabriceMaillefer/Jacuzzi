using Modbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Woopsa;

namespace Jacuzzi
{
    [WoopsaVisibility(WoopsaVisibility.DefaultIsVisible)]
    //[WoopsaVisibility(WoopsaVisibility.DefaultIsVisible)]
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
        public const int HistoriqueCountMax = 100;
        public Jacuzzi(string host)
        {
            Client = new ModbusClientTcp();
            Host = host;

            TempsActivation = 5;
            TempsCycle = 15;

            PompeMode = true;
            PompeManuel = true;

            Projecteur = false;
            LumiereSol = false;


            HistoriqueTemperatureEau = new List<MesureTemperature>(HistoriqueCountMax);
            HistoriqueTemperatureAir = new List<MesureTemperature>(HistoriqueCountMax);

        }

        #region Session Control

        public void ControlLightMain(int privilegeCode, bool state)
        {
            CheckControlAccess(privilegeCode);

            Projecteur = state;

            UpdateTimeout();
        }

        public void ControlLightFloor(int privilegeCode, bool state)
        {
            CheckControlAccess(privilegeCode);

            LumiereSol = state;

            UpdateTimeout();
        }

        public void ControlPumpMode(int privilegeCode, bool state)
        {
            CheckControlAccess(privilegeCode);

            PompeMode = state;

            UpdateTimeout();
        }
        public void ControlPumpManual(int privilegeCode, bool state)
        {
            CheckControlAccess(privilegeCode);

            PompeManuel = state;

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
            set
            {
                Client.WriteSingleCoil(0 + 4, value);
            }
        }
        public bool LumiereSol {
            get
            {
                return Client.ReadSingleCoil(0x200+5);
            }
            set
            {
                Client.WriteSingleCoil(0+5, value);
            }
        }
        public bool PompeManuel {
            get
            {
                return Client.ReadSingleCoil(0x200+1);
            }
            set
            {
                Client.WriteSingleCoil(0+1, value);
            }
        }

        public bool Chauffage {
            get
            {
                return Client.ReadSingleCoil(0x200);
            }
            set
            {
                Client.WriteSingleCoil(0, value);
            }
        }

        public bool PompeMode { get; private set; }        

        public uint TempsActivation { get; set; }
        public uint TempsCycle { get; set; }

        public List<MesureTemperature> HistoriqueTemperatureEau { get; set; }
        public List<MesureTemperature> HistoriqueTemperatureAir { get; set; }


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
