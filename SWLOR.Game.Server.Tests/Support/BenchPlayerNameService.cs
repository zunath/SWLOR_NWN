using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The player name instance with its seams answered by the test: the disguise, the descriptors and the script-context hook, in
/// <c>BenchPlayerNameService.Names.cs</c>. The part accessors name the service whose code a test reaches: this instance for the display
/// names and the known name caches, and a real part built on it for the known name writes and the name overrides
/// (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>).
/// </summary>
public sealed partial class BenchPlayerNameService : PlayerNameService
{
    public BenchPlayerNameService(IDBService db, IPlayerListRefresh list, IPortraitTellGuard portraits)
        : base(db, FacadeForwarder.LazyFor<IDisguiseService>(), FacadeForwarder.LazyFor<IPlayerDescriptorService>())
    {
        Overrides = new BenchPlayerNameOverrideService(this, list, portraits);
        KnownNames = new PlayerKnownNameService(db, this, Overrides, list);
    }

    /// <summary>The known name and descriptor caches, the display names and the audit names (the stay-part).</summary>
    public BenchPlayerNameService Names => this;

    /// <summary>The <c>/name</c> validation and writes: the real part, over this instance.</summary>
    public PlayerKnownNameService KnownNames { get; }

    /// <summary>The name overrides, AFK and the tell handler: the real part, over this instance.</summary>
    public BenchPlayerNameOverrideService Overrides { get; }
}
