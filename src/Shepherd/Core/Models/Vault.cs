using System;

namespace Shepherd.Core.Models
{
    public record Vault(Uri Address)
    {
        public override string ToString()
        {
            return Address.ToString();
        }
    }
}