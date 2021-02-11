using System.Threading.Tasks;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shepherd
{
    public static class Program
    {
        private static Task Main(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                       .ConfigureServices(Startup.ConfigureServices)
                       .ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                       .UseLamar((_, registry) => registry.Include(new ShepherdRegistry()))
                       .RunConsoleAsync();
        }
    }
}