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
    /// Normalizes DM-built lightsabers and saberstaffs (any Lightsaber/Saberstaff
    /// base item that is not part of the craftable training saber lines or the
    /// workbench-built sabers) to the tier 5 baseline: DMG, attack delay, and the
    /// skill requirement are set to the tier 5 values, while off-tier modifiers
    /// (weapon damage type, enhancement/damage/accuracy bonuses) are deliberately
    /// removed because the tier baseline carries none - that removal is the point
    /// of normalization. The weapon is stamped with the saber tier variable so the
    /// tiered upgrade kits recognize it. Owners keep their weapons; nothing is
    /// removed from their inventories.
    /// </summary>
    internal static class LegacySaberMigration
    {
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
        private const string SaberTierVariable = "SABER_TIER";
        private const int NormalizedTier = 5;

        private const int LightsaberTierDamage = 21;
        private const int SaberstaffTierDamage = 25;
        private const ItemPropertyAttackDelay LightsaberDelay = ItemPropertyAttackDelay.Delay240;
        private const ItemPropertyAttackDelay SaberstaffDelay = ItemPropertyAttackDelay.Delay290;
        private const int TierRequiredSkill = 40;
        private const int LightsaberSkillSubtype = 38;
        private const int SaberstaffSkillSubtype = 42;

        /// <summary>
        /// Property types that make up a saber's damage profile and attack math.
        /// All of these are stripped during normalization; only DMG, Delay, and
        /// RequiresSkill get tier 5 replacements - the tier baseline intentionally
        /// has no damage type, enhancement, damage, or accuracy bonuses. Everything
        /// else on the weapon (VFX, cast spell, etc.) is kept.
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
        /// Already-normalized sabers (tier variable set) are skipped.
        /// </summary>
        public static bool IsLegacySaber(uint item)
        {
            if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
                return false;

            var baseItemType = GetBaseItemType(item);
            if (baseItemType != BaseItem.Lightsaber && baseItemType != BaseItem.Saberstaff)
                return false;

            if (GetLocalInt(item, SaberTierVariable) > 0)
                return false;

            return !CraftableSaberResrefs.Contains(GetResRef(item));
        }

        /// <summary>
        /// Replaces a saber's damage profile and attack modifiers with the tier 5
        /// baseline and stamps the saber tier variable. The weapon's name,
        /// appearance, and remaining properties are preserved.
        /// </summary>
        private static void NormalizeSaber(uint item)
        {
            var isSaberstaff = GetBaseItemType(item) == BaseItem.Saberstaff;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (NormalizedPropertyTypes.Contains(GetItemPropertyType(ip)))
                {
                    RemoveItemProperty(item, ip);
                }
            }

            var damage = isSaberstaff ? SaberstaffTierDamage : LightsaberTierDamage;
            var delay = isSaberstaff ? SaberstaffDelay : LightsaberDelay;
            var skillSubtype = isSaberstaff ? SaberstaffSkillSubtype : LightsaberSkillSubtype;

            BiowareXP2.IPSafeAddItemProperty(item, ItemPropertyCustom(ItemPropertyType.DMG, -1, damage), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(item, ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)delay), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(item, ItemPropertyCustom(ItemPropertyType.RequiresSkill, skillSubtype, TierRequiredSkill), 0.0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            SetLocalInt(item, SaberTierVariable, NormalizedTier);
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
            SendMessageToPC(player, $"Your {saberText} been recalibrated to tier 5 as part of the combat overhaul. Tier 5.5 upgrade kits can be crafted through Engineering once their blueprint is recovered.");

            Log.Write(LogGroup.Migration, $"Normalized {normalized} legacy saber(s) to tier {NormalizedTier} for {GetName(player)} ({GetObjectUUID(player)}).");
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

        private static bool TryNormalizeSerializedSaber(string serialized, out string migrated)
        {
            migrated = serialized;
            if (string.IsNullOrWhiteSpace(serialized))
                return false;

            var obj = ObjectPlugin.Deserialize(serialized);
            if (!GetIsObjectValid(obj))
                return false;

            if (!IsLegacySaber(obj))
            {
                DestroyObject(obj);
                return false;
            }

            NormalizeSaber(obj);
            migrated = ObjectPlugin.Serialize(obj);
            DestroyObject(obj);
            return true;
        }
    }
}
