using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Characterization.Support;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using static SWLOR.Game.Server.EngineTests.Framework.ViewModelTestHelper;

namespace SWLOR.Game.Server.EngineTests.Characterization;

/// <summary>
/// Output locking for crafting: a headless player opens the crafting window for a real recipe with its components in
/// the inventory, and works the synthesis and touch actions one click at a time with the shared Random reseeded before
/// every click. Each row records the status line and the progress, quality, durability and crafting points the window
/// shows after the click; the scenario also records the item crafted, what the player's inventory holds afterwards and
/// the Smithery XP the attempt earned.
/// </summary>
public static class CraftingScenarios
{
    private const int Seed = 20261101;
    private const int MaximumClicks = 24;

    private sealed record Attempt(
        string Label,
        string SnapshotName,
        RecipeType Recipe,
        int SkillRank,
        string[] Opening,
        string Repeated);

    private static readonly Attempt FirstTier = new(
        "Twin Electroblade I", "TwinElectroblade1", RecipeType.TwinElectroblade1, 7,
        new[] { "OnClickBasicTouch", "OnClickBasicTouch" }, "OnClickBasicSynthesis");

    private static readonly Attempt ThirdTier = new(
        "Twin Electroblade III", "TwinElectroblade3", RecipeType.TwinElectroblade3, 24,
        new[] { "OnClickBasicTouch" }, "OnClickBasicSynthesis");

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
            "CraftViewModel (manual craft actions)",
            "Works the crafting window for a recipe one click at a time and records what the window shows after each click; the " +
            "last rows record the crafted item, the inventory left behind and the XP earned.",
            $"Recipe {attempt.Recipe} (level {recipe.Level}, {recipe.Skill}, makes {recipe.Quantity}x {recipe.Resref}, components " +
            $"{string.Join(", ", recipe.Components.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Key + " x" + pair.Value))}) " +
            $"crafted by a headless player with {recipe.Skill} rank {attempt.SkillRank}. Random is reseeded with {Seed} plus the click number " +
            $"before every click. The clicks are {(attempt.Opening.Length == 0 ? string.Empty : string.Join(", ", attempt.Opening) + ", then ")}" +
            $"{attempt.Repeated} until the craft ends or {MaximumClicks} clicks pass.",
            new[] { "click", "action" },
            new[] { "status", "progress", "quality", "durability", "crafting points", "crafting" },
            Seed) { AllowConstantOutputs = true };

        var xpBefore = DB.Get<Player>(fixture.Id).Skills[recipe.Skill].XP;
        CraftViewModel window = null;
        try
        {
            await scenario.ExecuteInCreatureContextAsync(player, () =>
            {
                window = OpenWindow<CraftViewModel>(fixture, GuiWindowType.Craft, new CraftPayload(attempt.Recipe, OBJECT_INVALID));
                Click(window, "OnClickManualCraft");
            });
            await scenario.WaitFrameAsync();
            table.Add(Row(0, "start crafting", window));

            var click = 0;
            while (window.IsInCraftMode && click < MaximumClicks)
            {
                var action = click < attempt.Opening.Length ? attempt.Opening[click] : attempt.Repeated;
                click++;
                await scenario.ExecuteInCreatureContextAsync(player, () =>
                {
                    scenario.SeedRandom(Seed + click);
                    Click(window, action);
                });
                await scenario.WaitFrameAsync();
                await scenario.WaitFrameAsync();
                table.Add(Row(click, action.Replace("OnClick", string.Empty), window));
            }

            scenario.Assert(!window.IsInCraftMode, "The attempt ended before the click limit");
        }
        finally
        {
            CloseWindows(fixture);
        }

        await scenario.WaitFrameAsync();
        var record = DB.Get<Player>(fixture.Id);
        var result = new SnapshotTable(
            "CraftViewModel (crafting result)",
            "What the player holds and has earned once the crafting attempt has ended.",
            $"The same attempt as the steps snapshot: recipe {attempt.Recipe}, {recipe.Skill} rank {attempt.SkillRank}, Random seeded as described there. " +
            "Items are written as 'blueprint xStackSize [Type/Subtype/Cost/Param, ...]'.",
            new[] { "what" },
            new[] { "value" },
            Seed) { AllowConstantOutputs = true };
        result.Add($"{recipe.Skill} XP earned", record.Skills[recipe.Skill].XP - xpBefore);
        result.Add($"{recipe.Skill} rank", record.Skills[recipe.Skill].Rank);
        result.Add("recipe recorded as crafted", record.CraftedRecipes.ContainsKey(attempt.Recipe));
        foreach (var item in ItemDescription.DescribeInventory(player))
            result.Add("inventory item", item);

        ScenarioSnapshot.Verify("Craft", attempt.SnapshotName + "Result", result);
        ScenarioSnapshot.Verify("Craft", attempt.SnapshotName + "Steps", table);
    }

    private static object[] Row(int click, string action, CraftViewModel window)
    {
        return new object[]
        {
            click, action, window.StatusText, window.ProgressText, window.QualityText, window.DurabilityText, window.CP,
            window.IsInCraftMode
        };
    }
}
