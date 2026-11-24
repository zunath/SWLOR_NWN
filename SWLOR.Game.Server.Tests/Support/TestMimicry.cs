#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The Mimicry instance with the calls into services that are not converted yet answered by the test. The seams are in partial files by the
/// part whose code calls them (<c>TestMimicry.Witness.cs</c>, <c>.Loadout.cs</c>; <c>.Shared.cs</c> holds the seams both call), so a part
/// that leaves <see cref="MimicryService"/> takes its file with it. This file is the stay-part: the technique catalog and the trait reads.
/// <para>
/// <see cref="Techniques"/>, <see cref="Witness"/> and <see cref="Loadout"/> are the part accessors of the split (MI-1, MI-2, MI-3): each is the one
/// instance until its part moves, and a test calls a member on the accessor of the part that owns it, so the pull request that moves the part
/// changes what the accessor returns and no test.
/// </para>
/// </summary>
public sealed partial class TestMimicry : MimicryService
{
    public TestMimicry(IDBService db, IQuickCastService quickCast, IDMPresenceService presence, IPlayerFeedbackService feedback, IGuiService gui)
        : base(db, quickCast, presence, feedback, gui, FacadeForwarder.For<IAbilityCatalog>(), FacadeForwarder.For<IPerkService>(), FacadeForwarder.LazyFor<IStatService>(),
            FacadeForwarder.For<ISkillService>(), FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.LazyFor<ICombatPointService>())
    {
    }

    /// <summary>MI-1, the stay-part: the technique catalog, descriptions, icons and the trait resistance read.</summary>
    public TestMimicry Techniques => this;

    /// <summary>MI-2: witnessing, analysis, learning and the witness sweep.</summary>
    public TestMimicry Witness => this;

    /// <summary>MI-3: slots, equip and unequip, the saved loadouts and the login restore.</summary>
    public TestMimicry Loadout => this;

    public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();
    public Dictionary<StatType, int> StatAdjustments { get; } = new();

    protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => Abilities;

    protected override int GetStatAdjustment(uint creature, StatType stat) => StatAdjustments.GetValueOrDefault(stat);
}
