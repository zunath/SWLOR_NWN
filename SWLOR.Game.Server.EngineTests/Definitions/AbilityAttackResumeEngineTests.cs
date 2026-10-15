using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class AbilityAttackResumeEngineTests
{
    [EngineTest("Vibroblade buff resumes controlled autoattacks", Category = "AbilityAttackResume", TimeoutSeconds = 60f)]
    public static Task VibrobladeBuff(EngineTestContext ctx) =>
        VerifyResume(ctx, FeatType.Invincible1, "nw_wswls001");

    [EngineTest("Heavy Vibroblade area resumes controlled autoattacks", Category = "AbilityAttackResume", TimeoutSeconds = 60f)]
    public static Task HeavyVibrobladeArea(EngineTestContext ctx) =>
        VerifyResume(ctx, FeatType.Flash1, "nw_wswgs001");

    [EngineTest("Lightsaber buff resumes controlled autoattacks", Category = "AbilityAttackResume", TimeoutSeconds = 60f)]
    public static Task LightsaberBuff(EngineTestContext ctx) =>
        VerifyResume(ctx, FeatType.SaberWard1, "vet_trnsaber");

    [EngineTest("Vibroblade queued strike resumes controlled autoattacks", Category = "AbilityAttackResume", TimeoutSeconds = 60f)]
    public static Task QueuedStrike(EngineTestContext ctx) =>
        VerifyResume(ctx, FeatType.RiotBlade1, "nw_wswls001");

    private static async Task VerifyResume(EngineTestContext ctx, FeatType feat, string weapon)
    {
        using var owner = await PlayerAbilityFixture.CreateAsync(ctx, -2f);
        var creature = ctx.SpawnCreature("nw_bandit001");
        SetAILevel(creature, AILevel.VeryLow);
        await ctx.ExecuteInCreatureContextAsync(owner.Creature, () => AddHenchman(owner.Creature, creature));
        ctx.AssertEqual(owner.Creature, GetMaster(creature), "player controls the attacker so resumption retains its chosen enemy");
        var ability = Ability.GetAbilityDetail(feat);
        ctx.SetNPCPerkLevel(creature, ability.EffectiveLevelPerkType, ability.AbilityLevel);
        ctx.SetNPCResources(creature, 1000, 1000);
        CreaturePlugin.AddFeat(creature, feat);
        await ctx.EquipItemAsync(creature, weapon, InventorySlot.RightHand);
        var enemy = ctx.SpawnCreature("nw_bandit001", 2f);
        SetAILevel(enemy, AILevel.VeryLow);
        ctx.MakeHostile(enemy);
        ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 120f);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), enemy, 120f);
        Combat.SetAutoAttackHitResolutionOverride(true);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(creature, () => ActionAttack(enemy));
            await ctx.WaitUntilAsync(() => GetAttackTarget(creature) == enemy && Combat.HasRecentAttackActivity(creature, 1f),
                15f, "controlled creature to begin autoattacking the enemy");

            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                // Unit coverage forces the between-update action values. Here the engine
                // chooses the timing; verify that the retained queue produces real swings.
                ctx.Assert(UsePerkFeat.TryUseAbility(creature, creature, feat, GetLocation(creature)),
                    $"{feat} activates: {Ability.GetLastActivationDenialReason()}");
            });
            await ctx.WaitUntilAsync(() => !Activity.IsBusy(creature), 10f, "the ability to complete");
            // Outwait the attack observation from before activation; only a resumed swing
            // can satisfy this, and no second ActionAttack is issued by the test.
            await ctx.DelaySecondsAsync(2f);
            await ctx.WaitUntilAsync(() => GetAttackTarget(creature) == enemy && Combat.HasRecentAttackActivity(creature, 1f),
                15f, "autoattacks to resume on the original enemy");
            if (ability.ActivationType == AbilityActivationType.Weapon)
                ctx.Assert(!UsePerkFeat.IsWeaponAbilityQueued(creature, feat), "the resumed swing consumed the queued ability");
        }
        finally
        {
            await ctx.ExecuteInCreatureContextAsync(creature, () => ClearAllActions(true));
            Combat.SetAutoAttackHitResolutionOverride(null);
        }
    }
}
