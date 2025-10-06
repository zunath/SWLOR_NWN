using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.Events.Events.NWNX;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;

namespace SWLOR.Component.Combat.Feature
{
    public class EquipmentStats
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventsPluginService _eventsPlugin;
        private readonly IObjectPluginService _objectPlugin;

        public EquipmentStats(IDatabaseService db, IServiceProvider serviceProvider, IEventsPluginService eventsPlugin, IObjectPluginService objectPlugin)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _eventsPlugin = eventsPlugin;
            _objectPlugin = objectPlugin;
            
            // Initialize lazy services
            _statService = new Lazy<IStatService>(() => _serviceProvider.GetRequiredService<IStatService>());
        }

        // Lazy-loaded service to break circular dependency
        private readonly Lazy<IStatService> _statService;
        
        private IStatService StatService => _statService.Value;
        
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
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    StatService.AdjustPlayerMaxHP(dbPlayer, creature, amount);
                }
                else
                {
                    StatService.AdjustPlayerMaxHP(dbPlayer, creature, -amount);
                }

                _db.Set(dbPlayer);
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
        private void ApplyFPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustPlayerMaxFP(dbPlayer, amount, creature);
                }
                else
                {
                    StatService.AdjustPlayerMaxFP(dbPlayer, -amount, creature);
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
        private void ApplyFPRegenBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustFPRegen(dbPlayer, amount);
                }
                else
                {
                    StatService.AdjustFPRegen(dbPlayer, -amount);
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
        private void ApplySTMBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustPlayerMaxSTM(dbPlayer, amount, creature);
                }
                else
                {
                    StatService.AdjustPlayerMaxSTM(dbPlayer, -amount, creature);
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
        private void ApplySTMRegenBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustSTMRegen(dbPlayer, amount);
                }
                else
                {
                    StatService.AdjustSTMRegen(dbPlayer, -amount);
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
        private void ApplyAbilityRecastReduction(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustPlayerRecastReduction(dbPlayer, amount);
                }
                else
                {
                    StatService.AdjustPlayerRecastReduction(dbPlayer, -amount);
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
        private void ApplyAttack(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustAttack(dbPlayer, amount);
                }
                else
                {
                    StatService.AdjustAttack(dbPlayer, -amount);
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
        private void ApplyForceAttack(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustForceAttack(dbPlayer, amount);
                }
                else
                {
                    StatService.AdjustForceAttack(dbPlayer, -amount);
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
        private void ApplyDefense(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustDefense(dbPlayer, damageType, amount);
                }
                else
                {
                    StatService.AdjustDefense(dbPlayer, damageType, -amount);
                }

                _db.Set(dbPlayer);
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
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _db.Get<Player>(playerId);

                if (isAdding)
                {
                    StatService.AdjustEvasion(dbPlayer, amount);
                }
                else
                {
                    StatService.AdjustEvasion(dbPlayer, -amount);
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
        private void ApplyControl(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustControl(dbPlayer, skillType, amount);
                }
                else
                {
                    StatService.AdjustControl(dbPlayer, skillType, -amount);
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
        private void ApplyCraftsmanship(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustCraftsmanship(dbPlayer, skillType, amount);
                }
                else
                {
                    StatService.AdjustCraftsmanship(dbPlayer, skillType, -amount);
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
        private void ApplyCPBonus(uint creature, uint item, ItemProperty ip, bool isAdding)
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
                    StatService.AdjustCPBonus(dbPlayer, skillType, amount);
                }
                else
                {
                    StatService.AdjustCPBonus(dbPlayer, skillType, -amount);
                }

                _db.Set(dbPlayer);
            }
        }
    }
}
