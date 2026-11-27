#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The loadout part with its calls into unconverted code answered by the test: the hotbar feat registration, the status effect cleanup when a
/// technique is revoked, and the perk level and ability details it shares with the <see cref="TestMimicry"/> it reads the technique table from.
/// </summary>
public sealed class TestMimicryLoadoutService : MimicryLoadoutService
{
    private readonly TestMimicry _mimicry;

    public TestMimicryLoadoutService(TestMimicry mimicry, IDBService db, IQuickCastService quickCast, IGuiService gui)
        : base(db, quickCast, gui, FacadeForwarder.For<IPerkService>(), FacadeForwarder.For<IStatusEffectService>(), mimicry, FacadeForwarder.For<IAbilityCatalog>())
    {
        _mimicry = mimicry;
    }

    public HashSet<FeatType> Registered { get; } = new();
    public List<(uint Creature, Type Type, bool Message)> RemovedEffects { get; } = new();
    public List<(uint Source, Type Type, bool Message, AbilityDetail Ability)> RemovedSourceEffects { get; } = new();

    protected override bool IsFeatRegistered(FeatType feat) => Registered.Contains(feat);

    protected override AbilityDetail GetAbilityDetail(FeatType feat) => _mimicry.Abilities[feat];

    protected override int GetPerkLevel(uint creature, PerkType perkType) => _mimicry.PerkLevels.GetValueOrDefault(perkType);

    protected override void RemoveStatusEffect(uint creature, Type statusEffectType, bool sendsWornOffMessage) =>
        RemovedEffects.Add((creature, statusEffectType, sendsWornOffMessage));

    protected override void RemoveStatusEffectsFromAllTargetsBySource(uint source, Type statusEffectType,
        bool sendsWornOffMessage, AbilityDetail originatingAbility) =>
        RemovedSourceEffects.Add((source, statusEffectType, sendsWornOffMessage, originatingAbility));
}
