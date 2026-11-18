using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The instance with its calls into unconverted code answered by the bench: part 1 of the status effect split,
/// <c>StatusEffectService</c>, with every part's seams until the part moves. Each part's seams are in a file of their own
/// (<c>.Core.cs</c> for the tracker), as the other parts' seams are in their own doubles: the catalog's is
/// <see cref="TestStatusEffectCatalogService"/> (P5-5c), the session's <see cref="TestStatusEffectSessionService"/> (P5-5e),
/// the native effect's <see cref="TestStatusEffectNativeService"/> (P5-5h).
/// What a seam answers is a property of the bench (<c>StatusEffectBench.Seams.cs</c>).
/// </summary>
public sealed partial class TestStatusEffectService : StatusEffectService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectService(
        StatusEffectBench bench,
        IStatusEffectCatalogService statusCatalog,
        IMessagingService messaging,
        IPlayerNameService playerName,
        IGuiService gui)
        : base(statusCatalog, messaging, playerName, gui)
    {
        _bench = bench;
    }
}
