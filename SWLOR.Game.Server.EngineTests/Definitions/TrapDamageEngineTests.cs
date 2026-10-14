using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TrapDamageEngineTests
    {
        [EngineTest("Razor Trap I retains Perception damage and its cast through delayed detonation", Category = "TrapDamage", TimeoutSeconds = 20f)]
        public static Task RazorTrapOne(EngineTestContext ctx) =>
            VerifyActiveTrap(ctx, FeatType.RazorTrap1, 14, CombatDamageType.Physical);

        [EngineTest("Razor Trap II retains Perception damage and its cast through delayed detonation", Category = "TrapDamage", TimeoutSeconds = 20f)]
        public static Task RazorTrapTwo(EngineTestContext ctx) =>
            VerifyActiveTrap(ctx, FeatType.RazorTrap2, 30, CombatDamageType.Physical);

        [EngineTest("Razor Trap retains its captured damage after owner death", Category = "TrapDamage", TimeoutSeconds = 20f)]
        public static Task RazorTrapOwnerDeath(EngineTestContext ctx) =>
            VerifyActiveTrap(ctx, FeatType.RazorTrap1, 14, CombatDamageType.Physical, ownerDiesAfterPlacement: true);

        [EngineTest("Captured recurring impacts still require a living owner by default", Category = "TrapDamage", TimeoutSeconds = 20f)]
        public static async Task RecurringImpactOwnerDeath(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var invoked = false;
            Ability.BeginAbilityImpact(caster, Ability.GetAbilityDetail(FeatType.RazorTrap1));
            var trigger = Ability.CaptureRepeatedAbilityImpact(caster, () => invoked = true, baseDamage: 14);
            Ability.EndAbilityImpact(caster);
            ObjectPlugin.SetCurrentHitPoints(caster, 0);
            ctx.Assert(GetIsObjectValid(caster) && GetCurrentHitPoints(caster) <= 0,
                "The default recurring impact has a valid dead owner");
            trigger();
            ctx.Assert(!invoked, "Other recurring impacts keep their existing living-owner requirement");
        }

        [EngineTest("Shock Trap retains Perception damage and its cast through delayed detonation", Category = "TrapDamage", TimeoutSeconds = 20f)]
        public static Task ShockTrap(EngineTestContext ctx) =>
            VerifyActiveTrap(ctx, FeatType.ShockTrap, 22, CombatDamageType.Electrical);

        [EngineTest("Kit trap proximity damage uses Perception without an active ability tracker", Category = "TrapDamage", TimeoutSeconds = 20f)]
        public static async Task KitTrapPerception(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_rat001");
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, caster, target);

            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                Ability.ClearLastCompletedAbilityImpactSummary(caster);
                ctx.SeedRandom(12345);
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    ctx.Assert(Traps.TryPlaceKitTrap(caster, GetLocation(target), 1), "The tier-one kit trap is placed"));
                ctx.AssertEqual(1000, GetCurrentHitPoints(target), "A kit trap does not damage before arming");
                await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) < 1000, 10f,
                    "the kit trap's real arming delay and proximity detonation");
                StatusEffect.RemoveAllStatusEffects(target);

                var observed = 1000 - GetCurrentHitPoints(target);
                AssertPerceptionDamage(ctx, caster, target, 20, CombatDamageType.Physical, observed, null);
                ctx.AssertEqual(caster, GetLastDamager(target), "Kit damage is attributed to its owner");
                ctx.Assert(Ability.GetLastCompletedAbilityImpactSummary(caster) == null,
                    "The kit exercises the path without a tracked active ability");
                ctx.Log($"Tier-one kit: native damage={observed}, PER={GetAbilityScore(caster, AbilityType.Perception)}, MGT={GetAbilityScore(caster, AbilityType.Might)}");
            }
            finally
            {
                Traps.ClearTraps(caster);
                StatusEffect.RemoveAllStatusEffects(target);
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        private static async Task VerifyActiveTrap(
            EngineTestContext ctx, FeatType feat, int baseDamage, CombatDamageType damageType,
            bool ownerDiesAfterPlacement = false)
        {
            const int capturedDamageBonus = 31;
            const int newerDamageBonus = 73;
            var caster = ctx.SpawnCreature("nw_rat001");
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, caster, target);
            var trapAbility = Ability.GetAbilityDetail(feat);
            var renewal = Ability.GetAbilityDetail(FeatType.Renewal1);
            var trackerOpen = false;

            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                ctx.SeedRandom(12345);
                Combat.GrantNextSkillAbilityBonuses(caster, (int)SkillType.Espionage, capturedDamageBonus, 0, 120);
                Ability.BeginAbilityImpact(caster, trapAbility);
                trackerOpen = true;
                await ctx.ExecuteInCreatureContextAsync(caster,
                    () => trapAbility.ImpactAction(caster, target, trapAbility.AbilityLevel, GetLocation(target)));
                var placement = Ability.EndAbilityImpact(caster);
                trackerOpen = false;
                ctx.AssertEqual(0, placement.AttributedDamage, "Placement queues no immediate damage");
                ctx.AssertEqual(1000, GetCurrentHitPoints(target), "The trap waits for its arming delay");

                Combat.GrantNextSkillAbilityBonuses(caster, (int)SkillType.Espionage, newerDamageBonus, 0, 120);
                Ability.BeginAbilityImpact(caster, renewal);
                trackerOpen = true;
                await ctx.ExecuteInCreatureContextAsync(caster,
                    () => renewal.ImpactAction(caster, caster, renewal.AbilityLevel, GetLocation(caster)));
                Ability.EndAbilityImpact(caster);
                trackerOpen = false;
                ctx.Assert(ReferenceEquals(renewal, Ability.GetLastCompletedAbilityImpactSummary(caster)?.Ability),
                    "A different registered ability completes between placement and detonation");

                Ability.BeginAbilityImpact(caster, renewal);
                trackerOpen = true;
                if (ownerDiesAfterPlacement)
                {
                    StatusEffect.RemoveAllStatusEffects(caster);
                    ObjectPlugin.SetCurrentHitPoints(caster, 0);
                    ctx.Assert(GetIsObjectValid(caster) && GetCurrentHitPoints(caster) <= 0,
                        "The trap owner remains valid but is dead before detonation");
                }
                await ctx.WaitUntilAsync(
                    () => ReferenceEquals(trapAbility, Ability.GetLastCompletedAbilityImpactSummary(caster)?.Ability) &&
                          Ability.GetLastCompletedAbilityImpactSummary(caster).AttributedDamage > 0,
                    10f, "the real trap trigger to complete its captured originating impact");
                var detonation = Ability.GetLastCompletedAbilityImpactSummary(caster);
                await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) < 1000, 3f,
                    "the trap's attributed damage effect to reach native hit points");
                StatusEffect.RemoveAllStatusEffects(target);

                ctx.AssertEqual(SkillType.Espionage, detonation.SkillType, "Detonation retains the originating skill");
                ctx.AssertEqual(1, detonation.ImpactedTargetCount, "Detonation hits the one nearby enemy");
                ctx.AssertEqual(detonation.AttributedDamage, 1000 - GetCurrentHitPoints(target),
                    "The captured trap summary matches damage received in the native engine");
                ctx.AssertEqual(caster, GetLastDamager(target), "Trap damage is attributed to its owner");
                AssertPerceptionDamage(ctx, caster, target, baseDamage + capturedDamageBonus,
                    damageType, detonation.AttributedDamage, detonation.CriticalHitCount > 0);

                var restored = Ability.EndAbilityImpact(caster);
                trackerOpen = false;
                ctx.Assert(ReferenceEquals(renewal, restored.Ability),
                    "Detonation restores the newer in-progress ability tracker");
                ctx.AssertEqual(0, restored.AttributedDamage, "The newer ability does not inherit trap damage");
                ctx.AssertEqual(newerDamageBonus,
                    Combat.ConsumeNextSkillAbilityBonuses(caster, SkillType.Espionage).DamageBonus,
                    "The delayed trap does not consume a bonus granted after placement");
                ctx.Log($"{feat}: native damage={detonation.AttributedDamage}, criticals={detonation.CriticalHitCount}, captured bonus={capturedDamageBonus}; newer tracker and bonus preserved");
            }
            finally
            {
                if (trackerOpen)
                    Ability.AbortAbilityImpact(caster);
                Traps.ClearTraps(caster);
                StatusEffect.RemoveAllStatusEffects(caster);
                StatusEffect.RemoveAllStatusEffects(target);
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        private static void AssertPerceptionDamage(
            EngineTestContext ctx, uint caster, uint target, int baseDamage,
            CombatDamageType damageType, int observed, bool? wasCritical)
        {
            var defenseAbility = damageType.GetDefenseAbilityType();
            var defense = Stat.GetDefense(target, damageType, defenseAbility);
            var defenderStat = GetAbilityScore(target, defenseAbility);
            var damage = baseDamage + Combat.GetAbilityDamageBonus(caster, SkillType.Espionage);
            var minimumCritical = wasCritical == true ? Combat.StandardCriticalRating : 0;
            var maximumCritical = wasCritical == false ? 0 : Combat.StandardCriticalRating;
            var (perceptionMinimum, _) = Combat.CalculateDamageRange(
                Stat.GetAttack(caster, AbilityType.Perception, SkillType.Espionage), damage,
                GetAbilityScore(caster, AbilityType.Perception), defense, defenderStat, minimumCritical);
            var (_, perceptionMaximum) = Combat.CalculateDamageRange(
                Stat.GetAttack(caster, AbilityType.Perception, SkillType.Espionage), damage,
                GetAbilityScore(caster, AbilityType.Perception), defense, defenderStat, maximumCritical);
            var (_, mightMaximum) = Combat.CalculateDamageRange(
                Stat.GetAttack(caster, AbilityType.Might, SkillType.Espionage), damage,
                GetAbilityScore(caster, AbilityType.Might), defense, defenderStat, Combat.StandardCriticalRating);

            ctx.Assert(perceptionMinimum > mightMaximum,
                $"The fixture separates PER damage {perceptionMinimum}-{perceptionMaximum} from even critical MGT damage <= {mightMaximum}");
            ctx.Assert(observed >= perceptionMinimum && observed <= perceptionMaximum,
                $"Real delayed trap damage must use PER: expected {perceptionMinimum}-{perceptionMaximum}, observed {observed}");
        }

        private static void Prepare(EngineTestContext ctx, uint caster, uint target)
        {
            CreaturePlugin.SetRawAbilityScore(caster, AbilityType.Might, 8);
            CreaturePlugin.SetRawAbilityScore(caster, AbilityType.Perception, 40);
            CreaturePlugin.SetRawAbilityScore(target, AbilityType.Vitality, 10);
            foreach (var creature in new[] { caster, target })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                SetAILevel(creature, AILevel.VeryLow);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 120f);
                Stat.SetNPCMaxHitPoints(creature, 1000, true);
            }
            ctx.MakeHostile(target);
            ctx.AssertEqual(0, Stat.GetStatAdjustment(caster, StatType.TrapBonus), "Trap Bonus is isolated");
            ctx.AssertEqual(0, Stat.GetStatAdjustment(caster, StatType.TrapPlacementSpeedPercent), "The actual three-second arming delay is used");
            ctx.AssertEqual(0, Resistance.GetResistance(target, ResistanceType.Electrical), "Electrical resistance is isolated");
            Ability.ClearLastCompletedAbilityImpactSummary(caster);
        }
    }
}
