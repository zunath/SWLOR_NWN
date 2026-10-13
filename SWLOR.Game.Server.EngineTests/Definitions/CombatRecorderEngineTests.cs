using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class CombatRecorderEngineTests
    {
        private const string CreatureResref = "nw_bandit001";
        private const string TemporaryHitPointKey = "RECORDER_TEST";
        private const int TemporaryHitPoints = 5;
        private const int ResourcePool = 100;

        /// <summary>
        /// One NPC damages another with Force Lightning through a temporary-HP pool and a third
        /// heals it with Benevolence. The recording must attribute each change to its caster and
        /// ability, report the real absorbed amount, and total both in the summary.
        /// </summary>
        [EngineTest("Combat recorder attributes ability damage, temporary HP absorbs and healing", Category = "CombatRecorder", TimeoutSeconds = 60f)]
        public static async Task RecordsAttributedDamageAbsorbAndHealing(EngineTestContext ctx)
        {
            ctx.Assert(!CombatRecorder.IsRecording, "no combat recording may be running before the test");

            var arena = await QuietArena.CreateAsync(ctx);
            var target = arena.Spawn(CreatureResref, 0f, 0f);
            var attacker = arena.Spawn(CreatureResref, 3f, 180f);
            var healer = arena.Spawn(CreatureResref, -9f, 0f);
            await ctx.DelaySecondsAsync(1f);

            SetName(target, "Recorder Target");
            SetName(attacker, "Recorder Attacker");
            SetName(healer, "Recorder Healer");
            foreach (var creature in new[] { target, attacker, healer })
            {
                ctx.SuppressNPCNaturalRegen(creature);
                SetPlotFlag(creature, false);
                ctx.SetNPCResources(creature, ResourcePool, ResourcePool);
            }
            CreaturePlugin.AddFeat(attacker, FeatType.ForceLightning1);
            CreaturePlugin.AddFeat(healer, FeatType.Benevolence1);
            Stat.SetNPCMaxHitPoints(target, 1000, true);
            ObjectPlugin.SetCurrentHitPoints(target, 500);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 120f);
            ctx.MakeHostile(attacker);
            ctx.SeedRandom(4242);

            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            CombatRecordWriter writer;
            int targetHitPointsBeforeDamage;
            try
            {
                ctx.Assert(CombatRecorder.Start(arena.Area, "engine test", out var startMessage), startMessage);
                writer = CombatRecorder.LastWriter;

                TemporaryHitPointEffects.ApplyFlatFromSource(healer, target, TemporaryHitPointKey, TemporaryHitPoints, 120f);
                targetHitPointsBeforeDamage = GetCurrentHitPoints(target) - TemporaryHitPointEffects.GetRemaining(target);

                await UseAbilityAsync(ctx, attacker, target, FeatType.ForceLightning1);
                await ctx.WaitUntilAsync(
                    () => GetCurrentHitPoints(target) - TemporaryHitPointEffects.GetRemaining(target) < targetHitPointsBeforeDamage,
                    10f,
                    "Force Lightning I to damage the target");

                var hitPointsBeforeHeal = GetCurrentHitPoints(target);
                await UseAbilityAsync(ctx, healer, target, FeatType.Benevolence1);
                await ctx.WaitUntilAsync(
                    () => GetCurrentHitPoints(target) > hitPointsBeforeHeal,
                    10f,
                    "Benevolence I to heal the target");

                // At least one snapshot cycle while everyone is registered.
                await ctx.DelaySecondsAsync(1.5f);
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
                if (CombatRecorder.IsRecording)
                    CombatRecorder.Stop("engine test", out _);
            }

            await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the recording writer to finish its files");
            ctx.Assert(writer.Error == null, $"the recording writer failed: {writer.Error}");
            var summary = writer.Completion.Result;
            var events = File.ReadAllLines(writer.EventsPath)
                .Select(ParseLine)
                .ToList();

            var targetId = ParticipantId(ctx, events, "Recorder Target");
            var attackerId = ParticipantId(ctx, events, "Recorder Attacker");
            var healerId = ParticipantId(ctx, events, "Recorder Healer");

            AssertDamage(ctx, events, summary, attackerId, targetId, healerId, targetHitPointsBeforeDamage);
            AssertHealing(ctx, events, summary, healerId, targetId);
            AssertAbilityCost(ctx, events, attackerId);

            ctx.Assert(events.Any(e => Type(e) == "status_apply" && e.GetProperty("target").GetInt32() == targetId &&
                                       e.GetProperty("effect").GetString() == "Shock" &&
                                       e.GetProperty("ability").GetString() == "Force Lightning I"),
                "Force Lightning's Shock must be recorded with its originating ability");
            ctx.Assert(events.Any(e => Type(e) == "snapshot" && e.GetProperty("creature").GetInt32() == targetId),
                "the target must receive at least one snapshot");
            ctx.Assert(events.All(e => e.GetProperty("ts_ms").GetInt64() > 0 && e.GetProperty("frame").GetUInt64() > 0),
                "every event must carry a timestamp and a server frame");
            ctx.AssertEqual("recording_stopped", Type(events.Last()), "the last event");
            ctx.AssertEqual(events.Count, summary.EventCount, "summary event count");

            AssertParticipantsDeclaredFirst(ctx, events);
            File.Delete(writer.EventsPath);
            File.Delete(writer.SummaryPath);
        }

        /// <summary>
        /// Changes that happen outside the casting script must still reach their real source: a
        /// reactive status heal during an enemy's ability, a status tick that dispatches Direct
        /// damage through AssignCommand, an ability's delayed healing pulses, and a kill where one
        /// creature damaged the victim before another landed the lethal blow.
        /// </summary>
        [EngineTest("Combat recorder attributes reactive, deferred, delayed and lethal events", Category = "CombatRecorder", TimeoutSeconds = 90f)]
        public static async Task RecordsReactiveDeferredDelayedAndLethalEvents(EngineTestContext ctx)
        {
            ctx.Assert(!CombatRecorder.IsRecording, "no combat recording may be running before the test");

            var arena = await QuietArena.CreateAsync(ctx);
            var guarded = arena.Spawn(CreatureResref, 0f, 0f);
            var attacker = arena.Spawn(CreatureResref, 3f, 180f);
            var burned = arena.Spawn(CreatureResref, -7f, 0f);
            var victim = arena.Spawn(CreatureResref, 12f, 0f);
            var healer = arena.Spawn(CreatureResref, -14f, 0f);
            var smoldering = arena.Spawn(CreatureResref, -7f, 90f);
            var arsonist = arena.Spawn(CreatureResref, -10f, 90f);
            var bystander = arena.Spawn(CreatureResref, 12f, 90f);
            await ctx.DelaySecondsAsync(1f);

            Prepare(ctx, guarded, "Recorder Guarded", 500);
            Prepare(ctx, attacker, "Recorder Attacker", 1000);
            Prepare(ctx, burned, "Recorder Burned", 1000);
            Prepare(ctx, victim, "Recorder Victim", 50);
            Prepare(ctx, healer, "Recorder Healer", 500);
            Prepare(ctx, smoldering, "Recorder Smoldering", 1000);
            Prepare(ctx, bystander, "Recorder Bystander", 50);
            foreach (var held in new[] { guarded, burned, victim, smoldering, bystander })
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), held, 120f);
            CreaturePlugin.AddFeat(attacker, FeatType.ForceLightning1);
            CreaturePlugin.AddFeat(healer, FeatType.KoltoMist1);
            ctx.MakeHostile(attacker);
            ctx.SeedRandom(4343);

            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            CombatRecordWriter writer;
            try
            {
                ctx.Assert(CombatRecorder.Start(arena.Area, "engine test deferred", out var startMessage), startMessage);
                writer = CombatRecorder.LastWriter;

                ctx.Assert(StatusEffect.ApplyStatusEffect(guarded, guarded, new GuardiansResolveStatusEffect(1000), 60f),
                    "Guardian's Resolve applies to the guarded creature");
                ctx.Assert(StatusEffect.ApplyStatusEffect(attacker, burned, new BurnStatusEffect(1), 30f),
                    "Burn applies to the burned creature");
                ctx.Assert(StatusEffect.ApplyStatusEffect(arsonist, smoldering, new BurnStatusEffect(1), 30f),
                    "Burn applies to the smoldering creature");
                DestroyObject(arsonist);

                // The bystander's first recorded event is the hit that kills it.
                AssignCommand(healer, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(9999), bystander));

                await UseAbilityAsync(ctx, attacker, guarded, FeatType.ForceLightning1);
                await UseAbilityAsync(ctx, healer, healer, FeatType.KoltoMist1);

                // The attacker wounds the victim first; the healer lands the lethal blow.
                AssignCommand(attacker, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(5), victim));
                await ctx.DelaySecondsAsync(0.5f);
                AssignCommand(healer, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(9999), victim));
                await ctx.WaitUntilAsync(() => GetIsDead(victim) || !GetIsObjectValid(victim), 5f, "the victim to die");

                // Burn ticks every 6 seconds on the shared status interval; Kolto Mist pulses every 3.
                await ctx.DelaySecondsAsync(14f);
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
                if (CombatRecorder.IsRecording)
                    CombatRecorder.Stop("engine test", out _);
            }

            await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the recording writer to finish its files");
            ctx.Assert(writer.Error == null, $"the recording writer failed: {writer.Error}");
            var events = File.ReadAllLines(writer.EventsPath).Select(ParseLine).ToList();

            var guardedId = ParticipantId(ctx, events, "Recorder Guarded");
            var attackerId = ParticipantId(ctx, events, "Recorder Attacker");
            var burnedId = ParticipantId(ctx, events, "Recorder Burned");
            var victimId = ParticipantId(ctx, events, "Recorder Victim");
            var healerId = ParticipantId(ctx, events, "Recorder Healer");
            var smolderingId = ParticipantId(ctx, events, "Recorder Smoldering");
            var bystanderId = ParticipantId(ctx, events, "Recorder Bystander");

            var orphanedTicks = events.Where(e => Type(e) == "damage" && e.GetProperty("target").GetInt32() == smolderingId).ToList();
            ctx.Assert(orphanedTicks.Count > 0, "Burn must keep ticking after its source is gone");
            ctx.Assert(orphanedTicks.All(e => e.GetProperty("source_kind").GetString() == "status_effect" &&
                                              e.GetProperty("source_name").GetString() == "Burn"),
                "a Burn tick whose source is gone must still be recorded as Burn");
            ctx.Assert(events.Any(e => Type(e) == "death" && e.GetProperty("target").GetInt32() == bystanderId),
                "the bystander's death must be recorded");

            var guardedHeals = events.Where(e => Type(e) == "heal" && e.GetProperty("target").GetInt32() == guardedId).ToList();
            ctx.Assert(guardedHeals.Any(e => e.GetProperty("source").GetInt32() == guardedId &&
                                             e.GetProperty("source_kind").GetString() == "status_effect" &&
                                             e.GetProperty("source_name").GetString() == "Guardian's Resolve"),
                "Guardian's Resolve healing must belong to its status source");
            ctx.Assert(guardedHeals.All(e => e.GetProperty("source").GetInt32() != attackerId),
                "the attacker must not be credited with the defender's reactive healing");
            foreach (var heal in guardedHeals)
            {
                var healIndex = events.IndexOf(heal);
                var hitBefore = events.Take(healIndex).LastOrDefault(e => Type(e) == "damage" && e.GetProperty("target").GetInt32() == guardedId);
                if (hitBefore.ValueKind != JsonValueKind.Object)
                    continue;
                ctx.AssertEqual(hitBefore.GetProperty("hp_after").GetInt32(), heal.GetProperty("hp_before").GetInt32(),
                    "a reactive heal starts from the HP the hit left");
            }

            ctx.Assert(events.Any(e => Type(e) == "damage" &&
                                       e.GetProperty("target").GetInt32() == burnedId &&
                                       e.GetProperty("source").GetInt32() == attackerId &&
                                       e.GetProperty("source_kind").GetString() == "status_effect" &&
                                       e.GetProperty("source_name").GetString() == "Burn"),
                "a Burn tick must be attributed to Burn and its source");

            var mistImpact = events.Single(e => Type(e) == "ability_impact" && e.GetProperty("caster").GetInt32() == healerId);
            var pulseHeals = events.Where(e => Type(e) == "heal" &&
                                               e.GetProperty("source").GetInt32() == healerId &&
                                               e.GetProperty("source_kind").GetString() == "ability" &&
                                               e.GetProperty("source_name").GetString() == "Kolto Mist I" &&
                                               e.GetProperty("ts_ms").GetInt64() >= mistImpact.GetProperty("ts_ms").GetInt64() + 2000)
                .ToList();
            ctx.Assert(pulseHeals.Count > 0, "Kolto Mist's delayed pulses must be attributed to Kolto Mist I");
            ctx.Assert(!events.Any(e => Type(e) == "heal" && e.GetProperty("source_kind").GetString() == "unattributed"),
                "every heal in the scenario has a known source");

            var lethalIndex = events.FindIndex(e => Type(e) == "damage" &&
                                                    e.GetProperty("target").GetInt32() == victimId &&
                                                    e.GetProperty("source").GetInt32() == healerId);
            var deathIndex = events.FindIndex(e => Type(e) == "death" && e.GetProperty("target").GetInt32() == victimId);
            ctx.Assert(lethalIndex >= 0, "the lethal hit must be recorded");
            ctx.Assert(deathIndex > lethalIndex, "the death must be recorded after the damage that caused it");
            ctx.AssertEqual(healerId, events[deathIndex].GetProperty("last_damage_source").GetInt32(), "last damage source of the victim");
            ctx.AssertEqual(healerId, events[deathIndex].GetProperty("killer").GetInt32(), "killer of the victim");
            var lethalHit = events[lethalIndex];
            ctx.AssertEqual(lethalHit.GetProperty("hp_before").GetInt32(), lethalHit.GetProperty("hp_lost").GetInt32(),
                "a lethal hit removes only the HP the victim had left");
            ctx.Assert(lethalHit.GetProperty("overkill").GetInt32() > 0, "the rest of the lethal hit is overkill");

            AssertParticipantsDeclaredFirst(ctx, events);
            File.Delete(writer.EventsPath);
            File.Delete(writer.SummaryPath);
        }

        /// <summary>
        /// Weapon hits are applied by the engine itself while damage-over-time ticks run from a
        /// closure; the recording must tell them apart, and a stat-driven autoattack refund must
        /// carry its StatType instead of being unattributed. A recurring pulse of a cast records
        /// its own impact, and the max HP read natively before each hit matches the script value.
        /// The Force portion a defender's stat splits off an ability hit stays with that ability,
        /// and an ally's Blazing Spikes on the defender reflects as Blazing Spikes.
        /// </summary>
        [EngineTest("Combat recorder separates weapon hits from script damage and names stat refunds", Category = "CombatRecorder", TimeoutSeconds = 90f)]
        public static async Task RecordsWeaponHitsAndStatRefunds(EngineTestContext ctx)
        {
            ctx.Assert(!CombatRecorder.IsRecording, "no combat recording may be running before the test");

            var arena = await QuietArena.CreateAsync(ctx);
            var dummy = arena.Spawn(CreatureResref, 0f, 0f);
            var brawler = arena.Spawn(CreatureResref, 1.5f, 180f);
            var pyro = arena.Spawn(CreatureResref, -9f, 0f);
            var mark = arena.Spawn(CreatureResref, 9f, 90f);
            await ctx.DelaySecondsAsync(1f);

            Prepare(ctx, dummy, "Recorder Dummy", 1000);
            Prepare(ctx, brawler, "Recorder Brawler", 1000);
            Prepare(ctx, pyro, "Recorder Pyro", 1000);
            Prepare(ctx, mark, "Recorder Mark", 1000);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), mark, 120f);
            TemporaryStatModifier.Add(dummy, StatType.IncomingPhysicalToForceConversionPercent, 50, 120f);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), dummy, 120f);
            TemporaryStatModifier.Add(brawler, StatType.AutoAttackFPRestore, 1, 120f);
            Stat.ReduceFP(brawler, 50);
            ctx.MakeHostile(brawler);
            // The trap only goes off for creatures hostile to its owner.
            ChangeToStandardFaction(mark, StandardFaction.Defender);
            ctx.SeedRandom(4444);

            Combat.SetAutoAttackHitResolutionOverride(true);
            CombatRecordWriter writer;
            int dummyMaxHitPoints;
            var registeredInsideImpact = true;
            var registeredOutsideImpact = false;
            try
            {
                ctx.Assert(CombatRecorder.Start(arena.Area, "engine test weapon", out var startMessage), startMessage);
                writer = CombatRecorder.LastWriter;

                ctx.Assert(StatusEffect.ApplyStatusEffect(brawler, dummy, typeof(BleedStatusEffect), 30f),
                    "Bleed applies to the dummy");
                ctx.Assert(StatusEffect.ApplyStatusEffect(pyro, dummy, typeof(BlazingSpikesStatusEffect), 60f),
                    "the pyro's Blazing Spikes applies to the dummy");
                var startingHitPoints = GetCurrentHitPoints(dummy);
                AssignCommand(brawler, () => ActionAttack(dummy));
                await ctx.WaitUntilAsync(
                    () => GetCurrentHitPoints(dummy) < startingHitPoints && GetLastDamager(dummy) == brawler,
                    45f,
                    "the brawler's first damage on the dummy");

                // Bleed ticks every 6 seconds on the shared status interval.
                await ctx.DelaySecondsAsync(13f);

                Ability.BeginAbilityImpact(brawler, Ability.GetAbilityDetail(FeatType.ForceLightning1));
                var pulse = Ability.CaptureRepeatedAbilityImpact(brawler, () => Ability.ApplyCombatImpact(
                    brawler, dummy, GetLocation(dummy), SkillType.Force, 10, 0, null, false,
                    resolvesHit: false, canCritical: false, useUnscaledDamage: true), baseDamage: 10);
                Ability.EndAbilityImpact(brawler);
                await ctx.ExecuteInCreatureContextAsync(brawler, pulse);
                await ctx.DelaySecondsAsync(1f);
                dummyMaxHitPoints = GetMaxHitPoints(dummy);

                // A Razor Trap placed on the mark goes off seconds later, outside any scope.
                Combat.SetAbilityHitResolutionOverride(true);
                var placed = false;
                await ctx.ExecuteInCreatureContextAsync(brawler, () =>
                {
                    using var placement = CombatRecorder.BeginAbilityFeatAttribution(brawler, FeatType.RazorTrap1);
                    placed = Traps.TryPlaceTrap(brawler, GetLocation(mark), 10, CombatDamageType.Physical,
                        typeof(BleedStatusEffect), 6, VisualEffect.Vfx_Com_Blood_Spark_Medium, VisualEffect.Vfx_Dur_Aura_Pulse_Orange_White);
                });
                ctx.Assert(placed, "the Razor Trap is placed on the mark");
                // Three seconds to arm, then a one-second proximity check.
                await ctx.DelaySecondsAsync(5f);

                // Damage-over-time computed during the source's own ability impact is queued with
                // that impact, so it registers nothing; outside an impact it is registered.
                await ctx.ExecuteInCreatureContextAsync(pyro, () =>
                {
                    Ability.BeginAbilityImpact(pyro, Ability.GetAbilityDetail(FeatType.CreepingTerror1));
                    Combat.ApplyDamageTakenModifiers(dummy, 7, pyro, CombatDamageType.Force, deliveryType: CombatDamageDeliveryType.DamageOverTime);
                    registeredInsideImpact = CombatRecorder.HasPendingDamage(pyro, dummy);
                    Ability.EndAbilityImpact(pyro);
                    Combat.ApplyDamageTakenModifiers(dummy, 7, pyro, CombatDamageType.Force, deliveryType: CombatDamageDeliveryType.DamageOverTime);
                    registeredOutsideImpact = CombatRecorder.HasPendingDamage(pyro, dummy);
                });
            }
            finally
            {
                Combat.SetAutoAttackHitResolutionOverride(null);
                Combat.SetAbilityHitResolutionOverride(null);
                AssignCommand(brawler, () => ClearAllActions());
                if (CombatRecorder.IsRecording)
                    CombatRecorder.Stop("engine test", out _);
            }

            await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the recording writer to finish its files");
            ctx.Assert(writer.Error == null, $"the recording writer failed: {writer.Error}");
            var events = File.ReadAllLines(writer.EventsPath).Select(ParseLine).ToList();

            ctx.Assert(!registeredInsideImpact, "damage queued into the source's own impact must not leave a registration");
            ctx.Assert(registeredOutsideImpact, "damage dispatched later must be registered");

            var dummyId = ParticipantId(ctx, events, "Recorder Dummy");
            var brawlerId = ParticipantId(ctx, events, "Recorder Brawler");
            var hits = events.Where(e => Type(e) == "damage" &&
                                         e.GetProperty("source").GetInt32() == brawlerId &&
                                         e.GetProperty("target").GetInt32() == dummyId).ToList();

            var weaponHits = hits.Where(e => e.GetProperty("source_kind").GetString() == "autoattack").ToList();
            ctx.Assert(weaponHits.Any(e => e.GetProperty("engine_applied").GetBoolean()),
                "forced autoattack hits must be recorded as engine-applied autoattack damage");
            ctx.Assert(hits.Any(e => e.GetProperty("source_kind").GetString() == "ability" &&
                                     e.GetProperty("source_name").GetString() == "Force Lightning I" &&
                                     !e.GetProperty("engine_applied").GetBoolean() &&
                                     e.GetProperty("damage").TryGetProperty("force", out _)),
                "the Force portion split off the pulse's physical hit must stay with Force Lightning I");

            var bleedTicks = hits.Where(e => e.GetProperty("source_kind").GetString() == "status_effect" &&
                                             e.GetProperty("source_name").GetString() == "Bleed").ToList();
            ctx.Assert(bleedTicks.Count > 0, "a Bleed tick must be recorded as Bleed damage");
            ctx.Assert(bleedTicks.All(e => !e.GetProperty("engine_applied").GetBoolean()),
                "status ticks are applied from a closure");
            ctx.Assert(hits.All(e => e.GetProperty("source_kind").GetString() != "unattributed"),
                "every brawler hit on the dummy has a known source");
            var markId = ParticipantId(ctx, events, "Recorder Mark");
            var trapHits = events.Where(e => Type(e) == "damage" &&
                                             e.GetProperty("source").GetInt32() == brawlerId &&
                                             e.GetProperty("target").GetInt32() == markId).ToList();
            ctx.Assert(trapHits.Count > 0, "the Razor Trap must go off on the mark");
            ctx.Assert(trapHits.All(e => e.GetProperty("source_kind").GetString() == "ability" &&
                                         e.GetProperty("source_name").GetString() == "Razor Trap I"),
                "the trap's blast must be attributed to the Razor Trap that placed it");
            ctx.Assert(events.Any(e => Type(e) == "damage" &&
                                       e.GetProperty("source").GetInt32() == dummyId &&
                                       e.GetProperty("target").GetInt32() == brawlerId &&
                                       e.GetProperty("source_kind").GetString() == "status_effect" &&
                                       e.GetProperty("source_name").GetString() == "Blazing Spikes"),
                "the dummy's reflection must belong to the Blazing Spikes the pyro applied");
            ctx.Assert(hits.All(e => e.GetProperty("max_hp").GetInt32() == dummyMaxHitPoints),
                $"each hit's max HP must match the script value {dummyMaxHitPoints}");

            ctx.Assert(events.Any(e => Type(e) == "ability_impact" &&
                                       e.GetProperty("caster").GetInt32() == brawlerId &&
                                       e.GetProperty("ability").GetString() == "Force Lightning I" &&
                                       e.GetProperty("recurring").GetBoolean() &&
                                       e.GetProperty("feat").GetString() == nameof(FeatType.ForceLightning1) &&
                                       e.GetProperty("targets_hit").GetInt32() == 1 &&
                                       e.GetProperty("damage_queued").GetInt32() > 0),
                "a recurring pulse must record its own impact with the cast's feat");
            ctx.Assert(hits.Any(e => e.GetProperty("source_kind").GetString() == "ability" &&
                                     e.GetProperty("source_name").GetString() == "Force Lightning I" &&
                                     e.TryGetProperty("feat", out var feat) &&
                                     feat.GetString() == nameof(FeatType.ForceLightning1)),
                "the pulse's damage must be attributed to its ability and feat");

            ctx.Assert(events.Any(e => Type(e) == "resource" &&
                                       e.GetProperty("creature").GetInt32() == brawlerId &&
                                       e.GetProperty("resource").GetString() == "fp" &&
                                       e.GetProperty("delta").GetInt32() > 0 &&
                                       e.GetProperty("reason").GetString() == "stat_trigger" &&
                                       e.GetProperty("reason_name").GetString() == nameof(StatType.AutoAttackFPRestore)),
                "the autoattack FP refund must be attributed to AutoAttackFPRestore");

            AssertParticipantsDeclaredFirst(ctx, events);
            File.Delete(writer.EventsPath);
            File.Delete(writer.SummaryPath);
        }

        /// <summary>
        /// Reactions to an enemy's ability belong to the reacting creature: the low-HP shield the
        /// defender's stats grant when the hit crosses its threshold (written after that hit, and
        /// named on the hits it later absorbs), the damage the defender's reflection stat sends
        /// back, and the recoil Soul Devourer Stance deals its own user. A Kolto Mist cast before
        /// the recording starts still credits its later pulses.
        /// </summary>
        [EngineTest("Combat recorder credits reactive shields and stance recoil to their own source", Category = "CombatRecorder", TimeoutSeconds = 60f)]
        public static async Task RecordsReactiveShieldAndStanceRecoil(EngineTestContext ctx)
        {
            ctx.Assert(!CombatRecorder.IsRecording, "no combat recording may be running before the test");

            var arena = await QuietArena.CreateAsync(ctx);
            var defender = arena.Spawn(CreatureResref, 0f, 0f);
            var attacker = arena.Spawn(CreatureResref, 3f, 180f);
            var medic = arena.Spawn(CreatureResref, -12f, 0f);
            await ctx.DelaySecondsAsync(1f);

            Prepare(ctx, defender, "Recorder Shielded", 600);
            Prepare(ctx, attacker, "Recorder Devourer", 1000);
            Prepare(ctx, medic, "Recorder Medic", 500);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), defender, 120f);
            // At 600 of 1000 HP, any hit crosses a 60% threshold.
            TemporaryStatModifier.Add(defender, StatType.LowHPTemporaryHPThresholdPercent, 60, 120f);
            TemporaryStatModifier.Add(defender, StatType.LowHPTemporaryHPPercent, 5, 120f);
            TemporaryStatModifier.Add(defender, StatType.LowHPTemporaryHPDurationSeconds, 60, 120f);
            TemporaryStatModifier.Add(defender, StatType.ForceDamageReflectionPercentAdjustment, 50, 120f);
            TemporaryStatModifier.Add(attacker, StatType.DarkForceDamageHPCostPercent, 2, 120f);
            CreaturePlugin.AddFeat(attacker, FeatType.ForceLightning1);
            CreaturePlugin.AddFeat(medic, FeatType.KoltoMist1);
            ctx.MakeHostile(attacker);
            ctx.SeedRandom(4545);

            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            CombatRecordWriter writer;
            var partialHit = 0;
            try
            {
                // The mist's pulses are scheduled before the recording exists.
                await UseAbilityAsync(ctx, medic, medic, FeatType.KoltoMist1);
                await ctx.WaitUntilAsync(
                    () => Ability.GetLastCompletedAbilityImpactSummary(medic)?.Ability?.Name == "Kolto Mist I",
                    10f,
                    "Kolto Mist I to land before the recording starts");
                ctx.Assert(!CombatRecorder.IsRecording, "the mist landed before the recording started");

                ctx.Assert(CombatRecorder.Start(arena.Area, "engine test reactions", out var startMessage), startMessage);
                writer = CombatRecorder.LastWriter;

                ctx.Assert(StatusEffect.ApplyStatusEffect(attacker, attacker, typeof(SoulDevourerStanceStatusEffect), 60f),
                    "Soul Devourer Stance applies to the attacker");
                var attackerHitPoints = GetCurrentHitPoints(attacker);
                await UseAbilityAsync(ctx, attacker, defender, FeatType.ForceLightning1);
                await ctx.WaitUntilAsync(
                    () => TemporaryHitPointEffects.GetRemaining(defender) > 0 && GetCurrentHitPoints(attacker) < attackerHitPoints,
                    10f,
                    "the defender's low-HP shield and the attacker's stance recoil");

                // A plain hit that the shield absorbs.
                AssignCommand(attacker, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(5), defender));
                // Kolto Mist pulses every 3 seconds.
                await ctx.DelaySecondsAsync(4f);

                // Activating the active stance turns it off.
                CreaturePlugin.AddFeat(attacker, FeatType.SoulDevourerStance1);
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                    UsePerkFeat.TryUseAbility(attacker, attacker, FeatType.SoulDevourerStance1, GetLocation(attacker)));
                await ctx.WaitUntilAsync(
                    () => !StatusEffect.HasStatusEffect<SoulDevourerStanceStatusEffect>(attacker),
                    10f,
                    "Soul Devourer Stance to be toggled off");

                // A hit that only partly drains the shield but crosses 60% again replaces the
                // shield during the hit; only what the hit took counts as absorbed.
                ObjectPlugin.SetCurrentHitPoints(defender, 590);
                var shieldLeft = TemporaryHitPointEffects.GetRemaining(defender);
                partialHit = GetCurrentHitPoints(defender) - 599;
                ctx.Assert(partialHit > 0 && partialHit < shieldLeft,
                    $"setup: a {partialHit} HP hit must cross 60% while only partly draining the {shieldLeft} HP shield");
                var hit = partialHit;
                AssignCommand(attacker, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(hit), defender));
                await ctx.DelaySecondsAsync(1f);
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
                if (CombatRecorder.IsRecording)
                    CombatRecorder.Stop("engine test", out _);
            }

            await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the recording writer to finish its files");
            ctx.Assert(writer.Error == null, $"the recording writer failed: {writer.Error}");
            var events = File.ReadAllLines(writer.EventsPath).Select(ParseLine).ToList();

            var defenderId = ParticipantId(ctx, events, "Recorder Shielded");
            var attackerId = ParticipantId(ctx, events, "Recorder Devourer");
            var medicId = ParticipantId(ctx, events, "Recorder Medic");

            var shieldIndex = events.FindIndex(e => Type(e) == "temp_hp_grant" &&
                                                    e.GetProperty("target").GetInt32() == defenderId &&
                                                    e.GetProperty("effect_key").GetString() == "LOW_HP_SHIELD");
            ctx.Assert(shieldIndex >= 0, "the low-HP shield grant must be recorded");
            var firstHitIndex = events.FindIndex(e => Type(e) == "damage" &&
                                                      e.GetProperty("source").GetInt32() == attackerId &&
                                                      e.GetProperty("target").GetInt32() == defenderId);
            ctx.Assert(firstHitIndex >= 0 && firstHitIndex < shieldIndex,
                "the hit that crossed the threshold must be written before the shield it triggered");
            var shield = events[shieldIndex];
            ctx.AssertEqual(defenderId, shield.GetProperty("source").GetInt32(), "the low-HP shield's granter");
            ctx.AssertEqual("stat_trigger", shield.GetProperty("source_kind").GetString(), "the low-HP shield's source kind");
            ctx.AssertEqual(nameof(StatType.LowHPTemporaryHPPercent), shield.GetProperty("source_name").GetString(),
                "the low-HP shield's source name");

            ctx.Assert(events.Any(e => Type(e) == "damage" &&
                                       e.GetProperty("source").GetInt32() == attackerId &&
                                       e.GetProperty("target").GetInt32() == attackerId &&
                                       e.GetProperty("source_kind").GetString() == "status_effect" &&
                                       e.GetProperty("source_name").GetString() == "Soul Devourer Stance"),
                "the stance recoil must be attributed to Soul Devourer Stance");
            ctx.Assert(events.Any(e => Type(e) == "damage" &&
                                       e.GetProperty("source").GetInt32() == attackerId &&
                                       e.GetProperty("target").GetInt32() == attackerId &&
                                       e.GetProperty("source_kind").GetString() == "ability_cost" &&
                                       e.GetProperty("source_name").GetString() == "Force Lightning I" &&
                                       e.GetProperty("total").GetInt32() == 20),
                "the Dark Force HP cost must be recorded as Force Lightning I's cost");

            var shieldAbsorbs = events.Where(e => Type(e) == "temp_hp_absorb" &&
                                                  e.GetProperty("effect_key").GetString() == "LOW_HP_SHIELD").ToList();
            ctx.Assert(shieldAbsorbs.Count > 0, "a later hit must be absorbed by the low-HP shield");
            ctx.Assert(shieldAbsorbs.All(e => e.GetProperty("granted_kind").GetString() == "stat_trigger" &&
                                              e.GetProperty("granted_name").GetString() == nameof(StatType.LowHPTemporaryHPPercent)),
                "the shield's absorbs must name the stat that granted it");

            ctx.Assert(events.Any(e => Type(e) == "damage" &&
                                       e.GetProperty("source").GetInt32() == defenderId &&
                                       e.GetProperty("target").GetInt32() == attackerId &&
                                       e.GetProperty("source_kind").GetString() == "stat_trigger" &&
                                       e.GetProperty("source_name").GetString() == nameof(StatType.ForceDamageReflectionPercentAdjustment)),
                "reflected damage must belong to the defender's reflection stat, not the attacker's ability");

            var partialHitEvent = events.Last(e => Type(e) == "damage" &&
                                                   e.GetProperty("source").GetInt32() == attackerId &&
                                                   e.GetProperty("target").GetInt32() == defenderId &&
                                                   e.GetProperty("total").GetInt32() == partialHit);
            ctx.AssertEqual(partialHit, partialHitEvent.GetProperty("temp_hp_absorbed").GetInt32(),
                "a shield replaced during the hit is measured against what it had left");
            ctx.AssertEqual(2, events.Count(e => Type(e) == "temp_hp_grant" &&
                                                 e.GetProperty("target").GetInt32() == defenderId &&
                                                 e.GetProperty("effect_key").GetString() == "LOW_HP_SHIELD"),
                "the second threshold crossing grants a new shield");

            ctx.Assert(events.Any(e => Type(e) == "ability_cast" &&
                                       e.GetProperty("caster").GetInt32() == attackerId &&
                                       e.GetProperty("ability").GetString() == "Soul Devourer Stance" &&
                                       e.GetProperty("toggled_off").GetBoolean()),
                "turning the stance off must be recorded as a use of it");

            var mistStatuses = events.Where(e => (Type(e) == "status_apply" || Type(e) == "status_refresh") &&
                                                 e.GetProperty("source").GetInt32() == medicId).ToList();
            ctx.Assert(mistStatuses.Count > 0, "Kolto Mist's pulses must apply their healing status during the recording");
            ctx.Assert(mistStatuses.All(e => e.GetProperty("ability").GetString() == "Kolto Mist I"),
                "statuses applied by a delayed pulse must name its ability");

            var mistHeals = events.Where(e => Type(e) == "heal" && e.GetProperty("source").GetInt32() == medicId).ToList();
            ctx.Assert(mistHeals.Count > 0, "Kolto Mist must pulse during the recording");
            ctx.Assert(mistHeals.All(e => e.GetProperty("source_kind").GetString() == "ability" &&
                                          e.GetProperty("source_name").GetString() == "Kolto Mist I"),
                "pulses of a mist cast before the recording must still be credited to Kolto Mist I");

            AssertParticipantsDeclaredFirst(ctx, events);
            File.Delete(writer.EventsPath);
            File.Delete(writer.SummaryPath);
        }

        /// <summary>
        /// Every participant id an event refers to must be declared by an earlier participant event.
        /// </summary>
        private static void AssertParticipantsDeclaredFirst(EngineTestContext ctx, List<JsonElement> events)
        {
            var declared = new HashSet<int>();
            var references = new[]
            {
                "source", "target", "caster", "attacker", "defender", "creature", "killer", "last_damage_source",
                "granted_by", "reviver", "subdued_by"
            };
            for (var index = 0; index < events.Count; index++)
            {
                var recordEvent = events[index];
                if (Type(recordEvent) == "participant")
                {
                    declared.Add(recordEvent.GetProperty("id").GetInt32());
                    continue;
                }

                foreach (var reference in references)
                {
                    if (recordEvent.TryGetProperty(reference, out var value) &&
                        value.ValueKind == JsonValueKind.Number &&
                        value.GetInt32() > 0)
                    {
                        ctx.Assert(declared.Contains(value.GetInt32()),
                            $"event {index} ({Type(recordEvent)}) refers to participant {value.GetInt32()} before it is declared");
                    }
                }
            }
        }

        private static void Prepare(EngineTestContext ctx, uint creature, string name, int hitPoints)
        {
            SetName(creature, name);
            ctx.SuppressNPCNaturalRegen(creature);
            SetPlotFlag(creature, false);
            ctx.SetNPCResources(creature, ResourcePool, ResourcePool);
            Stat.SetNPCMaxHitPoints(creature, 1000, true);
            ObjectPlugin.SetCurrentHitPoints(creature, hitPoints);
        }

        private static void AssertDamage(
            EngineTestContext ctx, List<JsonElement> events, CombatRecordSummary summary,
            int attackerId, int targetId, int healerId, int targetHitPointsBeforeDamage)
        {
            var hits = events.Where(e => Type(e) == "damage" &&
                                         e.GetProperty("source").GetInt32() == attackerId &&
                                         e.GetProperty("target").GetInt32() == targetId &&
                                         e.GetProperty("source_kind").GetString() == "ability" &&
                                         e.GetProperty("source_name").GetString() == "Force Lightning I")
                .ToList();
            ctx.Assert(hits.Count > 0, "Force Lightning I damage must be recorded with its caster and ability");

            var first = hits[0];
            var hitPointsLost = first.GetProperty("hp_lost").GetInt32();
            var absorbed = first.GetProperty("temp_hp_absorbed").GetInt32();
            ctx.AssertEqual(targetHitPointsBeforeDamage, first.GetProperty("hp_before").GetInt32(), "HP before the first hit");
            ctx.Assert(first.GetProperty("damage").GetProperty("electrical").GetInt32() > 0, "the hit carries its damage type");
            ctx.AssertEqual(Math.Min(TemporaryHitPoints, hitPointsLost + absorbed), absorbed,
                "the absorbed amount is what the pool actually took, not the full hit");

            var absorbEvent = events.First(e => Type(e) == "temp_hp_absorb" && e.GetProperty("attacker").GetInt32() == attackerId);
            ctx.AssertEqual(TemporaryHitPointKey, absorbEvent.GetProperty("effect_key").GetString(), "absorbing pool");
            ctx.AssertEqual(absorbed, absorbEvent.GetProperty("amount").GetInt32(), "absorb event amount");
            ctx.AssertEqual(healerId, absorbEvent.GetProperty("granted_by").GetInt32(), "the pool's granter");
            ctx.Assert(events.Any(e => Type(e) == "temp_hp_grant" &&
                                       e.GetProperty("source").GetInt32() == healerId &&
                                       e.GetProperty("amount").GetInt32() == TemporaryHitPoints),
                "the temporary HP grant must be recorded with its source");

            var effective = hits.Sum(e => e.GetProperty("hp_lost").GetInt32() + e.GetProperty("temp_hp_absorbed").GetInt32());
            ctx.AssertEqual((long)effective, summary.GetParticipant(attackerId).DamageDealt["Force Lightning I"].Effective,
                "attacker's Force Lightning I damage dealt");
            ctx.AssertEqual((long)effective, summary.GetParticipant(targetId).DamageTaken["Force Lightning I"].Effective,
                "target's Force Lightning I damage taken");
            ctx.AssertEqual((long)absorbed, summary.GetParticipant(targetId).TemporaryHitPointsAbsorbed.Values.Sum(total => total.Amount),
                "target's absorbed temporary HP");
            ctx.AssertEqual((long)absorbed, summary.GetParticipant(healerId).TemporaryHitPointsGrantedAbsorbed.Values.Sum(total => total.Amount),
                "healer's granted temporary HP that absorbed damage");
        }

        private static void AssertHealing(
            EngineTestContext ctx, List<JsonElement> events, CombatRecordSummary summary, int healerId, int targetId)
        {
            var heals = events.Where(e => Type(e) == "heal" &&
                                          e.GetProperty("source").GetInt32() == healerId &&
                                          e.GetProperty("target").GetInt32() == targetId &&
                                          e.GetProperty("source_kind").GetString() == "ability" &&
                                          e.GetProperty("source_name").GetString() == "Benevolence I")
                .ToList();
            ctx.AssertEqual(1, heals.Count, "Benevolence I heals recorded with their caster and ability");

            var heal = heals[0];
            var effective = heal.GetProperty("effective").GetInt32();
            ctx.Assert(effective > 0, "the heal restored HP");
            ctx.AssertEqual(heal.GetProperty("hp_after").GetInt32() - heal.GetProperty("hp_before").GetInt32(), effective,
                "effective healing is the HP actually restored");
            ctx.AssertEqual(heal.GetProperty("requested").GetInt32() - effective, heal.GetProperty("overheal").GetInt32(),
                "overheal");
            ctx.AssertEqual((long)effective, summary.GetParticipant(healerId).HealingDone["Benevolence I"].Effective,
                "healer's Benevolence I healing done");
            ctx.AssertEqual((long)effective, summary.GetParticipant(targetId).HealingReceived["Benevolence I"].Effective,
                "target's Benevolence I healing received");
        }

        private static void AssertAbilityCost(EngineTestContext ctx, List<JsonElement> events, int attackerId)
        {
            var cost = events.Single(e => Type(e) == "ability_cost" &&
                                          e.GetProperty("caster").GetInt32() == attackerId &&
                                          e.GetProperty("ability").GetString() == "Force Lightning I");
            var fpPaid = cost.GetProperty("fp").GetInt32();
            ctx.Assert(fpPaid > 0, "Force Lightning I must cost FP");
            ctx.AssertEqual(nameof(FeatType.ForceLightning1), cost.GetProperty("feat").GetString(), "the cost event's feat");

            var fpSpent = events.Where(e => Type(e) == "resource" &&
                                            e.GetProperty("creature").GetInt32() == attackerId &&
                                            e.GetProperty("resource").GetString() == "fp" &&
                                            e.GetProperty("reason").GetString() == "ability_cost")
                .Sum(e => -e.GetProperty("delta").GetInt32());
            ctx.AssertEqual(fpPaid, fpSpent, "FP resource events attributed to the cost");
            ctx.Assert(events.Any(e => Type(e) == "ability_cast" &&
                                       e.GetProperty("caster").GetInt32() == attackerId &&
                                       e.GetProperty("feat").GetString() == nameof(FeatType.ForceLightning1)),
                "the cast must be recorded with its feat");
        }

        private static async Task UseAbilityAsync(EngineTestContext ctx, uint caster, uint target, FeatType feat)
        {
            var used = false;
            var denial = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                used = UsePerkFeat.TryUseAbility(caster, target, feat, GetLocation(target));
                if (!used)
                    denial = Ability.GetLastActivationDenialReason();
            });
            ctx.Assert(used, $"{feat} activation was denied: {denial}");
        }

        private static int ParticipantId(EngineTestContext ctx, List<JsonElement> events, string name)
        {
            var participant = events.SingleOrDefault(e => Type(e) == "participant" && e.GetProperty("name").GetString() == name);
            ctx.Assert(participant.ValueKind == JsonValueKind.Object, $"{name} must be registered as a participant");
            ctx.Assert(!participant.GetProperty("player").GetBoolean(), $"{name} is not a player");
            return participant.GetProperty("id").GetInt32();
        }

        private static string Type(JsonElement recordEvent)
        {
            return recordEvent.GetProperty("type").GetString();
        }

        private static JsonElement ParseLine(string line)
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        }
    }
}
