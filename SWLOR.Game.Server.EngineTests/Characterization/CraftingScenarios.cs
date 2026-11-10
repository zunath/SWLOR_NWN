using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Characterization.Support;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Characterization;

/// <summary>
/// Output locking for crafting: a headless player starts a craft session for a real recipe with its components in
/// the inventory, finishes it with a full quality meter and a fixed number of actions, and the scenario records the
/// session that started, the outcome the quality produced, the item crafted, what the player's inventory holds afterwards
/// and the XP the attempt earned. The clock is frozen, so the pace the XP is held to is a function of the steps.
/// </summary>
public static class CraftingScenarios
{
    private const int Seed = 20261101;
    private const int ActionsTaken = 10;

    private sealed record Attempt(string Label, string SnapshotName, RecipeType Recipe, int SkillRank);

    private static readonly Attempt FirstTier = new("Twin Electroblade I", "TwinElectroblade1", RecipeType.TwinElectroblade1, 7);

    private static readonly Attempt ThirdTier = new("Twin Electroblade III", "TwinElectroblade3", RecipeType.TwinElectroblade3, 24);

    [EngineTest("Characterization: crafting - Twin Electroblade I", Category = "Characterization", TimeoutSeconds = 240f)]
    public static Task TwinElectrobladeI(EngineTestContext ctx) => Run(ctx, FirstTier);

    [EngineTest("Characterization: crafting - Twin Electroblade III", Category = "Characterization", TimeoutSeconds = 240f)]
    public static Task TwinElectrobladeIII(EngineTestContext ctx) => Run(ctx, ThirdTier);

    private static async Task Run(EngineTestContext ctx, Attempt attempt)
    {
        using var clock = new FrozenClock();
        using var arena = await ScenarioArena.CreateAsync(ctx);
        var scenario = arena.Context;

        using var fixture = await PlayerAbilityFixture.CreateAsync(scenario);
        var player = fixture.Creature;
        var recipe = Craft.GetRecipe(attempt.Recipe);
        fixture.Update(record =>
        {
            foreach (var skill in Enum.GetValues<SkillType>().Where(skill => skill != SkillType.Invalid))
            {
                record.Skills[skill].Rank = 0;
                record.Skills[skill].XP = 0;
            }

            record.Skills[recipe.Skill].Rank = attempt.SkillRank;
            record.XPDebt = 0;
            record.DMXPBonus = 0;
            record.CraftedRecipes.Remove(attempt.Recipe);
        });
        scenario.Assert(Craft.CanPlayerCraftRecipe(player, attempt.Recipe), "The player meets the recipe requirements");

        await scenario.ExecuteInCreatureContextAsync(player, () =>
        {
            foreach (var (resref, quantity) in recipe.Components)
                scenario.Assert(GetIsObjectValid(CreateItemOnObject(resref, player, quantity)), $"Component {resref} was created");
        });
        await scenario.WaitFrameAsync();
        await scenario.WaitFrameAsync();

        var table = new SnapshotTable(
            "CraftSessionService (start and succeed)",
            "Starts a craft session for a recipe, finishes it with a full quality meter and records the session and the outcome the " +
            "quality produced.",
            $"Recipe {attempt.Recipe} (level {recipe.Level}, {recipe.Skill}, makes {recipe.Quantity}x {recipe.Resref}, components " +
            $"{string.Join(", ", recipe.Components.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + " x" + pair.Value))}) " +
            $"crafted by a headless player with {recipe.Skill} rank {attempt.SkillRank}. Random is reseeded with {Seed} before the craft " +
            $"succeeds with a full quality meter after {ActionsTaken} actions.",
            new[] { "step" },
            new[] { "value" },
            Seed) { AllowConstantOutputs = true };

        var xpBefore = DB.Get<Player>(fixture.Id).Skills[recipe.Skill].XP;
        CraftSession session = null;
        await scenario.ExecuteInCreatureContextAsync(player, () =>
        {
            var start = CraftSessions.Start(player, new CraftSessionStartRequest(attempt.Recipe, OBJECT_INVALID, new uint[0]));
            table.Add("start succeeded", start.Succeeded);
            table.Add("start message", start.Message ?? string.Empty);
            session = start.Session;
        });

        // The components leave the inventory when the starting script ends.
        await scenario.WaitFrameAsync();
        await scenario.ExecuteInCreatureContextAsync(player, () =>
            table.Add("inventory items once the components are committed", ItemDescription.DescribeInventory(player).Count()));

        scenario.Assert(session != null, "The craft session started");
        await scenario.ExecuteInCreatureContextAsync(player, () =>
        {
            scenario.SeedRandom(Seed);
            var success = CraftSessions.Succeed(player, session, new MinigameMeter(100, 100), ActionsTaken);
            scenario.Assert(success != null, "The craft succeeds");
            var outcome = success.Outcome;
            table.Add("quality fraction", outcome.QualityFraction);
            table.Add("quality percent", outcome.QualityPercent);
            table.Add("outcome XP", outcome.Xp);
            table.Add("vendor value bonus", outcome.VendorValueBonus);
            table.Add("enhancements", outcome.Enhancements.Count);
            table.Add("food duration bonus seconds", outcome.FoodDurationBonusSeconds);
            table.Add("food bonus charges", outcome.FoodBonusCharges);
        });

        await scenario.WaitFrameAsync();
        var record = DB.Get<Player>(fixture.Id);
        var result = new SnapshotTable(
            "CraftSessionService (crafting result)",
            "What the player holds and has earned once the crafting attempt has ended.",
            $"The same attempt as the steps snapshot: recipe {attempt.Recipe}, {recipe.Skill} rank {attempt.SkillRank}, Random seeded as described there. " +
            "Items are written as 'blueprint xStackSize [Type/Subtype/Cost/Param, ...]'.",
            new[] { "what" },
            new[] { "value" },
            Seed) { AllowConstantOutputs = true };
        result.Add($"{recipe.Skill} XP earned", record.Skills[recipe.Skill].XP - xpBefore);
        result.Add($"{recipe.Skill} rank", record.Skills[recipe.Skill].Rank);
        result.Add("recipe recorded as crafted", record.CraftedRecipes.ContainsKey(attempt.Recipe));
        result.Add("items held at the station", record.HeldCraftItems?.Count ?? 0);
        foreach (var item in ItemDescription.DescribeInventory(player))
            result.Add("inventory item", item);

        ScenarioSnapshot.Verify("Craft", attempt.SnapshotName + "Result", result);
        ScenarioSnapshot.Verify("Craft", attempt.SnapshotName + "Steps", table);
    }
}
