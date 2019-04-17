using System.Security.Cryptography.X509Certificates;
using FluentTimeSpan;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ProductSearchService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args: args)
                .Run();
        }

        private static IWebHost CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder<Startup>(args: args)
                .UseKestrel()
                .ConfigureKestrel((context, options) =>
                {
                    if (context.HostingEnvironment.EnvironmentName == "Development")
                    {
                        options.ListenAnyIP(5101);
                    }
                })
                .UseHealthChecks(path: "/health", timeout: 3.Seconds())
                .UseApplicationInsights(instrumentationKey: "ProductSearchService.API")
                .UseSerilog()
                .ConfigureLogging(configureLogging: (hostingContext, logging) =>
                {
                    logging.AddConfiguration(configuration: hostingContext.Configuration.GetSection(key: "Logging"));
                    logging.AddConsole(configure =>
                    {
                        configure.IncludeScopes = false;
                        configure.TimestampFormat = "hh:mm:ss.FFF";

                    });
                })
                .Build();
    }
}
