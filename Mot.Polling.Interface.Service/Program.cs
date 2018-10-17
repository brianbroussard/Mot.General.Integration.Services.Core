using System;
using System.ServiceProcess;
using Topshelf;
using NLog;

namespace TransformerPollingService
{
    internal static class ConfigureService
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Program).FullName);

        internal static void Configure()
        {
            Logger.Info("Starting MOT Transformer Service");

            try
            {
                HostFactory.Run(x =>
                {
                    x.Service<MotTransformerPollingInterface>(s =>
                    {
                        s.ConstructUsing(name => new MotTransformerPollingInterface());
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                    });

                    x.StartAutomatically();
                    x.RunAsLocalSystem();

                    x.SetDescription("MOT Polling Data Transformation Interface");
                    x.SetDisplayName("motNext Polling Transformer");
                    x.SetServiceName("motNextPollingTransformer");

                    x.EnableShutdown();
                    x.BeforeUninstall(StopService);

                    x.EnableServiceRecovery(r => { r.RestartService(0); });
                });

                Logger.Info($"Service started and reports status as {GetStatus()}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to start service: {ex.Message}");
                throw;
            }
        }

        private static string GetStatus()
        {
            try
            {
                var service = new ServiceController("motNextTransformer");
                return service.Status.ToString();
            }
            catch (Exception ex)
            {
                Logger.Info($"Caught while waiting for service status: {ex.Message}");
            }

            return "No Status";
        }

        private static void StopService()
        {
            var service = new ServiceController("motNextTransformer");

            try
            {
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                    service.Refresh();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during Stop service operation.");
            }
        }

        private class Program
        {
            public static void Main()
            {
                Configure();
            }
        }
    }
}
