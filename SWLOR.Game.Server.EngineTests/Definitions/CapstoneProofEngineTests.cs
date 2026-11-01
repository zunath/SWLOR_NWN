using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class CapstoneProofEngineTests
{
    private const string QuestId = "perfect_flurry_foundation";

    /// <summary>
    /// A Perfect Flurry foundation step counts any adept of its dungeon tier (here a blueprint first built for another
    /// line), but only once the player has made it bleed: the line's proof.
    /// </summary>
    [EngineTest("Capstone kill steps count tier enemies only after the line's proof", Category = "CapstoneProof", TimeoutSeconds = 90f)]
    public static async Task KillStepsRequireTheProof(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record => record.Skills[SkillType.Throwing].Rank = 50);
        await ctx.ExecuteInCreatureContextAsync(player,
            () => ctx.Assert(Quest.AcceptQuest(player, GetModule(), QuestId), "The qualified player accepts the foundation step."));
        ctx.AssertEqual(6, Remaining(fixture), "The step starts with six kills to go.");

        // Without a proof the kill does not count.
        var unproven = SpawnTierAdept(ctx);
        await KillAsync(ctx, player, unproven);
        ctx.AssertEqual(6, Remaining(fixture), "A kill without the proof does not count.");

        // A sibling line's adept counts once the player made it bleed.
        var proven = SpawnTierAdept(ctx);
        await ctx.ExecuteInCreatureContextAsync(player,
            () => ctx.Assert(StatusEffect.ApplyStatusEffect<BleedStatusEffect>(player, proven, 30f), "The player makes the adept bleed."));
        ctx.Assert(CapstoneProof.HasProof(player, proven, "perfect_flurry"), "Bleed proves Perfect Flurry on that adept.");
        ctx.Assert(!CapstoneProof.HasProof(player, proven, "kill_box"), "Bleed does not prove another line's mechanic.");
        await KillAsync(ctx, player, proven);
        ctx.AssertEqual(5, Remaining(fixture), "The proven kill of a tier enemy counts.");
    }

    private static uint SpawnTierAdept(EngineTestContext ctx)
    {
        // Thermal Detonator adepts share the Qion Test Site adept tier with Perfect Flurry.
        var enemy = ctx.SpawnCreature("cp_thermdet_ad", 2f);
        ctx.AssertEqual((int)NPCGroupType.Hutlar_ThermalDetonator_Adept, GetLocalInt(enemy, "QUEST_NPC_GROUP_ID"),
            "The adept carries its quest group.");
        ctx.MakeHostile(enemy);
        SetPlotFlag(enemy, false);
        return enemy;
    }

    private static async Task KillAsync(EngineTestContext ctx, uint player, uint enemy)
    {
        // Kill credit goes to the player with the most enmity, as it does after a real fight; both kills get the same
        // enmity, so only the proof differs.
        await ctx.ExecuteInCreatureContextAsync(enemy, () => Enmity.ModifyEnmity(player, enemy, 100));
        await ctx.ExecuteInCreatureContextAsync(enemy,
            () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), enemy));
        await ctx.WaitUntilAsync(() => !GetIsObjectValid(enemy) || GetIsDead(enemy), 10f, "the adept dies");
        await ctx.DelaySecondsAsync(0.5f);
    }

    private static int Remaining(PlayerAbilityFixture fixture)
    {
        var record = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(fixture.Id));
        return record.Quests[QuestId].KillProgresses[NPCGroupType.Hutlar_PerfectFlurry_Adept];
    }
}
