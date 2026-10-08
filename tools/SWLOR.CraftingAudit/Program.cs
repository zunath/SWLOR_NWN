using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Feature.PerkDefinition;

if (args.Length > 0 && args[0] == "--simulate") { CraftingSimulation.Run(args); return; }

// Export the real recipe builders without initializing NWN, Redis, or world caches.
var assembly = typeof(IRecipeListDefinition).Assembly;
Skill.CacheXPChartData();
// The final PerkBuilder.Build resolves icons through NWN. Read only the three
// recipe-gating definitions before that UI-only finalization.
var espionageDefinition = new EspionagePerkDefinition();
var privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
foreach (var method in new[] { "Poisoncraft", "Trapcraft", "MasterSaboteur" })
    typeof(EspionagePerkDefinition).GetMethod(method, privateInstance)!.Invoke(espionageDefinition, null);
var perkBuilder = typeof(EspionagePerkDefinition).GetField("_builder", privateInstance)!.GetValue(espionageDefinition)!;
var espionagePerks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder).GetField("_perks", privateInstance)!.GetValue(perkBuilder)!;
var recipes = new List<object>();
var ids = new HashSet<RecipeType>();
foreach (var type in assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface && typeof(IRecipeListDefinition).IsAssignableFrom(t)).OrderBy(t => t.FullName))
{
    var definition = (IRecipeListDefinition)Activator.CreateInstance(type)!;
    foreach (var (id, recipe) in definition.BuildRecipes())
    {
        if (!ids.Add(id)) throw new InvalidOperationException($"Duplicate recipe: {id}");
        recipes.Add(new {
            Id = id, NumericId = (int)id, Definition = type.FullName,
            recipe.Resref, recipe.Skill, recipe.Category, recipe.Level,
            recipe.IsActive, recipe.Quantity, recipe.PracticeRankLimit,
            recipe.EnhancementType, recipe.EnhancementSlots, recipe.ResearchCostModifier,
            recipe.Components,
            Requirements = recipe.Requirements.Select(r => r.RequirementText).ToArray(),
            PerkSkillGates = recipe.Requirements.OfType<RecipePerkRequirement>().SelectMany(requirement => {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var perk = (PerkType)typeof(RecipePerkRequirement).GetField("_perkType", flags)!.GetValue(requirement)!;
                var rank = (int)typeof(RecipePerkRequirement).GetField("_requiredLevel", flags)!.GetValue(requirement)!;
                if (!espionagePerks.ContainsKey(perk)) throw new InvalidOperationException($"Unmapped crafting perk: {perk}");
                return espionagePerks[perk].PerkLevels.Where(level => level.Key <= rank)
                    .SelectMany(level => level.Value.Requirements.OfType<PerkRequirementSkill>())
                    .Select(gate => new { Skill = gate.Type, Rank = gate.RequiredRank });
            }).ToArray(),
            RequiredRank = Craft.GetRequiredSkillRankForRecipe(recipe),
            BaseXPByRank = Enumerable.Range(0, 51).Select(rank => Craft.GetBaseRecipeXP(recipe, rank)).ToArray()
        });
    }
}
var chart = new RecipeLevelChart();
var levels = Enumerable.Range(1, 80).ToDictionary(level => level, level => chart.GetByLevel(level));
var bonusType = assembly.GetType("SWLOR.Game.Server.Service.CraftService.BlueprintBonuses")!;
var bonusInstance = Activator.CreateInstance(bonusType)!;
var blueprintBonuses = bonusType.GetField("_bonusesByEnhancementType", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(bonusInstance);
var options = new JsonSerializerOptions { WriteIndented = true };
options.Converters.Add(new JsonStringEnumConverter());
var path = Path.GetFullPath(args.Length > 0 ? args[0] : "design/testing/crafting-source-data.json");
Directory.CreateDirectory(Path.GetDirectoryName(path)!);
File.WriteAllText(path, JsonSerializer.Serialize(new { Recipes = recipes, Levels = levels, BlueprintBonuses = blueprintBonuses }, options));
Console.WriteLine($"Exported {recipes.Count} recipes, 80 levels, and blueprint bonus pools to {path}");
