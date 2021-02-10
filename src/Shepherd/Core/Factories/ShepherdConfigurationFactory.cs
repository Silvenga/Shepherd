using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

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
            return new(
                _configuration.GetValue<string>("vault_token"),
                _configuration.GetValue<Uri>("vault_ha_address"),
                _configuration.GetValue<List<Uri>>("vault_member_addresses"),
                _configuration.GetValue<List<string>>("wrapped_unsealing_keys")
            );
        }
    }
}