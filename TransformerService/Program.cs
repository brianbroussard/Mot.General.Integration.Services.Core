// MIT license
//
// Copyright (c) 2018 by Peter H. Jenney and Medicine-On-Time, LLC.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System;
using System.ServiceProcess;
using MotParserLib;
using NLog;
using Topshelf;

//using TransformerService.Controllers;

namespace TransformerService
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
                    x.Service<MotTransformerInterface>(s =>
                    {
                        s.ConstructUsing(name => new MotTransformerInterface());
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                    });

                    x.StartAutomatically();
                    x.RunAsLocalSystem();

                    x.SetDescription("MOT Data Transformation Interface");
                    x.SetDisplayName("motNext Transformer");
                    x.SetServiceName("motNextTransformer");

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