using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Xml;
using Microsoft.Deployment.WindowsInstaller;

namespace Mot.Installer.CustomActions
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult StopMotIntegrationService(Session session)
        {
            return StopService(session, "motNextTransformer");
        }

        private static ActionResult StopService(Session session, string serviceName)
        {
            session.Log($"Begin stopping {serviceName}...");
            try
            {
                var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
                if (service == null)
                {
                    session.Log($"{serviceName} doesn't exist.");
                    return ActionResult.Success;
                }
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                    service.Refresh();
                    session.Log($"{serviceName} is stopped.");
                }
                else
                {
                    session.Log($"{serviceName} isn't running.");
                }
            }
            catch (Exception ex)
            {
                session.Log($"Error: {ex.Message}");
                return ActionResult.Failure;
            }
            session.Log($"End stopping {serviceName}.");

            return ActionResult.Success;
        }
    }
}
