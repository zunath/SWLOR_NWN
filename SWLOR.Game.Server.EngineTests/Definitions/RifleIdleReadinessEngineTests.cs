using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class RifleIdleReadinessEngineTests
    {
        [EngineTest("Despawned combat creatures release retained offensive activity", Category = "RifleIdleReadiness", TimeoutSeconds = 35f)]
        public static async Task DespawnedCreaturesReleaseActivity(EngineTestContext ctx)
        {
            var attacks = (Dictionary<uint, DateTime>)typeof(Combat)
                .GetField("_lastAttackActivity", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var abilities = (Dictionary<uint, DateTime>)typeof(Combat)
                .GetField("_lastCombatAbilityUse", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            for (var cycle = 1; cycle <= 2; cycle++)
            {
                var creature = ctx.SpawnCreature("nw_bandit001");
                await ctx.WaitFrameAsync();
                await ctx.ExecuteInCreatureContextAsync(creature, () =>
                {
                    Combat.TrackAttackActivity(creature);
                    Combat.TrackQueuedWeaponAbilityUse(creature, Ability.GetAbilityDetail(FeatType.Headshot1));
                });
                ctx.Assert(attacks.ContainsKey(creature) && abilities.ContainsKey(creature),
                    "the live combat creature has both offensive activity records");
                DestroyObject(creature);
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(creature), 3f,
                    "the creature to despawn without dying");
                await ctx.WaitUntilAsync(() => !attacks.ContainsKey(creature) && !abilities.ContainsKey(creature),
                    12f, "the real module heartbeat to remove the despawned creature's activity records");
                ctx.Log($"Despawn cycle {cycle}: both retained activity records were removed by the module heartbeat.");
            }
        }

        [EngineTest("Rifle idle bonuses and readiness return while an enemy keeps attacking", Category = "RifleIdleReadiness", TimeoutSeconds = 60f)]
        public static async Task ReadinessDuringIncomingAttacks(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx, -0.5f);
            var player = fixture.Creature;
            fixture.Update(record =>
            {
                record.Perks.Clear();
                record.Perks[PerkType.Patience] = 3;
                record.Perks[PerkType.SteadyAim] = 1;
                record.Perks[PerkType.DeadCenter] = 1;
                record.Skills[SkillType.Rifle].Rank = 50;
            });
            await ctx.EquipItemAsync(player, "b_rifle", InventorySlot.RightHand);
            var enemy = ctx.SpawnCreature("nw_bandit001", 1f);
            await ctx.DelaySecondsAsync(1f);
            await ctx.EquipItemAsync(enemy, "b_longsword", InventorySlot.RightHand);
            ctx.MakeHostile(enemy);
            foreach (var creature in new[] { player, enemy })
            {
                ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), creature, 120f);
                SetAILevel(creature, AILevel.VeryLow);
            }
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneImmobilize(), enemy, 120f);

            Combat.SetAutoAttackHitResolutionOverride(true);
            try
            {
                for (var cycle = 1; cycle <= 2; cycle++)
                {
                    var previous = Combat.GetLastCompletedOffensiveActivityAt(player);
                    SetCommandable(true, player);
                    AssignCommand(player, () => ActionAttack(enemy));
                    try
                    {
                        await ctx.WaitUntilAsync(() => Combat.GetLastCompletedOffensiveActivityAt(player) > previous,
                            10f, "the player's real attack roll");
                    }
                    catch (EngineTestAssertionException)
                    {
                        ctx.Fail($"Player attack did not resolve: action={GetCurrentAction(player)}, distance={GetDistanceBetween(player, enemy)}, " +
                            $"hostile={GetIsReactionTypeHostile(player, enemy)}, enemyHP={GetCurrentHitPoints(enemy)}, combat={GetIsInCombat(player)}.");
                    }
                    await ctx.ExecuteInCreatureContextAsync(player, () =>
                    {
                        ClearAllActions(false);
                        SetCommandable(false, player);
                    });
                    var stoppedAt = Combat.GetLastCompletedOffensiveActivityAt(player);
                    var hp = GetCurrentHitPoints(player);
                    Enmity.ModifyEnmity(player, enemy, 10000);
                    AssignCommand(enemy, () => ActionAttack(player));
                    await ctx.WaitUntilAsync(() => GetAttackTarget(enemy) == player &&
                        Combat.GetLastCompletedOffensiveActivityAt(enemy) >= stoppedAt,
                        10f, "the enemy to swing at the player during the pause");
                    await ctx.DelaySecondsAsync(1f);
                    if ((DateTime.UtcNow - stoppedAt).TotalSeconds < 3f)
                    {
                        AssertBonuses(ctx, player, false);
                        ctx.Assert(!StatusEffect.HasStatusEffect<IdleSkillAbilityReadyStatusEffect>(player),
                            "the readiness message must not be emitted before three seconds");
                    }
                    await ctx.WaitUntilAsync(() => (DateTime.UtcNow - stoppedAt).TotalSeconds >= 3.5,
                        5f, "three seconds without attacking during combat");
                    ctx.Log($"Cycle {cycle}: idle={(DateTime.UtcNow - stoppedAt).TotalSeconds:F3}s, " +
                        $"incoming damage={hp - GetCurrentHitPoints(player)}, ready={StatusEffect.HasStatusEffect<IdleSkillAbilityReadyStatusEffect>(player)}, " +
                        $"last activity={Combat.GetLastCompletedOffensiveActivityAt(player):O}, stopped at={stoppedAt:O}, action={GetCurrentAction(player)}.");
                    ctx.AssertEqual(stoppedAt, Combat.GetLastCompletedOffensiveActivityAt(player),
                        "incoming attacks do not count as the player's attacks");
                    AssertBonuses(ctx, player, true);
                    ctx.Assert(GetIsInCombat(player), "the idle rifleman remains in native combat");
                    ctx.Assert(StatusEffect.HasStatusEffect<IdleSkillAbilityReadyStatusEffect>(player),
                        "Patience/Dead Center readiness and its message return during combat");
                    ctx.Assert(StatusEffect.HasStatusEffect<OpeningAttackReadyStatusEffect>(player),
                        "Steady Aim/Dead Center auto-attack readiness also returns");
                }
            }
            finally
            {
                SetCommandable(true, player);
                Combat.SetAutoAttackHitResolutionOverride(null);
            }
        }

        [EngineTest("Rifle readiness survives recent ability checks after a queued ability", Category = "RifleIdleReadiness", TimeoutSeconds = 20f)]
        public static async Task RecentAbilityChecksPreserveReadiness(EngineTestContext ctx)
        {
            var rifleman = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), rifleman, 30f);
            foreach (var perk in Enum.GetValues<PerkType>())
                ctx.SetNPCPerkLevel(rifleman, perk, 0);
            ctx.SetNPCPerkLevel(rifleman, PerkType.Patience, 3);
            ctx.SetNPCPerkLevel(rifleman, PerkType.DeadCenter, 1);
            await ctx.ExecuteInCreatureContextAsync(rifleman, () =>
                Combat.TrackQueuedWeaponAbilityUse(rifleman, Ability.GetAbilityDetail(FeatType.Headshot1)));
            var completedAt = Combat.GetLastCompletedOffensiveActivityAt(rifleman);
            while ((DateTime.UtcNow - completedAt).TotalSeconds < 3.5)
            {
                Combat.HasRecentCombatAbilityUse(rifleman, 2f);
                await ctx.DelaySecondsAsync(0.25f);
            }
            AssertBonuses(ctx, rifleman, true);
            ctx.Log($"After recent-use polling: completed timestamp retained={Combat.GetLastCompletedOffensiveActivityAt(rifleman) == completedAt}, " +
                $"ready={StatusEffect.HasStatusEffect<IdleSkillAbilityReadyStatusEffect>(rifleman)}.");
            ctx.Assert(StatusEffect.HasStatusEffect<IdleSkillAbilityReadyStatusEffect>(rifleman),
                "checking a short recent-use window must not cancel the three-second readiness callback");
            await ctx.ExecuteInCreatureContextAsync(rifleman, () => Combat.TrackAttackActivity(rifleman));
            completedAt = Combat.GetLastCompletedOffensiveActivityAt(rifleman);
            while ((DateTime.UtcNow - completedAt).TotalSeconds < 3.5)
            {
                Combat.HasRecentAttackActivity(rifleman, 1f);
                await ctx.DelaySecondsAsync(0.25f);
                if ((DateTime.UtcNow - completedAt).TotalSeconds < 3f)
                    AssertBonuses(ctx, rifleman, false);
            }
            ctx.AssertEqual(completedAt, Combat.GetLastCompletedOffensiveActivityAt(rifleman),
                "a short recent-attack check retains the timestamp needed by the longer idle window");
            AssertBonuses(ctx, rifleman, true);
            ctx.Assert(StatusEffect.HasStatusEffect<IdleSkillAbilityReadyStatusEffect>(rifleman),
                "recent-attack polling also preserves the readiness callback");
            ctx.Assert(StatusEffect.HasStatusEffect<OpeningAttackReadyStatusEffect>(rifleman),
                "the global auto-attack skill selector also receives readiness feedback");
            Combat.RefreshStatDrivenTrackerEffects(rifleman);
            ctx.Assert(StatusEffect.HasStatusEffect<OpeningAttackReadyStatusEffect>(rifleman),
                "a stat refresh preserves global auto-attack readiness");
        }

        [EngineTest("Rifle idle damage is delivered by Aimed Shot and consumed by the next attack", Category = "RifleIdleReadiness", TimeoutSeconds = 60f)]
        public static async Task AimedShotDeliversIdleBonuses(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.DelaySecondsAsync(1f);
            foreach (var creature in new[] { caster, target })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                Stat.SetNPCMaxHitPoints(creature, 10000, true);
                SetAILevel(creature, AILevel.VeryLow);
                foreach (var perk in Enum.GetValues<PerkType>())
                    ctx.SetNPCPerkLevel(creature, perk, 0);
            }
            ctx.MakeHostile(target);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 120f);
            await ctx.EquipItemAsync(caster, "b_rifle", InventorySlot.RightHand);
            ctx.SetNPCPerkLevel(caster, PerkType.AimedShot, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.Patience, 3);
            ctx.SetNPCPerkLevel(caster, PerkType.DeadCenter, 1);
            ctx.SetNPCPerkLevel(caster, PerkType.SteadyAim, 1);
            ctx.SetNPCResources(caster, 1000, 1000);
            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(caster, () => Combat.TrackAttackActivity(caster));
                AssertBonuses(ctx, caster, false);
                var normal = await CastAimedShot(ctx, caster, target);
                await ctx.DelaySecondsAsync(3.5f);
                AssertBonuses(ctx, caster, true);
                var idle = await CastAimedShot(ctx, caster, target);
                ctx.AssertEqual(normal.CriticalHitCount, idle.CriticalHitCount, "seeded casts use the same critical result");
                ctx.Assert(idle.AttributedDamage > normal.AttributedDamage,
                    $"idle Aimed Shot must deal more damage ({idle.AttributedDamage}) than a recent-attack cast ({normal.AttributedDamage})");
                AssertBonuses(ctx, caster, false);
                ctx.Assert(!StatusEffect.HasStatusEffect<IdleSkillAbilityReadyStatusEffect>(caster),
                    "casting consumes the readiness marker");

                await ctx.DelaySecondsAsync(3.5f);
                AssertBonuses(ctx, caster, true);
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    Combat.PrepareOpeningAutoAttack(caster, SkillType.Rifle);
                    ctx.AssertEqual(12, TemporaryStatModifier.GetStatAdjustment(caster,
                        StatType.CurrentAutoAttackDamageBonus, StatType.CurrentAutoAttackDamageBonus),
                        "Steady Aim prepares its next swing's damage");
                    ctx.AssertEqual(15, Combat.ConsumeOpeningAutoAttackCriticalDamageAdjustment(caster),
                        "Dead Center prepares its next swing's critical damage");
                    Combat.ClearOpeningAutoAttackModifiers(caster);
                });
                var before = Combat.GetLastCompletedOffensiveActivityAt(caster);
                SetCommandable(true, caster);
                AssignCommand(caster, () => ActionAttack(target));
                await ctx.WaitUntilAsync(() => Combat.GetLastCompletedOffensiveActivityAt(caster) > before,
                    10f, "a real missed rifle auto-attack to consume idle readiness");
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    ClearAllActions(false);
                    SetCommandable(false, caster);
                });
                AssertBonuses(ctx, caster, false);
                ctx.Assert(!StatusEffect.HasStatusEffect<IdleSkillAbilityReadyStatusEffect>(caster),
                    "a missed auto-attack also consumes readiness");
                ctx.Log($"Seeded Aimed Shot: recent attack={normal.AttributedDamage} DMG, idle={idle.AttributedDamage} DMG; " +
                    "Patience accuracy +8, Dead Center critical damage +15%, Steady Aim next swing +12 DMG.");
            }
            finally
            {
                SetCommandable(true, caster);
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
            }
        }

        private static async Task<AbilityImpactSummary> CastAimedShot(EngineTestContext ctx, uint caster, uint target)
        {
            Ability.ClearLastCompletedAbilityImpactSummary(caster);
            Recast.ReduceRecastDelay(caster, RecastGroup.AimedShot, 60f);
            var hp = GetCurrentHitPoints(target);
            ctx.SeedRandom(16351);
            SetCommandable(true, caster);
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                ctx.Assert(UsePerkFeat.TryUseAbility(caster, target, FeatType.AimedShot1, GetLocation(target)),
                    "Aimed Shot activates through the normal feat pipeline");
                ClearAllActions(false);
            });
            SetCommandable(false, caster);
            await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(caster)?.AttributedDamage > 0 &&
                GetCurrentHitPoints(target) < hp, 5f, "Aimed Shot's attributed damage and real HP loss");
            return Ability.GetLastCompletedAbilityImpactSummary(caster);
        }

        private static void AssertBonuses(EngineTestContext ctx, uint creature, bool ready)
        {
            var bonuses = Combat.GetIdleSkillAbilityBonuses(creature, SkillType.Rifle);
            ctx.AssertEqual(ready ? 14 : 0, bonuses.DamageBonus, "Patience III damage");
            ctx.AssertEqual(ready ? 8 : 0, bonuses.HitChancePercentAdjustment, "Patience III accuracy");
            ctx.AssertEqual(ready ? 15 : 0, bonuses.CriticalDamagePercentAdjustment, "Dead Center critical damage");
        }
    }
}
