using SWLOR.Component.Character.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
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

        public void SetMaxHP(uint creature, int value)
        {
            SetStat(creature, StatType.MaxHP, value, new OnCharacterMaxHPChanged());
        }

        public void SetMaxFP(uint creature, int value)
        {
            SetStat(creature, StatType.MaxFP, value, new OnCharacterMaxFPChanged());
        }

        public void SetMaxSTM(uint creature, int value)
        {
            SetStat(creature, StatType.MaxSTM, value, new OnCharacterMaxSTMChanged());
        }

        public void SetHPRegen(uint creature, int value)
        {
            SetStat(creature, StatType.HPRegen, value, new OnCharacterHPRegenChanged());
        }

        public void SetFPRegen(uint creature, int value)
        {
            SetStat(creature, StatType.FPRegen, value, new OnCharacterFPRegenChanged());
        }

        public void SetSTMRegen(uint creature, int value)
        {
            SetStat(creature, StatType.STMRegen, value, new OnCharacterSTMRegenChanged());
        }

        public void SetDefense(uint creature, int value)
        {
            SetStat(creature, StatType.Defense, value, new OnCharacterDefenseChanged());
        }

        public void SetAttack(uint creature, int value)
        {
            SetStat(creature, StatType.Attack, value, new OnCharacterAttackChanged());
        }

        public void SetRecastReduction(uint creature, int value)
        {
            SetStat(creature, StatType.RecastReduction, value, new OnCharacterRecastReductionChanged());
        }

        public void SetEvasion(uint creature, int value)
        {
            SetStat(creature, StatType.Evasion, value, new OnCharacterEvasionChanged());
        }

        public void SetAccuracy(uint creature, int value)
        {
            SetStat(creature, StatType.Accuracy, value, new OnCharacterAccuracyChanged());
        }

        public void SetForceAttack(uint creature, int value)
        {
            SetStat(creature, StatType.ForceAttack, value, new OnCharacterForceAttackChanged());
        }

        public void SetMight(uint creature, int value)
        {
            SetStat(creature, StatType.Might, value, new OnCharacterMightChanged());
        }

        public void SetPerception(uint creature, int value)
        {
            SetStat(creature, StatType.Perception, value, new OnCharacterPerceptionChanged());
        }

        public void SetVitality(uint creature, int value)
        {
            SetStat(creature, StatType.Vitality, value, new OnCharacterVitalityChanged());
        }

        public void SetAgility(uint creature, int value)
        {
            SetStat(creature, StatType.Agility, value, new OnCharacterAgilityChanged());
        }

        public void SetWillpower(uint creature, int value)
        {
            SetStat(creature, StatType.Willpower, value, new OnCharacterWillpowerChanged());
        }

        public void SetSocial(uint creature, int value)
        {
            SetStat(creature, StatType.Social, value, new OnCharacterSocialChanged());
        }

        public void SetShieldDeflection(uint creature, int value)
        {
            SetStat(creature, StatType.ShieldDeflection, value, new OnCharacterShieldDeflectionChanged());
        }

        public void SetAttackDeflection(uint creature, int value)
        {
            SetStat(creature, StatType.AttackDeflection, value, new OnCharacterAttackDeflectionChanged());
        }

        public void SetCriticalRate(uint creature, int value)
        {
            SetStat(creature, StatType.CriticalRate, value, new OnCharacterCriticalRateChanged());
        }

        public void SetEnmity(uint creature, int value)
        {
            SetStat(creature, StatType.Enmity, value, new OnCharacterEnmityChanged());
        }

        public void SetHaste(uint creature, int value)
        {
            SetStat(creature, StatType.Haste, value, new OnCharacterHasteChanged());
        }

        public void SetSlow(uint creature, int value)
        {
            SetStat(creature, StatType.Slow, value, new OnCharacterSlowChanged());
        }

        public void SetForceDefense(uint creature, int value)
        {
            SetStat(creature, StatType.ForceDefense, value, new OnCharacterForceDefenseChanged());
        }

        public void SetQueuedDMGBonus(uint creature, int value)
        {
            SetStat(creature, StatType.QueuedDMGBonus, value, new OnCharacterQueuedDMGBonusChanged());
        }

        public void SetParalysis(uint creature, int value)
        {
            SetStat(creature, StatType.Paralysis, value, new OnCharacterParalysisChanged());
        }

        public void SetAccuracyModifier(uint creature, int value)
        {
            SetStat(creature, StatType.AccuracyModifier, value, new OnCharacterAccuracyModifierChanged());
        }

        public void SetRecastReductionModifier(uint creature, int value)
        {
            SetStat(creature, StatType.RecastReductionModifier, value, new OnCharacterRecastReductionModifierChanged());
        }

        public void SetDefenseBypassModifier(uint creature, int value)
        {
            SetStat(creature, StatType.DefenseBypassModifier, value, new OnCharacterDefenseBypassModifierChanged());
        }

        public void SetHealingModifier(uint creature, int value)
        {
            SetStat(creature, StatType.HealingModifier, value, new OnCharacterHealingModifierChanged());
        }

        public void SetFPRestoreOnHit(uint creature, int value)
        {
            SetStat(creature, StatType.FPRestoreOnHit, value, new OnCharacterFPRestoreOnHitChanged());
        }

        public void SetDefenseModifier(uint creature, int value)
        {
            SetStat(creature, StatType.DefenseModifier, value, new OnCharacterDefenseModifierChanged());
        }

        public void SetForceDefenseModifier(uint creature, int value)
        {
            SetStat(creature, StatType.ForceDefenseModifier, value, new OnCharacterForceDefenseModifierChanged());
        }

        public void SetAttackModifier(uint creature, int value)
        {
            SetStat(creature, StatType.AttackModifier, value, new OnCharacterAttackModifierChanged());
        }

        public void SetForceAttackModifier(uint creature, int value)
        {
            SetStat(creature, StatType.ForceAttackModifier, value, new OnCharacterForceAttackModifierChanged());
        }

        public void SetEvasionModifier(uint creature, int value)
        {
            SetStat(creature, StatType.EvasionModifier, value, new OnCharacterEvasionModifierChanged());
        }

        public void SetXPModifier(uint creature, int value)
        {
            SetStat(creature, StatType.XPModifier, value, new OnCharacterXPModifierChanged());
        }

        public void SetPoisonResist(uint creature, int value)
        {
            SetStat(creature, StatType.PoisonResist, value, new OnCharacterPoisonResistChanged());
        }

        public void SetLevel(uint creature, int value)
        {
            SetStat(creature, StatType.Level, value, new OnCharacterLevelChanged());
        }

        public void SetControlSmithery(uint creature, int value)
        {
            SetStat(creature, StatType.ControlSmithery, value, new OnCharacterControlSmitheryChanged());
        }

        public void SetControlFabrication(uint creature, int value)
        {
            SetStat(creature, StatType.ControlFabrication, value, new OnCharacterControlFabricationChanged());
        }

        public void SetControlEngineering(uint creature, int value)
        {
            SetStat(creature, StatType.ControlEngineering, value, new OnCharacterControlEngineeringChanged());
        }

        public void SetControlAgriculture(uint creature, int value)
        {
            SetStat(creature, StatType.ControlAgriculture, value, new OnCharacterControlAgricultureChanged());
        }

        public void SetCraftsmanshipSmithery(uint creature, int value)
        {
            SetStat(creature, StatType.CraftsmanshipSmithery, value, new OnCharacterCraftsmanshipSmitheryChanged());
        }

        public void SetCraftsmanshipFabrication(uint creature, int value)
        {
            SetStat(creature, StatType.CraftsmanshipFabrication, value, new OnCharacterCraftsmanshipFabricationChanged());
        }

        public void SetCraftsmanshipEngineering(uint creature, int value)
        {
            SetStat(creature, StatType.CraftsmanshipEngineering, value, new OnCharacterCraftsmanshipEngineeringChanged());
        }

        public void SetCraftsmanshipAgriculture(uint creature, int value)
        {
            SetStat(creature, StatType.CraftsmanshipAgriculture, value, new OnCharacterCraftsmanshipAgricultureChanged());
        }

    }
}
