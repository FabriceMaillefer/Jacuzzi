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
                    woopsaServer.BeforeWoopsaModelAccess += (object sender, EventArgs e) => { Monitor.Enter(root.locker); };
                    woopsaServer.AfterWoopsaModelAccess += (object sender, EventArgs e) => { Monitor.Exit(root.locker); };
                    
                    woopsaServer.WebServer.Routes.Add("web", HTTPMethod.GET, new RouteHandlerEmbeddedResources("HTML", Assembly.GetEntryAssembly()));
                    woopsaServer.WebServer.Routes.Add("web", HTTPMethod.GET, new RouteHandlerEmbeddedResources("HTML.index.html", Assembly.GetEntryAssembly()));

                    Console.WriteLine("Woopsa server started. Client at http://localhost:10001/web/");

                    for (;;)
                    {
                        try
                        {
                            lock (root.locker)
                                root.CyclicUpdate();
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
