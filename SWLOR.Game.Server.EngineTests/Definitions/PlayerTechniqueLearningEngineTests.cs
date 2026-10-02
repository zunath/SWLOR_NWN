using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PlayerTechniqueLearningEngineTests
    {
        [EngineTest("Player learns only witnessed techniques and respects rank and slot gates", Category = "PlayerTechniques", TimeoutSeconds = 60f)]
        public static async Task WitnessAndLoadout(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            using var secondFixture = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
            var player = fixture.Creature;
            var feat = FeatType.SnapRushTechnique;
            var detail = Mimicry.GetTechniqueDetail(feat);
            var npc = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.DelaySecondsAsync(1f);
            fixture.Update(record => record.Skills[SkillType.Mimicry].Rank = Math.Max(0, detail.MimicrySkillRequirement - 1));
            secondFixture.Update(record => record.Skills[SkillType.Mimicry].Rank = Math.Max(0, detail.MimicrySkillRequirement - 1));
            // Use the public observation path and the real death event. No learn-all shortcut.
            await ctx.ExecuteInCreatureContextAsync(npc, () =>
            {
                Mimicry.OnCreatureAbilityUsed(npc, detail.MimicrySourceFeat);
            });
            var witnesses = (Dictionary<uint, Dictionary<string, HashSet<FeatType>>>)typeof(Mimicry)
                .GetField("_witnesses", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            ctx.Assert(witnesses.TryGetValue(npc, out var recorded) && recorded.TryGetValue(fixture.Id, out var witnessed) && witnessed.Contains(feat),
                "native PC proximity records the witnessed technique");
            ctx.Assert(recorded.TryGetValue(secondFixture.Id, out var secondWitnessed) && secondWitnessed.Contains(feat),
                "both nearby players witness the same technique");
            await ctx.ExecuteInCreatureContextAsync(npc, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), npc));
            await ctx.WaitUntilAsync(() => !witnesses.ContainsKey(npc), 5f, "death to clear the witness cache");
            ctx.Assert(!DB.Get<Player>(fixture.Id).LearnedTechniques.ContainsKey(feat), "insufficient rank prevents learning");
            ctx.Assert(!DB.Get<Player>(secondFixture.Id).LearnedTechniques.ContainsKey(feat), "insufficient rank prevents learning for the second player");

            fixture.Update(record => record.Skills[SkillType.Mimicry].Rank = 50);
            secondFixture.Update(record => record.Skills[SkillType.Mimicry].Rank = 50);
            for (var attempt = 0; attempt < 20 &&
                (!DB.Get<Player>(fixture.Id).LearnedTechniques.ContainsKey(feat) ||
                 !DB.Get<Player>(secondFixture.Id).LearnedTechniques.ContainsKey(feat)); attempt++)
            {
                npc = ctx.SpawnCreature("nw_rat001", 2f);
                await ctx.WaitFrameAsync();
                ctx.SeedRandom(attempt + 1);
                await ctx.ExecuteInCreatureContextAsync(npc, () =>
                {
                    Mimicry.OnCreatureAbilityUsed(npc, detail.MimicrySourceFeat);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), npc);
                });
                await ctx.WaitUntilAsync(() => !witnesses.ContainsKey(npc), 5f, "learning death event");
            }
            var learned = DB.Get<Player>(fixture.Id);
            ctx.Assert(learned.LearnedTechniques.ContainsKey(feat), "a witnessed eligible technique is learned on NPC death");
            ctx.AssertEqual(1, learned.LearnedTechniques.Count, "unwitnessed techniques are not granted");
            ctx.Assert(DB.Get<Player>(secondFixture.Id).LearnedTechniques.ContainsKey(feat), "the death event also rolls learning for the second witness");
            ctx.AssertEqual(1, DB.Get<Player>(secondFixture.Id).LearnedTechniques.Count, "the second player only learns the witnessed technique");
            ctx.Assert(!Mimicry.IsTechniqueEquipped(player, feat), "learning does not automatically equip");
            fixture.Update(record => record.Perks.Remove(PerkType.CombatAnalyzer));
            ctx.Assert(!Mimicry.CanEquip(player, feat, out _), "zero available slots denies equipment");
            fixture.Update(record => record.Perks[PerkType.CombatAnalyzer] = 1);
            ctx.Assert(Mimicry.EquipTechnique(player, feat), "learned technique equips within the slot budget");
            ctx.Assert(GetHasFeat(feat, player), "active technique feat is granted");
            fixture.Update(record => record.Skills[SkillType.Mimicry].Rank = Math.Max(0, detail.MimicrySkillRequirement - 1));
            Mimicry.EnforceSlotBudget(player);
            ctx.Assert(!Mimicry.IsTechniqueEquipped(player, feat) && !GetHasFeat(feat, player), "skill decay removes an invalid loadout and its feat");
        }

    }
}
