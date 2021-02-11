namespace Shepherd.Core.Models
{
    public record VaultEvent(Vault Vault);

    public record UnsealedVaultEvent(Vault Vault) : VaultEvent(Vault);
}
