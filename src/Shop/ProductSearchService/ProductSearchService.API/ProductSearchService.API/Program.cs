using FluentTimeSpan;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

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
                .ConfigureKestrel(configureOptions: (context, options) =>
                {
                    if (context.HostingEnvironment.EnvironmentName == "Development")
                    {
                        options.ListenAnyIP(port: 5101);
                    }
                })
                .UseHealthChecks(path: "/health", timeout: 3.Seconds())
                .UseApplicationInsights(instrumentationKey: "ProductSearchService.API")
                .ConfigureLogging(configureLogging: (hostingContext, logging) =>
                {
                    logging.AddConsole();
                })
                .Build();
    }
}
