using System.Threading.Tasks;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shepherd
{
    public static class Program
    {
        private static Task Main(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                       .UseLamar((_, registry) => registry.Include(new ShepherdRegistry()))
                       .ConfigureAppConfiguration(builder =>
                       {
                           builder.AddJsonFile("/etc/vault-shepherd/settings.json", true);
                           builder.AddJsonFile(@"C:\ProgramData\vault-shepherd\settings.json", true);
                       })
                       .RunConsoleAsync();
        }
    }
}