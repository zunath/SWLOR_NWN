using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;

namespace SWLOR.Component.Combat.Feature
{
    public class EquipmentStats
    {
        private readonly IEventsPluginService _eventsPlugin;
        private readonly IObjectPluginService _objectPlugin;
        private readonly ICharacterStatService _characterStatService;
        private readonly ICharacterResourceService _characterResourceService;

        public EquipmentStats(
            IEventsPluginService eventsPlugin, 
            IObjectPluginService objectPlugin,
            ICharacterStatService characterStatService,
            ICharacterResourceService characterResourceService)
        {
            _eventsPlugin = eventsPlugin;
            _objectPlugin = objectPlugin;
            _characterStatService = characterStatService;
            _characterResourceService = characterResourceService;
        }
        
        private delegate void ApplyStatChangeDelegate(uint player, uint item, ItemProperty ip, bool isAdding);
        private readonly Dictionary<ItemPropertyType, ApplyStatChangeDelegate> _statChangeActions = new();

        /// <summary>
        /// When the module loads, cache the actions taken for each type of custom item property.
        /// </summary>
        [ScriptHandler<OnModuleLoad>]
        public void RegisterStatActions()
        {
            InitializeStatActions();
        }

        private void InitializeStatActions()
        {
            _statChangeActions[ItemPropertyType.HP] = ApplyHPBonus;
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

        private void ReapplyNPCStat(uint npc, ItemPropertyType ipType, int amount, bool isAdding)
        {
            var skin = GetItemInSlot(InventorySlotType.CreatureArmor, npc);
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
        [ScriptHandler<OnSWLORItemEquipValidBefore>]
        public void ApplyStats()
        {
            ApplyStatsInternal();
        }

        private void ApplyStatsInternal()
        {
            var creature = OBJECT_SELF;
            if (GetIsDM(creature) || GetIsDMPossessed(creature)) return;

            var item = StringToObject(_eventsPlugin.GetEventData("ITEM"));
            var slot = (InventorySlotType)Convert.ToInt32(_eventsPlugin.GetEventData("SLOT"));

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
        [ScriptHandler<OnItemUnequipBefore>]
        public void RemoveStats()
        {
            RemoveStatsInternal();
        }

        private void RemoveStatsInternal()
        {
            var creature = OBJECT_SELF;
            if (GetIsDM(creature) || GetIsDMPossessed(creature)) return;

            var item = StringToObject(_eventsPlugin.GetEventData("ITEM"));

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
        private void ApplyHPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifyMaxHP(creature, amount);
                }
                else
                {
                    _characterStatService.ModifyMaxHP(creature, -amount);
                }
            }
            else
            {
                var skin = GetItemInSlot(InventorySlotType.CreatureArmor, creature);

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
                    _objectPlugin.SetMaxHitPoints(creature, maxHP);
                }

                if (_characterResourceService.GetCurrentHP(creature) > GetMaxHitPoints(creature))
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
        private void ApplyFPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifyMaxFP(creature, amount);
                }
                else
                {
                    _characterStatService.ModifyMaxFP(creature, -amount);
                }
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
        private void ApplyFPRegenBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifyFPRegen(creature, amount);
                }
                else
                {
                    _characterStatService.ModifyFPRegen(creature, -amount);
                }
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
        private void ApplySTMBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifyMaxSTM(creature, amount);
                }
                else
                {
                    _characterStatService.ModifyMaxSTM(creature, -amount);
                }
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
        private void ApplySTMRegenBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifySTMRegen(creature, amount);
                }
                else
                {
                    _characterStatService.ModifySTMRegen(creature, -amount);
                }
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
        private void ApplyAbilityRecastReduction(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifyRecastReduction(creature, amount);
                }
                else
                {
                    _characterStatService.ModifyRecastReduction(creature, -amount);
                }
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
        private void ApplyAttack(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifyAttack(creature, amount);
                }
                else
                {
                    _characterStatService.ModifyAttack(creature, -amount);
                }
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
        private void ApplyForceAttack(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifyForceAttack(creature, amount);
                }
                else
                {
                    _characterStatService.ModifyForceAttack(creature, -amount);
                }
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
        private void ApplyDefense(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);
            var damageType = (CombatDamageType)GetItemPropertySubType(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    if (damageType == CombatDamageType.Force)
                    {
                        _characterStatService.ModifyForceDefense(creature, amount);
                    }
                    else
                    {
                        _characterStatService.ModifyDefense(creature, amount);
                    }
                }
                else
                {
                    if (damageType == CombatDamageType.Force)
                    {
                        _characterStatService.ModifyForceDefense(creature, -amount);
                    }
                    else
                    {
                        _characterStatService.ModifyDefense(creature, -amount);
                    }
                }
            }
            else
            {
                var skin = GetItemInSlot(InventorySlotType.CreatureArmor, creature);
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
        private void ApplyEvasion(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                if (isAdding)
                {
                    _characterStatService.ModifyEvasion(creature, amount);
                }
                else
                {
                    _characterStatService.ModifyEvasion(creature, -amount);
                }
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
        private void ApplyControl(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var subType = GetItemPropertySubType(ip);

                // Types are defined in iprp_crafttype.2da
                if (isAdding)
                {
                    switch (subType)
                    {
                        case 1: // Smithery
                            _characterStatService.ModifyControlSmithery(creature, amount);
                            break;
                        case 2: // Engineering
                            _characterStatService.ModifyControlEngineering(creature, amount);
                            break;
                        case 3: // Fabrication
                            _characterStatService.ModifyControlFabrication(creature, amount);
                            break;
                        case 4: // Agriculture
                            _characterStatService.ModifyControlAgriculture(creature, amount);
                            break;
                        default:
                            throw new Exception($"Unable to determine skill type for {nameof(ApplyControl)}");
                    }
                }
                else
                {
                    switch (subType)
                    {
                        case 1: // Smithery
                            _characterStatService.ModifyControlSmithery(creature, -amount);
                            break;
                        case 2: // Engineering
                            _characterStatService.ModifyControlEngineering(creature, -amount);
                            break;
                        case 3: // Fabrication
                            _characterStatService.ModifyControlFabrication(creature, -amount);
                            break;
                        case 4: // Agriculture
                            _characterStatService.ModifyControlAgriculture(creature, -amount);
                            break;
                        default:
                            throw new Exception($"Unable to determine skill type for {nameof(ApplyControl)}");
                    }
                }
            }
        }

        /// <summary>
        /// Applies or removes craftsmanship on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding craftsmanship, if false we're removing it.</param>
        private void ApplyCraftsmanship(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var subType = GetItemPropertySubType(ip);

                // Types are defined in iprp_crafttype.2da
                if (isAdding)
                {
                    switch (subType)
                    {
                        case 1: // Smithery
                            _characterStatService.ModifyCraftsmanshipSmithery(creature, amount);
                            break;
                        case 2: // Engineering
                            _characterStatService.ModifyCraftsmanshipEngineering(creature, amount);
                            break;
                        case 3: // Fabrication
                            _characterStatService.ModifyCraftsmanshipFabrication(creature, amount);
                            break;
                        case 4: // Agriculture
                            _characterStatService.ModifyCraftsmanshipAgriculture(creature, amount);
                            break;
                    }
                }
                else
                {
                    switch (subType)
                    {
                        case 1: // Smithery
                            _characterStatService.ModifyCraftsmanshipSmithery(creature, -amount);
                            break;
                        case 2: // Engineering
                            _characterStatService.ModifyCraftsmanshipEngineering(creature, -amount);
                            break;
                        case 3: // Fabrication
                            _characterStatService.ModifyCraftsmanshipFabrication(creature, -amount);
                            break;
                        case 4: // Agriculture
                            _characterStatService.ModifyCraftsmanshipAgriculture(creature, -amount);
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// Applies or removes CP bonuses on a creature.
        /// </summary>
        /// <param name="creature">The creature to adjust</param>
        /// <param name="item">The item being equipped or unequipped</param>
        /// <param name="ip">The item property associated with this change</param>
        /// <param name="isAdding">If true, we're adding the CP bonus, if false we're removing it.</param>
        private void ApplyCPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
        {
            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var amount = GetItemPropertyCostTableValue(ip);

            if (GetIsPC(creature))
            {
                var subType = GetItemPropertySubType(ip);

                // Types are defined in iprp_crafttype.2da
                if (isAdding)
                {
                    switch (subType)
                    {
                        case 1: // Smithery
                            _characterStatService.ModifyCPSmithery(creature, amount);
                            break;
                        case 2: // Engineering
                            _characterStatService.ModifyCPEngineering(creature, amount);
                            break;
                        case 3: // Fabrication
                            _characterStatService.ModifyCPFabrication(creature, amount);
                            break;
                        case 4: // Agriculture
                            _characterStatService.ModifyCPAgriculture(creature, amount);
                            break;
                    }
                }
                else
                {
                    switch (subType)
                    {
                        case 1: // Smithery
                            _characterStatService.ModifyCPSmithery(creature, -amount);
                            break;
                        case 2: // Engineering
                            _characterStatService.ModifyCPEngineering(creature, -amount);
                            break;
                        case 3: // Fabrication
                            _characterStatService.ModifyCPFabrication(creature, -amount);
                            break;
                        case 4: // Agriculture
                            _characterStatService.ModifyCPAgriculture(creature, -amount);
                            break;
                    }
                }
            }
        }
    }
}
