using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    /// <summary>
    /// Recalibrates custom legacy sabers to current tier and crafting budgets,
    /// retaining bounded damage and accuracy bonuses and evidenced Chiro upgrades.
    /// Names, appearances, and unrelated properties remain intact.
    /// </summary>
    internal static class LegacySaberMigration
    {
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
        private const string SaberTierVariable = "SABER_TIER";
        private const string LegacyUpgradeVariable = "LIGHTSABER_UPGRADE_COUNT";
        private const ItemPropertyAttackDelay LightsaberDelay = ItemPropertyAttackDelay.Delay240;
        private const ItemPropertyAttackDelay SaberstaffDelay = ItemPropertyAttackDelay.Delay240;
        private const int LightsaberSkillSubtype = 38;
        private const int SaberstaffSkillSubtype = 42;

        /// <summary>
        /// Property types that make up a saber's damage profile and attack math.
        /// These are collected before replacement so compatible damage and accuracy
        /// survive within current crafting limits. Separate elemental damage effects
        /// are replaced by the saber's single damage profile.
        /// </summary>
        private static readonly HashSet<ItemPropertyType> NormalizedPropertyTypes = new()
        {
            ItemPropertyType.DMG,
            ItemPropertyType.Delay,
            ItemPropertyType.RequiresSkill,
            ItemPropertyType.WeaponDamageType,
            ItemPropertyType.EnhancementBonus,
            ItemPropertyType.DamageBonus,
            ItemPropertyType.AccuracyBonus,
            ItemPropertyType.Accuracy,
        };

        /// <summary>
        /// Sabers produced by crafting or the lightsaber workbench. These follow
        /// the established rules already and are never normalized.
        /// </summary>
        private static readonly HashSet<string> CraftableSaberResrefs = new(StringComparer.OrdinalIgnoreCase)
        {
            "saber_train_1",
            "saber_train_2",
            "saber_train_3",
            "saber_train_4",
            "saber_train_5",
            "fld_trnsaber",
            "vet_trnsaber",
            "prm_trnsaber",
            "asc_trnsaber",
            "trn_saberstaff_1",
            "trn_saberstaff_2",
            "trn_saberstaff_3",
            "trn_saberstaff_4",
            "trn_saberstaff_5",
            "fld_trnsabstaff",
            "vet_trnsabstaff",
            "prm_trnsabstaff",
            "asc_trnsabstaff",
            "ls_custom",
            "ss_custom",
        };

        /// <summary>
        /// Determines whether a resref belongs to a saber players can obtain
        /// through crafting or the lightsaber workbench. These are never normalized.
        /// </summary>
        public static bool IsCraftableSaberResref(string resref)
        {
            return !string.IsNullOrWhiteSpace(resref) &&
                   CraftableSaberResrefs.Contains(resref);
        }

        /// <summary>
        /// Determines whether an item is a DM-built saber that must be normalized.
        /// Tiered sabers are skipped except previously recalibrated tier 5 weapons
        /// whose historical upgrade marker proves that their Chiro step was lost.
        /// </summary>
        public static bool IsLegacySaber(uint item)
        {
            if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
                return false;

            var baseItemType = GetBaseItemType(item);
            if (baseItemType != BaseItem.Lightsaber && baseItemType != BaseItem.Saberstaff)
                return false;

            if (Item.IsEconomyRestricted(item))
                return false;

            if (CraftableSaberResrefs.Contains(GetResRef(item)))
                return false;

            var tier = GetLocalInt(item, SaberTierVariable);
            return tier <= 0 || (tier == 5 && GetLocalInt(item, LegacyUpgradeVariable) > 0);
        }

        /// <summary>
        /// Replaces obsolete damage properties with a bounded current profile.
        /// </summary>
        private static void NormalizeSaber(uint item)
        {
            var isSaberstaff = GetBaseItemType(item) == BaseItem.Saberstaff;
            var damage = 0;
            var accuracy = 0;

            var propertiesToRemove = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.DMG)
                    damage += GetItemPropertyCostTableValue(ip);
                else if (type == ItemPropertyType.DamageBonus)
                {
                    var row = GetItemPropertyCostTableValue(ip);
                    if (int.TryParse(Get2DAString("iprp_damagecost", "NumDice", row), out var dice) &&
                        int.TryParse(Get2DAString("iprp_damagecost", "Die", row), out var die))
                        damage += SaberRecalibration.CalculateLegacyDamageBonus(dice, die);
                }
                else if (type == ItemPropertyType.Accuracy || AccuracyItemPropertyMigration.IsLegacyAccuracyProperty(type))
                    accuracy += GetItemPropertyCostTableValue(ip);
                if (NormalizedPropertyTypes.Contains(type))
                    propertiesToRemove.Add(ip);
            }

            var upgraded = GetLocalInt(item, LegacyUpgradeVariable) > 0;
            // A previous sweep erased the original bonuses. Only the saved upgrade
            // marker can be recovered; add the current Chiro delta without inventing mods.
            if (GetLocalInt(item, SaberTierVariable) == 5 && upgraded)
                damage += 3;

            var profile = SaberRecalibration.CalculateProfile(isSaberstaff, damage, accuracy, upgraded);

            foreach (var property in propertiesToRemove)
                MigrationObject.RemoveProperty(item, property);

            var delay = isSaberstaff ? SaberstaffDelay : LightsaberDelay;
            var skillSubtype = isSaberstaff ? SaberstaffSkillSubtype : LightsaberSkillSubtype;

            MigrationObject.AddProperty(item, ItemPropertyCustom(ItemPropertyType.DMG, -1, profile.Damage), AddItemPropertyPolicy.ReplaceExisting);
            MigrationObject.AddProperty(item, ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)delay), AddItemPropertyPolicy.ReplaceExisting);
            MigrationObject.AddProperty(item, ItemPropertyCustom(ItemPropertyType.RequiresSkill, skillSubtype, profile.RequiredSkill), AddItemPropertyPolicy.ReplaceExisting);
            if (profile.Accuracy > 0)
                MigrationObject.AddProperty(item, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, profile.Accuracy), AddItemPropertyPolicy.ReplaceExisting);

            SetLocalInt(item, SaberTierVariable, profile.Tier);
        }

        /// <summary>
        /// Login sweep for the live player object. Normalizes legacy sabers in
        /// equipped slots, carried inventory, nested containers, and constructed
        /// droids, then tells the player what happened.
        /// </summary>
        public static void MigratePlayer(uint player)
        {
            var normalized = NormalizeSabersOnObject(player);
            if (normalized <= 0)
                return;

            var saberText = normalized == 1 ? "lightsaber has" : "lightsabers have";
            SendMessageToPC(player, $"Your {saberText} been recalibrated to current crafting limits, retaining supported bonuses and previously applied Chiro upgrades.");

            Log.Write(LogGroup.Migration, $"Recalibrated {normalized} legacy saber(s) to current crafting limits for {GetName(player)} ({GetObjectUUID(player)}).");
        }

        private static int NormalizeSabersOnObject(uint obj)
        {
            if (!GetIsObjectValid(obj))
                return 0;

            var normalized = 0;
            var objectType = GetObjectType(obj);

            if (objectType == ObjectType.Item)
            {
                if (IsLegacySaber(obj))
                {
                    EquipmentRequirementMigration.MigrateObject(obj);
                    SerializedItemWeaponDamageTypeMigration.MigrateObject(obj);
                    NormalizeSaber(obj);
                    return 1;
                }

                normalized += NormalizeSabersInConstructedDroid(obj);
            }
            else if (objectType == ObjectType.Creature)
            {
                for (var index = 0; index < NumberOfInventorySlots; index++)
                {
                    normalized += NormalizeSabersOnObject(GetItemInSlot((InventorySlot)index, obj));
                }
            }

            if (!GetIsObjectValid(obj) || !GetHasInventory(obj))
                return normalized;

            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
            {
                normalized += NormalizeSabersOnObject(item);
            }

            return normalized;
        }

        /// <summary>
        /// Stored-object sweep for offline surfaces. Normalizes legacy sabers in
        /// place, whether the stored object is the saber itself or a container
        /// holding one. Creature roots (DM creatures) are skipped so NPC gear
        /// stays intact.
        /// </summary>
        public static bool MigrateStoredObject(uint obj, out int normalizedCount)
        {
            normalizedCount = 0;
            if (!GetIsObjectValid(obj))
                return false;

            var objectType = GetObjectType(obj);
            if (objectType == ObjectType.Creature)
                return false;

            normalizedCount = NormalizeSabersOnObject(obj);
            return normalizedCount > 0;
        }

        private static int NormalizeSabersInConstructedDroid(uint controllerItem)
        {
            var serialized = GetLocalString(controllerItem, ConstructedDroidVariable);
            if (string.IsNullOrWhiteSpace(serialized))
                return 0;

            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
            if (droid == null)
                return 0;

            var normalized = 0;

            if (droid.EquippedItems != null)
            {
                foreach (var slot in droid.EquippedItems.Keys.ToList())
                {
                    if (TryNormalizeSerializedSaber(droid.EquippedItems[slot], out var migrated))
                    {
                        droid.EquippedItems[slot] = migrated;
                        normalized++;
                    }
                }
            }

            if (droid.Inventory != null)
            {
                foreach (var key in droid.Inventory.Keys.ToList())
                {
                    if (TryNormalizeSerializedSaber(droid.Inventory[key], out var migrated))
                    {
                        droid.Inventory[key] = migrated;
                        normalized++;
                    }
                }
            }

            if (normalized <= 0)
                return 0;

            SetLocalString(controllerItem, ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
            return normalized;
        }

        /// <summary>
        /// Recalibrates a serialized legacy saber while retaining its saved identity and always releasing the temporary object.
        /// </summary>
        private static bool TryNormalizeSerializedSaber(string serialized, out string migrated)
        {
            migrated = serialized;
            if (string.IsNullOrWhiteSpace(serialized))
                return false;

            var obj = MigrationObject.Deserialize(serialized);
            if (!GetIsObjectValid(obj))
                return false;

            try
            {
                if (NormalizeSabersOnObject(obj) <= 0)
                {
                    return false;
                }

                migrated = MigrationObject.Serialize(obj, serialized);
                return true;
            }
            finally
            {
                MigrationObject.DestroyTemporaryObject(obj);
            }
        }
    }
}
