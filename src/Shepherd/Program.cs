using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shepherd
{
    public class Program
    {
        private static Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                           .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                           .UseLamar((_, registry) => registry.Include(new ShepherdRegistry()))
                           .ConfigureAppConfiguration(builder =>
                           {
                               builder.AddJsonFilesFromDirectory("/etc/vault-shepherd/");
                               builder.AddJsonFilesFromDirectory(@"C:\ProgramData\vault-shepherd\");
                           })
                           .UseConsoleLifetime()
                           .Build();

            Startup(host.Services);

            return host.RunAsync();
        }

        private static void Startup(IServiceProvider services)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            var configuration = (IConfigurationRoot) services.GetRequiredService<IConfiguration>();

            var paths = configuration.Providers
                                     .Select(x => x as FileConfigurationProvider)
                                     .Select(x => x?.Source?.Path);
            foreach (var path in paths)
            {
                if (path != null
                    && File.Exists(path))
                {
                    logger.LogInformation($"Sourced JSON file '{path}'.");
                }
            }
        }
    }
}