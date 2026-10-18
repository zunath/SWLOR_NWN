using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class MimicryTraitPotencyEngineTests
{
    [EngineTest("Overclocked Analyzer boosts equipped defensive traits and restores them on expiry", Category = "MimicryTraitPotency", TimeoutSeconds = 30f)]
    public static async Task DefensiveTraitsFollowOverloadLifetime(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var record = DB.Get<Player>(fixture.Id);
        record.EquippedTechniques.AddRange(new[] { FeatType.IronCarapaceTechnique, FeatType.ChitinGuardTechnique });
        DB.Set(record);
        AssertDefense(ctx, player, 12, 20, 15);
        var physicalBefore = Stat.GetDefense(player, CombatDamageType.Physical, AbilityType.Vitality);
        var forceBefore = Stat.GetDefense(player, CombatDamageType.Force, AbilityType.Willpower);
        var fireBefore = Resistance.GetResistance(player, ResistanceType.Fire);

        var overload = Ability.GetAbilityDetail(FeatType.Overload);
        await ctx.ExecuteInCreatureContextAsync(player, () => overload.ImpactAction(player, player, 1, GetLocation(player)));
        ctx.Assert(StatusEffect.HasStatusEffect<OverloadStatusEffect>(player), "the real capstone applies Overload");
        AssertDefense(ctx, player, 18, 30, 23);
        ctx.Assert(Stat.GetDefense(player, CombatDamageType.Physical, AbilityType.Vitality) > physicalBefore,
            "the character sheet's Physical DEF getter increases");
        ctx.Assert(Stat.GetDefense(player, CombatDamageType.Force, AbilityType.Willpower) > forceBefore,
            "the character sheet's Force DEF getter increases");
        ctx.AssertEqual(fireBefore + 10, Resistance.GetResistance(player, ResistanceType.Fire), "boost reaches total resistance");
        ctx.AssertEqual(50, Stat.GetStatAdjustment(player, StatType.MimicryPotencyPercent), "direct damage potency remains 50%");

        await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect<OverloadStatusEffect>(player), 16f, "12-second Overload expiry");
        AssertDefense(ctx, player, 12, 20, 15);
        ctx.AssertEqual(physicalBefore, Stat.GetDefense(player, CombatDamageType.Physical, AbilityType.Vitality), "Physical DEF is restored");
        ctx.AssertEqual(forceBefore, Stat.GetDefense(player, CombatDamageType.Force, AbilityType.Willpower), "Force DEF is restored");
        ctx.SetResultDetail("Actual Overload impact and expiry: Iron Carapace + Chitin Guard defense 12% -> 18% -> 12%, Fire/Poison 20 -> 30 -> 20, Trauma 15 -> 23 -> 15; total defense and resistance getters verified.");
    }

    [EngineTest("Overclocked trait potency stays per wearer and preserves the separate proc bonus", Category = "MimicryTraitPotency", TimeoutSeconds = 20f)]
    public static async Task PotencyDoesNotMutateTraitDefinitions(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var record = DB.Get<Player>(fixture.Id);
        record.EquippedTechniques.AddRange(new[] { FeatType.IronCarapaceTechnique, FeatType.MaulingBiteTechnique });
        DB.Set(record);
        var iron = Ability.GetAbilityDetail(FeatType.IronCarapaceTechnique);
        var before = Stat.GetStatSources(player, StatType.PhysicalDefensePercentAdjustment)
            .Single(source => source.Key == $"trait:{(int)FeatType.IronCarapaceTechnique}");
        StatusEffect.ApplyStatusEffect(player, player, new OverloadStatusEffect(), 30f);
        ctx.AssertEqual(12, Mimicry.GetStatBonus(player, StatType.PhysicalDefensePercentAdjustment), "only equipped Iron Carapace contributes");
        var boosted = Stat.GetStatSources(player, StatType.PhysicalDefensePercentAdjustment)
            .Single(source => source.Key == before.Key);
        ctx.AssertEqual(12, boosted[StatType.PhysicalDefensePercentAdjustment], "source payload agrees with aggregate");
        ctx.AssertEqual(8, before[StatType.PhysicalDefensePercentAdjustment], "earlier source remains unchanged");
        ctx.AssertEqual(8, iron.MimicryTraitStats[StatType.PhysicalDefensePercentAdjustment], "cached trait stats stay unchanged");
        ctx.AssertEqual(15, iron.MimicryTraitResistances[ResistanceType.Trauma], "cached resistance stays unchanged");
        ctx.AssertEqual(29, Stat.GetStatAdjustment(player, StatType.DamageDealtBleedChance), "9% trait gains 50% potency plus the separate 15-point proc bonus");

        record.EquippedTechniques.Clear();
        DB.Set(record);
        AssertDefense(ctx, player, 0, 0, 0);
        ctx.AssertEqual(15, Stat.GetStatAdjustment(player, StatType.DamageDealtBleedChance), "unequipping leaves only Overload's direct proc bonus");
        record.EquippedTechniques.Add(FeatType.IronCarapaceTechnique);
        DB.Set(record);
        StatusEffect.RemoveStatusEffect<OverloadStatusEffect>(player);
        ctx.AssertEqual(8, Mimicry.GetStatBonus(player, StatType.PhysicalDefensePercentAdjustment), "removal restores base trait stats immediately");
    }

    private static void AssertDefense(EngineTestContext ctx, uint player, int defense, int elementalResistance, int trauma)
    {
        ctx.AssertEqual(defense, Mimicry.GetStatBonus(player, StatType.PhysicalDefensePercentAdjustment), "trait Physical DEF percent");
        ctx.AssertEqual(defense, Mimicry.GetStatBonus(player, StatType.ForceDefensePercentAdjustment), "trait Force DEF percent");
        ctx.AssertEqual(elementalResistance, Mimicry.GetResistanceBonus(player, ResistanceType.Fire), "trait Fire Resistance");
        ctx.AssertEqual(elementalResistance, Mimicry.GetResistanceBonus(player, ResistanceType.Poison), "trait Poison Resistance");
        ctx.AssertEqual(trauma, Mimicry.GetResistanceBonus(player, ResistanceType.Trauma), "trait Trauma Resistance");
    }
}
