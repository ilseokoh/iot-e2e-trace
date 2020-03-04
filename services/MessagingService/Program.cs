using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessagingService
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builtConfig = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            return Host.CreateDefaultBuilder(args)
               .ConfigureServices(services =>
               {
                   services.AddLogging();
                   services.AddHostedService<MessagingService>();
               })
               .ConfigureAppConfiguration((hostingContext, config) =>
               {
                   config.AddConfiguration(builtConfig);
               })
               .ConfigureLogging(logging =>
               {
                   logging.AddConsole();
               });
        }
    }
}
