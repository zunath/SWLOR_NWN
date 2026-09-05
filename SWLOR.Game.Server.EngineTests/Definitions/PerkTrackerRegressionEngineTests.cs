using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PerkTrackerRegressionEngineTests
    {
        [EngineTest("Area ability pulses hit nearby enemies once per cast across impact phases", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task AreaPulseOncePerCast(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -1f);
            var first = ctx.SpawnCreature("nw_rat001", 1f);
            var nearby = ctx.SpawnCreature("nw_rat001", 3f);
            var distant = ctx.SpawnCreature("nw_rat001", 8f);
            var friendly = ctx.SpawnCreature("nw_rat001", 2f, 1f);
            await ctx.WaitFrameAsync();
            var creatures = new[] { caster, first, nearby, distant, friendly };
            foreach (var creature in creatures)
            {
                PrepareStationaryCreature(ctx, creature);
            }
            foreach (var hostile in new[] { first, nearby, distant })
            {
                ctx.MakeHostile(hostile);
            }

            var bloom = Ability.GetAbilityDetail(FeatType.TempestBloom1);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(caster, () =>
                {
                    Ability.BeginAbilityImpact(caster, bloom);
                    try { bloom.ImpactAction(caster, first, 1, GetLocation(caster)); }
                    finally { Ability.EndAbilityImpact(caster); }
                });
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(8, Stat.GetStatAdjustment(caster, StatType.AreaAbilityPulseDamage), "Tempest Bloom's real area impact grants the pulse buff");
            ctx.AssertEqual(5, Stat.GetStatAdjustment(caster, StatType.AreaAbilityPulseRadiusMeters), "the buff uses the approved 5m radius");
            var ability = new AbilityDetail { IsHostileAbility = true, IsAreaAbility = true, SkillType = SkillType.Force };
            var startingHP = creatures.ToDictionary(creature => creature, GetCurrentHitPoints);
            var sequence = new AbilityImpactSequence();
            Ability.BeginAbilityImpact(caster, ability, sequence: sequence);
            try
            {
                // Zero direct damage isolates the rider and also covers landed control-only areas.
                Ability.ApplyHostileCombatImpact(caster, first, SkillType.Force, 0, CombatDamageType.Force, awardsCombatPoints: false);
                Ability.ApplyHostileCombatImpact(caster, nearby, SkillType.Force, 0, CombatDamageType.Force, awardsCombatPoints: false);
            }
            finally
            {
                Ability.EndAbilityImpact(caster);
            }
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(first) < startingHP[first], 5f, "the immediate pulse to deal damage");
            ctx.AssertEqual(8, startingHP[first] - GetCurrentHitPoints(first), "pulse includes its first struck target");
            ctx.AssertEqual(8, startingHP[nearby] - GetCurrentHitPoints(nearby), "multiple struck enemies do not multiply the pulse");
            ctx.AssertEqual(startingHP[distant], GetCurrentHitPoints(distant), "5m pulse excludes distant enemies");
            ctx.AssertEqual(startingHP[friendly], GetCurrentHitPoints(friendly), "pulse excludes friendlies");
            ctx.AssertEqual(startingHP[caster], GetCurrentHitPoints(caster), "pulse excludes the source");

            // Persistent fields and delayed shapes retain this sequence across separate impacts.
            Ability.BeginAbilityImpact(caster, ability, countsAsAttackAttempt: false, sequence: sequence);
            try
            {
                Ability.ApplyHostileCombatImpact(caster, nearby, SkillType.Force, 0, CombatDamageType.Force, awardsCombatPoints: false);
            }
            finally
            {
                Ability.EndAbilityImpact(caster);
            }
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(8, startingHP[nearby] - GetCurrentHitPoints(nearby), "later impacts of the same cast do not pulse again");

            // A separately cast ability receives a new pulse, independent of the source skill.
            ability.SkillType = SkillType.TwinBlade;
            Ability.BeginAbilityImpact(caster, ability);
            try
            {
                Ability.ApplyHostileCombatImpact(caster, first, SkillType.TwinBlade, 0, CombatDamageType.Physical, awardsCombatPoints: false);
            }
            finally
            {
                Ability.EndAbilityImpact(caster);
            }
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(first) == startingHP[first] - 16, 5f, "the next cast to produce its own pulse");
            ctx.AssertEqual(16, startingHP[nearby] - GetCurrentHitPoints(nearby), "second cast pulses once");
            ctx.AssertEqual(0, Stat.GetStatAdjustment(caster, StatType.AreaAbilityFragmentationDamage), "pulse does not grant fragmentation");
        }

        [EngineTest("Warden Order heals party members with outgoing, readiness, and received modifiers", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task WardenOrderPartyHealing(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var ally = ctx.SpawnCreature("nw_bandit001", 2f);
            var outsider = ctx.SpawnCreature("nw_bandit001", 3f);
            await ctx.WaitFrameAsync();
            foreach (var creature in new[] { caster, ally, outsider })
            {
                PrepareStationaryCreature(ctx, creature);
                AssignCommand(creature, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(600, DamageType.Slashing), creature));
            }
            AssignCommand(caster, () => AddHenchman(caster, ally));
            await ctx.WaitUntilAsync(() => Party.IsInParty(caster, ally), 5f, "a real associate party link");
            TemporaryStatModifier.Add(caster, StatType.OutgoingAbilityHealingPercentAdjustment, 20, 30f);
            TemporaryStatModifier.Add(caster, StatType.CombatReadinessPercent, 10, 30f);
            TemporaryStatModifier.Add(ally, StatType.HealingReceivedPercentAdjustment, -50, 30f);
            var casterHP = GetCurrentHitPoints(caster);
            var allyHP = GetCurrentHitPoints(ally);
            var outsiderHP = GetCurrentHitPoints(outsider);
            var ability = Ability.GetAbilityDetail(FeatType.WardenOrderTechnique);
            Ability.BeginAbilityImpact(caster, ability);
            try
            {
                ability.ImpactAction(caster, caster, 1, GetLocation(caster));
            }
            finally
            {
                Ability.EndAbilityImpact(caster);
            }
            await ctx.WaitFrameAsync();
            // 1000 * 15% = 150; +20% outgoing = 180; +10% readiness = 198; -50% received = 99.
            ctx.AssertEqual(198, GetCurrentHitPoints(caster) - casterHP, "caster healing includes outgoing and readiness");
            ctx.AssertEqual(99, GetCurrentHitPoints(ally) - allyHP, "party healing respects the recipient's healing penalty");
            ctx.AssertEqual(outsiderHP, GetCurrentHitPoints(outsider), "nonparty neighbor receives no healing");
        }

        [EngineTest("Warden Wall grants one defense bonus to its source and nearby party", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task WardenWallDoesNotDoubleCasterBonus(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var ally = ctx.SpawnCreature("nw_bandit001", 2f);
            var outsider = ctx.SpawnCreature("nw_bandit001", 3f);
            await ctx.WaitFrameAsync();
            foreach (var creature in new[] { caster, ally, outsider })
            {
                PrepareStationaryCreature(ctx, creature);
            }
            AssignCommand(caster, () => AddHenchman(caster, ally));
            await ctx.WaitUntilAsync(() => Party.IsInParty(caster, ally), 5f, "the party to include the ally");
            ctx.Assert(!AbilityTargeting.GetFriendlyTargetsNearLocation(caster, GetLocation(caster), 10f, false).Contains(caster),
                "explicit source exclusion must override party membership");
            StatusEffect.ApplyStatusEffect(caster, caster, new WardenWallStatusEffect(), 60f);
            await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect(ally, typeof(WardenWallAuraStatusEffect)), 10f, "the wall to reach the nearby ally");
            foreach (var stat in new[] { StatType.PhysicalDefensePercentAdjustment, StatType.ForceDefensePercentAdjustment })
            {
                ctx.AssertEqual(20, Stat.GetStatAdjustment(caster, stat), $"caster {stat} is not doubled by its own aura");
                ctx.AssertEqual(20, Stat.GetStatAdjustment(ally, stat), $"ally {stat}");
            }
            ctx.Assert(!StatusEffect.HasStatusEffect(caster, typeof(WardenWallAuraStatusEffect)), "caster must not receive its own aura effect");
            ctx.Assert(!StatusEffect.HasStatusEffect(outsider, typeof(WardenWallAuraStatusEffect)), "wall excludes a nonparty neighbor");
        }

        [EngineTest("Area-use deflection and FP rewards work across skills without requiring a hit", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task AreaUseRewardsAcrossSkills(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            ctx.SetNPCResources(caster, 100, 100);
            Stat.ReduceFP(caster, 50);
            // Spinning Deflection III + Tempest Stance + Saber Cyclone; Force Gyre refunds once.
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityUsedAttackDeflection, 7, 45f);
            StatusEffect.ApplyStatusEffect(caster, caster, new TempestStanceStatusEffect(), 45f);
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityUsedAttackDeflection, 8, 45f);
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityUsedAttackDeflectionDurationSeconds, 30, 45f);
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityUsedFPRestore, 4, 45f);
            TemporaryStatModifier.Add(caster, StatType.AbilityGrantedAttackDeflectionFPRestore, 2, 45f);
            TemporaryStatModifier.Add(caster, StatType.AbilityGrantedAttackDeflectionFPRestoreCooldownSeconds, 6, 45f);
            var beforeFP = Stat.GetCurrentFP(caster);
            var area = new AbilityDetail { SkillType = SkillType.Force, IsHostileAbility = true, IsAreaAbility = true };
            Combat.ApplyAbilityActivatedEffects(caster, OBJECT_INVALID, FeatType.Invalid, area, new AbilityImpactSummary());
            ctx.AssertEqual(20, Stat.GetStatAdjustment(caster, StatType.RangedDeflection), "all three deflection sources stack after an empty Force area cast");
            ctx.AssertEqual(beforeFP + 6, Stat.GetCurrentFP(caster), "Saber Cyclone and Force Gyre refund FP");
            area.SkillType = SkillType.Mimicry;
            Combat.ApplyAbilityActivatedEffects(caster, OBJECT_INVALID, FeatType.Invalid, area, new AbilityImpactSummary());
            ctx.AssertEqual(beforeFP + 10, Stat.GetCurrentFP(caster), "next area refunds four FP while Force Gyre remains on cooldown");

            TemporaryStatModifier.Add(caster, StatType.AreaAbilityAfterDeflectionDamagePercentAdjustment, 10, 45f);
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityAfterDeflectionWindowSeconds, 30, 45f);
            Combat.TrackDeflection(caster, DeflectionSource.Ranged);
            foreach (var skill in new[] { SkillType.Force, SkillType.Saberstaff, SkillType.TwinBlade, SkillType.Mimicry })
            {
                ctx.AssertEqual(110, Combat.ApplyAreaAbilityAfterDeflectionDamageModifier(caster, skill, 100, true), $"Tempest Focus boosts {skill} areas");
                ctx.AssertEqual(100, Combat.ApplyAreaAbilityAfterDeflectionDamageModifier(caster, skill, 100, false), "single-target damage is excluded");
            }
        }

        [EngineTest("Instant area self-buffs trigger their deflection reward only once", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task InstantAreaBuffRewardOnce(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            PrepareStationaryCreature(ctx, target);
            ctx.MakeHostile(target);
            ctx.SetNPCResources(caster, 100, 100);
            // An uncapped reward reveals duplicate applications that a cooldown would hide.
            TemporaryStatModifier.Add(caster, StatType.AbilityGrantedAttackDeflectionFPRestore, 2, 30f);
            var ability = Ability.GetAbilityDetail(FeatType.CircleSlash1);
            try
            {
                foreach (var scenario in new (bool Lands, bool Immune)[] { (true, false), (true, true), (false, true) })
                {
                    Combat.SetAbilityHitResolutionOverride(scenario.Lands);
                    TemporaryStatModifier.Replace(target, StatType.PhysicalDamageImmunity, scenario.Immune ? 1 : 0, 30f);
                    await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    {
                        Ability.BeginAbilityImpact(caster, ability);
                        try
                        {
                            // Prepare the deficit after entering the assigned creature context;
                            // queued NPC spawn initialization may refill resources before it runs.
                            Stat.ReduceFP(caster, 20);
                            var before = Stat.GetCurrentFP(caster);
                            ability.ImpactAction(caster, target, 1, GetLocation(caster));
                            ctx.AssertEqual(before + (scenario.Lands ? 2 : 0), Stat.GetCurrentFP(caster),
                                "landed instant impacts grant one reward even at zero damage; misses grant none");
                            var summary = Ability.GetActiveAbilityImpactSummary(caster);
                            ctx.AssertEqual(scenario.Lands, summary.ImpactedTargetCount > 0, "the hit result is recorded independently of damage");
                            if (scenario.Immune)
                                ctx.AssertEqual(0, summary.AttributedDamage, "physical immunity makes the impact deal zero damage");
                        }
                        finally { Ability.EndAbilityImpact(caster); }
                    });
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }

            ctx.AssertEqual(4, Stat.GetStatAdjustment(caster, StatType.RangedDeflection), "Circle Slash grants its self-buff");
        }

        [EngineTest("Scheduled device channels retain area rewards and share one pulse per cast", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task ScheduledDeviceAreaContext(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -1f);
            var first = ctx.SpawnCreature("nw_rat001", 1f);
            var second = ctx.SpawnCreature("nw_rat001", 3f);
            await ctx.WaitFrameAsync();
            foreach (var creature in new[] { caster, first, second })
                PrepareStationaryCreature(ctx, creature);
            ctx.MakeHostile(first);
            ctx.MakeHostile(second);
            ctx.SetNPCResources(caster, 100, 100);
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityPulseDamage, 8, 30f, "pulse");
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityPulseRadiusMeters, 5, 30f, "pulse");
            TemporaryStatModifier.Add(caster, StatType.AreaHitStaminaRestorePerTarget, 2, 30f, "reward");
            TemporaryStatModifier.Add(caster, StatType.AreaHitStaminaRestoreMaximum, 6, 30f, "reward");
            var area = Ability.GetAbilityDetail(FeatType.KillzoneBeacon1);
            var firstHP = GetCurrentHitPoints(first);
            var secondHP = GetCurrentHitPoints(second);
            var stamina = 0;

            // Use the production scheduler with zero direct damage to isolate the shared
            // pulse rider across the beacon's physical and electrical channels.
            void ScheduleChannel(CombatDamageType damageType, float duration)
            {
                DeviceAbilityEffects.ScheduleAreaHostilePulses(caster, GetLocation(first), SkillType.Devices,
                    0, 0, null, 5f, duration, damageType, SWLOR.NWN.API.NWScript.Enum.VisualEffect.VisualEffect.None,
                    appliesBeaconPulseBonuses: true, showAreaIndicator: false);
            }

            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                Stat.ReduceStamina(caster, 50);
                stamina = Stat.GetCurrentStamina(caster);
                Ability.BeginAbilityImpact(caster, area);
                try
                {
                    ScheduleChannel(CombatDamageType.Physical, 6f);
                    ScheduleChannel(CombatDamageType.Electrical, 6f);
                }
                finally { Ability.EndAbilityImpact(caster); }
            });
            Combat.GrantNextAbilityDamageBonus(caster, (int)PerkType.KillzoneBeacon, 100, 30);

            // A pulse can also be forced while a different ability is being resolved.
            // Keep that tracker alive across the timer to verify that it is restored.
            var otherAbility = new AbilityDetail { IsSingleTargetAbility = true, SkillType = SkillType.Mimicry };
            Ability.BeginAbilityImpact(caster, otherAbility);
            var otherSummary = Ability.GetActiveAbilityImpactSummary(caster);
            var otherSequence = Ability.GetAbilityImpactSequence(caster);
            try
            {
                await ctx.WaitUntilAsync(() => Stat.GetCurrentStamina(caster) >= stamina + 8, 8f, "both channels to grant their area-hit rewards");
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(firstHP - 8, GetCurrentHitPoints(first), "the two channels share one bonus pulse");
                ctx.AssertEqual(secondHP - 8, GetCurrentHitPoints(second), "the bonus pulse reaches both targets");
                ctx.Assert(ReferenceEquals(otherSummary, Ability.GetActiveAbilityImpactSummary(caster)), "the surrounding impact summary survives");
                ctx.Assert(ReferenceEquals(otherSequence, Ability.GetAbilityImpactSequence(caster)), "the surrounding cast sequence survives");
                ctx.Assert(otherSummary.IsSingleTargetAbility && !otherSummary.IsAreaAbility, "field hits are not attributed to the other ability");
            }
            finally { Ability.EndAbilityImpact(caster); }

            await ctx.WaitUntilAsync(() => Stat.GetCurrentStamina(caster) >= stamina + 16, 8f, "the second scheduled pulse of each channel");
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(firstHP - 8, GetCurrentHitPoints(first), "later ticks do not restart the once-per-cast bonus");
            ctx.AssertEqual(100, Combat.ConsumeNextAbilityDamageBonus(caster, PerkType.KillzoneBeacon), "scheduled ticks leave the next activation's bonus untouched");
            ctx.Assert(Ability.GetActiveAbilityImpactSummary(caster) == null, "scheduled impacts leave no active tracker");

            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                Ability.BeginAbilityImpact(caster, area);
                try { ScheduleChannel(CombatDamageType.Physical, 3f); }
                finally { Ability.EndAbilityImpact(caster); }
            });
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(first) == firstHP - 16, 8f, "a new cast to receive its own bonus pulse");
        }

        [EngineTest("Resource damage bonuses retain independent strict thresholds", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task IndependentResourceThresholds(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            ctx.SetNPCResources(caster, 100, 100);
            SetLocalInt(caster, $"PERK_LEVEL_{(int)PerkType.BalancedCurrent}", 3);
            TemporaryStatModifier.Add(caster, StatType.MaxFP, 100 - Stat.GetMaxFP(caster), 45f);
            TemporaryStatModifier.Add(caster, StatType.MaxStamina, 100 - Stat.GetMaxStamina(caster), 45f);
            TemporaryStatModifier.Add(caster, StatType.HighFPAndStaminaAbilityDamageBonus, 7, 45f, "third-resource-source");
            TemporaryStatModifier.Add(caster, StatType.HighFPAndStaminaAbilityDamageBonusThresholdPercent, 80, 45f, "third-resource-source");
            StatusEffect.ApplyStatusEffect(caster, caster, new InfiniteConduitStatusEffect(), 45f);
            Ability.BeginAbilityImpact(caster, new AbilityDetail { IsHostileAbility = true, IsAreaAbility = true });
            try
            {
                var sources = Ability.GetAbilityImpactStatSources(caster, StatType.HighFPAndStaminaAbilityDamageBonus);
                foreach (var (resource, expected) in new[] { (81, 39), (80, 32), (71, 32), (70, 12), (65, 12), (61, 12), (60, 0) })
                {
                    Stat.ReduceFP(caster, Stat.GetCurrentFP(caster) - resource);
                    Stat.ReduceStamina(caster, Stat.GetCurrentStamina(caster) - resource);
                    ctx.AssertEqual(expected, Combat.GetHighResourceAbilityDamageBonus(caster), $"flat ability bonus at {resource}% FP and STM");
                    ctx.Assert(ReferenceEquals(sources, Ability.GetAbilityImpactStatSources(caster, StatType.HighFPAndStaminaAbilityDamageBonus)),
                        "resource thresholds are evaluated per target while source descriptions are reused");
                }
            }
            finally { Ability.EndAbilityImpact(caster); }
        }

        [EngineTest("Evasive Challenge refunds stamina once while retaining its evasion and timer", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task EvasiveChallengeRetainsBuff(EngineTestContext ctx)
        {
            var beast = ctx.SpawnCreature("nw_bandit001");
            var attacker = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, beast);
            PrepareStationaryCreature(ctx, attacker);
            ctx.MakeHostile(attacker);
            ctx.SetNPCResources(beast, 0, 100);
            Stat.ReduceStamina(beast, 50);
            var startingStamina = Stat.GetCurrentStamina(beast);
            StatusEffect.ApplyStatusEffect(beast, beast, new EvasiveChallenge1SelfStatusEffect(), 30f);
            var effect = StatusEffect.GetStatusEffect(beast, typeof(EvasiveChallenge1SelfStatusEffect));
            var duration = effect.DurationTicks;
            Combat.TrackAvoidedAttack(beast, attacker);
            Combat.TrackAvoidedAttack(beast, attacker);
            ctx.AssertEqual(startingStamina + 1, Stat.GetCurrentStamina(beast), "only the first evasion restores stamina");
            ctx.AssertEqual(8, Stat.GetStatAdjustment(beast, StatType.EvasionPercentAdjustment), "evasion remains after the refund");
            ctx.Assert(ReferenceEquals(effect, StatusEffect.GetStatusEffect(beast, typeof(EvasiveChallenge1SelfStatusEffect))), "original status instance remains active");
            ctx.AssertEqual(duration, effect.DurationTicks, "consuming the refund does not reset or expire the buff");
        }

        [EngineTest("Unbreakable Beast blocks knockdown, daze, and pulls without blanket resistance", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task UnbreakableBeastImmunities(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var beast = ctx.SpawnCreature("nw_bandit001", 4f);
            var ordinary = ctx.SpawnCreature("nw_bandit001", 3f);
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            foreach (var target in new[] { beast, ordinary })
            {
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 1000, true);
                SetAILevel(target, AILevel.VeryLow);
                ctx.MakeHostile(target);
            }
            StatusEffect.ApplyStatusEffect(beast, beast, new UnbreakableBeast1StatusEffect(), 30f);
            TemporaryStatModifier.Add(beast, StatType.MobilityResistance, -20, 30f);
            TemporaryStatModifier.Add(beast, StatType.MindResistance, -20, 30f);
            ctx.Assert(!StatusEffect.ApplyStatusEffect(caster, beast, new KnockdownStatusEffect(), 6f), "explicit immunity survives a Mobility vulnerability");
            ctx.Assert(!StatusEffect.ApplyStatusEffect(caster, beast, new DazedStatusEffect(), 15f), "explicit immunity survives a Mind vulnerability");
            // A native knockdown exercises pulling an incapacitated creature.
            ApplyEffectToObject(DurationType.Temporary, EffectKnockdown(), ordinary, 3f);
            var maul = Ability.GetAbilityDetail(FeatType.WardenMaulTechnique);
            Combat.SetAbilityHitResolutionOverride(true);
            Ability.BeginAbilityImpact(caster, maul);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(caster, () => maul.ImpactAction(caster, beast, 1, GetLocation(beast)));
            }
            finally
            {
                Ability.EndAbilityImpact(caster);
                Combat.SetAbilityHitResolutionOverride(null);
            }
            await ctx.DelaySecondsAsync(1f);
            ctx.Assert(GetDistanceBetween(caster, beast) > 3f, "Warden Maul cannot pull an immune beast adjacent to its source");
            ctx.Assert(GetDistanceBetween(caster, ordinary) < 1.5f, "the same impact pulls a nonimmune target immediately while it is knocked down");
            ctx.AssertEqual(-20, Stat.GetStatAdjustment(beast, StatType.MindResistance), "the capstone does not grant blanket Mind immunity");
            ctx.AssertEqual(-25, Stat.GetStatAdjustment(beast, StatType.DamageTakenPercentAdjustment), "damage reduction remains active");
        }

        [EngineTest("Static Burst creates at most two extra arcs for a multi-target cast", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task StaticBurstArcLimit(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var targets = new[] { ctx.SpawnCreature("nw_rat001", 1f), ctx.SpawnCreature("nw_rat001", 2f), ctx.SpawnCreature("nw_rat001", 3f) };
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            foreach (var target in targets)
            {
                PrepareStationaryCreature(ctx, target);
                ctx.MakeHostile(target);
            }
            var initialHP = targets.ToDictionary(target => target, GetCurrentHitPoints);
            var burst = Ability.GetAbilityDetail(FeatType.StaticBurstTechnique);
            // Exercise the actual definition's chain callback separately from its primary damage.
            // This isolates arc fanout and keeps the fixture on the arena's small walkable anchor.
            var closure = burst.ImpactAction.Target;
            var chain = (Action<uint, uint>)closure.GetType()
                .GetField("afterSuccessfulHit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                .GetValue(closure);
            Combat.SetAbilityHitResolutionOverride(true);
            Ability.BeginAbilityImpact(caster, burst);
            try
            {
                ChangeToStandardFaction(targets[1], StandardFaction.Defender);
                ChangeToStandardFaction(targets[2], StandardFaction.Defender);
                chain(caster, targets[0]);
                ctx.MakeHostile(targets[1]);
                ctx.MakeHostile(targets[2]);
                foreach (var target in targets)
                    chain(caster, target);
            }
            finally
            {
                Ability.EndAbilityImpact(caster);
                Combat.SetAbilityHitResolutionOverride(null);
            }
            await ctx.WaitUntilAsync(() => targets.Any(target => GetCurrentHitPoints(target) < initialHP[target]), 5f, "the chain damage to resolve");
            ctx.AssertEqual(2, targets.Count(target => GetCurrentHitPoints(target) < initialHP[target]), "the cast has two total extra arcs, not two per primary hit");
        }

        [EngineTest("Last Bastion includes every enemy in its radius", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task LastBastionNoHiddenTargetCap(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            var enemies = Enumerable.Range(0, 12).Select(index => ctx.SpawnCreature("nw_rat001", 2f + index % 4, index / 4f)).ToArray();
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            foreach (var enemy in enemies)
            {
                PrepareStationaryCreature(ctx, enemy);
                ctx.MakeHostile(enemy);
            }
            Ability.GetAbilityDetail(FeatType.LastBastionTechnique).ImpactAction(caster, caster, 1, GetLocation(caster));
            ctx.AssertEqual(12, enemies.Count(enemy => StatusEffect.HasStatusEffect(enemy, typeof(LastBastionStatusEffect))), "no undocumented ten-target cap");
        }

        [EngineTest("Area combat traits grant per-target haste and capped stamina across skills", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task AreaCombatTraitsAcrossSkills(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            ctx.SetNPCResources(caster, 100, 100);
            SetLocalInt(caster, $"PERK_LEVEL_{(int)PerkType.Momentum}", 1);
            SetLocalInt(caster, $"PERK_LEVEL_{(int)PerkType.SpinningRhythm}", 3);
            SetLocalInt(caster, $"PERK_LEVEL_{(int)PerkType.SweepingAdvance}", 1);
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityUsedEvasionPercentAdjustment, 10, 45f);
            TemporaryStatModifier.Add(caster, StatType.AreaAbilityUsedEvasionDurationSeconds, 30, 45f);
            Stat.ReduceStamina(caster, 50);
            var before = Stat.GetCurrentStamina(caster);
            Combat.ApplyAbilityImpactEffects(caster, new AbilityImpactSummary
                { SkillType = SkillType.Force, IsAreaAbility = true, ImpactedTargetCount = 4 });
            ctx.AssertEqual(17, Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent), "four targets grant one Momentum stack and three independent Spinning Rhythm stacks");
            ctx.AssertEqual(before + 6, Stat.GetCurrentStamina(caster), "Sweeping Advance caps four targets at six STM");
            Combat.ApplyAbilityImpactEffects(caster, new AbilityImpactSummary
                { SkillType = SkillType.Mimicry, IsAreaAbility = true, ImpactedTargetCount = 1 });
            ctx.AssertEqual(17, Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent), "one struck enemy grants no haste stack");
            ctx.AssertEqual(before + 8, Stat.GetCurrentStamina(caster), "one target restores two STM across skills");
            Combat.ApplyAbilityImpactEffects(caster, new AbilityImpactSummary
                { SkillType = SkillType.Mimicry, IsAreaAbility = true, ImpactedTargetCount = 2 });
            ctx.AssertEqual(22, Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent), "Momentum gains its second stack while Spinning Rhythm remains capped");
            Combat.ApplyAbilityImpactEffects(caster, new AbilityImpactSummary
                { SkillType = SkillType.Force, IsAreaAbility = true, ImpactedTargetCount = 6 });
            ctx.AssertEqual(27, Stat.GetStatAdjustment(caster, StatType.AttackDelayReductionPercent), "both perks reach their own caps");
            Combat.ApplyAbilityActivatedEffects(caster, OBJECT_INVALID, FeatType.Invalid,
                new AbilityDetail { SkillType = SkillType.Force, IsAreaAbility = true, IsHostileAbility = true }, new AbilityImpactSummary());
            ctx.AssertEqual(10, Stat.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment), "Flowing Footwork triggers on an empty Force area cast");
        }

        [EngineTest("Blade Vortex restores stamina once only after three targets land", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task BladeVortexTargetThreshold(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            ctx.SetNPCResources(caster, 100, 100);
            var ability = Ability.GetAbilityDetail(FeatType.BladeVortex2);
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                for (var count = 1; count <= 4; count++)
                {
                    var target = ctx.SpawnCreature("nw_rat001", count);
                    await ctx.WaitFrameAsync();
                    PrepareStationaryCreature(ctx, target);
                    ctx.MakeHostile(target);
                    Stat.ReduceStamina(caster, 20);
                    var before = Stat.GetCurrentStamina(caster);
                    var hp = GetCurrentHitPoints(target);
                    Ability.BeginAbilityImpact(caster, ability);
                    try { await ctx.ExecuteInCreatureContextAsync(caster, () => ability.ImpactAction(caster, target, 1, GetLocation(caster))); }
                    finally { Ability.EndAbilityImpact(caster); }
                    await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) < hp, 5f, "the area impact to land");
                    ctx.AssertEqual(before + (count >= 3 ? 6 : 0), Stat.GetCurrentStamina(caster), $"{count} targets must refund once only at the three-target threshold");
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        [EngineTest("Finishing Drive reaches three stacks through its real cooldown", Category = "PerkTracker", TimeoutSeconds = 40f)]
        public static async Task FinishingDriveStacking(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            ctx.SuppressNPCNaturalRegen(caster);
            ctx.SetNPCResources(caster, 100, 100);
            SWLOR.NWN.API.NWNX.CreaturePlugin.AddFeat(caster, FeatType.FinishingDriveTechnique);
            var ability = Ability.GetAbilityDetail(FeatType.FinishingDriveTechnique);
            var requiresPlayerLoadout = ability.IsMimicryTechnique;
            try
            {
                // The NPC fixture cannot own a player loadout. Keep the real activation
                // and recast pipeline while bypassing only that gate for this serial test.
                ability.IsMimicryTechnique = false;
                for (var stack = 1; stack <= 3; stack++)
                {
                    if (stack > 1)
                        await ctx.DelaySecondsAsync(10.1f);
                    var used = false;
                    AssignCommand(caster, () => used = UsePerkFeat.TryUseAbility(caster, caster, FeatType.FinishingDriveTechnique, GetLocation(caster), true));
                    await ctx.WaitUntilAsync(() => used, 3f, "Finishing Drive to activate after its cooldown");
                    var expected = stack;
                    await ctx.WaitUntilAsync(() => (StatusEffect.GetStatusEffect(caster, typeof(FinishingDriveMomentumStatusEffect)) as FinishingDriveMomentumStatusEffect)?.Stacks == expected,
                        3f, "the next Momentum stack");
                    ctx.AssertEqual(stack * 8, Stat.GetStatAdjustment(caster, StatType.MimicryPotencyPercent), "each cast adds eight percent potency");
                }
            }
            finally { ability.IsMimicryTechnique = requiresPlayerLoadout; }
        }

        [EngineTest("Droid programming accepts only an owned instruction disc", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task DroidInstructionInputValidation(EngineTestContext ctx)
        {
            var programmer = ctx.SpawnCreature("nw_bandit001");
            var other = ctx.SpawnCreature("nw_bandit001", 2f);
            await ctx.WaitFrameAsync();
            uint disc = OBJECT_INVALID, otherDisc = OBJECT_INVALID, weapon = OBJECT_INVALID, controller = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(programmer, () =>
            {
                disc = CreateItemOnObject("id_adrenal1", programmer);
                otherDisc = CreateItemOnObject("id_adrenal1", other);
                weapon = CreateItemOnObject("nw_wswss001", programmer);
                controller = CreateItemOnObject(Droid.DroidControlItemResref, programmer);
            });
            foreach (var item in new[] { disc, otherDisc, weapon, controller })
                ctx.Assert(GetIsObjectValid(item), "the programming fixture item exists");
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidInstruction,
                (int)SWLOR.Game.Server.Service.PerkService.PerkType.AdrenalStim, 1), controller);
            ctx.AssertEqual(string.Empty, Droid.GetInstructionDiscValidationError(programmer, disc), "an owned instruction disc is accepted");
            foreach (var item in new[] { otherDisc, weapon, controller, OBJECT_INVALID })
                ctx.Assert(!string.IsNullOrEmpty(Droid.GetInstructionDiscValidationError(programmer, item)), "invalid programming inputs are rejected before consumption");
        }

        [EngineTest("Ground Quake converts Dazed into Knockdown while preserving immune targets", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task GroundQuakeControlConversion(EngineTestContext ctx)
        {
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                foreach (var feat in new[] { FeatType.GroundQuake1, FeatType.GroundQuake2 })
                {
                    var caster = ctx.SpawnCreature("nw_bandit001");
                    var target = ctx.SpawnCreature("nw_rat001", 1f);
                    var immune = ctx.SpawnCreature("nw_rat001", 2f);
                    var unaffected = ctx.SpawnCreature("nw_rat001", 3f);
                    await ctx.WaitFrameAsync();
                    foreach (var creature in new[] { caster, target, immune, unaffected })
                        PrepareStationaryCreature(ctx, creature);
                    foreach (var enemy in new[] { target, immune, unaffected })
                        ctx.MakeHostile(enemy);
                    foreach (var dazed in new[] { target, immune })
                        ctx.Assert(StatusEffect.ApplyStatusEffect(caster, dazed, new DazedStatusEffect(), 30f), "setup Dazed applies");
                    TemporaryStatModifier.Add(immune, StatType.KnockdownImmunity, 1, 30f);
                    var originalDaze = StatusEffect.GetStatusEffect(immune, typeof(DazedStatusEffect));
                    var ability = Ability.GetAbilityDetail(feat);
                    await ctx.ExecuteInCreatureContextAsync(caster, () =>
                    {
                        Ability.BeginAbilityImpact(caster, ability);
                        try { ability.ImpactAction(caster, target, ability.AbilityLevel, GetLocation(caster)); }
                        finally { Ability.EndAbilityImpact(caster); }
                    });
                    ctx.Assert(StatusEffect.HasStatusEffect<KnockdownStatusEffect>(target), $"{feat} converts existing Dazed");
                    ctx.Assert(!StatusEffect.HasStatusEffect<DazedStatusEffect>(target), "conversion replaces the previous control");
                    ctx.Assert(!StatusEffect.HasStatusEffect<KnockdownStatusEffect>(immune), "explicit immunity prevents conversion");
                    ctx.Assert(ReferenceEquals(originalDaze, StatusEffect.GetStatusEffect(immune, typeof(DazedStatusEffect))), "rejected conversion preserves Dazed and its timer");
                    ctx.Assert(!StatusEffect.HasStatusEffect<KnockdownStatusEffect>(unaffected), "targets without Dazed receive no knockdown");
                    foreach (var creature in new[] { caster, target, immune, unaffected })
                        DestroyObject(creature);
                    await ctx.WaitFrameAsync();
                }
            }
            finally { Combat.SetAbilityHitResolutionOverride(null); }
        }

        [EngineTest("Ranked self-buffs replace weaker effects and reject downgrades", Category = "PerkTracker", TimeoutSeconds = 30f)]
        public static async Task RankedSelfBuffReplacement(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            PrepareStationaryCreature(ctx, caster);
            foreach (var (lower, higher, stat, expected) in new[]
            {
                (typeof(IronHide1StatusEffect), typeof(IronHide3StatusEffect), StatType.PhysicalDamageTakenPercentAdjustment, -12),
                (typeof(EvasiveManeuver1StatusEffect), typeof(EvasiveManeuver3StatusEffect), StatType.EvasionPercentAdjustment, 14),
                (typeof(BolsterAttack1StatusEffect), typeof(BolsterAttack3StatusEffect), StatType.DamageDealtPercentAdjustment, 12)
            })
            {
                ctx.Assert(StatusEffect.ApplyStatusEffect(caster, caster, lower, 180f), "lower-rank buff applies");
                ctx.Assert(StatusEffect.ApplyStatusEffect(caster, caster, higher, 180f), "higher-rank buff upgrades the lower rank");
                ctx.Assert(!StatusEffect.HasStatusEffect(caster, lower), "the lower rank is removed");
                ctx.AssertEqual(expected, Stat.GetStatAdjustment(caster, stat), "ranks do not stack their payloads");
                ctx.Assert(!StatusEffect.ApplyStatusEffect(caster, caster, lower, 180f), "a weaker rank cannot replace the stronger buff");
            }
        }

        private static void PrepareStationaryCreature(EngineTestContext ctx, uint creature)
        {
            ctx.SuppressNPCNaturalRegen(creature);
            Stat.SetNPCMaxHitPoints(creature, 1000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 60f);
        }
    }
}
