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

        /// <summary>
        /// Creates a new beast configuration.
        /// </summary>
        /// <param name="type">The type of beast to associate with this configuration.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder Create(BeastType type)
        {
            _activeBeast = new BeastDetail();
            _beasts.Add(type, _activeBeast);

            return this;
        }

        /// <summary>
        /// Specifies the appearance type of the beast.
        /// </summary>
        /// <param name="appearance">The appearance to use.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder Appearance(AppearanceType appearance)
        {
            _activeBeast.Appearance = appearance;

            return this;
        }

        /// <summary>
        /// Specifies the portrait used for the beast.
        /// </summary>
        /// <param name="portraitId">The Id of the portrait</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder PortraitId(int portraitId)
        {
            _activeBeast.PortraitId = portraitId;

            return this;
        }

        /// <summary>
        /// Specifies the sound set used for the beast.
        /// </summary>
        /// <param name="soundSetId">The Id of the sound set</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder SoundSetId(int soundSetId)
        {
            _activeBeast.SoundSetId = soundSetId;

            return this;
        }

        /// <summary>
        /// Specifies the role used by the beast. Roles affect available perks.
        /// </summary>
        /// <param name="role">The type of role to configure</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder Role(BeastRoleType role)
        {
            _activeBeast.Role = role;

            return this;
        }

        /// <summary>
        /// Specifies the accuracy and damage stats used by the beast.
        /// </summary>
        /// <param name="accuracy">The stat used for accuracy calculations</param>
        /// <param name="damage">The stat used for damage calculations</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder CombatStats(AbilityType accuracy, AbilityType damage)
        {
            _activeBeast.AccuracyStat = accuracy;
            _activeBeast.DamageStat = damage;

            return this;
        }

        /// <summary>
        /// Adds a new level to be used by the beast.
        /// </summary>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder AddLevel()
        {
            var level = _activeBeast.Levels.Count + 1;

            _activeLevel = new BeastLevel();
            _activeBeast.Levels.Add(level, _activeLevel);

            return this;
        }

        /// <summary>
        /// Specifies the HP assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="amount">The amount of HP to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder HP(int amount)
        {
            _activeLevel.HP = amount;

            return this;
        }

        /// <summary>
        /// Specifies the FP assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="amount">The amount of FP to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder FP(int amount)
        {
            _activeLevel.FP = amount;

            return this;
        }

        /// <summary>
        /// Specifies the STM assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="amount">The amount of STM to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder STM(int amount)
        {
            _activeLevel.STM = amount;

            return this;
        }

        /// <summary>
        /// Specifies the DMG assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="amount">The amount of DMG to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder DMG(int amount)
        {
            _activeLevel.DMG = amount;

            return this;
        }

        /// <summary>
        /// Specifies the value of a specific stat for the beast at the current level being configured.
        /// </summary>
        /// <param name="type">The stat to assign</param>
        /// <param name="value">The amount of the stat to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder Stat(AbilityType type, int value)
        {
            _activeLevel.Stats[type] = value;

            return this;
        }

        /// <summary>
        /// Specifies the max attack bonus assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="max">The max amount of Attack to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder MaxAttackBonus(int max)
        {
            _activeLevel.MaxAttackBonus = max;

            return this;
        }

        /// <summary>
        /// Specifies the max accuracy bonus assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="max">The max amount of Accuracy to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder MaxAccuracyBonus(int max)
        {
            _activeLevel.MaxAccuracyBonus = max;

            return this;
        }

        /// <summary>
        /// Specifies the max evasion bonus assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="max">The max amount of Evasion to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder MaxEvasionBonus(int max)
        {
            _activeLevel.MaxEvasionBonus = max;

            return this;
        }

        /// <summary>
        /// Specifies the max defense bonus assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="type">The damage type.</param>
        /// <param name="max">The max amount of Defense to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder MaxDefenseBonus(CombatDamageType type, int max)
        {
            _activeLevel.MaxDefenseBonuses[type] = max;

            return this;
        }

        /// <summary>
        /// Specifies the max saving throw bonus assigned for the beast at the current level being configured.
        /// </summary>
        /// <param name="type">The saving throw type.</param>
        /// <param name="max">The max amount of Saving Throw Bonus to assign for this level.</param>
        /// <returns>A configured BeastBuilder object</returns>
        public BeastBuilder MaxSavingThrowBonus(SavingThrow type, int max)
        {
            _activeLevel.MaxSavingThrowBonuses[type] = max;

            return this;
        }

        /// <summary>
        /// Builds the beast's definition.
        /// </summary>
        /// <returns>A collection of beast details.</returns>
        public Dictionary<BeastType, BeastDetail> Build()
        {
            return _beasts;
        }

    }
}
