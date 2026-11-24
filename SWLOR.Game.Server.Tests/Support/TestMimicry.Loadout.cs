#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Part MI-3, MimicryLoadoutService: the hotbar feat registration and the status effect cleanup when a technique is revoked.
public sealed partial class TestMimicry
{
    public HashSet<FeatType> Registered { get; } = new();
    public List<(uint Creature, Type Type, bool Message)> RemovedEffects { get; } = new();
    public List<(uint Source, Type Type, bool Message, AbilityDetail Ability)> RemovedSourceEffects { get; } = new();

    protected override bool IsFeatRegistered(FeatType feat) => Registered.Contains(feat);

    protected override AbilityDetail GetAbilityDetail(FeatType feat) => Abilities[feat];

    protected override void RemoveStatusEffect(uint creature, Type statusEffectType, bool sendsWornOffMessage) =>
        RemovedEffects.Add((creature, statusEffectType, sendsWornOffMessage));

    protected override void RemoveStatusEffectsFromAllTargetsBySource(uint source, Type statusEffectType,
        bool sendsWornOffMessage, AbilityDetail originatingAbility) =>
        RemovedSourceEffects.Add((source, statusEffectType, sendsWornOffMessage, originatingAbility));
}
