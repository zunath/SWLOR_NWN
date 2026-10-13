using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

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

            File.Delete(writer.EventsPath);
            File.Delete(writer.SummaryPath);
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
