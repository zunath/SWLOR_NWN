using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
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

    [EngineTest("Rousing Shout shielding at full health proves Hold the Line", Category = "CapstoneProof", TimeoutSeconds = 60f)]
    public static async Task RousingShoutAtFullHealthCounts(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record => record.Skills[SkillType.Leadership].Rank = 50);
        await ctx.ExecuteInCreatureContextAsync(player,
            () => ctx.Assert(Quest.AcceptQuest(player, GetModule(), "hold_the_line_foundation"), "The quest is accepted."));
        var enemy = ctx.SpawnCreature("cp_holdline_ad", 2f);
        ctx.MakeHostile(enemy);
        SetPlotFlag(enemy, false);
        ctx.Assert(!CapstoneProof.HasProof(player, enemy, "hold_the_line"), "No support has been used yet.");

        var ability = Ability.GetAbilityDetail(FeatType.RousingShout1);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            Ability.BeginAbilityImpact(player, ability);
            try { ability.ImpactAction(player, player, 1, GetLocation(player)); }
            finally { Ability.EndAbilityImpact(player); }
        });
        ctx.Assert(!StatusEffect.HasEffect(typeof(RousingShout1StatusEffect), player), "Full-health shielding has no rescue rider.");
        ctx.Assert(CapstoneProof.HasProof(player, enemy, "hold_the_line"), "The shield itself proves the support action.");
        await KillAsync(ctx, player, enemy);
        var record = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(fixture.Id));
        ctx.AssertEqual(5, record.Quests["hold_the_line_foundation"].KillProgresses[NPCGroupType.MonCala_WardOrderlys],
            "A kill following ordinary shielding advances the quest.");
    }

    [EngineTest("Viral Cascade rejects item Venom and accepts Vibroknife Venom", Category = "CapstoneProof", TimeoutSeconds = 60f)]
    public static async Task VenomRequiresVibroknifeAbility(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        var enemy = ctx.SpawnCreature("cp_vitrupt_ad", 2f);
        ctx.MakeHostile(enemy);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            ctx.Assert(StatusEffect.ApplyStatusEffect<VenomStatusEffect>(player, enemy, 30f), "Unattributed Venom applies.");
            ctx.Assert(!CapstoneProof.HasProof(player, enemy, "viral_cascade"), "Item Venom alone does not prove the skill.");
            var ability = Ability.GetAbilityDetail(FeatType.VirulentBlade1);
            Ability.BeginAbilityImpact(player, ability);
            try
            {
                ctx.Assert(StatusEffect.ApplyStatusEffect<VenomStatusEffect>(player, enemy, 30f), "Vibroknife Venom applies.");
            }
            finally { Ability.EndAbilityImpact(player); }
        });
        ctx.Assert(CapstoneProof.HasProof(player, enemy, "viral_cascade"), "Venom from the Vibroknife ability proves the skill.");
    }

    [EngineTest("Any owned beast can prove all six Beast Mastery lines", Category = "CapstoneProof", TimeoutSeconds = 60f)]
    public static async Task OwnedBeastDamageProvesEveryLine(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        using var otherOwner = await PlayerAbilityFixture.CreateAsync(ctx, 6f);
        var beast = ctx.SpawnCreature("nw_rat001", 1f);
        var enemy = SpawnTierAdept(ctx);
        await ctx.DelaySecondsAsync(1f);
        BeastMastery.SetBeastType(beast, BeastType.Womprat);
        await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () => AddHenchman(fixture.Creature, beast));
        ctx.Assert(BeastMastery.IsPlayerBeast(beast), "The beast is owned by the player.");
        var keys = new[] { "primal_overrun", "untouchable_instinct", "force_bonded_beast", "apex_bite", "unbreakable_beast", "alpha_rhythm" };
        await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () =>
            Combat.ApplyDamageDealtEffects(fixture.Creature, enemy, 1, SkillType.Vibroblade, CombatDamageType.Physical));
        foreach (var key in keys)
            ctx.Assert(!CapstoneProof.HasProof(fixture.Creature, enemy, key), "The master's own hit does not prove " + key);
        await ctx.ExecuteInCreatureContextAsync(beast, () =>
            Combat.ApplyDamageDealtEffects(beast, enemy, 1, SkillType.BeastMastery, CombatDamageType.Physical));
        foreach (var key in keys)
        {
            ctx.Assert(CapstoneProof.HasProof(fixture.Creature, enemy, key), "The beast's hit proves " + key);
            ctx.Assert(!CapstoneProof.HasProof(otherOwner.Creature, enemy, key), "Another player's beast does not prove " + key);
        }
    }

    private static uint SpawnTierAdept(EngineTestContext ctx)
    {
        // Thermal Detonator adepts share the Qion Test Site adept tier with Perfect Flurry.
        var enemy = ctx.SpawnCreature("cp_thermdet_ad", 2f);
        ctx.AssertEqual((int)NPCGroupType.Hutlar_TestCrewTechnicians, GetLocalInt(enemy, "QUEST_NPC_GROUP_ID"),
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
        return record.Quests[QuestId].KillProgresses[NPCGroupType.Hutlar_TestCrewTechnicians];
    }
}
