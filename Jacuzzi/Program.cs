using System;
using System.IO;
using System.Linq;
using System.Threading;
using Woopsa;
using System.Reflection;

namespace Jacuzzi
{
    class Program
    {
        #region Manage arguments
        static string ManageHostArg(string[] args)
        {
            string host = "172.16.6.3";
            const string ipFlag = "-ip:";
            foreach (string arg in args)
            {
                if (arg.StartsWith(ipFlag))
                    host = arg.Substring(ipFlag.Count());
            }
            return host;
        }

        static bool ManageVerboseArg(string[] args)
        {
            bool verbose = false;
            const string verboseFlag = "-verbose:";
            foreach (string arg in args)
            {
                if (arg.StartsWith(verboseFlag))
                    verbose = true;
            }
            return verbose;
        }

        static int ManageRefreshArg(string[] args)
        {
            int refresh = 80;
            const string refreshFlag = "-refresh:";
            foreach (string arg in args)
            {
                if (arg.StartsWith(refreshFlag))
                    if (!int.TryParse(arg.Substring(refreshFlag.Count()), out refresh))
                    {
                        Console.WriteLine(refreshFlag + " bad format. Interger expected");
                    }
            }
            return refresh;
        }

        public static bool VerboseMode { get; private set; }
        #endregion

        #region Main entry point
        static void Main(string[] args)
        {
            int refresh = ManageRefreshArg(args);
            VerboseMode = ManageVerboseArg(args);

            try
            {
                Jacuzzi root = new Jacuzzi(ManageHostArg(args));
                using (WoopsaServer woopsaServer = new WoopsaServer(root, 10001))
                {
                    woopsaServer.AfterWoopsaModelAccess += (object sender, EventArgs e) => { Monitor.Exit(root.locker); };
                    woopsaServer.BeforeWoopsaModelAccess += (object sender, EventArgs e) => { Monitor.Enter(root.locker); };
                    
                    woopsaServer.WebServer.Routes.Add("web", HTTPMethod.GET, new RouteHandlerEmbeddedResources("HTML", Assembly.GetEntryAssembly()));
                    woopsaServer.WebServer.Routes.Add("web", HTTPMethod.GET, new RouteHandlerEmbeddedResources("HTML.index.html", Assembly.GetEntryAssembly()));

                    for (;;)
                    {
                        try
                        {
                            lock (root.locker)
                                CyclicProcess(root);
                        }
                        catch (Exception e)
                        {
                            ManageException(e);
                            Thread.Sleep(refresh * 3);
                        }
                        Thread.Sleep(refresh);
                    }
                }
            }
            catch (Exception e)
            {
                ManageException(e);
            }
        }
        #endregion

        #region Process
        static void CyclicProcess(Jacuzzi root)
        {
            // Gestion bouton poussoir
            bool buttonLed = root.ButtonLed;
            if (buttonLed && !_lastButonLed) // Detection de trigger
                root.LumiereSol = !root.LumiereSol;
            _lastButonLed = buttonLed;

            bool buttonProjo = root.ButtonProjo;
            if (buttonProjo && !_lastButonProjo) // Detection de trigger
                root.Projecteur = !root.Projecteur;
            _lastButonProjo = buttonProjo;

            bool buttonPompe = root.ButtonPompe;
            if (buttonPompe && !_lastButonPompe) // Detection de trigger
                root.PompeManuel = !root.PompeManuel;
            _lastButonPompe = buttonPompe;

            bool buttonChauffage = root.ButtonChauffage;
            if (buttonChauffage && !_lastButonChauffage) // Detection de trigger
                root.Chauffage = !root.Chauffage;
            _lastButonChauffage = buttonChauffage;

            // Gestion timer pompe
            if (root.PompeMode) // Mode auto
            {
                bool pompeManuel = root.PompeManuel;

                if (_pompeTimer.Elapsed)
                {
                    pompeManuel = !pompeManuel;

                    root.PompeManuel = pompeManuel;
                    
                    if (pompeManuel)
                        _pompeTimer.SetTimeout(TimeSpan.FromMinutes(root.TempsActivation));
                    else
                        _pompeTimer.SetTimeout(TimeSpan.FromMinutes(root.TempsCycle - root.TempsActivation));

                    _pompeTimer.Restart();
                }
            }
            else
            {
                _pompeTimer.SetTimeout(TimeSpan.FromMinutes(root.TempsActivation));
            }

            // Historique des mesures
            if (_measureTimer.Elapsed)
            {
                _measureTimer.Restart();

                root.HistoriqueTemperatureAir.Add(new MesureTemperature(DateTime.Now, root.TemperatureAir));
                root.HistoriqueTemperatureEau.Add(new MesureTemperature(DateTime.Now, root.TemperatureEau));

                if (root.HistoriqueTemperatureAir.Count >= Jacuzzi.HistoriqueCountMax)
                {
                    root.HistoriqueTemperatureAir.RemoveAt(0);
                }

                if (root.HistoriqueTemperatureEau.Count >= Jacuzzi.HistoriqueCountMax)
                {
                    root.HistoriqueTemperatureEau.RemoveAt(0);
                }
            }
        }
        static bool _lastButonLed;
        static bool _lastButonProjo;
        static bool _lastButonPompe;
        static bool _lastButonChauffage;
        static DownTimer _pompeTimer = new DownTimer();
        static DownTimer _measureTimer = new DownTimer(TimeSpan.FromSeconds(30));

        #endregion

        #region Error management
        static void ManageException(Exception e)
        {
            if (VerboseMode)
            {
                string message = string.Format("{0} - Error : {1}", DateTime.Now, e.Message);
                File.AppendAllText(@"Error.log", message + "\n");
                Console.WriteLine(message);
            }
        }
        #endregion
    }
}
