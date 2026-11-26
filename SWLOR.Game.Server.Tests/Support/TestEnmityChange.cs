#nullable enable
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The enmity change part over scripted neighbours; the unconverted hot core is answered by the overrides.</summary>
public sealed class TestEnmityChange : EnmityChangeService
{
    public TestEnmityChange(
        IEnmityService table,
        IEnmityAttackService attack,
        ICombatLeash leash,
        IHologramQuery hologram,
        IPartyRoster party,
        ICompanionLookup beastMastery)
        : base(table, attack, leash, hologram, party, beastMastery, FacadeForwarder.For<IStatService>(),
            FacadeForwarder.For<IStatusEffectService>())
    {
    }

    public Dictionary<(uint Creature, StatType Stat), int> StatAdjustments { get; } = new();
    public HashSet<uint> Droids { get; } = new();
    public Dictionary<uint, HashSet<IStatusEffect>> Effects { get; } = new();

    protected override int GetStatAdjustment(uint creature, StatType stat) =>
        StatAdjustments.GetValueOrDefault((creature, stat));

    protected override bool IsDroid(uint creature) => Droids.Contains(creature);

    protected override HashSet<IStatusEffect> GetAllStatusEffects(uint creature) =>
        Effects.GetValueOrDefault(creature) ?? new HashSet<IStatusEffect>();
}
