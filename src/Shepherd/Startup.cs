using Microsoft.Extensions.DependencyInjection;
using Shepherd.Core.Factories;

namespace Shepherd
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<Daemon>();

            services.AddSingleton(provider => provider.GetRequiredService<ShepherdConfigurationFactory>().Create());
            services.AddSingleton(provider => provider.GetRequiredService<VaultClientFactory>().CreateHaClient());
        }
    }
}