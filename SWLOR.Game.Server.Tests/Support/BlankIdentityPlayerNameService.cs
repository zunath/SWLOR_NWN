using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The player name instance whose identity key is blank, for the test that a target without an identity cannot be named.</summary>
public sealed class BlankIdentityPlayerNameService : PlayerNameService
{
    public BlankIdentityPlayerNameService(IDBService db)
        : base(db, null!, null!, FacadeForwarder.LazyFor<IDisguiseService>(), FacadeForwarder.LazyFor<IPlayerDescriptorService>())
    {
    }

    /// <summary>The part that validates a known name (this instance until the part moves).</summary>
    public BlankIdentityPlayerNameService KnownNames => this;

    protected override string GetIdentityKey(uint player) => " ";
}
