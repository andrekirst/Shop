using FluentTimeSpan;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ProductSearchService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args: args).Run();
        }

        private static IWebHost CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args: args)
                .UseStartup<Startup>()
                .UseHealthChecks(path: "/health", timeout: 3.Seconds())
                .UseApplicationInsights(instrumentationKey: "ProductSearchService.API")
                .UseSerilog()
                .ConfigureLogging(configureLogging: (hostingContext, logging) =>
                    {
                        logging.AddConfiguration(configuration: hostingContext.Configuration.GetSection(key: "Logging"));
                        logging.AddConsole();
                        logging.AddDebug();
                    })
                .Build();
    }
}
