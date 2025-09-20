using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Service;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;

namespace SWLOR.Game.Server.Feature
{
    public static class EquipmentStats
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        
        private delegate void ApplyStatChangeDelegate(uint player, uint item, ItemProperty ip, bool isAdding);
        private static readonly Dictionary<ItemPropertyType, ApplyStatChangeDelegate> _statChangeActions = new();

        /// <summary>
        /// When the module loads, cache the actions taken for each type of custom item property.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleLoad)]
        public static void RegisterStatActions()
        {
            _statChangeActions[ItemPropertyType.HPBonus] = ApplyHPBonus;
            _statChangeActions[ItemPropertyType.FP] = ApplyFPBonus;
            _statChangeActions[ItemPropertyType.FPRegen] = ApplyFPRegenBonus;
            _statChangeActions[ItemPropertyType.Stamina] = ApplySTMBonus;
            _statChangeActions[ItemPropertyType.STMRegen] = ApplySTMRegenBonus;
            _statChangeActions[ItemPropertyType.AbilityRecastReduction] = ApplyAbilityRecastReduction;
            _statChangeActions[ItemPropertyType.Attack] = ApplyAttack;
            _statChangeActions[ItemPropertyType.ForceAttack] = ApplyForceAttack;
            _statChangeActions[ItemPropertyType.Defense] = ApplyDefense;
            _statChangeActions[ItemPropertyType.Evasion] = ApplyEvasion;
            _statChangeActions[ItemPropertyType.Control] = ApplyControl;
            _statChangeActions[ItemPropertyType.Craftsmanship] = ApplyCraftsmanship;
            _statChangeActions[ItemPropertyType.CPBonus] = ApplyCPBonus;
        }

