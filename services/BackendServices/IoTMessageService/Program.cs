using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;

namespace IoTMessageService
{
    class Program
    {
        static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddHostedService<IoTMessageService>();
                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.AddSingleton<ITelemetryInitializer, CustomInitializer>();
                    services.AddHttpClient();
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json");
                    config.AddEnvironmentVariables(prefix: "IOT_E2E_");
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                });
        }
    }
}
