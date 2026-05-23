using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class SerializedItemWeaponDamageTypeMigration
    {
        private static readonly HashSet<BaseItem> WeaponBaseItemTypes =
            SWLOR.Game.Server.Service.Item.WeaponBaseItemTypes
                .Concat(new[]
                {
                    BaseItem.CreatureSlashWeapon,
                    BaseItem.CreaturePierceWeapon,
                    BaseItem.CreatureBludgeonWeapon,
                    BaseItem.CreatureSlashPierceWeapon,
                })
                .ToHashSet();

        private static readonly Dictionary<string, int> RawDamageEnhancementAmountsByResref = new()
        {
            ["gimp_tooth"] = 2,
            ["imp_melee_1"] = 2,
            ["imp_melee_2"] = 3,
            ["imp_melee_3"] = 4,
            ["imp_melee_4"] = 5,
            ["imp_melee_5"] = 6,
            ["slug_tooth"] = 3,
            ["wen_dmg_phy1"] = 2,
            ["wen_dmg_phy2"] = 3,
            ["wen_dmg_phy3"] = 4,
            ["womprattooth"] = 4,
        };

        private static readonly Dictionary<int, CombatDamageType> LegacyEnhancementDamageTypesBySubType = new()
        {
            [19] = CombatDamageType.Force,
            [20] = CombatDamageType.Fire,
            [21] = CombatDamageType.Poison,
            [22] = CombatDamageType.Electrical,
            [23] = CombatDamageType.Ice,
        };

        private static readonly Dictionary<string, int> DelayCostByResref = new(StringComparer.OrdinalIgnoreCase)
        {
            ["t_longsword"] = 34,
            ["t_katar"] = 32,
            ["t_twinblade"] = 48,
            ["t_knife"] = 32,
            ["t_staff"] = 44,
            ["t_rifle"] = 50,
            ["t_greatsword"] = 50,
            ["t_pistol"] = 40,
            ["t_electroblade"] = 36,
            ["t_spear"] = 46,
            ["t_shuriken"] = 32,
            ["t_twin_elec"] = 48,

            ["byyskwarriorswor"] = 31,
            ["sith_blade"] = 31,
            ["wswss002"] = 31,
        };

        public static bool MigrateSerializedObject(string serializedObject, out string migratedSerializedObject)
        {
            migratedSerializedObject = serializedObject;
            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            var wasMigrated = MigrateObject(obj);
            if (wasMigrated)
                migratedSerializedObject = ObjectPlugin.Serialize(obj);

            DestroyObject(obj);
            return wasMigrated;
        }

        public static bool MigrateObject(uint obj)
        {
            if (!GetIsObjectValid(obj))
                return false;

            var wasMigrated = false;
            var objectType = GetObjectType(obj);

            if (objectType == ObjectType.Item)
                wasMigrated |= MigrateItem(obj);
            else if (objectType == ObjectType.Creature)
                wasMigrated |= MigrateEquippedItems(obj);

            if (GetHasInventory(obj))
            {
                for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                {
                    wasMigrated |= MigrateObject(item);
                }
            }

            return wasMigrated;
        }

        private static bool MigrateEquippedItems(uint creature)
        {
            var wasMigrated = false;

            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                wasMigrated |= MigrateObject(item);
            }

            return wasMigrated;
        }

        private static bool MigrateItem(uint item)
        {
            var wasMigrated = MigrateEnhancementItem(item);
            var baseItem = GetBaseItemType(item);
            if (!WeaponBaseItemTypes.Contains(baseItem))
                return wasMigrated;

            wasMigrated |= MigrateWeaponItem(item);
            wasMigrated |= MigrateWeaponDelayItem(item);
            return wasMigrated;
        }

        private static bool MigrateWeaponItem(uint item)
        {
            var damageProperties = new List<(ItemProperty Property, int SubType, int Value)>();
            var damageTypeProperties = new List<(ItemProperty Property, int SubType)>();

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.DMG)
                {
                    damageProperties.Add((ip, GetItemPropertySubType(ip), GetItemPropertyCostTableValue(ip)));
                }
                else if (type == ItemPropertyType.WeaponDamageType)
                {
                    damageTypeProperties.Add((ip, GetItemPropertySubType(ip)));
                }
            }

            if (damageProperties.Count <= 0)
                return NormalizeDamageTypePropertiesWithoutDmg(item, damageTypeProperties);

            var damageType = ResolveDamageType(damageProperties, damageTypeProperties);
            if (!ShouldMigrate(damageProperties, damageTypeProperties, damageType))
                return false;

            var damage = damageProperties.Sum(x => x.Value);

            foreach (var property in damageProperties)
            {
                RemoveItemProperty(item, property.Property);
            }

            foreach (var property in damageTypeProperties)
            {
                RemoveItemProperty(item, property.Property);
            }

            BiowareXP2.IPSafeAddItemProperty(
                item,
                ItemPropertyCustom(ItemPropertyType.DMG, -1, damage),
                0.0f,
                AddItemPropertyPolicy.IgnoreExisting,
                false,
                false);

            if (!damageType.IsPhysicalDamageType())
            {
                BiowareXP2.IPSafeAddItemProperty(
                    item,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)damageType, 0),
                    0.0f,
                    AddItemPropertyPolicy.IgnoreExisting,
                    false,
                    false);
            }

            return true;
        }

        private static bool MigrateWeaponDelayItem(uint item)
        {
            var targetDelayCost = GetTargetDelayCost(item);
            if (!targetDelayCost.HasValue)
                return false;

            var delayProperties = new List<(ItemProperty Property, int Value)>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.Delay)
                    delayProperties.Add((ip, GetItemPropertyCostTableValue(ip)));
            }

            if (delayProperties.Count == 1 &&
                delayProperties[0].Value == targetDelayCost.Value)
            {
                return false;
            }

            foreach (var property in delayProperties)
            {
                RemoveItemProperty(item, property.Property);
            }

            BiowareXP2.IPSafeAddItemProperty(
                item,
                ItemPropertyCustom(ItemPropertyType.Delay, -1, targetDelayCost.Value),
                0.0f,
                AddItemPropertyPolicy.ReplaceExisting,
                false,
                false);

            return true;
        }

        private static int? GetTargetDelayCost(uint item)
        {
            var resref = GetResRef(item);
            if (!string.IsNullOrWhiteSpace(resref) &&
                DelayCostByResref.TryGetValue(resref, out var resrefDelayCost))
            {
                return resrefDelayCost;
            }

            var baseItem = GetBaseItemType(item);
            return WeaponDelay.GetWeaponDelay(baseItem);
        }

        private static bool MigrateEnhancementItem(uint item)
        {
            var damageEnhancements = new List<(ItemProperty Property, int SubType, int Value, int Index)>();
            var damageTypeProperties = new List<(ItemProperty Property, int SubType, int Index)>();
            var index = 0;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.WeaponEnhancement)
                {
                    var subType = GetItemPropertySubType(ip);
                    if (IsWeaponDamageEnhancementSubType(subType))
                    {
                        damageEnhancements.Add((ip, subType, GetItemPropertyCostTableValue(ip), index));
                    }
                }
                else if (type == ItemPropertyType.WeaponDamageType)
                {
                    damageTypeProperties.Add((ip, GetItemPropertySubType(ip), index));
                }

                index++;
            }

            if (damageEnhancements.Count <= 0)
                return false;

            foreach (var property in damageEnhancements)
            {
                RemoveItemProperty(item, property.Property);
            }

            foreach (var property in damageTypeProperties)
            {
                RemoveItemProperty(item, property.Property);
            }

            foreach (var property in damageEnhancements.OrderBy(x => x.Index))
            {
                var damageType = ResolveEnhancementDamageType(property, damageTypeProperties);
                var amount = damageType.IsPhysicalDamageType()
                    ? ConvertRawEnhancementDamage(item, property.Value)
                    : property.Value;

                BiowareXP2.IPSafeAddItemProperty(
                    item,
                    ItemPropertyCustom(ItemPropertyType.WeaponEnhancement, (int)EnhancementSubType.DMG, amount),
                    0.0f,
                    AddItemPropertyPolicy.IgnoreExisting,
                    false,
                    false);

                if (!damageType.IsPhysicalDamageType())
                {
                    BiowareXP2.IPSafeAddItemProperty(
                        item,
                        ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)damageType, 0),
                        0.0f,
                        AddItemPropertyPolicy.IgnoreExisting,
                        false,
                        false);
                }
            }

            return true;
        }

        private static CombatDamageType ResolveEnhancementDamageType(
            (ItemProperty Property, int SubType, int Value, int Index) enhancement,
            List<(ItemProperty Property, int SubType, int Index)> damageTypeProperties)
        {
            if (LegacyEnhancementDamageTypesBySubType.TryGetValue(enhancement.SubType, out var legacyDamageType))
                return legacyDamageType;

            var pairedDamageType = damageTypeProperties.FirstOrDefault(x => x.Index == enhancement.Index + 1);
            if (TryGetCharacterDamageType(pairedDamageType.SubType, out var damageType))
                return damageType;

            return CombatDamageType.Physical;
        }

        private static bool IsWeaponDamageEnhancementSubType(int subType)
        {
            return subType == (int)EnhancementSubType.DMG ||
                   LegacyEnhancementDamageTypesBySubType.ContainsKey(subType);
        }

        private static int ConvertRawEnhancementDamage(uint item, int amount)
        {
            var resref = GetResRef(item);
            return RawDamageEnhancementAmountsByResref.GetValueOrDefault(resref, amount);
        }

        private static bool NormalizeDamageTypePropertiesWithoutDmg(
            uint item,
            List<(ItemProperty Property, int SubType)> damageTypeProperties)
        {
            if (damageTypeProperties.Count <= 1 &&
                damageTypeProperties.All(x => TryGetCharacterDamageType(x.SubType, out var damageType) && !damageType.IsPhysicalDamageType()))
            {
                return false;
            }

            var damageType = ResolveDamageType(Array.Empty<(ItemProperty Property, int SubType, int Value)>(), damageTypeProperties);

            foreach (var property in damageTypeProperties)
            {
                RemoveItemProperty(item, property.Property);
            }

            if (damageType.IsPhysicalDamageType())
                return damageTypeProperties.Count > 0;

            BiowareXP2.IPSafeAddItemProperty(
                item,
                ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)damageType, 0),
                0.0f,
                AddItemPropertyPolicy.IgnoreExisting,
                false,
                false);

            return true;
        }

        private static bool ShouldMigrate(
            List<(ItemProperty Property, int SubType, int Value)> damageProperties,
            List<(ItemProperty Property, int SubType)> damageTypeProperties,
            CombatDamageType damageType)
        {
            if (damageProperties.Count != 1 ||
                damageProperties.Any(x => x.SubType != -1 && x.SubType != 0))
                return true;

            if (damageType.IsPhysicalDamageType())
                return damageTypeProperties.Count > 0;

            return damageTypeProperties.Count != 1 ||
                   !TryGetCharacterDamageType(damageTypeProperties[0].SubType, out var existingDamageType) ||
                   existingDamageType != damageType;
        }

        private static CombatDamageType ResolveDamageType(
            IEnumerable<(ItemProperty Property, int SubType, int Value)> damageProperties,
            IEnumerable<(ItemProperty Property, int SubType)> damageTypeProperties)
        {
            var typedDamageAmounts = new Dictionary<CombatDamageType, int>();
            foreach (var property in damageProperties)
            {
                if (!TryGetCharacterDamageType(property.SubType, out var damageType))
                    continue;

                typedDamageAmounts[damageType] = typedDamageAmounts.TryGetValue(damageType, out var amount)
                    ? amount + property.Value
                    : property.Value;
            }

            if (typedDamageAmounts.Count > 0)
            {
                var elementalDamageType = typedDamageAmounts
                    .Where(x => x.Key.IsElementalDamageType())
                    .OrderByDescending(x => x.Value)
                    .Select(x => x.Key)
                    .FirstOrDefault();

                if (elementalDamageType != CombatDamageType.Invalid)
                    return elementalDamageType;

                if (typedDamageAmounts.ContainsKey(CombatDamageType.Force))
                    return CombatDamageType.Force;
            }

            var selectedType = CombatDamageType.Physical;
            foreach (var property in damageTypeProperties)
            {
                if (!TryGetCharacterDamageType(property.SubType, out var damageType))
                    continue;

                if (selectedType.IsElementalDamageType())
                    continue;

                if (damageType.IsElementalDamageType())
                {
                    selectedType = damageType;
                }
                else if (selectedType.IsPhysicalDamageType() && damageType == CombatDamageType.Force)
                {
                    selectedType = CombatDamageType.Force;
                }
            }

            return selectedType;
        }

        private static bool TryGetCharacterDamageType(int damageTypeId, out CombatDamageType damageType)
        {
            damageType = CombatDamageType.Invalid;
            if (!Enum.IsDefined(typeof(CombatDamageType), damageTypeId))
                return false;

            damageType = (CombatDamageType)damageTypeId;
            return damageType.IsCharacterDamageType();
        }
    }
}
