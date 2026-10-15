using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    public static class ForceWeaponEnhancementMigration
    {
        private static readonly IReadOnlyDictionary<RecipeType, RecipeType> ReplacementRecipes =
            new Dictionary<RecipeType, RecipeType>
            {
                [RecipeType.WeaponEnhancementDMGForce1] = RecipeType.WeaponEnhancementDMGPhysical1,
                [RecipeType.WeaponEnhancementDMGForce2] = RecipeType.WeaponEnhancementDMGPhysical2,
                [RecipeType.WeaponEnhancementDMGForce3] = RecipeType.WeaponEnhancementDMGPhysical3,
            };

        public static RecipeType GetReplacementRecipe(RecipeType recipe) =>
            ReplacementRecipes.GetValueOrDefault(recipe, recipe);

        public static string MigrateRecipeList(string recipes) => string.IsNullOrWhiteSpace(recipes) ? recipes :
            string.Join(",", recipes.Split(',').Select(entry =>
                int.TryParse(entry, out var id) && GetReplacementRecipe((RecipeType)id) != (RecipeType)id
                    ? ((int)GetReplacementRecipe((RecipeType)id)).ToString()
                    : entry));

        public static string GetReplacementName(string name)
        {
            foreach (var rank in new[] { "I", "II", "III" })
            {
                var oldName = $"Weapon Enhancement - Force Damage {rank}";
                if (name == oldName) return $"Weapon Enhancement - DMG {rank}";
                if (name == $"Blueprint: {oldName}") return $"Blueprint: Weapon Enhancement - DMG {rank}";
            }
            return name;
        }

        public static bool MigrateRecipeKnowledge(Player player)
        {
            var changed = MigrateRecipes(player.UnlockedRecipes);
            changed |= MigrateRecipes(player.CraftedRecipes);
            return changed;
        }

        public static bool MigrateRecipeKnowledge(JObject player)
        {
            var changed = MigrateRecipes(player[nameof(Player.UnlockedRecipes)] as JObject);
            changed |= MigrateRecipes(player[nameof(Player.CraftedRecipes)] as JObject);
            return changed;
        }

        private static bool MigrateRecipes(JObject recipes)
        {
            if (recipes == null) return false;
            var changed = false;
            foreach (var entry in recipes.Properties().ToArray())
            {
                if (!Enum.TryParse<RecipeType>(entry.Name, out var recipe)) continue;
                var replacement = GetReplacementRecipe(recipe);
                if (replacement == recipe) continue;

                var name = replacement.ToString();
                var numericKey = ((int)replacement).ToString();
                if (recipes.Property(name) == null && recipes.Property(numericKey) == null)
                    recipes[int.TryParse(entry.Name, out _) ? numericKey : name] = entry.Value.DeepClone();
                entry.Remove();
                changed = true;
            }
            return changed;
        }

        private static bool MigrateRecipes(Dictionary<RecipeType, DateTime> recipes)
        {
            if (recipes == null) return false;
            var changed = false;
            foreach (var (oldRecipe, replacement) in ReplacementRecipes)
            {
                if (!recipes.Remove(oldRecipe, out var date)) continue;
                recipes.TryAdd(replacement, date);
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// Edits archived item fields directly, retaining UUIDs, inventory slots,
        /// creature state, stack sizes, and every unrelated raw GFF field.
        /// </summary>
        public static string MigrateSerializedObject(string data) =>
            string.IsNullOrWhiteSpace(data) ? data : StoredObjectData.RemoveForceWeaponEnhancements(data);

        public static bool MigrateObject(uint obj) => MigrateObject(obj, new HashSet<uint>());

        private static bool MigrateObject(uint obj, HashSet<uint> visited)
        {
            if (!GetIsObjectValid(obj) || !visited.Add(obj)) return false;
            var changed = false;
            if (GetObjectType(obj) == ObjectType.Item)
            {
                changed |= MigrateItem(obj);
                var droid = GetLocalString(obj, "CONSTRUCTED_DROID");
                var migratedDroid = MigrateDroid(droid);
                if (droid != migratedDroid)
                {
                    SetLocalString(obj, "CONSTRUCTED_DROID", migratedDroid);
                    changed = true;
                }
            }
            else if (GetObjectType(obj) == ObjectType.Creature)
            {
                for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                    changed |= MigrateObject(GetItemInSlot((InventorySlot)slot, obj), visited);
            }
            if (GetHasInventory(obj))
                for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                    changed |= MigrateObject(item, visited);
            return changed;
        }

        private static bool MigrateItem(uint item)
        {
            if (Item.IsEconomyRestricted(item)) return false;
            var changed = false;
            var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                properties.Add(ip);
            foreach (var ip in properties)
            {
                var type = GetItemPropertyType(ip);
                var subtype = GetItemPropertySubType(ip);
                if (type == ItemPropertyType.WeaponDamageType && subtype == (int)CombatDamageType.Force)
                {
                    MigrationObject.RemoveProperty(item, ip);
                    changed = true;
                }
                else if (type == ItemPropertyType.WeaponEnhancement && subtype == 19)
                {
                    var amount = GetItemPropertyCostTableValue(ip);
                    MigrationObject.RemoveProperty(item, ip);
                    MigrationObject.AddProperty(item,
                        ItemPropertyCustom(ItemPropertyType.WeaponEnhancement, (int)EnhancementSubType.DMG, amount),
                        AddItemPropertyPolicy.IgnoreExisting);
                    changed = true;
                }
            }
            var name = GetName(item);
            var replacementName = GetReplacementName(name);
            if (name != replacementName)
            {
                SetName(item, replacementName);
                changed = true;
            }
            var recipe = (RecipeType)GetLocalInt(item, "BLUEPRINT_RECIPE_ID");
            var replacementRecipe = GetReplacementRecipe(recipe);
            if (recipe != replacementRecipe)
            {
                SetLocalInt(item, "BLUEPRINT_RECIPE_ID", (int)replacementRecipe);
                changed = true;
            }
            var recipes = GetLocalString(item, "RECIPES");
            var replacementRecipes = MigrateRecipeList(recipes);
            if (recipes != replacementRecipes)
            {
                SetLocalString(item, "RECIPES", replacementRecipes);
                changed = true;
            }
            return changed;
        }

        /// <summary>Retains unknown droid JSON fields and stable equipment/inventory keys.</summary>
        internal static string MigrateDroid(string data)
        {
            if (string.IsNullOrWhiteSpace(data)) return data;
            var droid = JObject.Parse(data);
            var changed = false;
            void MigrateField(JProperty field)
            {
                if (field.Value.Type != JTokenType.String) return;
                var original = field.Value.Value<string>();
                var migrated = MigrateSerializedObject(original);
                if (original == migrated) return;
                field.Value = migrated;
                changed = true;
            }
            foreach (var field in droid.Properties().Where(x => x.Name is
                         "SerializedCPU" or "SerializedHead" or "SerializedBody" or "SerializedArms" or "SerializedLegs"))
                MigrateField(field);
            foreach (var name in new[] { "EquippedItems", "Inventory" })
                if (droid[name] is JObject items)
                    foreach (var field in items.Properties()) MigrateField(field);
            return changed ? droid.ToString(Newtonsoft.Json.Formatting.None) : data;
        }
    }
}
