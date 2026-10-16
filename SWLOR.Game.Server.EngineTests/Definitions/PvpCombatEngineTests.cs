using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PvpCombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The PvP combat rules on live players: the combat timer, the damage and healing ramps, shorter
    /// hard crowd control, and the status icon that shows the healing step.
    /// </summary>
    public static class PvpCombatEngineTests
    {
        private const string Category = "PvpCombat";

        [EngineTest("A hit between players starts PvP combat for both; an NPC's hit does not", Category = Category, TimeoutSeconds = 40f)]
        public static async Task PlayerHitsStartPvpCombat(EngineTestContext ctx)
        {
            using var attacker = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var defender = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            var npc = ctx.SpawnCreature("nw_rat001", 8f);
            await ctx.WaitFrameAsync();
            try
            {
                await HitAsync(ctx, npc, defender.Creature);
                ctx.Assert(!PvpCombat.IsInPvpCombat(defender.Creature), "an NPC's hit is not PvP combat");

                await HitAsync(ctx, attacker.Creature, defender.Creature);
                ctx.Assert(PvpCombat.IsInPvpCombat(attacker.Creature), "the attacking player is in PvP combat");
                ctx.Assert(PvpCombat.IsInPvpCombat(defender.Creature), "the player hit is in PvP combat");
                await ctx.WaitUntilAsync(
                    () => StatusEffect.HasStatusEffect<PvpCombatStatusEffect>(attacker.Creature) &&
                          StatusEffect.HasStatusEffect<PvpCombatStatusEffect>(defender.Creature),
                    3f, "both players to show the PvP combat countdown");
                ctx.AssertEqual(0, PvpCombat.GetHealingReductionPercent(defender.Creature), "healing is untouched at the start");
            }
            finally
            {
                PvpCombat.EndForTesting(attacker.Creature);
                PvpCombat.EndForTesting(defender.Creature);
            }
        }

        [EngineTest("Damage between players rises with the target's own PvP combat time", Category = Category, TimeoutSeconds = 40f)]
        public static async Task DamageRampFollowsTheTarget(EngineTestContext ctx)
        {
            using var attacker = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var defender = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            var npc = ctx.SpawnCreature("nw_rat001", 8f);
            await ctx.WaitFrameAsync();
            try
            {
                int DamageTo(uint target, uint source) =>
                    Combat.ApplyDamageTakenModifiers(target, 100, source, CombatDamageType.Physical, isLandedAttack: false);

                var npcDamageBeforeTheFight = DamageTo(npc, defender.Creature);
                PvpCombat.SetCombatSecondsForTesting(defender.Creature, 0);
                ctx.AssertEqual(100, DamageTo(defender.Creature, attacker.Creature), "no increase at the start of PvP combat");

                PvpCombat.SetCombatSecondsForTesting(defender.Creature, 240);
                ctx.AssertEqual(160, DamageTo(defender.Creature, attacker.Creature), "+60% at 4:00");

                PvpCombat.SetCombatSecondsForTesting(defender.Creature, 300);
                ctx.AssertEqual(200, DamageTo(defender.Creature, attacker.Creature), "double damage from 5:00");
                ctx.AssertEqual(100, DamageTo(defender.Creature, npc), "an NPC's damage is not raised");
                ctx.AssertEqual(npcDamageBeforeTheFight, DamageTo(npc, defender.Creature), "the long fight doesn't raise the player's damage to NPCs");
                ctx.AssertEqual(100, DamageTo(attacker.Creature, defender.Creature),
                    "the player who just joined takes normal damage, whoever has been fighting longest");
            }
            finally
            {
                PvpCombat.EndForTesting(attacker.Creature);
                PvpCombat.EndForTesting(defender.Creature);
            }
        }

        [EngineTest("Damage passed on to a protecting player keeps them in PvP combat without being raised again", Category = Category, TimeoutSeconds = 40f)]
        public static async Task TransferredDamageKeepsTheProtectorInPvpCombat(EngineTestContext ctx)
        {
            using var attacker = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var protector = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            await ctx.WaitFrameAsync();
            try
            {
                PvpCombat.SetCombatSecondsForTesting(protector.Creature, 300);
                PvpCombat.EndForTesting(protector.Creature);
                ctx.Assert(!PvpCombat.IsInPvpCombat(protector.Creature), "the protector starts outside PvP combat");

                var transferred = Combat.ApplyDamageTakenModifiers(protector.Creature, 50, attacker.Creature,
                    CombatDamageType.Physical, CombatDamageDeliveryType.Transferred);
                ctx.Assert(PvpCombat.IsInPvpCombat(protector.Creature), "the protector enters PvP combat");
                ctx.Assert(PvpCombat.IsInPvpCombat(attacker.Creature), "the attacker stays in PvP combat");

                PvpCombat.SetCombatSecondsForTesting(protector.Creature, 300);
                ctx.AssertEqual(transferred, Combat.ApplyDamageTakenModifiers(protector.Creature, 50, attacker.Creature,
                        CombatDamageType.Physical, CombatDamageDeliveryType.Transferred, isLandedAttack: false),
                    "damage passed on is not doubled again, however long the protector has fought");
            }
            finally
            {
                PvpCombat.EndForTesting(attacker.Creature);
                PvpCombat.EndForTesting(protector.Creature);
            }
        }

        [EngineTest("Healing a player in a long PvP fight is cut, and the recording logs the cut apart from overheal", Category = Category, TimeoutSeconds = 40f)]
        public static async Task HealingRampCutsEveryHeal(EngineTestContext ctx)
        {
            ctx.Assert(!CombatRecorder.IsRecording, "no combat recording may be running before the test");
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            var healer = ctx.SpawnCreature("nw_rat001", 6f);
            await ctx.WaitFrameAsync();

            CombatRecordWriter writer;
            try
            {
                var maximum = await GiveNativeLevelsAsync(ctx, player.Creature);
                ctx.Assert(maximum >= 400, $"the player has room to heal (max HP {maximum})");
                ObjectPlugin.SetCurrentHitPoints(player.Creature, maximum - 300);

                await HealAsync(ctx, healer, player.Creature, 100);
                ctx.AssertEqual(maximum - 200, GetCurrentHitPoints(player.Creature), "a heal outside PvP combat lands in full");

                ctx.Assert(CombatRecorder.Start(GetArea(player.Creature), "engine test pvp healing", out var message), message);
                writer = CombatRecorder.LastWriter;
                PvpCombat.SetCombatSecondsForTesting(player.Creature, 240);
                ctx.AssertEqual(60, PvpCombat.GetHealingReductionPercent(player.Creature), "-60% from 4:00");
                await HealAsync(ctx, healer, player.Creature, 100);
                ctx.AssertEqual(maximum - 160, GetCurrentHitPoints(player.Creature), "the same heal restores 40 at 4:00");
                await ctx.DelaySecondsAsync(0.5f);
            }
            finally
            {
                if (CombatRecorder.IsRecording)
                    CombatRecorder.Stop("engine test", out _);
                PvpCombat.EndForTesting(player.Creature);
            }

            await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the recording writer to finish its files");
            ctx.Assert(writer.Error == null, $"the recording writer failed: {writer.Error}");
            var events = File.ReadAllLines(writer.EventsPath).Select(ParseLine).ToList();
            var heal = events.Single(e => e.GetProperty("type").GetString() == "heal" && e.GetProperty("pvp_reduced").GetInt32() > 0);
            ctx.AssertEqual(60, heal.GetProperty("pvp_reduced").GetInt32(), "the cut is recorded");
            ctx.AssertEqual(40, heal.GetProperty("requested").GetInt32(), "the heal asked for what was left after the cut");
            ctx.AssertEqual(0, heal.GetProperty("overheal").GetInt32(), "the cut is not overheal");
            File.Delete(writer.EventsPath);
            File.Delete(writer.SummaryPath);
        }

        [EngineTest("Hard crowd control a player applies lasts half as long on a player, not on an NPC", Category = Category, TimeoutSeconds = 40f)]
        public static async Task HardCrowdControlIsShorterAgainstPlayers(EngineTestContext ctx)
        {
            using var attacker = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var defender = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            var npc = ctx.SpawnCreature("nw_rat001", 8f);
            await ctx.WaitFrameAsync();
            SetAILevel(npc, AILevel.VeryLow);
            try
            {
                ctx.AssertEqual(0, Resistance.GetResistance(defender.Creature, ResistanceType.Mobility), "the player has no Mobility resistance");
                ctx.AssertEqual(0, Resistance.GetResistance(npc, ResistanceType.Mobility), "the NPC has no Mobility resistance");

                ctx.Assert(StatusEffect.ApplyStatusEffect(attacker.Creature, defender.Creature, typeof(StunnedStatusEffect), 10f),
                    "the stun lands on the player");
                ctx.Assert(StatusEffect.ApplyStatusEffect(attacker.Creature, npc, typeof(StunnedStatusEffect), 10f),
                    "the stun lands on the NPC");

                var onPlayer = StatusEffect.GetStatusEffect<StunnedStatusEffect>(defender.Creature).GetRemainingDurationSeconds(System.DateTime.UtcNow);
                var onNpc = StatusEffect.GetStatusEffect<StunnedStatusEffect>(npc).GetRemainingDurationSeconds(System.DateTime.UtcNow);
                ctx.Assert(onPlayer > 4f && onPlayer <= 5.1f, $"the player is stunned for 5 seconds, not 10 ({onPlayer:0.00}s left)");
                ctx.Assert(onNpc > 9f && onNpc <= 10.1f, $"the NPC is stunned for the full 10 seconds ({onNpc:0.00}s left)");
            }
            finally
            {
                StatusEffect.RemoveAllStatusEffects(defender.Creature);
                StatusEffect.RemoveAllStatusEffects(npc);
            }
        }

        [EngineTest("The PvP icon shows the healing step, and PvP combat ends after 30 seconds without PvP damage", Category = Category, TimeoutSeconds = 40f)]
        public static async Task IconFollowsTheStepAndCombatLapses(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            await ctx.WaitFrameAsync();
            try
            {
                PvpCombat.SetCombatSecondsForTesting(player.Creature, 0);
                ctx.Assert(StatusEffect.HasStatusEffect<PvpCombatStatusEffect>(player.Creature), "the countdown shows before 2:00");

                PvpCombat.SetCombatSecondsForTesting(player.Creature, 155);
                ctx.Assert(StatusEffect.HasStatusEffect<PvpHealingReduction2StatusEffect>(player.Creature), "step 2 shows at 2:35");
                ctx.Assert(!StatusEffect.HasStatusEffect<PvpCombatStatusEffect>(player.Creature), "the countdown is replaced");
                var remaining = StatusEffect.GetStatusEffect<PvpHealingReduction2StatusEffect>(player.Creature)
                    .GetRemainingDurationSeconds(System.DateTime.UtcNow);
                ctx.Assert(remaining > 20f && remaining <= 27f, $"the icon counts down to step 3 at 3:00 ({remaining:0.0}s left)");

                PvpCombat.SetCombatSecondsForTesting(player.Creature, 400);
                ctx.Assert(StatusEffect.HasStatusEffect<PvpHealingReduction5StatusEffect>(player.Creature), "the last step shows from 4:00");

                PvpCombat.SetCombatSecondsForTesting(player.Creature, 400, PvpCombatRules.CombatWindowSeconds + 1);
                ctx.Assert(!PvpCombat.IsInPvpCombat(player.Creature), "PvP combat ends after 30 seconds without PvP damage");
                ctx.Assert(!StatusEffect.HasStatusEffect<PvpHealingReduction5StatusEffect>(player.Creature), "the icon is removed");
                ctx.AssertEqual(0, PvpCombat.GetHealingReductionPercent(player.Creature), "healing is back to normal");
            }
            finally
            {
                PvpCombat.EndForTesting(player.Creature);
            }
        }

        [EngineTest("Failing to disarm another player's kit trap is PvP damage", Category = Category, TimeoutSeconds = 40f)]
        public static async Task FailedDisarmOfAPlayersTrapIsPvpDamage(EngineTestContext ctx)
        {
            using var owner = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            using var victim = await PlayerAbilityFixture.CreateAsync(ctx, -4f);
            await ctx.WaitFrameAsync();
            try
            {
                var maximum = await GiveNativeLevelsAsync(ctx, victim.Creature);
                ctx.Assert(maximum > 60, $"the victim survives the doubled hit (max HP {maximum})");
                PvpCombat.SetCombatSecondsForTesting(victim.Creature, 300);
                ctx.Assert(!PvpCombat.IsInPvpCombat(owner.Creature), "the trap's owner is not in PvP combat yet");

                // A tier 1 kit trap whose disarm fails, as Traps does when the roll misses.
                var recordType = typeof(Traps).GetNestedType("TrapRecord", BindingFlags.NonPublic);
                var record = Activator.CreateInstance(recordType, nonPublic: true);
                recordType.GetProperty("Owner").SetValue(record, owner.Creature);
                recordType.GetProperty("Tier").SetValue(record, 1);
                typeof(Traps).GetMethod("TriggerOnDisarmFailure", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new[] { record, victim.Creature });

                await ctx.WaitUntilAsync(() => GetCurrentHitPoints(victim.Creature) < maximum, 5f, "the trap's hit to land");
                ctx.AssertEqual(maximum - 40, GetCurrentHitPoints(victim.Creature), "the tier 1 trap's 20 damage is doubled at 5:00");
                ctx.Assert(PvpCombat.IsInPvpCombat(owner.Creature), "the trap's owner enters PvP combat");
            }
            finally
            {
                PvpCombat.EndForTesting(owner.Creature);
                PvpCombat.EndForTesting(victim.Creature);
            }
        }

        [EngineTest("Healing-received effects react to the heal left after the PvP cut", Category = Category, TimeoutSeconds = 40f)]
        public static async Task HealingReceivedEffectsFollowTheCutHeal(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, 4f);
            await ctx.WaitFrameAsync();
            try
            {
                TemporaryStatModifier.Add(player.Creature, StatType.HealingReceivedAttackPercentAdjustment, 10, 30f);
                TemporaryStatModifier.Add(player.Creature, StatType.HealingReceivedAttackDurationSeconds, 30, 30f);
                var maximum = GetMaxHitPoints(player.Creature);
                ctx.Assert(maximum >= 3, $"the fixture has room for a 2-HP heal (max HP {maximum})");
                ObjectPlugin.SetCurrentHitPoints(player.Creature, maximum - 2);
                var attackBefore = Stat.GetStatAdjustment(player.Creature, StatType.AttackPercentAdjustment);
                PvpCombat.SetCombatSecondsForTesting(player.Creature, 240);

                Stat.ApplyHealingReceivedEffects(player.Creature, 2);
                ctx.AssertEqual(attackBefore, Stat.GetStatAdjustment(player.Creature, StatType.AttackPercentAdjustment),
                    "the 60% cut leaves nothing of a 2-HP heal, so there is no Attack boost");

                Stat.ApplyHealingReceivedEffects(player.Creature, 3);
                ctx.AssertEqual(attackBefore + 10, Stat.GetStatAdjustment(player.Creature, StatType.AttackPercentAdjustment),
                    "a 3-HP heal still restores 1 HP after the cut");
            }
            finally
            {
                PvpCombat.EndForTesting(player.Creature);
            }
        }

        // The headless fixture starts with a few HP; normal native levels give heals and hits room to show.
        private static async Task<int> GiveNativeLevelsAsync(EngineTestContext ctx, uint player)
        {
            SetPlotFlag(player, false);
            CreaturePlugin.SetClassByPosition(player, 0, ClassType.Standard);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                typeof(PlayerInitialization).GetMethod("EnsureNativeLevels", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { player }));
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 10);
            await ctx.WaitFrameAsync();
            return GetMaxHitPoints(player);
        }

        private static async Task HitAsync(EngineTestContext ctx, uint attacker, uint target)
        {
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                Combat.ApplyTriggeredDamage(attacker, target, 5, CombatDamageType.Physical));
        }

        private static async Task HealAsync(EngineTestContext ctx, uint healer, uint target, int amount)
        {
            await ctx.ExecuteInCreatureContextAsync(healer, () =>
                ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target));
            await ctx.WaitFrameAsync();
        }

        private static JsonElement ParseLine(string line)
        {
            using var document = JsonDocument.Parse(line);
            return document.RootElement.Clone();
        }
    }
}