        private static void ReapplyNPCStat(uint npc, ItemPropertyType ipType, int amount, bool isAdding)
        {
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, npc);
            var value = 0;

            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                var type = GetItemPropertyType(ip);
                if (type == ipType)
                {
                    value += GetItemPropertyCostTableValue(ip);
                }
            }

            if (isAdding)
            {
                value += amount;
            }
            else
            {
                value -= amount;
            }

            if (value <= 0)
            {
                BiowareXP2.IPRemoveMatchingItemProperties(skin, ipType, DurationType.Invalid, -1);
            }
            else
            {
                var itemProperty = ItemPropertyCustom(ipType, -1, value);
                BiowareXP2.IPSafeAddItemProperty(skin, itemProperty, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
            }
        }

        /// <summary>
        /// When an item is equipped, if it has any custom status, apply them now.
        /// This should be run in the "after" event because any restrictions should be checked first.
        /// </summary>
        [ScriptHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        public static void ApplyStats()
        {
            var creature = OBJECT_SELF;
            if (GetIsDM(creature) || GetIsDMPossessed(creature)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));

            // The unequip event doesn't fire if an item is being swapped out.
            // If there's an item in the slot, run the stat removals first.
            var existingItemInSlot = GetItemInSlot(slot, creature);
            if (GetIsObjectValid(existingItemInSlot))
            {
                for (var ip = GetFirstItemProperty(existingItemInSlot); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(existingItemInSlot))
                {
                    var type = GetItemPropertyType(ip);
                    if (!_statChangeActions.ContainsKey(type)) continue;
                    _statChangeActions[type](creature, existingItemInSlot, ip, false);
                }
            }

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (!_statChangeActions.ContainsKey(type)) continue;
                _statChangeActions[type](creature, item, ip, true);
            }
        }

        /// <summary>
        /// When an item is unequipped, if it has any custom stats, remove them now.
        /// </summary>
        [ScriptHandler(ScriptName.OnItemUnequipBefore)]
        public static void RemoveStats()
        {
            var creature = OBJECT_SELF;
            if (GetIsDM(creature) || GetIsDMPossessed(creature)) return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (!_statChangeActions.ContainsKey(type)) continue;
                _statChangeActions[type](creature, item, ip, false);
            }
        }

        /// <summary>
        /// Applies or removes an HP bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change.</param>
        /// <param name="isAdding">If true, we're adding the HP, if false we're removing it</param>
        private static void ApplyHPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustPlayerMaxHP(dbPlayer, creature, amount);
                }
                else
                {
                    Stat.AdjustPlayerMaxHP(dbPlayer, creature, -amount);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);

                var maxHP = 0;
                for (var ipHP = GetFirstItemProperty(skin); GetIsItemPropertyValid(ipHP); ipHP = GetNextItemProperty(skin))
                {
                    if (GetItemPropertyType(ipHP) == ItemPropertyType.NPCHP)
                    {
                        maxHP += GetItemPropertyCostTableValue(ipHP);
                    }
                }

                if (isAdding)
                {
                    maxHP += amount;
                }
                else
                {
                    maxHP -= amount;
                }

                var newIP = ItemPropertyCustom(ItemPropertyType.NPCHP, -1, maxHP);
                BiowareXP2.IPSafeAddItemProperty(skin, newIP, 0f, AddItemPropertyPolicy.ReplaceExisting, true, true);

                if (maxHP > 0)
                {
                    ObjectPlugin.SetMaxHitPoints(creature, maxHP);
                }

                if (GetCurrentHitPoints(creature) > GetMaxHitPoints(creature))
                {
                    SetCurrentHitPoints(creature, GetMaxHitPoints(creature));
                }
            }
        }

        /// <summary>
        /// Applies or removes an FP bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP, if false we're removing it</param>
        private static void ApplyFPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustPlayerMaxFP(dbPlayer, amount, creature);
                }
                else
                {
                    Stat.AdjustPlayerMaxFP(dbPlayer, -amount, creature);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                ReapplyNPCStat(creature, ItemPropertyType.FP, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes an FP Regen bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP Regen, if false we're removing it</param>
        private static void ApplyFPRegenBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustFPRegen(dbPlayer, amount);
                }
                else
                {
                    Stat.AdjustFPRegen(dbPlayer, -amount);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                ReapplyNPCStat(creature, ItemPropertyType.FPRegen, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes a STM bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP, if false we're removing it</param>
        private static void ApplySTMBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustPlayerMaxSTM(dbPlayer, amount, creature);
                }
                else
                {
                    Stat.AdjustPlayerMaxSTM(dbPlayer, -amount, creature);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                ReapplyNPCStat(creature, ItemPropertyType.Stamina, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes a STM Regen bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the FP Regen, if false we're removing it</param>
        private static void ApplySTMRegenBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustSTMRegen(dbPlayer, amount);
                }
                else
                {
                    Stat.AdjustSTMRegen(dbPlayer, -amount);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                ReapplyNPCStat(creature, ItemPropertyType.STMRegen, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes an ability recast reduction bonus on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the reduction, if false we're removing it.</param>
        private static void ApplyAbilityRecastReduction(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustPlayerRecastReduction(dbPlayer, amount);
                }
                else
                {
                    Stat.AdjustPlayerRecastReduction(dbPlayer, -amount);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                ReapplyNPCStat(creature, ItemPropertyType.AbilityRecastReduction, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes attack bonuses. This affects the end result of the damage calculation (not to be confused with NWN's Attack Bonus property which is accuracy).
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the attack, if false we're removing it.</param>
        private static void ApplyAttack(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustAttack(dbPlayer, amount);
                }
                else
                {
                    Stat.AdjustAttack(dbPlayer, -amount);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                ReapplyNPCStat(creature, ItemPropertyType.Attack, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes force attack bonuses. This affects the end result of the damage calculation (not to be confused with NWN's Attack Bonus property which is accuracy).
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the force attack, if false we're removing it.</param>
        private static void ApplyForceAttack(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustForceAttack(dbPlayer, amount);
                }
                else
                {
                    Stat.AdjustForceAttack(dbPlayer, -amount);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                ReapplyNPCStat(creature, ItemPropertyType.ForceAttack, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes defense toward a particular damage type on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the defense, if false we're removing it.</param>
        private static void ApplyDefense(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var damageType = (CombatDamageType)GetItemPropertySubType(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustDefense(dbPlayer, damageType, amount);
                }
                else
                {
                    Stat.AdjustDefense(dbPlayer, damageType, -amount);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                var skin = GetItemInSlot(InventorySlot.CreatureArmor, creature);
                var value = 0;
                for (var defenseIP = GetFirstItemProperty(skin); GetIsItemPropertyValid(defenseIP); defenseIP = GetNextItemProperty(skin))
                {
                    if (GetItemPropertyType(defenseIP) == ItemPropertyType.Defense)
                    {
                        var subType = (CombatDamageType)GetItemPropertySubType(defenseIP);

                        if (subType == damageType)
                        {
                            value += GetItemPropertyCostTableValue(defenseIP);
                        }
                    }
                }

                if (isAdding)
                {
                    value += amount;
                }
                else
                {
                    value -= amount;
                }

                if (value <= 0)
                {
                    BiowareXP2.IPRemoveMatchingItemProperties(skin, ItemPropertyType.Defense, DurationType.Invalid, (int)damageType);
                }
                else
                {
                    var newIP = ItemPropertyCustom(ItemPropertyType.Defense, (int)damageType, value);
                    BiowareXP2.IPSafeAddItemProperty(skin, newIP, 0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
                }
            }
        }

        /// <summary>
        /// Applies or removes evasion on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the evasion, if false we're removing it.</param>
        private static void ApplyEvasion(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    Stat.AdjustEvasion(dbPlayer, amount);
                }
                else
                {
                    Stat.AdjustEvasion(dbPlayer, -amount);
                }

                _db.Set(dbPlayer);
            }
            else
            {
                ReapplyNPCStat(creature, ItemPropertyType.Evasion, amount, isAdding);
            }
        }

        /// <summary>
        /// Applies or removes control on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding control, if false we're removing it.</param>
        private static void ApplyControl(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);
                var subType = GetItemPropertySubType(ip);
                var skillType = SkillType.Invalid;

                // Types are defined in iprp_crafttype.2da
                switch (subType)
                {
                    case 1:
                        skillType = SkillType.Smithery;
                        break;
                    case 2:
                        skillType = SkillType.Engineering;
                        break;
                    case 3:
                        skillType = SkillType.Fabrication;
                        break;
                    case 4:
                        skillType = SkillType.Agriculture;
                        break;
                }

                if (skillType == SkillType.Invalid)
                {
                    throw new Exception($"Unable to determine skill type for {nameof(ApplyControl)}");
                }

                if (isAdding)
                {
                    Stat.AdjustControl(dbPlayer, skillType, amount);
                }
                else
                {
                    Stat.AdjustControl(dbPlayer, skillType, -amount);
                }

                _db.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Applies or removes craftsmanship on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding craftsmanship, if false we're removing it.</param>
        private static void ApplyCraftsmanship(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);
                var subType = GetItemPropertySubType(ip);
                var skillType = SkillType.Invalid;

                // Types are defined in iprp_crafttype.2da
                switch (subType)
                {
                    case 1:
                        skillType = SkillType.Smithery;
                        break;
                    case 2:
                        skillType = SkillType.Engineering;
                        break;
                    case 3:
                        skillType = SkillType.Fabrication;
                        break;
                    case 4:
                        skillType = SkillType.Agriculture;
                        break;
                }
                if (isAdding)
                {
                    Stat.AdjustCraftsmanship(dbPlayer, skillType, amount);
                }
                else
                {
                    Stat.AdjustCraftsmanship(dbPlayer, skillType, -amount);
                }

                _db.Set(dbPlayer);
            }
        }
        /// <summary>
        /// Applies or removes CP bonuses on a player.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the CP bonus, if false we're removing it.</param>
        private static void ApplyCPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);
                var subType = GetItemPropertySubType(ip);
                var skillType = SkillType.Invalid;

                // Types are defined in iprp_crafttype.2da
                switch (subType)
                {
                    case 1:
                        skillType = SkillType.Smithery;
                        break;
                    case 2:
                        skillType = SkillType.Engineering;
                        break;
                    case 3:
                        skillType = SkillType.Fabrication;
                        break;
                    case 4:
                        skillType = SkillType.Agriculture;
                        break;
                }
                if (isAdding)
                {
                    Stat.AdjustCPBonus(dbPlayer, skillType, amount);
                }
                else
                {
                    Stat.AdjustCPBonus(dbPlayer, skillType, -amount);
                }

                _db.Set(dbPlayer);
            }
        }
    }
}
