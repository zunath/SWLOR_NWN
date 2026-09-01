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
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class SerializedItemWeaponDamageTypeMigration
    {
        private const string BlueprintRecipeIdVariable = "BLUEPRINT_RECIPE_ID";
        private static readonly HashSet<BaseItem> WeaponBaseItemTypes = BuildWeaponBaseItemTypes();

        private static readonly WeaponDamageScale[] WeaponDamageScales =
        {
            new(SWLOR.Game.Server.Service.Item.VibrobladeBaseItemTypes, new[] { 6, 10, 15, 19, 24 }, new[] { 5, 9, 13, 17, 21 }),
            new(SWLOR.Game.Server.Service.Item.VibroknifeBaseItemTypes, new[] { 5, 9, 14, 18, 22 }, new[] { 5, 8, 12, 16, 19 }),
            new(SWLOR.Game.Server.Service.Item.HeavyVibrobladeBaseItemTypes, new[] { 8, 16, 29, 37, 43 }, new[] { 8, 15, 27, 34, 40 }),
            new(SWLOR.Game.Server.Service.Item.SpearBaseItemTypes, new[] { 8, 16, 29, 37, 43 }, new[] { 7, 14, 25, 32, 38 }),
            new(SWLOR.Game.Server.Service.Item.TwinBladeBaseItemTypes, new[] { 8, 13, 18, 22, 27 }, new[] { 7, 12, 16, 20, 25 }),
            new(SWLOR.Game.Server.Service.Item.SaberstaffBaseItemTypes, new[] { 8, 13, 18, 22, 27 }, new[] { 7, 12, 16, 20, 25 }),
            new(SWLOR.Game.Server.Service.Item.KatarBaseItemTypes, new[] { 8, 10, 13, 15, 19 }, new[] { 7, 9, 11, 13, 16 }),
            new(SWLOR.Game.Server.Service.Item.StaffBaseItemTypes, new[] { 6, 10, 15, 19, 24 }, new[] { 5, 9, 13, 17, 21 }),
            new(SWLOR.Game.Server.Service.Item.PistolBaseItemTypes, new[] { 6, 10, 15, 19, 24 }, new[] { 5, 9, 13, 16, 20 }),
            new(SWLOR.Game.Server.Service.Item.ThrowingWeaponBaseItemTypes, new[] { 4, 7, 11, 14, 17 }, new[] { 4, 6, 9, 12, 15 }),
            new(SWLOR.Game.Server.Service.Item.RifleBaseItemTypes, new[] { 8, 15, 27, 34, 39 }, new[] { 7, 14, 25, 31, 36 }),
            new(new[] { BaseItem.Lightsaber }, new[] { 8, 12, 16, 20, 24 }, new[] { 6, 10, 14, 17, 21 }),
            new(new[] { BaseItem.Electroblade }, new[] { 6, 10, 15, 19, 24 }, new[] { 5, 9, 13, 17, 21 }),
        };

        private static readonly WeaponDamageScale TrainingSaberDamageScale =
            new(new[] { BaseItem.Lightsaber }, new[] { 6, 10, 15, 19, 24 }, new[] { 5, 9, 13, 17, 21 });

        private static readonly HashSet<string> TrainingSaberDamageResrefs = new(StringComparer.OrdinalIgnoreCase)
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
        };

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

        private static readonly Dictionary<string, ItemPropertyAttackDelay> DelayByResref = new(StringComparer.OrdinalIgnoreCase)
        {
            ["t_longsword"] = ItemPropertyAttackDelay.Delay230,
            ["t_katar"] = ItemPropertyAttackDelay.Delay220,
            ["t_twinblade"] = ItemPropertyAttackDelay.Delay290,
            ["t_knife"] = ItemPropertyAttackDelay.Delay220,
            ["t_staff"] = ItemPropertyAttackDelay.Delay270,
            ["t_rifle"] = ItemPropertyAttackDelay.Delay300,
            ["t_greatsword"] = ItemPropertyAttackDelay.Delay300,
            ["t_pistol"] = ItemPropertyAttackDelay.Delay250,
            ["t_electroblade"] = ItemPropertyAttackDelay.Delay240,
            ["t_spear"] = ItemPropertyAttackDelay.Delay280,
            ["t_shuriken"] = ItemPropertyAttackDelay.Delay220,
            ["t_twin_elec"] = ItemPropertyAttackDelay.Delay290,

            ["byyskwarriorswor"] = ItemPropertyAttackDelay.Delay220,
            ["sith_blade"] = ItemPropertyAttackDelay.Delay220,
            ["wswss002"] = ItemPropertyAttackDelay.Delay220,
        };

        private static HashSet<BaseItem> BuildWeaponBaseItemTypes()
        {
            return new[]
                {
                    SWLOR.Game.Server.Service.Item.VibrobladeBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.KatarBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.TwinBladeBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.VibroknifeBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.StaffBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.RifleBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.HeavyVibrobladeBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.PistolBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.LightsaberBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.SpearBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.ThrowingWeaponBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.SaberstaffBaseItemTypes,
                    SWLOR.Game.Server.Service.Item.CreatureBaseItemTypes,
                }
                .SelectMany(x => x)
                .ToHashSet();
        }

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

            var hasTargetWeaponDelay = HasTargetWeaponDelay(item);
            wasMigrated |= MigrateWeaponItem(item);
            if (!hasTargetWeaponDelay)
                wasMigrated |= MigrateWeaponDamageAmountItem(item);

            wasMigrated |= MigrateWeaponDelayItem(item);
            return wasMigrated;
        }

        private static bool MigrateWeaponItem(uint item)
        {
            var baseItem = GetBaseItemType(item);
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
                return NormalizeDamageTypePropertiesWithoutDmg(item, baseItem, damageTypeProperties);

            var damageType = ResolveDamageType(damageProperties, damageTypeProperties);
            if (!ShouldMigrate(baseItem, damageProperties, damageTypeProperties, damageType))
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

            if (!ShouldRemoveWeaponDamageType(baseItem) &&
                !damageType.IsPhysicalDamageType())
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

        private static bool MigrateWeaponDamageAmountItem(uint item)
        {
            var baseItem = GetBaseItemType(item);
            var resref = GetResRef(item);
            if (!TryGetWeaponDamageScale(baseItem, resref, out var scale))
                return false;

            var damageProperties = new List<(ItemProperty Property, int Value)>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.DMG)
                    damageProperties.Add((ip, GetItemPropertyCostTableValue(ip)));
            }

            if (damageProperties.Count <= 0)
                return false;

            var currentDamage = damageProperties.Sum(x => x.Value);
            var targetDamage = CalculateScaledWeaponDamage(currentDamage, scale.OldDamage, scale.NewDamage);
            if (damageProperties.Count == 1 &&
                damageProperties[0].Value == targetDamage)
            {
                return false;
            }

            foreach (var property in damageProperties)
            {
                RemoveItemProperty(item, property.Property);
            }

            BiowareXP2.IPSafeAddItemProperty(
                item,
                ItemPropertyCustom(ItemPropertyType.DMG, -1, targetDamage),
                0.0f,
                AddItemPropertyPolicy.ReplaceExisting,
                false,
                false);

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
                delayProperties[0].Value == (int)targetDelayCost.Value)
            {
                return false;
            }

            foreach (var property in delayProperties)
            {
                RemoveItemProperty(item, property.Property);
            }

            BiowareXP2.IPSafeAddItemProperty(
                item,
                ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)targetDelayCost.Value),
                0.0f,
                AddItemPropertyPolicy.ReplaceExisting,
                false,
                false);

            return true;
        }

        private static bool HasTargetWeaponDelay(uint item)
        {
            var targetDelayCost = GetTargetDelayCost(item);
            if (!targetDelayCost.HasValue)
                return false;

            var delayPropertyCount = 0;
            var hasTargetDelay = false;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.Delay)
                    continue;

                delayPropertyCount++;
                hasTargetDelay |= GetItemPropertyCostTableValue(ip) == (int)targetDelayCost.Value;
            }

            return delayPropertyCount == 1 && hasTargetDelay;
        }

        private static ItemPropertyAttackDelay? GetTargetDelayCost(uint item)
        {
            var resref = GetResRef(item);
            if (!string.IsNullOrWhiteSpace(resref) &&
                DelayByResref.TryGetValue(resref, out var resrefDelay))
            {
                return resrefDelay;
            }

            var baseItem = GetBaseItemType(item);
            return WeaponDelay.GetWeaponDelay(baseItem);
        }

        private static bool TryGetWeaponDamageScale(BaseItem baseItem, string resref, out WeaponDamageScale scale)
        {
            if (!string.IsNullOrWhiteSpace(resref) &&
                TrainingSaberDamageResrefs.Contains(resref))
            {
                scale = TrainingSaberDamageScale;
                return true;
            }

            foreach (var candidate in WeaponDamageScales)
            {
                if (!candidate.BaseItems.Contains(baseItem))
                    continue;

                scale = candidate;
                return true;
            }

            scale = null;
            return false;
        }

        private static int CalculateScaledWeaponDamage(int damage, IReadOnlyList<int> oldDamage, IReadOnlyList<int> newDamage)
        {
            if (damage <= oldDamage[0])
            {
                return InterpolateWeaponDamage(
                    damage,
                    oldDamage[0],
                    oldDamage[1],
                    newDamage[0],
                    newDamage[1]);
            }

            for (var index = 0; index < oldDamage.Count - 1; index++)
            {
                if (damage > oldDamage[index + 1])
                    continue;

                return InterpolateWeaponDamage(
                    damage,
                    oldDamage[index],
                    oldDamage[index + 1],
                    newDamage[index],
                    newDamage[index + 1]);
            }

            var last = oldDamage.Count - 1;
            return InterpolateWeaponDamage(
                damage,
                oldDamage[last - 1],
                oldDamage[last],
                newDamage[last - 1],
                newDamage[last]);
        }

        private static int InterpolateWeaponDamage(
            int damage,
            int oldLow,
            int oldHigh,
            int newLow,
            int newHigh)
        {
            var oldRange = oldHigh - oldLow;
            if (oldRange == 0)
                return Math.Max(1, newLow);

            var scaled = newLow + (damage - oldLow) * (newHigh - newLow) / (double)oldRange;
            return Math.Max(1, (int)Math.Round(scaled, MidpointRounding.AwayFromZero));
        }

        private static bool MigrateEnhancementItem(uint item)
        {
            var damageEnhancements = new List<(ItemProperty Property, int SubType, int Value, int Index)>();
            var damageTypeProperties = new List<(ItemProperty Property, int SubType, int Index)>();
            var isBlueprint = GetLocalInt(item, BlueprintRecipeIdVariable) > 0;
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
                else if (type == ItemPropertyType.Blueprint)
                {
                    isBlueprint = true;
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

            var selectedEnhancements = damageEnhancements
                .OrderBy(x => x.Index)
                .Select(x => new EnhancementDamageProperty(
                    x.Value,
                    x.Index,
                    ResolveEnhancementDamageType(x, damageTypeProperties)))
                .ToList();

            if (isBlueprint)
                selectedEnhancements = SelectBlueprintDamageEnhancements(selectedEnhancements);

            foreach (var property in selectedEnhancements)
            {
                var amount = property.DamageType.IsPhysicalDamageType()
                    ? ConvertRawEnhancementDamage(item, property.Value)
                    : property.Value;

                BiowareXP2.IPSafeAddItemProperty(
                    item,
                    ItemPropertyCustom(ItemPropertyType.WeaponEnhancement, (int)EnhancementSubType.DMG, amount),
                    0.0f,
                    AddItemPropertyPolicy.IgnoreExisting,
                    false,
                    false);

                if (!property.DamageType.IsPhysicalDamageType())
                {
                    BiowareXP2.IPSafeAddItemProperty(
                        item,
                        ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)property.DamageType, 0),
                        0.0f,
                        AddItemPropertyPolicy.IgnoreExisting,
                        false,
                        false);
                }
            }

            return true;
        }

        private static List<EnhancementDamageProperty> SelectBlueprintDamageEnhancements(
            List<EnhancementDamageProperty> damageEnhancements)
        {
            var elementalDamageEnhancements = damageEnhancements
                .Where(damageEnhancement => damageEnhancement.DamageType.IsElementalDamageType())
                .ToList();
            var elementalDamageTypes = elementalDamageEnhancements
                .Select(damageEnhancement => damageEnhancement.DamageType)
                .Distinct()
                .ToList();

            if (elementalDamageTypes.Count <= 1)
                return damageEnhancements;

            var selectedElementalDamageType = elementalDamageTypes[
                SWLOR.Game.Server.Service.Random.Next(elementalDamageTypes.Count)];
            return damageEnhancements
                .Where(damageEnhancement =>
                    !damageEnhancement.DamageType.IsElementalDamageType() ||
                    damageEnhancement.DamageType == selectedElementalDamageType)
                .ToList();
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
            BaseItem baseItem,
            List<(ItemProperty Property, int SubType)> damageTypeProperties)
        {
            if (ShouldRemoveWeaponDamageType(baseItem) &&
                damageTypeProperties.Count > 0)
            {
                foreach (var property in damageTypeProperties)
                {
                    RemoveItemProperty(item, property.Property);
                }

                return true;
            }

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
            BaseItem baseItem,
            List<(ItemProperty Property, int SubType, int Value)> damageProperties,
            List<(ItemProperty Property, int SubType)> damageTypeProperties,
            CombatDamageType damageType)
        {
            if (ShouldRemoveWeaponDamageType(baseItem) &&
                damageTypeProperties.Count > 0)
            {
                return true;
            }

            if (damageProperties.Count != 1 ||
                damageProperties.Any(x => x.SubType != -1 && x.SubType != 0))
                return true;

            if (damageType.IsPhysicalDamageType())
                return damageTypeProperties.Count > 0;

            return damageTypeProperties.Count != 1 ||
                   !TryGetCharacterDamageType(damageTypeProperties[0].SubType, out var existingDamageType) ||
                   existingDamageType != damageType;
        }

        private static bool ShouldRemoveWeaponDamageType(BaseItem baseItem)
        {
            return SWLOR.Game.Server.Service.Item.VibroknifeBaseItemTypes.Contains(baseItem);
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

        private sealed record WeaponDamageScale(
            IReadOnlyCollection<BaseItem> BaseItems,
            IReadOnlyList<int> OldDamage,
            IReadOnlyList<int> NewDamage);

        private sealed record EnhancementDamageProperty(
            int Value,
            int Index,
            CombatDamageType DamageType);
    }
}
