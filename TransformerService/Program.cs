using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Topshelf;
using NLog;
using MotCommonLib;
using MotListenerLib;
using MotParserLib;

namespace TransformerService
{
    public class MotTransformerInterface
    {
        private Logger EventLogger;
        private Hl7SocketListener SocketListener;
        private FilesystemListener FilesystemListener;
        private string GatewayAddress;
        private int GatewayPort;
        private int ListenerPort;
        private string MonitorDirectory;
        private bool WatchFileSystem;
        private bool WatchSocket;

        private void LoadConfiguration()
        {
            var appSettings = ConfigurationManager.AppSettings;

            ListenerPort = Convert.ToInt32(appSettings["ListenerPort"] ?? "24025");
            GatewayPort = Convert.ToInt32(appSettings["GatewayPort"] ?? "24042");
            GatewayAddress = appSettings["GatewayAddress"] ?? "127.0.0.1";
            MonitorDirectory = appSettings["MonitorDirectory"] ?? @"c:\motnext\io";

            WatchFileSystem = (appSettings["WatchFileSystem"] ?? "false") == "true";
            WatchSocket = (appSettings["WatchSocket"] ?? "false") == "true";
        }
        private string Parse(string data)
        {
            var resp = data;

            using (var GatewaySocket = new MotSocket(GatewayAddress, GatewayPort))
            {
                using (var p = new MotParser(GatewaySocket, data, InputDataFormat.AutoDetect))
                {
                    EventLogger.Info(p.ResponseMessage);
                    resp = p.ResponseMessage;
                }
            }

            return resp;
        }
        /// <summary>
        /// Constructor
        /// </summary>
        public MotTransformerInterface()
        {
            try
            {
                LoadConfiguration();
                EventLogger = LogManager.GetLogger("Mot.Transformer.Service");
            }
            catch (Exception ex)
            {
                EventLogger?.Fatal("Failed to start construct HL7Execute: {0}", ex.Message);
                throw new Exception("Constructor Failure");
            }
        }
        /// <summary>
        /// <c>Startup</c>
        /// The method caled by Topshelf to start the service running
        /// </summary>
        public void Start()
        {
            if (WatchSocket)
            {
                SocketListener = new Hl7SocketListener(ListenerPort, Parse);
                SocketListener.RunAsService = true;
                SocketListener.Go();
            }

            if (WatchFileSystem)
            {
                FilesystemListener = new FilesystemListener(MonitorDirectory, Parse);
                FilesystemListener.RunAsService = true;
                FilesystemListener.Go();
            }

            EventLogger.Info("Service started");
        }
        /// <summary>
        /// <c>StopListener</c>
        /// The method called by Topshelf to halt the service
        /// </summary>
        public void Stop()
        {
            if (WatchSocket)
            {
                SocketListener.ShutDown();
            }

            if(WatchFileSystem)
            {
                FilesystemListener.ShutDown();
            }

            EventLogger.Info("sevice stopped");
        }
        /// <summary>
        /// <c>ShutDown</c>
        /// Same as stop for now
        /// </summary>
        public void ShutDown()
        {
            this.Stop();
        }
        /// <summary>
        /// <c>Restart</c>
        /// Method called by Topshellf to restart the service
        /// </summary>
        public void Restart()
        {
            if (WatchSocket)
            {
                SocketListener.ShutDown();
                SocketListener.Go();
            }

            if (WatchFileSystem)
            {
                FilesystemListener.ShutDown();
                FilesystemListener.Go();
            }

            EventLogger.Info("Service restarted");
        }
    }
    internal static class ConfigureService
    {
        internal static void Configure()
        {
            HostFactory.Run(x =>
            {
                x.Service<MotTransformerInterface>(s =>
                {
                    s.ConstructUsing(name => new MotTransformerInterface());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());

                });

                x.StartAutomatically();
                x.RunAsLocalSystem();

                x.SetDescription("MOT Universal Interface");
                x.SetDisplayName("MotNext Transformer");
                x.SetServiceName("MotNextTransformer");

                x.DependsOnMsmq();
                x.EnableShutdown();

                x.EnableServiceRecovery(r =>
                {
                    r.RestartService(0);
                });
            });
        }
        class Program
        {
            public static void Main()
            {
                ConfigureService.Configure();
            }
        }
    }
}

