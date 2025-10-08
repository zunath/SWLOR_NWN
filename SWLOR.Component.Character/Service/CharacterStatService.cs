using SWLOR.Component.Character.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.Events;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Repositories;
namespace SWLOR.Component.Character.Service
{
    internal class CharacterStatService : ICharacterStatService
    {
        private readonly IPlayerRepository _playerRepo;
        private readonly IEventAggregator _eventAggregator;
        public CharacterStatService(
            IPlayerRepository playerRepo,
            IEventAggregator eventAggregator)
        {
            _playerRepo = playerRepo;
            _eventAggregator = eventAggregator;
            // Initialize the inverse mapping dictionary
            _itemPropertyToStat = new Dictionary<ItemPropertyType, StatType>();
            foreach (var (stat, ip) in _statToItemProperty)
            {
                _itemPropertyToStat[ip] = stat;
            }
        }
        private readonly Dictionary<uint, StatGroup> _npcStats = new();
        private readonly Dictionary<StatType, ItemPropertyType> _statToItemProperty = new()
        {
            {StatType.MaxHP, ItemPropertyType.HP },
            {StatType.MaxFP, ItemPropertyType.FP },
            {StatType.MaxSTM, ItemPropertyType.Stamina },
            {StatType.HPRegen, ItemPropertyType.HPRegen },
            {StatType.FPRegen, ItemPropertyType.FPRegen },
            {StatType.STMRegen, ItemPropertyType.STMRegen },
            {StatType.RecastReduction, ItemPropertyType.AbilityRecastReduction },
            {StatType.Defense, ItemPropertyType.Defense },
            {StatType.Evasion, ItemPropertyType.Evasion },
            {StatType.Accuracy, ItemPropertyType.AccuracyStat },
            {StatType.Attack, ItemPropertyType.Attack },
            {StatType.ForceAttack, ItemPropertyType.ForceAttack },
            {StatType.Might, ItemPropertyType.Might },
            {StatType.Perception, ItemPropertyType.Perception },
            {StatType.Vitality, ItemPropertyType.Vitality },
            {StatType.Agility, ItemPropertyType.Agility },
            {StatType.Willpower, ItemPropertyType.Willpower },
            {StatType.Social, ItemPropertyType.Social },
            {StatType.ShieldDeflection, ItemPropertyType.ShieldDeflection },
            {StatType.AttackDeflection, ItemPropertyType.AttackDeflection },
            {StatType.CriticalRate, ItemPropertyType.CriticalRate },
            {StatType.Enmity, ItemPropertyType.Enmity },
            {StatType.Haste, ItemPropertyType.Haste },
            {StatType.Slow, ItemPropertyType.Slow },
            {StatType.AccuracyModifier, ItemPropertyType.AccuracyModifier },
            {StatType.RecastReductionModifier, ItemPropertyType.RecastReductionModifier },
            {StatType.DefenseBypassModifier, ItemPropertyType.DefenseBypassModifier },
            {StatType.HealingModifier, ItemPropertyType.HealingModifier },
            {StatType.FPRestoreOnHit, ItemPropertyType.FPRestoreOnHit },
            {StatType.DefenseModifier, ItemPropertyType.DefenseModifier },
            {StatType.ForceDefenseModifier, ItemPropertyType.ForceDefenseModifier },
            {StatType.AttackModifier, ItemPropertyType.AttackModifier },
            {StatType.ForceAttackModifier, ItemPropertyType.ForceAttackModifier },
            {StatType.EvasionModifier, ItemPropertyType.EvasionModifier },
            {StatType.Level, ItemPropertyType.NPCLevel },
        };
        private readonly Dictionary<ItemPropertyType, StatType> _itemPropertyToStat;
        /// <inheritdoc />
        public void RegisterNPC(uint npc)
        {
            if (!_npcStats.ContainsKey(npc))
                _npcStats[npc] = LoadNPCStats(npc);
        }
        public void UnregisterNPC(uint npc)
        {
            _npcStats.Remove(npc);
        }
        private StatGroup LoadNPCStats(uint npc)
        {
            var statGroup = new StatGroup();
            var skin = GetItemInSlot(InventorySlotType.CreatureArmor, npc);
            if (GetIsObjectValid(skin))
            {
                for (var ip = GetFirstItemProperty(skin); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(skin))
                {
                    var type = GetItemPropertyType(ip);
                    var value = GetItemPropertyCostTableValue(ip);
                    if (_itemPropertyToStat.ContainsKey(type))
                    {
                        var stat = _itemPropertyToStat[type];
                        statGroup.SetStat(stat, value);
                    }
                }
            }
            var might = statGroup.GetStat(AbilityType.Might) + GetAbilityScore(npc, AbilityType.Might);
            statGroup.SetStat(StatType.Might, might);
            var perception = statGroup.GetStat(AbilityType.Perception) + GetAbilityScore(npc, AbilityType.Perception);
            statGroup.SetStat(StatType.Perception, perception);
            var vitality = statGroup.GetStat(AbilityType.Vitality) + GetAbilityScore(npc, AbilityType.Vitality);
            statGroup.SetStat(StatType.Vitality, vitality);
            var agility = statGroup.GetStat(AbilityType.Agility) + GetAbilityScore(npc, AbilityType.Agility);
            statGroup.SetStat(StatType.Agility, agility);
            var willpower = statGroup.GetStat(AbilityType.Willpower) + GetAbilityScore(npc, AbilityType.Willpower);
            statGroup.SetStat(StatType.Willpower, willpower);
            var social = statGroup.GetStat(AbilityType.Social) + GetAbilityScore(npc, AbilityType.Social);
            statGroup.SetStat(StatType.Social, social);
            return statGroup;
        }
        /// <inheritdoc />
        public int GetStat(uint creature, StatType stat)
        {
            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _playerRepo.GetById(playerId);
                return dbPlayer.Stats.ContainsKey(stat)
                    ? dbPlayer.Stats[stat]
                    : 0;
            }
            else
            {
                var statGroup = _npcStats[creature];
                return statGroup.GetStat(stat);
            }
        }
        private void SetStat(uint creature, StatType stat, int value, IEvent evt)
        {
            if (GetIsPC(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _playerRepo.GetById(playerId);
                dbPlayer.Stats[stat] = value;
                _playerRepo.Save(dbPlayer);
            }
            else
            {
                var statGroup = _npcStats[creature];
                statGroup.SetStat(stat, value);
            }
            _eventAggregator.Publish(evt, creature);
        }
        /// <inheritdoc />
        public void SetMaxHP(uint creature, int value)
        {
            SetStat(creature, StatType.MaxHP, value, new OnCharacterMaxHPChanged());
        }
        /// <inheritdoc />
        public void SetMaxFP(uint creature, int value)
        {
            SetStat(creature, StatType.MaxFP, value, new OnCharacterMaxFPChanged());
        }
        /// <inheritdoc />
        public void SetMaxSTM(uint creature, int value)
        {
            SetStat(creature, StatType.MaxSTM, value, new OnCharacterMaxSTMChanged());
        }
        /// <inheritdoc />
        public void SetHPRegen(uint creature, int value)
        {
            SetStat(creature, StatType.HPRegen, value, new OnCharacterHPRegenChanged());
        }
        /// <inheritdoc />
        public void SetFPRegen(uint creature, int value)
        {
            SetStat(creature, StatType.FPRegen, value, new OnCharacterFPRegenChanged());
        }
        /// <inheritdoc />
        public void SetSTMRegen(uint creature, int value)
        {
            SetStat(creature, StatType.STMRegen, value, new OnCharacterSTMRegenChanged());
        }
        /// <inheritdoc />
        public void SetDefense(uint creature, int value)
        {
            SetStat(creature, StatType.Defense, value, new OnCharacterDefenseChanged());
        }
        /// <inheritdoc />
        public void SetAttack(uint creature, int value)
        {
            SetStat(creature, StatType.Attack, value, new OnCharacterAttackChanged());
        }
        /// <inheritdoc />
        public void SetRecastReduction(uint creature, int value)
        {
            SetStat(creature, StatType.RecastReduction, value, new OnCharacterRecastReductionChanged());
        }
        /// <inheritdoc />
        public void SetEvasion(uint creature, int value)
        {
            SetStat(creature, StatType.Evasion, value, new OnCharacterEvasionChanged());
        }
        /// <inheritdoc />
        public void SetAccuracy(uint creature, int value)
        {
            SetStat(creature, StatType.Accuracy, value, new OnCharacterAccuracyChanged());
        }
        /// <inheritdoc />
        public void SetForceAttack(uint creature, int value)
        {
            SetStat(creature, StatType.ForceAttack, value, new OnCharacterForceAttackChanged());
        }
        /// <inheritdoc />
        public void SetMight(uint creature, int value)
        {
            SetStat(creature, StatType.Might, value, new OnCharacterMightChanged());
        }
        /// <inheritdoc />
        public void SetPerception(uint creature, int value)
        {
            SetStat(creature, StatType.Perception, value, new OnCharacterPerceptionChanged());
        }
        /// <inheritdoc />
        public void SetVitality(uint creature, int value)
        {
            SetStat(creature, StatType.Vitality, value, new OnCharacterVitalityChanged());
        }
        /// <inheritdoc />
        public void SetAgility(uint creature, int value)
        {
            SetStat(creature, StatType.Agility, value, new OnCharacterAgilityChanged());
        }
        /// <inheritdoc />
        public void SetWillpower(uint creature, int value)
        {
            SetStat(creature, StatType.Willpower, value, new OnCharacterWillpowerChanged());
        }
        /// <inheritdoc />
        public void SetSocial(uint creature, int value)
        {
            SetStat(creature, StatType.Social, value, new OnCharacterSocialChanged());
        }
        /// <inheritdoc />
        public void SetShieldDeflection(uint creature, int value)
        {
            SetStat(creature, StatType.ShieldDeflection, value, new OnCharacterShieldDeflectionChanged());
        }
        /// <inheritdoc />
        public void SetAttackDeflection(uint creature, int value)
        {
            SetStat(creature, StatType.AttackDeflection, value, new OnCharacterAttackDeflectionChanged());
        }
        /// <inheritdoc />
        public void SetCriticalRate(uint creature, int value)
        {
            SetStat(creature, StatType.CriticalRate, value, new OnCharacterCriticalRateChanged());
        }
        /// <inheritdoc />
        public void SetEnmity(uint creature, int value)
        {
            SetStat(creature, StatType.Enmity, value, new OnCharacterEnmityChanged());
        }
        /// <inheritdoc />
        public void SetHaste(uint creature, int value)
        {
            SetStat(creature, StatType.Haste, value, new OnCharacterHasteChanged());
        }
        /// <inheritdoc />
        public void SetSlow(uint creature, int value)
        {
            SetStat(creature, StatType.Slow, value, new OnCharacterSlowChanged());
        }
        /// <inheritdoc />
        public void SetForceDefense(uint creature, int value)
        {
            SetStat(creature, StatType.ForceDefense, value, new OnCharacterForceDefenseChanged());
        }
        /// <inheritdoc />
        public void SetQueuedDMGBonus(uint creature, int value)
        {
            SetStat(creature, StatType.QueuedDMGBonus, value, new OnCharacterQueuedDMGBonusChanged());
        }
        /// <inheritdoc />
        public void SetParalysis(uint creature, int value)
        {
            SetStat(creature, StatType.Paralysis, value, new OnCharacterParalysisChanged());
        }
        /// <inheritdoc />
        public void SetAccuracyModifier(uint creature, int value)
        {
            SetStat(creature, StatType.AccuracyModifier, value, new OnCharacterAccuracyModifierChanged());
        }
        /// <inheritdoc />
        public void SetRecastReductionModifier(uint creature, int value)
        {
            SetStat(creature, StatType.RecastReductionModifier, value, new OnCharacterRecastReductionModifierChanged());
        }
        /// <inheritdoc />
        public void SetDefenseBypassModifier(uint creature, int value)
        {
            SetStat(creature, StatType.DefenseBypassModifier, value, new OnCharacterDefenseBypassModifierChanged());
        }
        /// <inheritdoc />
        public void SetHealingModifier(uint creature, int value)
        {
            SetStat(creature, StatType.HealingModifier, value, new OnCharacterHealingModifierChanged());
        }
        /// <inheritdoc />
        public void SetFPRestoreOnHit(uint creature, int value)
        {
            SetStat(creature, StatType.FPRestoreOnHit, value, new OnCharacterFPRestoreOnHitChanged());
        }
        /// <inheritdoc />
        public void SetDefenseModifier(uint creature, int value)
        {
            SetStat(creature, StatType.DefenseModifier, value, new OnCharacterDefenseModifierChanged());
        }
        /// <inheritdoc />
        public void SetForceDefenseModifier(uint creature, int value)
        {
            SetStat(creature, StatType.ForceDefenseModifier, value, new OnCharacterForceDefenseModifierChanged());
        }
        /// <inheritdoc />
        public void SetAttackModifier(uint creature, int value)
        {
            SetStat(creature, StatType.AttackModifier, value, new OnCharacterAttackModifierChanged());
        }
        /// <inheritdoc />
        public void SetForceAttackModifier(uint creature, int value)
        {
            SetStat(creature, StatType.ForceAttackModifier, value, new OnCharacterForceAttackModifierChanged());
        }
        /// <inheritdoc />
        public void SetEvasionModifier(uint creature, int value)
        {
            SetStat(creature, StatType.EvasionModifier, value, new OnCharacterEvasionModifierChanged());
        }
        /// <inheritdoc />
        public void SetXPModifier(uint creature, int value)
        {
            SetStat(creature, StatType.XPModifier, value, new OnCharacterXPModifierChanged());
        }
        /// <inheritdoc />
        public void SetPoisonResist(uint creature, int value)
        {
            SetStat(creature, StatType.PoisonResist, value, new OnCharacterPoisonResistChanged());
        }
        /// <inheritdoc />
        public void SetLevel(uint creature, int value)
        {
            SetStat(creature, StatType.Level, value, new OnCharacterLevelChanged());
        }
        /// <inheritdoc />
        public void SetControlSmithery(uint creature, int value)
        {
            SetStat(creature, StatType.ControlSmithery, value, new OnCharacterControlSmitheryChanged());
        }
        /// <inheritdoc />
        public void SetControlFabrication(uint creature, int value)
        {
            SetStat(creature, StatType.ControlFabrication, value, new OnCharacterControlFabricationChanged());
        }
        /// <inheritdoc />
        public void SetControlEngineering(uint creature, int value)
        {
            SetStat(creature, StatType.ControlEngineering, value, new OnCharacterControlEngineeringChanged());
        }
        /// <inheritdoc />
        public void SetControlAgriculture(uint creature, int value)
        {
            SetStat(creature, StatType.ControlAgriculture, value, new OnCharacterControlAgricultureChanged());
        }
        /// <inheritdoc />
        public void SetCraftsmanshipSmithery(uint creature, int value)
        {
            SetStat(creature, StatType.CraftsmanshipSmithery, value, new OnCharacterCraftsmanshipSmitheryChanged());
        }
        /// <inheritdoc />
        public void SetCraftsmanshipFabrication(uint creature, int value)
        {
            SetStat(creature, StatType.CraftsmanshipFabrication, value, new OnCharacterCraftsmanshipFabricationChanged());
        }
        /// <inheritdoc />
        public void SetCraftsmanshipEngineering(uint creature, int value)
        {
            SetStat(creature, StatType.CraftsmanshipEngineering, value, new OnCharacterCraftsmanshipEngineeringChanged());
        }
        /// <inheritdoc />
        public void SetCraftsmanshipAgriculture(uint creature, int value)
        {
            SetStat(creature, StatType.CraftsmanshipAgriculture, value, new OnCharacterCraftsmanshipAgricultureChanged());
        }
        /// <inheritdoc />
        public void SetCPSmithery(uint creature, int value)
        {
            SetStat(creature, StatType.CPSmithery, value, new OnCharacterCPSmitheryChanged());
        }
        /// <inheritdoc />
        public void SetCPFabrication(uint creature, int value)
        {
            SetStat(creature, StatType.CPFabrication, value, new OnCharacterCPFabricationChanged());
        }
        /// <inheritdoc />
        public void SetCPEngineering(uint creature, int value)
        {
            SetStat(creature, StatType.CPEngineering, value, new OnCharacterCPEngineeringChanged());
        }
        /// <inheritdoc />
        public void SetCPAgriculture(uint creature, int value)
        {
            SetStat(creature, StatType.CPAgriculture, value, new OnCharacterCPAgricultureChanged());
        }
        /// <inheritdoc />
        public void ModifyMaxHP(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.MaxHP);
            SetMaxHP(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyMaxFP(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.MaxFP);
            SetMaxFP(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyMaxSTM(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.MaxSTM);
            SetMaxSTM(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyHPRegen(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.HPRegen);
            SetHPRegen(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyFPRegen(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.FPRegen);
            SetFPRegen(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifySTMRegen(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.STMRegen);
            SetSTMRegen(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyDefense(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Defense);
            SetDefense(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyAttack(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Attack);
            SetAttack(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyRecastReduction(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.RecastReduction);
            SetRecastReduction(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyEvasion(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Evasion);
            SetEvasion(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyAccuracy(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Accuracy);
            SetAccuracy(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyForceAttack(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ForceAttack);
            SetForceAttack(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyMight(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Might);
            SetMight(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyPerception(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Perception);
            SetPerception(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyVitality(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Vitality);
            SetVitality(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyAgility(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Agility);
            SetAgility(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyWillpower(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Willpower);
            SetWillpower(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifySocial(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Social);
            SetSocial(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyShieldDeflection(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ShieldDeflection);
            SetShieldDeflection(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyAttackDeflection(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.AttackDeflection);
            SetAttackDeflection(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCriticalRate(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CriticalRate);
            SetCriticalRate(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyEnmity(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Enmity);
            SetEnmity(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyHaste(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Haste);
            SetHaste(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifySlow(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Slow);
            SetSlow(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyForceDefense(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ForceDefense);
            SetForceDefense(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyQueuedDMGBonus(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.QueuedDMGBonus);
            SetQueuedDMGBonus(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyParalysis(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Paralysis);
            SetParalysis(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyAccuracyModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.AccuracyModifier);
            SetAccuracyModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyRecastReductionModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.RecastReductionModifier);
            SetRecastReductionModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyDefenseBypassModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.DefenseBypassModifier);
            SetDefenseBypassModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyHealingModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.HealingModifier);
            SetHealingModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyFPRestoreOnHit(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.FPRestoreOnHit);
            SetFPRestoreOnHit(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyDefenseModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.DefenseModifier);
            SetDefenseModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyForceDefenseModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ForceDefenseModifier);
            SetForceDefenseModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyAttackModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.AttackModifier);
            SetAttackModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyForceAttackModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ForceAttackModifier);
            SetForceAttackModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyEvasionModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.EvasionModifier);
            SetEvasionModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyXPModifier(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.XPModifier);
            SetXPModifier(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyPoisonResist(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.PoisonResist);
            SetPoisonResist(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyLevel(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.Level);
            SetLevel(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyControlSmithery(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ControlSmithery);
            SetControlSmithery(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyControlFabrication(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ControlFabrication);
            SetControlFabrication(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyControlEngineering(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ControlEngineering);
            SetControlEngineering(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyControlAgriculture(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.ControlAgriculture);
            SetControlAgriculture(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCraftsmanshipSmithery(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CraftsmanshipSmithery);
            SetCraftsmanshipSmithery(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCraftsmanshipFabrication(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CraftsmanshipFabrication);
            SetCraftsmanshipFabrication(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCraftsmanshipEngineering(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CraftsmanshipEngineering);
            SetCraftsmanshipEngineering(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCraftsmanshipAgriculture(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CraftsmanshipAgriculture);
            SetCraftsmanshipAgriculture(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCPSmithery(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CPSmithery);
            SetCPSmithery(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCPFabrication(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CPFabrication);
            SetCPFabrication(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCPEngineering(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CPEngineering);
            SetCPEngineering(creature, currentValue + adjustment);
        }
        /// <inheritdoc />
        public void ModifyCPAgriculture(uint creature, int adjustment)
        {
            var currentValue = GetStat(creature, StatType.CPAgriculture);
            SetCPAgriculture(creature, currentValue + adjustment);
        }
    }
}
