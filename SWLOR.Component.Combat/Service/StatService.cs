using Microsoft.Extensions.DependencyInjection;
using NWN.Native.API;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Combat.Service
{
    public class StatService : IStatService
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICreaturePluginService _creaturePlugin;
        private readonly IObjectPluginService _objectPlugin;
        private readonly ICharacterResourceService _characterResourceService;
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<IEnmityService> _enmityService;
        private readonly Lazy<IAbilityService> _abilityService;
        private readonly Lazy<IPerkService> _perkService;
        private readonly Lazy<IStatCalculationService> _statCalculationService;

        public StatService(
            IDatabaseService db,
            IServiceProvider serviceProvider,
            ICreaturePluginService creaturePlugin,
            IObjectPluginService objectPlugin,
            ICharacterResourceService characterResourceService)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _creaturePlugin = creaturePlugin;
            _objectPlugin = objectPlugin;
            _characterResourceService = characterResourceService;
            
            // Initialize lazy services
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _enmityService = new Lazy<IEnmityService>(() => _serviceProvider.GetRequiredService<IEnmityService>());
            _abilityService = new Lazy<IAbilityService>(() => _serviceProvider.GetRequiredService<IAbilityService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
            _statCalculationService = new Lazy<IStatCalculationService>(() => _serviceProvider.GetRequiredService<IStatCalculationService>());
        }
        
        // Lazy-loaded services to break circular dependencies
        private IItemService ItemService => _itemService.Value;
        private IEnmityService EnmityService => _enmityService.Value;
        private IAbilityService AbilityService => _abilityService.Value;
        private IPerkService PerkService => _perkService.Value;
        private IStatCalculationService StatCalculationService => _statCalculationService.Value;
        
        public int BaseHP => 70;
        public int BaseFP => 10;
        public int BaseSTM => 10;

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        public void ApplyPlayerStats()
        {
            ApplyTemporaryPlayerStats();
        }

        /// <summary>
        /// When a player enters the server, apply any temporary stats which do not persist.
        /// </summary>
        private void ApplyTemporaryPlayerStats()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            ApplyPlayerMovementRate(player);
        }






        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        public void ReapplyFoodHP()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            // Player returned after the server restarted. They no longer have the food status effect.
            // Reduce their HP by the amount tracked in the DB.
            if (dbPlayer.TemporaryFoodHP > 0 )//&& !_statusEffectService.HasStatusEffect(player, StatusEffectType.Food))
            {
                // Reduce the player's stored MaxHP by the temporary food HP amount
                dbPlayer.MaxHP -= dbPlayer.TemporaryFoodHP;
                dbPlayer.TemporaryFoodHP = 0;
                _db.Set(dbPlayer);

                // Apply the new max HP to the creature
                const int MaxHPPerLevel = 254;
                var nwnLevelCount = GetLevelByPosition(1, player) +
                                    GetLevelByPosition(2, player) +
                                    GetLevelByPosition(3, player);

                var hpToApply = dbPlayer.MaxHP;

                // All levels must have at least 1 HP, so apply those right now.
                for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
                {
                    hpToApply--;
                    _creaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, 1);
                }

                // Apply the remaining HP.
                if (hpToApply > 0)
                {
                    for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
                    {
                        if (hpToApply > MaxHPPerLevel) // Levels can only contain a max of 255 HP
                        {
                            _creaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, 255);
                            hpToApply -= 254;
                        }
                        else // Remaining value gets set to the level. (<255 hp)
                        {
                            _creaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, hpToApply + 1);
                            break;
                        }
                    }
                }

                // If player's current HP is higher than max, deal the difference in damage to bring them back down to their new maximum.
                var currentHP = _characterResourceService.GetCurrentHP(player);
                var maxHP = GetMaxHitPoints(player);
                if (currentHP > maxHP)
                {
                    SetCurrentHitPoints(player, maxHP);
                }
            }
        }

        /// <summary>
        /// Modifies a player's maximum HP by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPlayerMaxHP(Player entity, uint player, int adjustBy)
        {
            // Note: It's possible for Max HP to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxHP += adjustBy;

            // Note - must call CalculateMaxHP here to account for ability-based increase to HP cap.
            if (entity.HP > StatCalculationService.CalculateMaxHP(player))
                entity.HP = StatCalculationService.CalculateMaxHP(player);

            // Current HP, however, should never drop below zero.
            if (entity.HP < 0)
                entity.HP = 0;
        }

        /// <summary>
        /// Applies the calculated maximum HP to a player.
        /// </summary>
        /// <param name="player">The player to apply max HP to</param>
        public void ApplyPlayerMaxHP(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var maxHP = StatCalculationService.CalculateMaxHP(player);
            SetCurrentHitPoints(player, maxHP);
        }

        /// <summary>
        /// Modifies a player's maximum FP by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player)
        {
            // Note: It's possible for Max FP to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxFP += adjustBy;

            // Note - must call CalculateMaxFP here to account for ability-based increase to FP cap.
            if (entity.FP > StatCalculationService.CalculateMaxFP(player))
                entity.FP = StatCalculationService.CalculateMaxFP(player);

            // Current FP, however, should never drop below zero.
            if (entity.FP < 0)
                entity.FP = 0;
        }

        /// <summary>
        /// Modifies a player's maximum STM by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player)
        {
            // Note: It's possible for Max STM to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.MaxStamina += adjustBy;

            // Note - must call CalculateMaxSTM here to account for ability-based increase to STM cap.
            if (entity.Stamina > StatCalculationService.CalculateMaxSTM(player))
                entity.Stamina = StatCalculationService.CalculateMaxSTM(player);

            // Current STM, however, should never drop below zero.
            if (entity.Stamina < 0)
                entity.Stamina = 0;
        }
        
        public void ApplyPlayerMovementRate(uint player)
        {
            if (GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player))
            {
                _creaturePlugin.SetMovementRate(player, MovementRateType.PC);
            }

            var movementRate = 1.0f;
            if (AbilityService.IsAbilityToggled(player, AbilityToggleType.Dash))
            {
                var level = PerkService.GetPerkLevel(player, PerkType.Dash);
                switch (level)
                {
                    case 1:
                        movementRate += 0.1f; // 10%
                        break;
                    case 2:
                        movementRate += 0.25f; // 25%
                        break;
                }
            }

            for (var effect = GetFirstEffect(player); GetIsEffectValid(effect); effect = GetNextEffect(player))
            {
                var type = GetEffectType(effect);
                float amount;
                if (type == EffectScriptType.MovementSpeedIncrease)
                {
                    amount = GetEffectInteger(effect, 0) - 100;
                    movementRate += amount * 0.01f;
                }
                else if (type == EffectScriptType.MovementSpeedDecrease)
                {
                    amount = GetEffectInteger(effect, 0);
                    movementRate -= amount * 0.01f;
                }
            }

            if (movementRate > 1.5f)
                movementRate = 1.5f;

            _creaturePlugin.SetMovementRateFactor(player, movementRate);
        }

        /// <summary>
        /// Calculates a player's stat based on their skill bonuses, upgrades, etc. and applies the changes to one ability score.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="player">The player object</param>
        /// <param name="ability">The ability score to apply to.</param>
        public void ApplyPlayerStat(Player entity, uint player, AbilityType ability)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (ability == AbilityType.Invalid) return;

            var totalStat = entity.BaseStats[ability] + entity.UpgradedStats[ability];
            _creaturePlugin.SetRawAbilityScore(player, ability, totalStat);
        }

        /// <summary>
        /// Modifies the ability recast reduction of a player by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustPlayerRecastReduction(Player entity, int adjustBy)
        {
            entity.AbilityRecastReduction += adjustBy;
        }

        /// <summary>
        /// Modifies a player's HP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustHPRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for HP Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.HPRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's FP Regen by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustFPRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for FP Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.FPRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's STM Regen by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustSTMRegen(Player entity, int adjustBy)
        {
            // Note: It's possible for STM Regen to drop to a negative number. This is expected to ensure calculations stay in sync.
            // If there are any visual indicators (GUI elements for example) be sure to account for this scenario.
            entity.STMRegen += adjustBy;
        }

        /// <summary>
        /// Modifies a player's defense toward a particular damage type by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="type">The type of damage</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustDefense(Player entity, CombatDamageType type, int adjustBy)
        {
            entity.Defenses[type] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's evasion by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustEvasion(Player entity, int adjustBy)
        {
            entity.Evasion += adjustBy;
        }

        /// <summary>
        /// Modifies a player's attack by a certain amount. Attack affects damage output.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustAttack(Player entity, int adjustBy)
        {
            entity.Attack += adjustBy;
        }

        /// <summary>
        /// Modifies a player's force attack by a certain amount. Force Attack affects damage output.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustForceAttack(Player entity, int adjustBy)
        {
            entity.ForceAttack += adjustBy;
        }

        /// <summary>
        /// Modifies a player's control by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustControl(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.Control.ContainsKey(skillType))
                entity.Control[skillType] = 0;

            entity.Control[skillType] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's craftsmanship by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.Craftsmanship.ContainsKey(skillType))
                entity.Craftsmanship[skillType] = 0;

            entity.Craftsmanship[skillType] += adjustBy;
        }

        /// <summary>
        /// Modifies a player's CP bonus by a certain amount.
        /// This method will not persist the changes so be sure you call _db.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="skillType">The skill type to modify</param>
        /// <param name="adjustBy">The amount to adjust by</param>
        public void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy)
        {
            if (!entity.CPBonus.ContainsKey(skillType))
                entity.CPBonus[skillType] = 0;

            entity.CPBonus[skillType] += adjustBy;
        }
        
        /// <summary>
        /// Retrieves the native stat value of a given type on a particular creature.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="statType">The type of stat to check</param>
        /// <returns>The stat value of a creature based on the ability type</returns>
        public int GetStatValueNative(CNWSCreature creature, AbilityType statType)
        {
            var stat = 0;
            switch (statType)
            {
                case AbilityType.Might:
                    stat = creature.m_pStats.GetSTRStat();
                    break;
                case AbilityType.Perception:
                    stat = creature.m_pStats.GetDEXStat();
                    break;
                case AbilityType.Vitality:
                    stat = creature.m_pStats.GetCONStat();
                    break;
                case AbilityType.Willpower:
                    stat = creature.m_pStats.GetWISStat();
                    break;
                case AbilityType.Agility:
                    stat = creature.m_pStats.GetINTStat();
                    break;
                case AbilityType.Social:
                    stat = creature.m_pStats.GetCHAStat();
                    break;
                default:
                    stat = 0;
                    break;
            }

            // Check for negative modifiers.  A modifier of -2 is represented as 254.
            if (stat > 128) stat -= 256;

            return stat;
        }

        /// <summary>
        /// Applies the total number of attacks per round to a player.
        /// If a valid weapon is passed in the associated mastery perk will also be checked.
        /// </summary>
        /// <param name="creature">The player to apply attacks to</param>
        /// <param name="rightHandWeapon">The weapon equipped to the right hand.</param>
        /// <param name="offHandItem">The off hand item equipped to the left hand.</param>
        public void ApplyAttacksPerRound(uint creature, uint rightHandWeapon, uint offHandItem = OBJECT_INVALID)
        {
            int GetBABForAttacks(int attacks)
            {
                switch (attacks)
                {
                    case 1:
                        return 1;
                    case 2:
                        return 6;
                    case 3:
                        return 11;
                    case 4:
                        return 16;
                    case 5:
                        return 21;
                    case 6:
                        return 26;
                    case 7:
                        return 31;
                    case 8:
                        return 36;
                    case 9:
                        return 41;
                }

                return 1;
            }

            int GetRapidShotBonus(uint pc)
            {
                return PerkService.GetPerkLevel(pc, PerkType.RapidShot);
            }

            int GetFlurryBonus(uint pc)
            {
                return PerkService.GetPerkLevel(pc, PerkType.FlurryStyle);
            }

            int GetShieldBonus(uint pc)
            {
                return PerkService.GetPerkLevel(pc, PerkType.ShieldMaster);
            }

            if (GetIsDM(creature) || GetIsDMPossessed(creature))
                return;

            var itemType = GetBaseItemType(rightHandWeapon);
            var offHandType = GetBaseItemType(offHandItem);
            var numberOfAttacks = 1;
            var perkType = PerkType.Invalid;

            // Martial Arts
            if (ItemService.KatarBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.KatarMastery;
            }
            else if (ItemService.StaffBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.StaffMastery;
                numberOfAttacks += GetFlurryBonus(creature);
            }
            // Ranged (Pistol & Rifle only. Throwing is intentionally excluded from Rapid Shot because they get Doublehand)
            else if (ItemService.PistolBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.PistolMastery;
                numberOfAttacks += GetRapidShotBonus(creature);
            }
            else if (ItemService.ThrowingWeaponBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.ThrowingWeaponMastery;
            }
            else if (ItemService.RifleBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.RifleMastery;
            }
            // One-Handed
            else if (ItemService.VibrobladeBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.VibrobladeMastery;
            }
            else if (ItemService.FinesseVibrobladeBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.FinesseVibrobladeMastery;
            }
            else if (ItemService.LightsaberBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.LightsaberMastery;
            }
            // Two-Handed
            else if (ItemService.HeavyVibrobladeBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.HeavyVibrobladeMastery;
            }
            else if (ItemService.PolearmBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.PolearmMastery;
            }
            else if (ItemService.TwinBladeBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.TwinBladeMastery;
            }
            else if (ItemService.SaberstaffBaseItemTypes.Contains(itemType))
            {
                perkType = PerkType.SaberstaffMastery;
            }

            if (ItemService.ShieldBaseItemTypes.Contains(offHandType)) 
                numberOfAttacks += GetShieldBonus(creature);

            var effectiveMasteryLevel = PerkService.GetPerkLevel(creature, perkType);
            numberOfAttacks += effectiveMasteryLevel;

            // Beast Speed (1-3)
            numberOfAttacks += PerkService.GetPerkLevel(creature, PerkType.BeastSpeed);

            var bab = GetBABForAttacks(numberOfAttacks);
            _creaturePlugin.SetBaseAttackBonus(creature, bab);
        }

        public void ApplyCritModifier(uint player, uint rightHandWeapon)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            var critMod = 0;
            var itemType = GetBaseItemType(rightHandWeapon);
            var offhandType = GetBaseItemType(GetItemInSlot(InventorySlotType.LeftHand, player));
            if (ItemService.OneHandedMeleeItemTypes.Contains(itemType) || ItemService.ThrowingWeaponBaseItemTypes.Contains(itemType))
            {
                if (ItemService.OneHandedMeleeItemTypes.Contains(offhandType))
                    critMod += PerkService.GetPerkLevel(player, PerkType.WailingBlows) * 3; // 15% for WB
                else if(offhandType == BaseItemType.Invalid || ItemService.ShieldBaseItemTypes.Contains(offhandType))
                    critMod += PerkService.GetPerkLevel(player, PerkType.Duelist);
            }

            if(ItemService.ThrowingWeaponBaseItemTypes.Contains(itemType) || ItemService.PistolBaseItemTypes.Contains(itemType))
            {
                critMod += PerkService.GetPerkLevel(player, PerkType.DirtyBlow) * 2; // 10% for DB
            }

            critMod += PerkService.GetPerkLevel(player, PerkType.InnerStrength);

            _creaturePlugin.SetCriticalRangeModifier(player, -critMod, 0, true);
        }

        /// <summary>
        /// Returns the three-character shortened version of ability names.
        /// </summary>
        /// <param name="type">The type of ability to retrieve.</param>
        /// <returns>A three-character shortened version of the ability name.</returns>
        public string GetAbilityNameShort(AbilityType type)
        {
            switch (type)
            {
                default:
                case AbilityType.Invalid:
                    return "INV";
                case AbilityType.Might:
                    return "MGT";
                case AbilityType.Perception:
                    return "PER";
                case AbilityType.Vitality:
                    return "VIT";
                case AbilityType.Agility:
                    return "AGI";
                case AbilityType.Willpower:
                    return "WIL";
                case AbilityType.Social:
                    return "SOC";
            }
        }

        /// <summary>
        /// Calculates the base value for a particular type of saving throw.
        /// This does not factor in stat modifiers.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="type">The type of saving throw.</param>
        /// <param name="offHandItem">The off hand item equipped to the left hand.</param>
        /// <returns>The base saving throw value</returns>
        public int CalculateBaseSavingThrow(uint player, SavingThrowCategoryType type, uint offHandItem = OBJECT_INVALID)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return 0;

            var offHandType = GetBaseItemType(offHandItem);
            var amount = 0;

            if (ItemService.ShieldBaseItemTypes.Contains(offHandType))
            {
                amount += PerkService.GetPerkLevel(player, PerkType.ShieldResistance);
            }

            return amount;
        }

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        public void LoadNPCStats()
        {
            var self = OBJECT_SELF;
            var skin = GetItemInSlot(InventorySlotType.CreatureArmor, self);

            var maxHP = 0;
            for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.NPCHP)
                {
                    maxHP += GetItemPropertyCostTableValue(ip);
                }
            }

            if (maxHP > 30000)
                maxHP = 30000;

            if (maxHP > 0)
            {
                _objectPlugin.SetMaxHitPoints(self, maxHP);
                _objectPlugin.SetCurrentHitPoints(self, maxHP);
            }

            SetLocalInt(self, "FP", StatCalculationService.CalculateMaxFP(self));
            SetLocalInt(self, "STAMINA", StatCalculationService.CalculateMaxSTM(self));
        }

        /// <summary>
        /// Restores an NPC's STM and FP.
        /// </summary>
        public void RestoreNPCStats(bool outOfCombatRegen)
        {
            var self = OBJECT_SELF;
            var maxFP = StatCalculationService.CalculateMaxFP(self);
            var maxSTM = StatCalculationService.CalculateMaxSTM(self);
            var fp = GetLocalInt(self, "FP") + 1;
            var stm = GetLocalInt(self, "STAMINA") + 1;

            if (fp > maxFP)
                fp = maxFP;
            if (stm > maxSTM)
                stm = maxSTM;

            SetLocalInt(self, "FP", fp);
            SetLocalInt(self, "STAMINA", stm);

            if (outOfCombatRegen)
            {
                // If out of combat - restore HP at 10% per tick.
                if (!GetIsInCombat(self) &&
                    !GetIsObjectValid(EnmityService.GetHighestEnmityTarget(self)) &&
                    _characterResourceService.GetCurrentHP(self) < GetMaxHitPoints(self))
                {
                    var hpToHeal = GetMaxHitPoints(self) * 0.1f;
                    ApplyEffectToObject(DurationType.Instant, EffectHeal((int)hpToHeal), self);
                }
            }
        }
    }
}
