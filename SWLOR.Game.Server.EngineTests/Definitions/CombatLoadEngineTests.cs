using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Reproducible combat load for frame-time measurement. Two teams fight in a quiet arena while a
    /// driver keeps every combatant casting heals, party buffs, auras, zones and area attacks, and the
    /// frame profiler records the frame-time distribution and per-frame work. Opt-in: runs only when
    /// SWLOR_ENGINE_TEST_FILTER names the CombatLoad category.
    /// </summary>
    public static class CombatLoadEngineTests
    {
        private const string LoadCategory = "CombatLoad";
        private const string Resref = "nw_bandit001";
        private const int PartySize = 5;
        private const int HitPoints = 30000;
        private const int Resources = 100000;
        private const float BaselineSeconds = 10f;
        private const float LoadSeconds = 60f;
        private const float DriverIntervalSeconds = 0.25f;
        private const int CastersPerTick = 5;
        private const int Seed = 1012;
        // Production Player records (2026-10-12 backup): median 6 KB, p90 16 KB, p99 51 KB.
        private const int PlayerRecordBytes = 16 * 1024;

        private static readonly (FeatType Feat, CombatLoadTarget Target)[] Rotation =
        {
            (FeatType.ForceBurst1, CombatLoadTarget.Enemy),
            (FeatType.Benevolence3, CombatLoadTarget.Ally),
            (FeatType.CreepingTerror3, CombatLoadTarget.Enemy),
            (FeatType.ForceSanctuary1, CombatLoadTarget.Self),
            (FeatType.Flamethrower3, CombatLoadTarget.Enemy),
            (FeatType.Renewal3, CombatLoadTarget.Ally),
            (FeatType.SonicBurst3, CombatLoadTarget.Enemy),
            (FeatType.PressTheAttack3, CombatLoadTarget.Self),
            (FeatType.RadiantLance3, CombatLoadTarget.Enemy),
            (FeatType.MedKit4, CombatLoadTarget.Ally),
            (FeatType.FragGrenade3, CombatLoadTarget.Enemy),
            (FeatType.KoltoMist2, CombatLoadTarget.Self),
            (FeatType.ForceDrain3, CombatLoadTarget.Enemy),
            (FeatType.RousingShout3, CombatLoadTarget.Self),
            (FeatType.PurifyingWave1, CombatLoadTarget.Enemy),
            (FeatType.GuardianWard4, CombatLoadTarget.Self),
            (FeatType.IncendiaryField3, CombatLoadTarget.Enemy),
            (FeatType.ChargeOrder2, CombatLoadTarget.Self),
            (FeatType.NightmareField1, CombatLoadTarget.Enemy),
            (FeatType.Shielding3, CombatLoadTarget.Self),
            (FeatType.FieldRecovery2, CombatLoadTarget.Self),
        };

        // Stat-gated perks read the NPC's PERK_LEVEL local with no max-level fallback.
        private static readonly Dictionary<PerkType, int> PinnedPerks = new()
        {
            [PerkType.SurroundedNotOutmatched] = 1,
            [PerkType.CenterOfTheStorm] = 1,
            [PerkType.ChargeOrder] = 2,
            [PerkType.FieldRecovery] = 2,
        };

        [EngineTest("Combat load: two teams of 15 NPCs cast heals, buffs, auras, zones and area attacks for 60 s", Category = LoadCategory, TimeoutSeconds = 300f)]
        public static Task NPCSkirmish(EngineTestContext ctx) => RunAsync(ctx, 15, 0, 15, 0);

        // Twenty player observers, like the 19-player test. Headless players cannot be made hostile to
        // each other, so they fight NPCs; every combat message still resolves known names per observer.
        [EngineTest("Combat load: twenty headless players fight 15 NPCs while casting for 60 s", Category = LoadCategory, TimeoutSeconds = 420f)]
        public static Task PlayerSkirmish(EngineTestContext ctx) => RunAsync(ctx, 0, 20, 15, 0);

        private static async Task RunAsync(EngineTestContext ctx, int npcsA, int playersA, int npcsB, int playersB)
        {
            if (ApplicationSettings.Get().EngineTestFilter?.Contains(LoadCategory, StringComparison.OrdinalIgnoreCase) != true)
                ctx.Skip($"Load measurement runs only when SWLOR_ENGINE_TEST_FILTER names {LoadCategory}.");

            var arena = await QuietArena.CreateAsync(ctx);
            var anchor = arena.Spawn(Resref, 8f, 90f);
            var isolated = new EngineTestContext(ctx.TestName, arena.Area, GetLocation(anchor));
            DestroyObject(anchor);
            var maxHenchmen = GetMaxHenchmen();
            var fixtures = new List<PlayerAbilityFixture>();
            try
            {
                await RunInArenaAsync(ctx, isolated, npcsA, playersA, npcsB, playersB, fixtures);
            }
            finally
            {
                if (FrameProfiler.IsCapturing)
                    FrameProfiler.EndCapture();
                foreach (var fixture in fixtures)
                    fixture.Dispose();
                SetMaxHenchmen(maxHenchmen);
                isolated.Cleanup();
            }
        }

        private static async Task RunInArenaAsync(EngineTestContext report, EngineTestContext ctx,
            int npcsA, int playersA, int npcsB, int playersB, List<PlayerAbilityFixture> fixtures)
        {
            FrameProfiler.BeginCapture();
            await ctx.DelaySecondsAsync(BaselineSeconds);
            var baseline = FrameProfiler.EndCapture();

            var teamA = new List<uint>();
            var teamB = new List<uint>();
            for (var index = 0; index < npcsA; index++)
                teamA.Add(ctx.SpawnCreature(Resref, -2.5f - index % 2, index - 7f));
            for (var index = 0; index < npcsB; index++)
            {
                var enemy = ctx.SpawnCreature(Resref, 2.5f + index % 2, index - 7f);
                ctx.MakeHostile(enemy);
                teamB.Add(enemy);
            }

            var playersOfA = await CreatePlayersAsync(ctx, playersA, -1f, fixtures);
            var playersOfB = await CreatePlayersAsync(ctx, playersB, 1f, fixtures);
            foreach (var player in playersOfA)
            {
                foreach (var enemy in playersOfB)
                {
                    SetPCDislike(player, enemy);
                    SetPCDislike(enemy, player);
                }
            }

            // Spawn initialization resets NPC pools and stats after the first frame.
            await ctx.DelaySecondsAsync(1f);
            var npcs = teamA.Concat(teamB).ToList();
            foreach (var npc in npcs)
            {
                SetAILevel(npc, AILevel.High);
                Stat.SetNPCMaxHitPoints(npc, HitPoints, true);
                SetImmortal(npc, true);
                ctx.SetNPCResources(npc, Resources, Resources);
                foreach (var (perk, level) in PinnedPerks)
                    ctx.SetNPCPerkLevel(npc, perk, level);
            }

            SetMaxHenchmen(PartySize);
            FormParties(teamA);
            FormParties(teamB);
            await ctx.WaitFrameAsync();

            var players = fixtures.Select(fixture => fixture.Creature).ToList();
            teamA.AddRange(playersOfA);
            teamB.AddRange(playersOfB);
            for (var index = 0; index < Math.Max(teamA.Count, teamB.Count); index++)
            {
                Enmity.ModifyEnmity(teamA[index % teamA.Count], teamB[index % teamB.Count], 100);
                Enmity.ModifyEnmity(teamB[index % teamB.Count], teamA[index % teamA.Count], 100);
            }
            KeepPlayersAttacking(players);

            var casters = teamA.Concat(teamB).ToList();
            var rotationIndex = casters.Select((_, index) => index * 7 % Rotation.Length).ToArray();
            var successes = new Dictionary<FeatType, int>();
            var denials = new Dictionary<string, int>();
            var attempts = 0;
            var peakEmbattled = 0;

            ctx.SeedRandom(Seed);
            FrameProfiler.BeginCapture();
            var end = DateTime.UtcNow.AddSeconds(LoadSeconds);
            for (var tick = 0; DateTime.UtcNow < end; tick++)
            {
                for (var slot = 0; slot < CastersPerTick; slot++)
                {
                    var index = (tick * CastersPerTick + slot) % casters.Count;
                    var entry = Rotation[rotationIndex[index]++ % Rotation.Length];
                    if (TryCast(casters[index], entry.Feat, entry.Target, successes, denials))
                        attempts++;
                }

                if (tick % 12 == 0)
                {
                    KeepPlayersAttacking(players);
                    peakEmbattled = Math.Max(peakEmbattled, casters.Count(creature => StatusEffect.HasStatusEffect<EmbattledStatusEffect>(creature)));
                }

                await ctx.DelaySecondsAsync(DriverIntervalSeconds);
            }

            var load = FrameProfiler.EndCapture();
            var sample = casters[0];
            var fighting = casters.Count(creature => GetIsObjectValid(GetAttackTarget(creature)));
            var casts = successes.Values.Sum();
            var detail =
                $"NPCs {npcsA}+{npcsB}, players {playersA}+{playersB}; casts {casts}/{attempts}; peak Embattled {peakEmbattled}/{casters.Count}; attacking at end {fighting}; " +
                $"first caster Embattled stat {Stat.GetStatAdjustment(sample, StatType.EmbattledStackDefensePercent)}; " +
                $"top denials: {string.Join(", ", denials.OrderByDescending(pair => pair.Value).Take(8).Select(pair => $"{pair.Key} x{pair.Value}"))}; " +
                $"casts by ability: {string.Join(", ", successes.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key} {pair.Value}"))} | " +
                $"BASELINE {baseline} | LOAD {load}";
            Console.WriteLine($"[COMBAT_LOAD] {report.TestName}: {detail}");
            report.Log(detail);
            report.SetResultDetail(detail);

            ctx.Assert(load.Frames > 0, "The load capture recorded frames.");
            ctx.Assert(casts >= 100, $"The driver kept combatants casting (only {casts} of {attempts} activations succeeded).");
            ctx.Assert(peakEmbattled > 0, "Embattled refreshed during the fight.");
        }

        private static bool TryCast(uint caster, FeatType feat, CombatLoadTarget kind,
            Dictionary<FeatType, int> successes, Dictionary<string, int> denials)
        {
            if (!GetIsObjectValid(caster) || GetIsDead(caster))
                return false;

            var target = kind switch
            {
                CombatLoadTarget.Self => caster,
                CombatLoadTarget.Ally => GetNearestCreature(CreatureType.Reputation, (int)ReputationType.Friend, caster),
                _ => GetNearestCreature(CreatureType.Reputation, (int)ReputationType.Enemy, caster),
            };
            if (!GetIsObjectValid(target))
                target = caster;

            AssignCommand(caster, () =>
            {
                if (UsePerkFeat.TryUseAbility(caster, target, feat, GetLocation(target)))
                {
                    successes[feat] = successes.GetValueOrDefault(feat) + 1;
                    return;
                }

                var reason = Ability.GetLastActivationDenialReason() ?? "unknown";
                denials[$"{feat}: {reason}"] = denials.GetValueOrDefault($"{feat}: {reason}") + 1;
            });
            return true;
        }

        private static async Task<List<uint>> CreatePlayersAsync(EngineTestContext ctx, int count, float side, List<PlayerAbilityFixture> fixtures)
        {
            var players = new List<uint>();
            for (var index = 0; index < count; index++)
            {
                var fixture = await PlayerAbilityFixture.CreateAsync(ctx, side * (2.5f + index % 4));
                fixtures.Add(fixture);
                PreparePlayer(fixture);
                players.Add(fixture.Creature);
            }

            return players;
        }

        private static void FormParties(List<uint> team)
        {
            for (var leaderIndex = 0; leaderIndex < team.Count; leaderIndex += PartySize)
            {
                var leader = team[leaderIndex];
                var members = team.Skip(leaderIndex + 1).Take(PartySize - 1).ToArray();
                AssignCommand(leader, () =>
                {
                    foreach (var member in members)
                        AddHenchman(leader, member);
                });
            }
        }

        private static void PreparePlayer(PlayerAbilityFixture fixture)
        {
            var player = fixture.Creature;
            SetImmortal(player, true);
            SetPlotFlag(player, false);
            // The fixture is loaded from an NPC template. Players do not run creature event scripts.
            for (var script = EventScript.Creature_OnHeartbeat; script <= EventScript.Creature_OnBlockedByDoor; script++)
                SetEventScript(player, script, string.Empty);
            // Players regenerate normally; natural regeneration writes the player record.
            DeleteLocalInt(player, Stat.SuppressNaturalRegenVariable);
            fixture.Update(record =>
            {
                foreach (var (feat, _) in Rotation)
                {
                    var ability = Ability.GetAbilityDetail(feat);
                    record.Perks[ability.EffectiveLevelPerkType] =
                        Math.Max(record.Perks.GetValueOrDefault(ability.EffectiveLevelPerkType), ability.AbilityLevel);
                }

                foreach (var (perk, level) in PinnedPerks)
                    record.Perks[perk] = Math.Max(record.Perks.GetValueOrDefault(perk), level);
                foreach (var skill in new[] { SkillType.Devices, SkillType.Leadership, SkillType.Lightsaber })
                    record.Skills[skill].Rank = 50;

                for (var index = 0; JsonConvert.SerializeObject(record).Length < PlayerRecordBytes; index++)
                    record.MapProgressions[$"combat_load_{index:D3}"] = new string('0', 200);
            });
        }

        private static void KeepPlayersAttacking(IEnumerable<uint> players)
        {
            foreach (var player in players)
            {
                if (GetIsObjectValid(GetAttackTarget(player)))
                    continue;

                var enemy = GetNearestCreature(CreatureType.Reputation, (int)ReputationType.Enemy, player);
                if (GetIsObjectValid(enemy))
                    AssignCommand(player, () => ActionAttack(enemy));
            }
        }
    }
}
