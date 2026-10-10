using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class MimicryObservationEngineTests
    {
        [EngineTest("Observation preserves Force XP with higher and capped Mimicry", Category = "MimicryObservation", TimeoutSeconds = 60f)]
        public static async Task ObservationPreservesForceXP(EngineTestContext ctx)
        {
            using var baseline = await PlayerAbilityFixture.CreateAsync(ctx);
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx, 3f);

            foreach (var mimicryRank in new[] { 4, 50 })
            {
                Configure(baseline, mimicryRank, false);
                Configure(observer, mimicryRank, true);
                var npc = ctx.SpawnCreature("nw_rat001", 2f);
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(0, Stat.GetNPCStats(npc).Level, "stock rat supplies a level-zero XP comparison");
                CombatPoint.AddCombatPoint(baseline.Creature, npc, SkillType.Force, 3);
                CombatPoint.AddCombatPoint(observer.Creature, npc, SkillType.Force, 3);

                Observe(npc);
                ctx.AssertEqual(StartingExperience(mimicryRank), Experience(observer, SkillType.Mimicry),
                    "observation pays nothing before death");
                await Kill(ctx, npc, observer.Creature);

                var forceXP = Experience(baseline, SkillType.Force);
                ctx.Assert(forceXP > 0, "the baseline fight awards Force XP");
                ctx.AssertEqual(forceXP, Experience(observer, SkillType.Force),
                    $"automatic observation with Mimicry {mimicryRank} does not lower Force XP");
                ctx.AssertEqual(mimicryRank == 50 ? 0 : AfterPenalty(Mimicry.CalculateAnalysisXP(0, mimicryRank)),
                    Experience(observer, SkillType.Mimicry) - StartingExperience(mimicryRank),
                    "observation grants only its independent rank-relative reward");
                ctx.Assert(!Observers.ContainsKey(npc), "the kill consumes the observer's credit");
            }
        }

        [EngineTest("Observation pays once per kill within its cap despite repeated casts and XP bonuses", Category = "MimicryObservation", TimeoutSeconds = 60f)]
        public static async Task ObservationRewardIsBounded(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            Configure(fixture, 0, true);
            fixture.Update(record => record.DMXPBonus = 1000);
            var npc = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            CombatPoint.AddCombatPoint(fixture.Creature, npc, SkillType.Force, 3);

            for (var cast = 0; cast < 20; cast++)
                Observe(npc);

            ctx.AssertEqual(1, Observers[npc].Count, "all observed casts and techniques produce one credit");
            ctx.AssertEqual(0, Experience(fixture, SkillType.Mimicry), "surviving enemies pay no analysis XP");
            await Kill(ctx, npc, fixture.Creature);
            var paidXP = Experience(fixture, SkillType.Mimicry);
            ctx.AssertEqual(AfterPenalty(30), paidXP, "character XP bonuses cannot multiply the observation cap");

            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
            {
                EventsPlugin.PushEventData("NPC", ObjectToString(npc));
                EventsPlugin.SignalEvent("SWLOR_COMBAT_POINT_DISTRIBUTED", fixture.Creature);
            });
            ctx.AssertEqual(paidXP, Experience(fixture, SkillType.Mimicry), "repeated payout events cannot reuse observation credit");
        }

        [EngineTest("Active Mimicry continues sharing utility XP when observation also occurs", Category = "MimicryObservation", TimeoutSeconds = 60f)]
        public static async Task ActiveMimicryStillSharesXP(EngineTestContext ctx)
        {
            using var baseline = await PlayerAbilityFixture.CreateAsync(ctx);
            using var observer = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            Configure(baseline, 4, false);
            Configure(observer, 4, true);
            var npc = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            foreach (var player in new[] { baseline.Creature, observer.Creature })
            {
                // Use the same credit path as active techniques; observation must not replace it.
                CombatPoint.AddCombatPoint(player, npc, SkillType.Force, 3);
                CombatPoint.AddCombatPoint(player, npc, SkillType.Mimicry, 3);
            }
            Observe(npc);
            await Kill(ctx, npc, observer.Creature);

            var forceXP = Experience(baseline, SkillType.Force);
            var activeMimicryXP = Experience(baseline, SkillType.Mimicry) - StartingExperience(4);
            ctx.Assert(forceXP > 0 && forceXP < Skill.GetDeltaXP(0) / 2,
                "the higher actively used Mimicry rank still reduces and shares the utility pool");
            ctx.AssertEqual(forceXP, activeMimicryXP, "equal active credits still receive equal shares");
            ctx.AssertEqual(forceXP, Experience(observer, SkillType.Force), "observation does not alter active sharing");
            ctx.AssertEqual(activeMimicryXP + AfterPenalty(Mimicry.CalculateAnalysisXP(0, 4)),
                Experience(observer, SkillType.Mimicry) - StartingExperience(4),
                "observation adds only its small separate reward to active Mimicry XP");
        }

        [EngineTest("Observation requires prior participation and cleans unpaid credit without a reward", Category = "MimicryObservation", TimeoutSeconds = 60f)]
        public static async Task ObservationRequiresParticipation(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            Configure(fixture, 0, true);
            var npc = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            Observe(npc);
            ctx.Assert(!Observers.ContainsKey(npc), "a nearby bystander receives no observation credit");
            CombatPoint.AddCombatPoint(fixture.Creature, npc, SkillType.Force, 3);
            await Kill(ctx, npc, fixture.Creature);
            ctx.AssertEqual(0, Experience(fixture, SkillType.Mimicry), "joining after observation cannot claim its reward");

            Configure(fixture, 0, true);
            npc = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            CombatPoint.AddCombatPoint(fixture.Creature, npc, SkillType.Force, 3);
            Observe(npc);
            ctx.Assert(Observers.ContainsKey(npc), "an engaged observer receives credit");
            var originalPosition = GetPosition(fixture.Creature);
            try
            {
                ObjectPlugin.SetPosition(fixture.Creature, originalPosition + new System.Numerics.Vector3(100f, 0f, 0f));
                await Kill(ctx, npc, fixture.Creature);
                ctx.AssertEqual(0, Experience(fixture, SkillType.Mimicry), "an observer beyond the combat payout distance earns nothing");
                Sweep();
                ctx.Assert(!Observers.ContainsKey(npc), "dead creatures lose unpaid observation credit");
            }
            finally
            {
                ObjectPlugin.SetPosition(fixture.Creature, originalPosition);
            }

            npc = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            CombatPoint.AddCombatPoint(fixture.Creature, npc, SkillType.Force, 3);
            Observe(npc);
            DestroyObject(npc);
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(npc), 5f, "creature despawn");
            Sweep();
            ctx.Assert(!Observers.ContainsKey(npc), "despawn clears observation credit");
            ctx.AssertEqual(0, Experience(fixture, SkillType.Mimicry), "despawn pays no observation XP");
            typeof(CombatPoint).GetMethod("ClearPlayerCombatPoints", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { fixture.Creature });
        }

        private static Dictionary<uint, HashSet<string>> Observers =>
            (Dictionary<uint, HashSet<string>>)typeof(Mimicry)
                .GetField("_analysisObservers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

        private static void Configure(PlayerAbilityFixture fixture, int mimicryRank, bool analyzer)
        {
            fixture.Update(record =>
            {
                foreach (var skill in record.Skills.Values)
                {
                    skill.Rank = 0;
                    skill.XP = 0;
                }
                record.Skills[SkillType.Mimicry].Rank = mimicryRank;
                record.XPDebt = 0;
                record.DMXPBonus = 0;
                if (analyzer)
                    record.Perks[PerkType.CombatAnalyzer] = 1;
                else
                    record.Perks.Remove(PerkType.CombatAnalyzer);
                // Separate analysis XP from the existing reward for learning a new technique.
                record.LearnedTechniques[FeatType.SonicShriekTechnique] = DateTime.UtcNow;
                record.LearnedTechniques[FeatType.StaticWebTechnique] = DateTime.UtcNow;
            });
        }

        private static void Observe(uint npc)
        {
            Mimicry.OnCreatureAbilityUsed(npc, FeatType.SonicShriek);
            Mimicry.OnCreatureAbilityUsed(npc, FeatType.StaticWeb);
        }

        private static async Task Kill(EngineTestContext ctx, uint npc, uint player)
        {
            await ctx.ExecuteInCreatureContextAsync(npc, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), npc));
            await ctx.WaitUntilAsync(() => !CombatPoint.HasCombatPoints(player, npc), 5f, "kill XP payout and cleanup");
        }

        private static int Experience(PlayerAbilityFixture fixture, SkillType skill)
        {
            var playerSkill = DB.Get<Player>(fixture.Id).Skills[skill];
            return StartingExperience(playerSkill.Rank) + playerSkill.XP;
        }

        private static int StartingExperience(int rank) => Enumerable.Range(0, rank).Sum(Skill.GetRequiredXP);

        private static int AfterPenalty(int xp) => xp - (int)(xp * 0.3f);

        private static void Sweep() => typeof(Mimicry)
            .GetMethod("SweepStaleWitnesses", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    }
}
