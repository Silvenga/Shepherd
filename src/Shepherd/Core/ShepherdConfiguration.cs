using System;
using System.Collections.Generic;

namespace Shepherd.Core
{
    public record ShepherdConfiguration(
        string VaultToken,
        Uri VaultHaAddress,
        IReadOnlyList<Uri> VaultMembers,
        IReadOnlyList<string> WrappedUnsealingKeys,
        string TransitKeyName
    );
}