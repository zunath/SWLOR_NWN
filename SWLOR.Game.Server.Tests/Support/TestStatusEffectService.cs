using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The instance with its calls into unconverted code answered by the bench: part 1 of the status effect split,
/// <c>StatusEffectService</c>, with every part's seams until the part moves. Each part's seams are in a file of their own
/// (<c>.Core.cs</c> for the tracker, then <c>.Catalog.cs</c>, <c>.Application.cs</c>, <c>.Duration.cs</c>, <c>.Native.cs</c>,
/// <c>.CombatEvents.cs</c> and <c>.Session.cs</c>), so the pull request that moves a part moves its file into a double of that
/// part's type. What a seam answers is a property of the bench (<c>StatusEffectBench.Seams.cs</c>).
/// </summary>
public sealed partial class TestStatusEffectService : StatusEffectService
{
    private readonly StatusEffectBench _bench;

    public TestStatusEffectService(
        StatusEffectBench bench,
        IResistanceService resistance,
        IPvpCombatService pvpCombat,
        IStealthService stealth,
        IMeleeTechniqueService meleeTechnique,
        ICombatRecorderService combatRecorder,
        IMessagingService messaging,
        IPlayerNameService playerName,
        IGuiService gui,
        ICapstoneProofService capstoneProof,
        IDBService db,
        IQuickCastService quickCast)
        : base(resistance, pvpCombat, stealth, meleeTechnique, combatRecorder, messaging, playerName, gui, capstoneProof, db, quickCast, FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IActiveAbilityImpact>(), FacadeForwarder.LazyFor<ICombatAbilityImpactService>(), FacadeForwarder.LazyFor<ICombatAttackTimingService>())
    {
        _bench = bench;
    }
}
