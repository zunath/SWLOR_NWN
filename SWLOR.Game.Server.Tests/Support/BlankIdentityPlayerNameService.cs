using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The player name instance whose identity key is blank, for the test that a target without an identity cannot be named.</summary>
public sealed class BlankIdentityPlayerNameService : PlayerNameService
{
    public BlankIdentityPlayerNameService(IDBService db)
        : base(db, FacadeForwarder.LazyFor<IDisguiseService>(), FacadeForwarder.LazyFor<IPlayerDescriptorService>())
    {
        KnownNames = new PlayerKnownNameService(db, this, null!, null!);
    }

    /// <summary>The part that validates a known name, over this instance.</summary>
    public PlayerKnownNameService KnownNames { get; }

    public override string GetIdentityKey(uint player) => " ";
}
