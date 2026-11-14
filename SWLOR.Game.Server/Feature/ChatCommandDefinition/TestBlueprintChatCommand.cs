using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Crafting.Research;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Test-server shortcuts that hand a tester a fully researched blueprint for a level 50 or level 52 recipe.
    /// They exist only on a server whose SWLOR_ENVIRONMENT is explicitly test (every player) or dev (Admin).
    /// Production never registers them, and neither does an unset or mistyped environment.
    /// </summary>
    public class TestBlueprintChatCommand: IChatCommandListDefinition
    {
        public const int OrdinaryTopLevel = 50;
        public const int TopRecipeLevel = 52;
        private const int MaxListed = 12;

        /// <summary>Licensed runs a fully researched blueprint carries: the 1-3 a new blueprint starts with plus the two licensed-run levels.</summary>
        public const int ResearchedLicensedRuns = 5;

        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var settings = ApplicationSettings.Get();
            return BuildChatCommands(settings.ServerEnvironment, settings.ServerEnvironmentIsExplicit);
        }

        public Dictionary<string, ChatCommandDetail> BuildChatCommands(ServerEnvironmentType environment, bool environmentIsExplicit)
        {
            if (!IsAvailable(environment, environmentIsExplicit))
                return new Dictionary<string, ChatCommandDetail>();

            BuildCommand(OrdinaryTopLevel, "testblueprint50", "tbp50");
            BuildCommand(TopRecipeLevel, "testblueprint52", "tbp52");

            return _builder.Build();
        }

        /// <summary>Only an explicitly named test or development server gets these commands.</summary>
        public static bool IsAvailable(ServerEnvironmentType environment, bool environmentIsExplicit)
        {
            return environmentIsExplicit &&
                   environment is ServerEnvironmentType.Test or ServerEnvironmentType.Development;
        }

        private void BuildCommand(int level, params string[] names)
        {
            _builder.Create(names[0], names.Skip(1).ToArray())
                .Description($"Test server: gives you a fully researched blueprint (research level {ResearchGrantRules.MaxLevel}) for a level {level} recipe. " +
                             $"/{names[0]} lists the level {level} recipes; /{names[0]} <part of the item name> gives the one blueprint that matches.")
                .Permissions(AuthorizationLevel.Admin)
                .AvailableToAllOnTestEnvironment()
                .Action((user, target, location, args) =>
                {
                    var filter = string.Join(" ", args);
                    var matches = FindRecipes(Craft.GetAllResearchableRecipes(), Cache.GetItemNameByResref, level, filter);

                    if (matches.Count == 0)
                    {
                        SendMessageToPC(user, string.IsNullOrWhiteSpace(filter)
                            ? $"There are no level {level} recipes to research."
                            : $"No level {level} recipe matches '{filter}'. Use /{names[0]} to list them.");
                        return;
                    }

                    if (matches.Count > 1)
                    {
                        var listed = string.Join(", ", matches.Take(MaxListed).Select(match => match.Name));
                        var more = matches.Count > MaxListed ? $" ... and {matches.Count - MaxListed} more" : string.Empty;
                        SendMessageToPC(user, $"{matches.Count} level {level} recipes match. Add more of the name, such as /{names[0]} {matches[0].Name}: {listed}{more}");
                        return;
                    }

                    var match = matches[0];
                    var blueprint = CreateItemOnObject("blueprint", user);
                    if (!GetIsObjectValid(blueprint))
                    {
                        SendMessageToPC(user, "The blueprint could not be created.");
                        return;
                    }

                    SetName(blueprint, $"Blueprint: {Cache.GetItemNameByResref(Craft.GetRecipe(match.Recipe).Resref)}");
                    Craft.SetBlueprintDetails(blueprint, BuildResearchedBlueprint(match.Recipe));
                    SendMessageToPC(user, $"Gave you a fully researched blueprint: {match.Name} (recipe level {Craft.GetRecipe(match.Recipe).Level}).");
                });
        }

        /// <summary>
        /// The recipes of exactly this level that match the filter, ordered by name. A recipe whose item name another
        /// recipe of the level shares is labelled "Name - Skill". A label or name equal to the filter wins;
        /// otherwise every word of the filter must appear in the label.
        /// </summary>
        public static List<(RecipeType Recipe, string Name)> FindRecipes(
            IReadOnlyDictionary<RecipeType, RecipeDetail> recipes,
            System.Func<string, string> itemName,
            int level,
            string filter)
        {
            var named = recipes
                .Where(pair => pair.Value.IsActive && pair.Value.Level == level)
                .Select(pair => (Recipe: pair.Key, Item: itemName(pair.Value.Resref), pair.Value.Skill))
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Item))
                .ToList();

            var found = named
                .Select(pair => (pair.Recipe, pair.Item, Label: named.Count(other => string.Equals(other.Item, pair.Item, System.StringComparison.OrdinalIgnoreCase)) > 1
                    ? $"{pair.Item} - {pair.Skill}"
                    : pair.Item))
                .OrderBy(pair => pair.Label, System.StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(filter))
                return found.Select(pair => (pair.Recipe, pair.Label)).ToList();

            var text = filter.Trim();
            var exact = found.Where(pair =>
                string.Equals(pair.Label, text, System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Item, text, System.StringComparison.OrdinalIgnoreCase)).ToList();
            if (exact.Count > 0)
                return exact.Select(pair => (pair.Recipe, pair.Label)).ToList();

            var words = text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            return found
                .Where(pair => words.All(word => pair.Label.Contains(word, System.StringComparison.OrdinalIgnoreCase)))
                .Select(pair => (pair.Recipe, pair.Label))
                .ToList();
        }

        /// <summary>
        /// The details of a blueprint researched to the top: every fixed grant of research levels 1 to 10 and the best credit and time reductions.
        /// The guaranteed tier three item bonus of research level 7 is left out.
        /// </summary>
        public static BlueprintDetail BuildResearchedBlueprint(RecipeType recipe)
        {
            return new BlueprintDetail
            {
                Recipe = recipe,
                Level = ResearchGrantRules.MaxLevel,
                LicensedRuns = ResearchedLicensedRuns,
                ItemBonuses = 3,
                CreditReduction = ResearchGrantRules.ReductionMaximum,
                TimeReduction = ResearchGrantRules.ReductionMaximum,
                EnhancementSlots = 1
            };
        }
    }
}
