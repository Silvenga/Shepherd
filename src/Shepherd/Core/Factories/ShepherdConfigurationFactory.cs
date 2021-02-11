using Microsoft.Extensions.Configuration;
using Shepherd.Core.Models;

namespace Shepherd.Core.Factories
{
    public class ShepherdConfigurationFactory
    {
        private readonly IConfiguration _configuration;

        public ShepherdConfigurationFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ShepherdConfiguration Create()
        {
            return _configuration.Get<ShepherdConfiguration>();
        }
    }
}