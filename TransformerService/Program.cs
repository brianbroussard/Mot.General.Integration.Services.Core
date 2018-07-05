using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using NLog;
using MotCommonLib;
using MotListenerLib;
using MotParserLib;

//using TransformerService.Controllers;

namespace TransformerService
{
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

