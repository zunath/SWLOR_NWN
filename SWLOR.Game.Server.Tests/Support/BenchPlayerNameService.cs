using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The player name instance with its seams answered by the test: the disguise, the descriptors, the scheduler and the script-context
/// hook. The seams are split into partial files by the part whose code calls each one
/// (<c>Readmes/MaintainabilityRefactorCharacterCompanionsSplit.md</c>): <c>BenchPlayerNameService.Names.cs</c> and
/// <c>BenchPlayerNameService.Overrides.cs</c>. The part accessors name the service whose code a test reaches; each is this one
/// instance until the part moves, and the part's pull request changes only the type and the body of its accessor.
/// </summary>
public sealed partial class BenchPlayerNameService : PlayerNameService
{
    public BenchPlayerNameService(IDBService db, IPlayerListService list, IChatPortraitTellsService portraits)
        : base(db, list, portraits, FacadeForwarder.LazyFor<IDisguiseService>(), FacadeForwarder.LazyFor<IPlayerDescriptorService>())
    {
        KnownNames = new PlayerKnownNameService(db, this, list);
    }

    /// <summary>The known name and descriptor caches, the display names and the audit names (the stay-part).</summary>
    public BenchPlayerNameService Names => this;

    /// <summary>The <c>/name</c> validation and writes: the real part, over this instance.</summary>
    public PlayerKnownNameService KnownNames { get; }

    /// <summary>The name overrides, AFK and the tell handler.</summary>
    public BenchPlayerNameService Overrides => this;
}
