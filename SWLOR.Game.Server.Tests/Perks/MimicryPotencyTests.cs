using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Perks;

public class MimicryPotencyTests
{
    /// <summary>Checks stack replacement, the three-stack cap, and independent expiry of Momentum and Overload.</summary>
    [Test]
    public void MomentumReplacementAndExpiry_PreserveOtherPotencySources()
    {
        var effects = new CreatureStatusEffect();
        var overload = new OverloadStatusEffect();
        effects.Add(overload);
        FinishingDriveMomentumStatusEffect momentum = null;

        for (var cast = 1; cast <= 4; cast++)
        {
            if (momentum != null)
                effects.Remove(momentum);
            momentum = new FinishingDriveMomentumStatusEffect(cast);
            effects.Add(momentum);

            effects.StatGroup.Stats[StatType.MimicryPotencyPercent].Should().Be(
                50 + Math.Min(cast, 3) * 8,
                "each replacement contributes only the current stacks, alongside Overload");
        }

        effects.Remove(overload);
        effects.StatGroup.Stats[StatType.MimicryPotencyPercent].Should().Be(24,
            "Overload expiry must retain the three Momentum stacks");
        effects.Remove(momentum!);
        effects.StatGroup.Stats[StatType.MimicryPotencyPercent].Should().Be(0,
            "Momentum expiry must remove its complete bonus");
    }

    /// <summary>Ensures other skills and auto-attacks bypass potency without querying native creature state.</summary>
    [Test]
    public void PotencyDamageModifier_ExcludesEveryOtherSkillAndAutoAttacks()
    {
        foreach (var skill in Enum.GetValues<SkillType>())
        {
            // An invalid engine object also proves excluded paths do not query native stats.
            Combat.ApplyMimicryAbilityDamageModifier(uint.MaxValue, 100, skill, false)
                .Should().Be(100, $"{skill} auto-attacks are not Mimicry technique damage");
            if (skill != SkillType.Mimicry)
            {
                Combat.ApplyMimicryAbilityDamageModifier(uint.MaxValue, 100, skill, true)
                    .Should().Be(100, $"{skill} abilities, including NPC originals, are excluded");
            }
        }
    }

    /// <summary>Ensures potency cannot turn a zero-damage control or immune impact into damage.</summary>
    [Test]
    public void PotencyDamageModifier_PreservesControlOnlyAndImmuneImpacts()
    {
        Combat.ApplyMimicryAbilityDamageModifier(uint.MaxValue, 0, SkillType.Mimicry, true)
            .Should().Be(0, "potency must never create damage for a zero-damage impact");
    }
}
