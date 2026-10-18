using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Against a target whose Evasion puts every attack at the floor, a boss keeps
    /// Combat.BossMinimumHitRate and any other creature Combat.MinimumHitRate, for abilities and for
    /// weapon swings resolved by the native attack roll.
    /// </summary>
    public static class BossMinimumHitChanceEngineTests
    {
        private const string CreatureResref = "nw_bandit001";

        [EngineTest("Bosses keep a 35% hit chance against any Evasion; other creatures keep 20%", Category = "BossMinimumHitChance", TimeoutSeconds = 90f)]
        public static async Task BossesKeepTheirMinimumHitChance(EngineTestContext ctx)
        {
            ctx.Assert(!CombatRecorder.IsRecording, "no combat recording may be running before the test");

            var arena = await QuietArena.CreateAsync(ctx);
            var target = arena.Spawn(CreatureResref, 0f, 0f);
            var boss = arena.Spawn(CreatureResref, 1.5f, 180f);
            var regular = arena.Spawn(CreatureResref, -1.5f, 0f);
            await ctx.DelaySecondsAsync(1f);

            Prepare(ctx, target, "Evasive Target");
            Prepare(ctx, boss, "Test Boss");
            Prepare(ctx, regular, "Test Regular");
            SetLocalBool(boss, BossCreature.Variable, true);
            TemporaryStatModifier.Add(target, StatType.Evasion, 1000, 120f);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 120f);
            ctx.MakeHostile(boss);
            ctx.MakeHostile(regular);

            var bossAbilityRate = 0;
            var regularAbilityRate = 0;
            await ctx.ExecuteInCreatureContextAsync(boss, () =>
                Combat.TryResolveAbilityHit(boss, target, SkillType.Vibroblade, PerkType.Invalid, out bossAbilityRate));
            await ctx.ExecuteInCreatureContextAsync(regular, () =>
                Combat.TryResolveAbilityHit(regular, target, SkillType.Vibroblade, PerkType.Invalid, out regularAbilityRate));
            ctx.AssertEqual(Combat.BossMinimumHitRate, bossAbilityRate, "a boss's ability hit chance against overwhelming Evasion");
            ctx.AssertEqual(Combat.MinimumHitRate, regularAbilityRate, "another creature's ability hit chance against overwhelming Evasion");

            // Weapon swings: every swing misses so the target stays put, and the recorder logs each roll's hit chance.
            Combat.SetAutoAttackHitResolutionOverride(false);
            CombatRecordWriter writer;
            try
            {
                ctx.Assert(CombatRecorder.Start(arena.Area, "engine test boss hit chance", out var startMessage), startMessage);
                writer = CombatRecorder.LastWriter;
                AssignCommand(boss, () => ActionAttack(target));
                AssignCommand(regular, () => ActionAttack(target));
                await ctx.DelaySecondsAsync(10f);
            }
            finally
            {
                Combat.SetAutoAttackHitResolutionOverride(null);
                AssignCommand(boss, () => ClearAllActions());
                AssignCommand(regular, () => ClearAllActions());
                if (CombatRecorder.IsRecording)
                    CombatRecorder.Stop("engine test", out _);
            }

            await ctx.WaitUntilAsync(() => writer.Completion.IsCompleted, 10f, "the recording writer to finish its files");
            ctx.Assert(writer.Error == null, $"the recording writer failed: {writer.Error}");
            var events = File.ReadAllLines(writer.EventsPath).Select(line => JsonDocument.Parse(line).RootElement).ToList();
            File.Delete(writer.EventsPath);

            int ParticipantId(string name) => events.Single(e =>
                e.GetProperty("type").GetString() == "participant" && e.GetProperty("name").GetString() == name)
                .GetProperty("id").GetInt32();

            var targetId = ParticipantId("Evasive Target");
            int[] SwingHitChances(string attackerName)
            {
                var attackerId = ParticipantId(attackerName);
                return events
                    .Where(e => e.GetProperty("type").GetString() == "attack" &&
                                e.GetProperty("mode").GetString() == "autoattack" &&
                                e.GetProperty("attacker").GetInt32() == attackerId &&
                                e.GetProperty("defender").GetInt32() == targetId)
                    .Select(e => e.GetProperty("hit_chance").GetInt32())
                    .ToArray();
            }

            var bossSwings = SwingHitChances("Test Boss");
            var regularSwings = SwingHitChances("Test Regular");
            ctx.Log($"Boss swing hit chances: {string.Join(", ", bossSwings)}; other creature: {string.Join(", ", regularSwings)}");
            ctx.Assert(bossSwings.Length > 0 && regularSwings.Length > 0, "both attackers swung at the target");
            ctx.Assert(bossSwings.All(chance => chance == Combat.BossMinimumHitRate),
                $"every boss swing rolls at {Combat.BossMinimumHitRate}%: {string.Join(", ", bossSwings)}");
            ctx.Assert(regularSwings.All(chance => chance == Combat.MinimumHitRate),
                $"every other swing rolls at {Combat.MinimumHitRate}%: {string.Join(", ", regularSwings)}");
        }

        private static void Prepare(EngineTestContext ctx, uint creature, string name)
        {
            SetName(creature, name);
            ctx.SuppressNPCNaturalRegen(creature);
            SetPlotFlag(creature, false);
            ctx.SetNPCResources(creature, 100, 100);
            Stat.SetNPCMaxHitPoints(creature, 1000, true);
            ObjectPlugin.SetCurrentHitPoints(creature, 1000);
        }
    }
}
