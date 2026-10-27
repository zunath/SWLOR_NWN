using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks how the recipe and loot table builders turn their calls into data: defaults, clamping, what a repeated call does,
/// and which calls are rejected. Every recipe and loot table definition is written with these builders, so a change here
/// would change data that the definition snapshots only show after the fact.
/// </summary>
public class DefinitionBuilderCharacterizationTests
{
    [Test]
    public void RecipeBuilder()
    {
        var table = new SnapshotTable(
            "RecipeBuilder (Create, Category, Level, SkillUpToRank, Quantity, Resref, EffectDescription, EnhancementSlots, ResearchCostModifier, Inactive, Requirement*, Component, Build)",
            "What the recipe builder produces for a call sequence: the defaults of a new recipe, a quantity below 1 becoming 1, a practice rank limit below 1 being rejected, " +
            "a repeated component or enhancement slot call replacing the earlier one, effect lines adding up, and a recipe type created twice being rejected.",
            "Hand-written call sequences, one per row; the result lists skill, category, level, practice rank limit, quantity, resref, active flag, enhancement type and slots, " +
            "research cost modifier, components, requirements and effect lines of each recipe built, or the exception.",
            new[] { "case" },
            new[] { "result" })
        {
            AllowConstantOutputs = true
        };

        var cases = new (string Name, Func<RecipeBuilder, RecipeBuilder> Build)[]
        {
            ("defaults", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery)),
            ("category and level", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).Category(RecipeCategoryType.Knife).Level(12)),
            ("quantity 7", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).Quantity(7)),
            ("quantity 1", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).Quantity(1)),
            ("quantity 0 becomes 1", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).Quantity(0)),
            ("quantity -3 becomes 1", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).Quantity(-3)),
            ("practice rank limit 25", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).SkillUpToRank(25)),
            ("practice rank limit 1", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).SkillUpToRank(1)),
            ("practice rank limit 0 is rejected", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).SkillUpToRank(0)),
            ("practice rank limit -4 is rejected", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).SkillUpToRank(-4)),
            ("resref", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).Resref("b_knife")),
            ("components in order, repeated one replaced", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery)
                .Component("ref_veldite", 2).Component("wood", 1).Component("ref_veldite", 5)),
            ("component quantity zero kept", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).Component("wood", 0)),
            ("enhancement slots, second call replaces", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery)
                .EnhancementSlots(RecipeEnhancementType.Weapon, 1).EnhancementSlots(RecipeEnhancementType.Armor, 3)),
            ("research cost modifier", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).ResearchCostModifier(0.25f)),
            ("inactive", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).Inactive()),
            ("unlock requirement", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).RequirementUnlocked()),
            ("perk requirement", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery).RequirementPerk(PerkType.Poisoncraft, 2, "Poisoncraft")),
            ("requirements in order", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery)
                .RequirementPerk(PerkType.Trapcraft, 1, "Trapcraft").RequirementUnlocked()),
            ("effect lines add up", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery)
                .EffectDescription(new[] { "Line one", "Line two" }).EffectDescription(new[] { "Line three" })),
            ("two recipes keep creation order", builder => builder.Create(RecipeType.BasicSpear, SkillType.Smithery).Level(3)
                .Create(RecipeType.BasicKnife, SkillType.Engineering).Level(9)),
            ("same recipe type twice is rejected", builder => builder.Create(RecipeType.BasicKnife, SkillType.Smithery)
                .Create(RecipeType.BasicKnife, SkillType.Engineering)),
            ("settings before any recipe", builder => builder.Level(4)),
        };

        foreach (var (name, build) in cases)
            table.Add(name, Outcome.Of(() => Describe(build(new RecipeBuilder()).Build())));

        Snapshot.Verify("Craft", "RecipeBuilder", table);
    }

    [Test]
    public void LootTableBuilder()
    {
        var table = new SnapshotTable(
            "LootTableBuilder (Create, IsRare, MaxRareChanceBonus, AddItem, AddGold, AddSpawnAction, Build)",
            "What the loot table builder produces for a call sequence: the defaults of an item (largest quantity 1, not rare), gold as nw_it_gold001 with the amount as its " +
            "largest quantity, a spawn action attaching to the item added last, a table id created twice replacing the first table, and a spawn action with no item being rejected.",
            "Hand-written call sequences, one per row; the result lists each table built as id, rare flag, rare chance maximum and its items as resref/weight/largest quantity/rare/spawn action set, or the exception.",
            new[] { "case" },
            new[] { "result" })
        {
            AllowConstantOutputs = true
        };

        var cases = new (string Name, Func<LootTableBuilder, LootTableBuilder> Build)[]
        {
            ("empty table", builder => builder.Create("T")),
            ("item defaults", builder => builder.Create("T").AddItem("sword", 5)),
            ("item with quantity", builder => builder.Create("T").AddItem("arrow", 5, 20)),
            ("rare item", builder => builder.Create("T").AddItem("gem", 2, 1, true)),
            ("gold", builder => builder.Create("T").AddGold(150, 7)),
            ("items keep order", builder => builder.Create("T").AddItem("a", 1).AddGold(10, 2).AddItem("b", 3, 4, true)),
            ("rare table", builder => builder.Create("T").IsRare().AddItem("gem", 2, 1, true)),
            ("rare chance maximum", builder => builder.Create("T").MaxRareChanceBonus(20).AddItem("disc", 1)),
            ("spawn action goes to the last item", builder => builder.Create("T").AddItem("a", 1).AddItem("b", 1).AddSpawnAction(_ => { })),
            ("spawn action after gold", builder => builder.Create("T").AddGold(5, 1).AddSpawnAction(_ => { })),
            ("two tables", builder => builder.Create("A").AddItem("a", 1).Create("B").AddItem("b", 2)),
            ("same id twice replaces the first table", builder => builder.Create("T").AddItem("a", 1).Create("T").AddItem("b", 2)),
            ("rare flag applies to the active table only", builder => builder.Create("A").IsRare().Create("B").AddItem("b", 1)),
            ("spawn action after a new table still goes to the previous item", builder => builder.Create("A").AddItem("a", 1).Create("B").AddSpawnAction(_ => { })),
            ("spawn action with no item is rejected", builder => builder.Create("T").AddSpawnAction(_ => { })),
            ("item before any table is rejected", builder => builder.AddItem("a", 1)),
        };

        foreach (var (name, build) in cases)
            table.Add(name, Outcome.Of(() => Describe(build(new LootTableBuilder()).Build())));

        Snapshot.Verify("Loot", "LootTableBuilder", table);
    }

    private static string Describe(Dictionary<RecipeType, RecipeDetail> recipes)
    {
        return string.Join(" | ", recipes.Select(entry =>
        {
            var recipe = entry.Value;
            return $"{entry.Key}: {recipe.Skill}/{recipe.Category}/level {recipe.Level}/limit {DefinitionCell.Text(recipe.PracticeRankLimit)}/x{recipe.Quantity}/" +
                   $"{DefinitionCell.Text(recipe.Resref)}/active {DefinitionCell.Text(recipe.IsActive)}/{recipe.EnhancementType} {recipe.EnhancementSlots}/" +
                   $"cost {DefinitionCell.Text(recipe.ResearchCostModifier)}/components [{string.Join(", ", recipe.Components.Select(c => $"{c.Key} x{c.Value}"))}]/" +
                   $"requirements [{string.Join(", ", recipe.Requirements.Select(r => r.GetType().Name + " " + r.RequirementText))}]/effects [{string.Join(", ", recipe.EffectLines)}]";
        }));
    }

    private static string Describe(Dictionary<string, LootTable> tables)
    {
        return string.Join(" | ", tables.Select(entry =>
        {
            var items = entry.Value.Select(item =>
                $"{item.Resref}/{item.Weight}/{item.MaxQuantity}/{DefinitionCell.Text(item.IsRare)}/{DefinitionCell.Text(item.OnSpawn != null)}");
            return $"{entry.Key}: rare {DefinitionCell.Text(entry.Value.IsRare)}/max {DefinitionCell.Text(entry.Value.MaxRareChanceBonus)}/items [{string.Join(", ", items)}]";
        }));
    }
}
