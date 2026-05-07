using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration
{
    internal static class DroidBoostRecipeMigration
    {
        private static readonly Dictionary<string, string[]> RecipeNameMap = new()
        {
            { "OneHandedBoost1", new[] { "DroidVibrobladeBoost1", "DroidVibroknifeBoost1", "DroidLightsaberBoost1" } },
            { "LightWeaponBoost1", new[] { "DroidVibrobladeBoost1", "DroidVibroknifeBoost1", "DroidLightsaberBoost1" } },
            { "TwoHandedBoost1", new[] { "DroidHeavyVibrobladeBoost1", "DroidSpearBoost1", "DroidTwinBladeBoost1", "DroidSaberstaffBoost1" } },
            { "HeavyWeaponBoost1", new[] { "DroidHeavyVibrobladeBoost1", "DroidSpearBoost1", "DroidTwinBladeBoost1", "DroidSaberstaffBoost1" } },
            { "MartialArtsBoost1", new[] { "DroidKatarBoost1", "DroidStaffBoost1" } },
            { "KatarStaffBoost1", new[] { "DroidKatarBoost1", "DroidStaffBoost1" } },
            { "RangedBoost1", new[] { "DroidPistolBoost1", "DroidRifleBoost1", "DroidThrowingBoost1" } },
            { "ProjectileBoost1", new[] { "DroidPistolBoost1", "DroidRifleBoost1", "DroidThrowingBoost1" } },
            { "OneHandedBoost2", new[] { "DroidVibrobladeBoost2", "DroidVibroknifeBoost2", "DroidLightsaberBoost2" } },
            { "LightWeaponBoost2", new[] { "DroidVibrobladeBoost2", "DroidVibroknifeBoost2", "DroidLightsaberBoost2" } },
            { "TwoHandedBoost2", new[] { "DroidHeavyVibrobladeBoost2", "DroidSpearBoost2", "DroidTwinBladeBoost2", "DroidSaberstaffBoost2" } },
            { "HeavyWeaponBoost2", new[] { "DroidHeavyVibrobladeBoost2", "DroidSpearBoost2", "DroidTwinBladeBoost2", "DroidSaberstaffBoost2" } },
            { "MartialArtsBoost2", new[] { "DroidKatarBoost2", "DroidStaffBoost2" } },
            { "KatarStaffBoost2", new[] { "DroidKatarBoost2", "DroidStaffBoost2" } },
            { "RangedBoost2", new[] { "DroidPistolBoost2", "DroidRifleBoost2", "DroidThrowingBoost2" } },
            { "ProjectileBoost2", new[] { "DroidPistolBoost2", "DroidRifleBoost2", "DroidThrowingBoost2" } },
        };

        private static readonly Dictionary<int, string> RecipeIdToNameMap = new()
        {
            { 3488, "OneHandedBoost1" },
            { 3489, "TwoHandedBoost1" },
            { 3490, "MartialArtsBoost1" },
            { 3491, "RangedBoost1" },
            { 3492, "OneHandedBoost2" },
            { 3493, "TwoHandedBoost2" },
            { 3494, "MartialArtsBoost2" },
            { 3495, "RangedBoost2" },
        };

        private static readonly Dictionary<string, int> NewRecipeIdsByName = new()
        {
            { "DroidVibrobladeBoost1", 4799 },
            { "DroidVibroknifeBoost1", 4800 },
            { "DroidLightsaberBoost1", 4801 },
            { "DroidHeavyVibrobladeBoost1", 4802 },
            { "DroidSpearBoost1", 4803 },
            { "DroidTwinBladeBoost1", 4804 },
            { "DroidSaberstaffBoost1", 4805 },
            { "DroidKatarBoost1", 4806 },
            { "DroidStaffBoost1", 4807 },
            { "DroidPistolBoost1", 4808 },
            { "DroidRifleBoost1", 4809 },
            { "DroidThrowingBoost1", 4810 },
            { "DroidVibrobladeBoost2", 4811 },
            { "DroidVibroknifeBoost2", 4812 },
            { "DroidLightsaberBoost2", 4813 },
            { "DroidHeavyVibrobladeBoost2", 4814 },
            { "DroidSpearBoost2", 4815 },
            { "DroidTwinBladeBoost2", 4816 },
            { "DroidSaberstaffBoost2", 4817 },
            { "DroidKatarBoost2", 4818 },
            { "DroidStaffBoost2", 4819 },
            { "DroidPistolBoost2", 4820 },
            { "DroidRifleBoost2", 4821 },
            { "DroidThrowingBoost2", 4822 },
        };

        public static bool ExpandPlayerRecipeDictionaries(JObject player)
        {
            if (player == null)
                return false;

            var migrated = ExpandRecipeDictionaryKeys(player[nameof(Player.UnlockedRecipes)] as JObject);
            migrated |= ExpandRecipeDictionaryKeys(player[nameof(Player.CraftedRecipes)] as JObject);

            return migrated;
        }

        public static bool TryGetReplacementRecipeNames(JToken recipe, out string[] newRecipeNames)
        {
            newRecipeNames = null;

            if (recipe == null)
                return false;

            if (recipe.Type == JTokenType.Integer &&
                RecipeIdToNameMap.TryGetValue(recipe.Value<int>(), out var oldRecipeName))
            {
                return RecipeNameMap.TryGetValue(oldRecipeName, out newRecipeNames);
            }

            return recipe.Type == JTokenType.String &&
                   TryGetReplacementRecipeNames(recipe.Value<string>(), out newRecipeNames);
        }

        public static bool TryGetReplacementRecipeNames(string recipeKey, out string[] newRecipeNames)
        {
            newRecipeNames = null;

            if (int.TryParse(recipeKey, out var recipeId) &&
                RecipeIdToNameMap.TryGetValue(recipeId, out var oldRecipeName))
            {
                return RecipeNameMap.TryGetValue(oldRecipeName, out newRecipeNames);
            }

            return RecipeNameMap.TryGetValue(recipeKey, out newRecipeNames);
        }

        public static bool TryGetReplacementRecipeTypes(JToken recipe, out RecipeType[] recipeTypes)
        {
            recipeTypes = null;

            if (!TryGetReplacementRecipeNames(recipe, out var recipeNames))
                return false;

            recipeTypes = GetReplacementRecipeTypes(recipeNames).ToArray();
            return recipeTypes.Length > 0;
        }

        public static IEnumerable<int> GetReplacementRecipeIds(IEnumerable<string> recipeNames)
        {
            return recipeNames
                .Select(recipeName => NewRecipeIdsByName[recipeName])
                .Distinct();
        }

        private static IEnumerable<RecipeType> GetReplacementRecipeTypes(IEnumerable<string> recipeNames)
        {
            return GetReplacementRecipeIds(recipeNames)
                .Select(recipeId => (RecipeType)recipeId)
                .Distinct();
        }

        private static bool ExpandRecipeDictionaryKeys(JObject dictionary)
        {
            if (dictionary == null)
                return false;

            var migrated = false;

            foreach (var property in dictionary.Properties().ToList())
            {
                if (!TryGetReplacementRecipeNames(property.Name, out var newRecipeNames))
                    continue;

                foreach (var newRecipeName in newRecipeNames)
                {
                    if (dictionary[newRecipeName] == null)
                        dictionary[newRecipeName] = property.Value.DeepClone();
                }

                property.Remove();
                migrated = true;
            }

            return migrated;
        }
    }
}
