using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class BeastBuilder
    {
        private readonly Dictionary<BeastType, BeastDetail> _beasts = new();
        private BeastDetail _activeBeast;
        private BeastLevel _activeLevel;

        public BeastBuilder Create(BeastType type)
        {
            _activeBeast = new BeastDetail();
            _beasts.Add(type, _activeBeast);

            return this;
        }

        public BeastBuilder Appearance(AppearanceType appearance)
        {
            _activeBeast.Appearance = appearance;

            return this;
        }

        public BeastBuilder PortraitId(int portraitId)
        {
            _activeBeast.PortraitId = portraitId;

            return this;
        }

        public BeastBuilder SoundSetId(int soundSetId)
        {
            _activeBeast.SoundSetId = soundSetId;

            return this;
        }

        public BeastBuilder Role(BeastRoleType role)
        {
            _activeBeast.Role = role;

            return this;
        }

        public BeastBuilder CombatStats(AbilityType accuracy, AbilityType damage)
        {
            _activeBeast.AccuracyStat = accuracy;
            _activeBeast.DamageStat = damage;

            return this;
        }

        public BeastBuilder AddLevel()
        {
            var level = _activeBeast.Levels.Count + 1;

            _activeLevel = new BeastLevel();
            _activeBeast.Levels.Add(level, _activeLevel);

            return this;
        }

        public BeastBuilder HP(int amount)
        {
            _activeLevel.HP = amount;

            return this;
        }

        public BeastBuilder FP(int amount)
        {
            _activeLevel.FP = amount;

            return this;
        }

        public BeastBuilder STM(int amount)
        {
            _activeLevel.STM = amount;

            return this;
        }

        public BeastBuilder DMG(int amount)
        {
            _activeLevel.DMG = amount;

            return this;
        }

        public BeastBuilder Stat(AbilityType type, int value)
        {
            _activeLevel.Stats[type] = value;

            return this;
        }

        public BeastBuilder MaxAttackBonus(int max)
        {
            _activeLevel.MaxAttackBonus = max;

            return this;
        }
        public BeastBuilder MaxAccuracyBonus(int max)
        {
            _activeLevel.MaxAccuracyBonus = max;

            return this;
        }
        public BeastBuilder MaxEvasionBonus(int max)
        {
            _activeLevel.MaxEvasionBonus = max;

            return this;
        }

        public BeastBuilder MaxDefenseBonus(CombatDamageType type, int max)
        {
            _activeLevel.MaxDefenseBonuses[type] = max;

            return this;
        }

        public BeastBuilder MaxSavingThrowBonus(SavingThrow type, int max)
        {
            _activeLevel.MaxSavingThrowBonuses[type] = max;

            return this;
        }

        public Dictionary<BeastType, BeastDetail> Build()
        {
            return _beasts;
        }

    }
}
