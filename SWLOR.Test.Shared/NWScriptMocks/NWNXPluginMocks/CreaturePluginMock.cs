using System.Numerics;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWNX.Model;

namespace SWLOR.Test.Shared.NWScriptMocks.NWNXPluginMocks
{
    /// <summary>
    /// Mock implementation of the CreaturePlugin for testing purposes.
    /// Provides comprehensive creature management functionality including feat management, ability scores,
    /// spell casting, combat statistics, special abilities, and advanced creature properties.
    /// </summary>
    public class CreaturePluginMock: ICreaturePluginService
    {
        // Mock data storage
        private readonly Dictionary<uint, CreatureData> _creatureData = new();

        /// <summary>
        /// Adds a feat to the specified creature's feat list.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="feat">The feat type to add. See FeatType enum for available feats.</param>
        public void AddFeat(uint creature, FeatType feat)
        {
            GetCreatureData(creature).Feats.Add(feat);
        }

        /// <summary>
        /// Adds a feat to the creature's feat list and assigns it to a specific level.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="feat">The feat type to add. See FeatType enum for available feats.</param>
        /// <param name="level">The level at which the creature gained this feat. Must be a positive integer.</param>
        public void AddFeatByLevel(uint creature, FeatType feat, int level)
        {
            var creatureData = GetCreatureData(creature);
            if (!creatureData.FeatsByLevel.ContainsKey(level))
            {
                creatureData.FeatsByLevel[level] = new List<FeatType>();
            }
            creatureData.FeatsByLevel[level].Add(feat);
            creatureData.Feats.Add(feat);
        }

        /// <summary>
        /// Removes a feat from the creature's feat list.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="feat">The feat type to remove. See FeatType enum for available feats.</param>
        public void RemoveFeat(uint creature, FeatType feat)
        {
            var creatureData = GetCreatureData(creature);
            creatureData.Feats.Remove(feat);
            
            // Remove from all levels
            foreach (var level in creatureData.FeatsByLevel.Keys.ToList())
            {
                creatureData.FeatsByLevel[level].Remove(feat);
                if (creatureData.FeatsByLevel[level].Count == 0)
                {
                    creatureData.FeatsByLevel.Remove(level);
                }
            }
        }

        /// <summary>
        /// Determines whether the creature knows a specific feat.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat type to check. See FeatType enum for available feats.</param>
        /// <returns>True if the creature knows the feat, false otherwise.</returns>
        public bool GetKnowsFeat(uint creature, FeatType feat)
        {
            return GetCreatureData(creature).Feats.Contains(feat);
        }

        /// <summary>
        /// Retrieves the number of feats learned by the creature at a specific level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="level">The level to check. Must be a positive integer.</param>
        /// <returns>The number of feats learned at the specified level.</returns>
        public int GetFeatCountByLevel(uint creature, int level)
        {
            return GetCreatureData(creature).FeatsByLevel.TryGetValue(level, out var feats) ? feats.Count : 0;
        }

        /// <summary>
        /// Retrieves a specific feat learned by the creature at a given level and index.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="level">The level to check. Must be a positive integer.</param>
        /// <param name="index">The index of the feat at that level. Must be between 0 and GetFeatCountByLevel() - 1.</param>
        /// <returns>The feat type at the specified level and index.</returns>
        public FeatType GetFeatByLevel(uint creature, int level, int index)
        {
            var creatureData = GetCreatureData(creature);
            if (creatureData.FeatsByLevel.TryGetValue(level, out var feats) && index >= 0 && index < feats.Count)
            {
                return feats[index];
            }
            return FeatType.Invalid;
        }

        /// <summary>
        /// Sets the creature's ability score.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">The ability type to modify. See AbilityType enum for available abilities.</param>
        /// <param name="value">The new ability score value. Must be between 1 and 255.</param>
        public void SetAbilityScore(uint creature, AbilityType ability, int value)
        {
            GetCreatureData(creature).AbilityScores[ability] = Math.Max(1, Math.Min(255, value));
        }

        /// <summary>
        /// Gets the creature's ability score.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="ability">The ability type to query. See AbilityType enum for available abilities.</param>
        /// <returns>The creature's ability score for the specified ability.</returns>
        public int GetAbilityScore(uint creature, AbilityType ability)
        {
            return GetCreatureData(creature).AbilityScores.TryGetValue(ability, out var score) ? score : 10;
        }

        /// <summary>
        /// Sets the creature's skill rank.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="skill">The skill type to modify. See SkillType enum for available skills.</param>
        /// <param name="rank">The new skill rank value. Must be between 0 and 255.</param>
        public void SetSkillRank(uint creature, NWNSkillType skill, int rank)
        {
            GetCreatureData(creature).SkillRanks[skill] = Math.Max(0, Math.Min(255, rank));
        }

        /// <summary>
        /// Gets the creature's skill rank.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="skill">The skill type to query. See SkillType enum for available skills.</param>
        /// <returns>The creature's skill rank for the specified skill.</returns>
        public int GetSkillRank(uint creature, NWNSkillType skill)
        {
            return GetCreatureData(creature).SkillRanks.TryGetValue(skill, out var rank) ? rank : 0;
        }

        /// <summary>
        /// Sets the creature's class level.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classType">The class type to modify. See ClassType enum for available classes.</param>
        /// <param name="level">The new class level value. Must be between 0 and 255.</param>
        public void SetClassLevel(uint creature, ClassType classType, int level)
        {
            GetCreatureData(creature).ClassLevels[classType] = Math.Max(0, Math.Min(255, level));
        }

        /// <summary>
        /// Gets the creature's class level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="classType">The class type to query. See ClassType enum for available classes.</param>
        /// <returns>The creature's class level for the specified class.</returns>
        public int GetClassLevel(uint creature, ClassType classType)
        {
            return GetCreatureData(creature).ClassLevels.TryGetValue(classType, out var level) ? level : 0;
        }

        /// <summary>
        /// Retrieves the class taken by the creature at a specific level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="level">The level to check. Must be a positive integer.</param>
        /// <returns>The class type taken at the specified level.</returns>
        public ClassType GetClassByLevel(uint creature, int level)
        {
            // Mock implementation - in real tests, this would track class progression by level
            return ClassType.Fighter;
        }

        /// <summary>
        /// Sets the base armor class (AC) for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ac">The base AC value to set. Must be a non-negative integer.</param>
        public void SetBaseAC(uint creature, int ac)
        {
            GetCreatureData(creature).ArmorClass = Math.Max(0, ac);
        }

        /// <summary>
        /// Retrieves the base armor class (AC) of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's base AC value.</returns>
        public int GetBaseAC(uint creature)
        {
            return GetCreatureData(creature).ArmorClass;
        }

        /// <summary>
        /// Sets the raw ability score of the creature to a specific value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">The ability type to modify. See AbilityType enum for available abilities.</param>
        /// <param name="value">The raw ability score value to set. Must be between 1 and 255.</param>
        public void SetRawAbilityScore(uint creature, AbilityType ability, int value)
        {
            GetCreatureData(creature).AbilityScores[ability] = Math.Max(1, Math.Min(255, value));
        }

        /// <summary>
        /// Retrieves the raw ability score of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="ability">The ability type to query. See AbilityType enum for available abilities.</param>
        /// <returns>The creature's raw ability score value.</returns>
        public int GetRawAbilityScore(uint creature, AbilityType ability)
        {
            return GetCreatureData(creature).AbilityScores.TryGetValue(ability, out var score) ? score : 10;
        }

        /// <summary>
        /// Modifies the raw ability score of the creature by adding a modifier.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">The ability type to modify. See AbilityType enum for available abilities.</param>
        /// <param name="modifier">The modifier to apply to the ability score. Can be positive or negative.</param>
        public void ModifyRawAbilityScore(uint creature, AbilityType ability, int modifier)
        {
            var creatureData = GetCreatureData(creature);
            var currentScore = creatureData.AbilityScores.TryGetValue(ability, out var score) ? score : 10;
            creatureData.AbilityScores[ability] = Math.Max(1, Math.Min(255, currentScore + modifier));
        }

        /// <summary>
        /// Retrieves the raw ability score a polymorphed creature had before polymorphing.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="ability">The ability type to query. Only Strength, Dexterity, and Constitution are valid.</param>
        /// <returns>The pre-polymorph raw ability score value.</returns>
        public int GetPrePolymorphAbilityScore(uint creature, AbilityType ability)
        {
            // Mock implementation - in real tests, this would track pre-polymorph ability scores
            return 10;
        }

        /// <summary>
        /// Sets the creature's hit points.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="hitPoints">The new hit points value. Must be between 0 and 65535.</param>
        public void SetHitPoints(uint creature, int hitPoints)
        {
            GetCreatureData(creature).HitPoints = Math.Max(0, Math.Min(65535, hitPoints));
        }

        /// <summary>
        /// Gets the creature's hit points.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's current hit points.</returns>
        public int GetHitPoints(uint creature)
        {
            return GetCreatureData(creature).HitPoints;
        }

        /// <summary>
        /// Sets the creature's maximum hit points.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="maxHitPoints">The new maximum hit points value. Must be between 1 and 65535.</param>
        public void SetMaxHitPoints(uint creature, int maxHitPoints)
        {
            GetCreatureData(creature).MaxHitPoints = Math.Max(1, Math.Min(65535, maxHitPoints));
        }

        /// <summary>
        /// Gets the creature's maximum hit points.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's maximum hit points.</returns>
        public int GetMaxHitPoints(uint creature)
        {
            return GetCreatureData(creature).MaxHitPoints;
        }

        /// <summary>
        /// Sets the creature's armor class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="armorClass">The new armor class value. Must be between -10 and 50.</param>
        public void SetArmorClass(uint creature, int armorClass)
        {
            GetCreatureData(creature).ArmorClass = Math.Max(-10, Math.Min(50, armorClass));
        }

        /// <summary>
        /// Gets the creature's armor class.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's armor class.</returns>
        public int GetArmorClass(uint creature)
        {
            return GetCreatureData(creature).ArmorClass;
        }

        /// <summary>
        /// Sets the creature's base attack bonus.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="baseAttackBonus">The new base attack bonus value. Must be between 0 and 255.</param>
        public void SetBaseAttackBonus(uint creature, int baseAttackBonus)
        {
            GetCreatureData(creature).BaseAttackBonus = Math.Max(0, Math.Min(255, baseAttackBonus));
        }

        /// <summary>
        /// Gets the creature's base attack bonus.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's base attack bonus.</returns>
        public int GetBaseAttackBonus(uint creature)
        {
            return GetCreatureData(creature).BaseAttackBonus;
        }

        /// <summary>
        /// Sets the class ID in a specific position for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="position">The class position to modify. Must be 0, 1, or 2.</param>
        /// <param name="classId">The class ID to set. Must be a valid ID from classes.2da (0-255).</param>
        /// <param name="updateLevels">Whether to update the creature's levels after the change. Default is true.</param>
        public void SetClassByPosition(uint creature, int position, ClassType classId, bool updateLevels = true)
        {
            // Mock implementation - in real tests, this would set class by position
        }


        /// <summary>
        /// Retrieves the creature's current number of attacks per round.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="bBaseAPR">If true, returns the base attacks per round based on BAB and equipped weapons, ignoring overrides from SetBaseAttackBonus() builtin function.</param>
        /// <returns>The number of attacks per round the creature can make.</returns>
        public int GetAttacksPerRound(uint creature, bool bBaseAPR)
        {
            // Mock implementation - in real tests, this would calculate attacks per round
            return 1;
        }

        /// <summary>
        /// Sets the creature's saving throw bonus.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="savingThrow">The saving throw type to modify. See SavingThrowType enum for available types.</param>
        /// <param name="bonus">The new saving throw bonus value. Must be between -20 and 50.</param>
        public void SetSavingThrowBonus(uint creature, SavingThrowType savingThrow, int bonus)
        {
            GetCreatureData(creature).SavingThrowBonuses[savingThrow] = Math.Max(-20, Math.Min(50, bonus));
        }

        /// <summary>
        /// Gets the creature's saving throw bonus.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="savingThrow">The saving throw type to query. See SavingThrowType enum for available types.</param>
        /// <returns>The creature's saving throw bonus for the specified type.</returns>
        public int GetSavingThrowBonus(uint creature, SavingThrowType savingThrow)
        {
            return GetCreatureData(creature).SavingThrowBonuses.TryGetValue(savingThrow, out var bonus) ? bonus : 0;
        }

        /// <summary>
        /// Sets the corpse decay time for the creature in milliseconds.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="decayTimeMs">The decay time in milliseconds. Must be non-negative.</param>
        public void SetCorpseDecayTime(uint creature, int decayTimeMs)
        {
            // Mock implementation - in real tests, this would set corpse decay time
        }

        /// <summary>
        /// Retrieves the creature's base saving throw value and any modifiers set in the toolset.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="which">The saving throw type to query. Use SavingThrow enum values.</param>
        /// <returns>The base saving throw value for the specified type.</returns>
        public int GetBaseSavingThrow(uint creature, int which)
        {
            // Mock implementation - in real tests, this would calculate base saving throw
            return 0;
        }

        /// <summary>
        /// Sets the base saving throw value for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="which">The saving throw type to modify. See SavingThrow enum for available types.</param>
        /// <param name="value">The base saving throw value to set. Must be non-negative.</param>
        public void SetBaseSavingThrow(uint creature, SavingThrowCategoryType which, int value)
        {
            // Mock implementation - in real tests, this would set base saving throw
        }

        /// <summary>
        /// Sets the creature's spell resistance.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="spellResistance">The new spell resistance value. Must be between 0 and 50.</param>
        public void SetSpellResistance(uint creature, int spellResistance)
        {
            GetCreatureData(creature).SpellResistance = Math.Max(0, Math.Min(50, spellResistance));
        }

        /// <summary>
        /// Gets the creature's spell resistance.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's spell resistance.</returns>
        public int GetSpellResistance(uint creature)
        {
            return GetCreatureData(creature).SpellResistance;
        }

        /// <summary>
        /// Sets the creature's animal companion's name.
        /// </summary>
        /// <param name="creature">The master creature object. Must be a valid creature with an animal companion.</param>
        /// <param name="name">The name to set for the animal companion. Cannot be null or empty.</param>
        public void SetAnimalCompanionName(uint creature, string name)
        {
            // Mock implementation - in real tests, this would track animal companion names
        }

        /// <summary>
        /// Sets the creature's familiar's name.
        /// </summary>
        /// <param name="creature">The master creature object. Must be a valid creature with a familiar.</param>
        /// <param name="name">The name to set for the familiar. Cannot be null or empty.</param>
        public void SetFamiliarName(uint creature, string name)
        {
            // Mock implementation - in real tests, this would track familiar names
        }

        /// <summary>
        /// Retrieves whether the creature can be disarmed.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>True if the creature can be disarmed, false otherwise.</returns>
        public bool GetDisarmable(uint creature)
        {
            // Mock implementation - in real tests, this would track disarmable status
            return true;
        }

        /// <summary>
        /// Sets whether a creature can be disarmed.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="isDisarmable">True if the creature can be disarmed, false otherwise.</param>
        public void SetDisarmable(uint creature, bool isDisarmable)
        {
            // Mock implementation - in real tests, this would track disarmable status
        }

        /// <summary>
        /// Sets one of the creature's domains for a specific class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="class">The class type from the ClassType enum.</param>
        /// <param name="index">The domain index (0 for first domain, 1 for second domain).</param>
        /// <param name="domain">The domain ID to set. Must be a valid domain from domains.2da.</param>
        public void SetDomain(uint creature, ClassType @class, int index, int domain)
        {
            // Mock implementation - in real tests, this would track domains
        }

        /// <summary>
        /// Sets the creature's specialist school for a specific class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="class">The class type from the ClassType enum.</param>
        /// <param name="school">The school ID to set. Must be a valid school from schools.2da.</param>
        public void SetSpecialization(uint creature, ClassType @class, int school)
        {
            // Mock implementation - in real tests, this would track specializations
        }

        /// <summary>
        /// Sets the creature's faction to the specified faction ID.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="factionId">The faction ID to assign to the creature. Must be a valid faction ID.</param>
        public void SetFaction(uint creature, int factionId)
        {
            // Mock implementation - in real tests, this would track faction
        }

        /// <summary>
        /// Retrieves the creature's current faction ID.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The faction ID as an integer, or -1 if the creature is invalid.</returns>
        public int GetFaction(uint creature)
        {
            // Mock implementation - in real tests, this would track faction
            return -1;
        }

        /// <summary>
        /// Retrieves whether a creature is currently flat-footed.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>True if the creature is flat-footed, false otherwise.</returns>
        public bool GetFlatFooted(uint creature)
        {
            // Mock implementation - in real tests, this would track flat-footed status
            return false;
        }

        /// <summary>
        /// Serializes the creature's quickbar to a base64 string.
        /// </summary>
        /// <param name="creature">The creature object to serialize. Must be a valid creature.</param>
        /// <returns>A base64 string representation of the creature's quickbar.</returns>
        public string SerializeQuickbar(uint creature)
        {
            // Mock implementation - in real tests, this would serialize quickbar
            return "";
        }

        /// <summary>
        /// Deserializes a serialized quickbar for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="serializedQuickbar">A base64 string containing the quickbar data.</param>
        /// <returns>True if the quickbar was successfully deserialized, false otherwise.</returns>
        public bool DeserializeQuickbar(uint creature, string serializedQuickbar)
        {
            // Mock implementation - in real tests, this would deserialize quickbar
            return true;
        }

        /// <summary>
        /// Sets the encounter source of the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="encounter">The source encounter object. Use OBJECT_INVALID to remove encounter association.</param>
        public void SetEncounter(uint creature, uint encounter)
        {
            // Mock implementation - in real tests, this would track encounter
        }

        /// <summary>
        /// Retrieves the encounter source of the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <returns>The encounter object ID, or OBJECT_INVALID if no encounter is associated.</returns>
        public uint GetEncounter(uint creature)
        {
            // Mock implementation - in real tests, this would track encounter
            return OBJECT_INVALID;
        }

        /// <summary>
        /// Overrides the damage level display of the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="damageLevel">A damage level from damagelevels.2da (0-255), or -1 to remove the override.</param>
        public void OverrideDamageLevel(uint creature, int damageLevel)
        {
            // Mock implementation - in real tests, this would track damage level override
        }

        /// <summary>
        /// Retrieves whether the creature is currently bartering.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <returns>True if the creature is currently bartering, false otherwise.</returns>
        public bool GetIsBartering(uint creature)
        {
            // Mock implementation - in real tests, this would track bartering status
            return false;
        }

        /// <summary>
        /// Sets the caster level for the last item used by the creature.
        /// </summary>
        /// <param name="creature">The creature who used the item. Must be a valid creature.</param>
        /// <param name="casterLevel">The desired caster level to set. Must be non-negative.</param>
        public void SetLastItemCasterLevel(uint creature, int casterLevel)
        {
            // Mock implementation - in real tests, this would track item caster level
        }

        /// <summary>
        /// Retrieves the caster level of the last item used by the creature.
        /// </summary>
        /// <param name="creature">The creature who used the item. Must be a valid creature.</param>
        /// <returns>The caster level of the creature's last used item.</returns>
        public int GetLastItemCasterLevel(uint creature)
        {
            // Mock implementation - in real tests, this would track item caster level
            return 0;
        }

        /// <summary>
        /// Retrieves the armor class of the attacked creature against the attacking creature.
        /// </summary>
        /// <param name="attacked">The creature being attacked. Must be a valid creature.</param>
        /// <param name="versus">The creature doing the attacking. Must be a valid creature.</param>
        /// <param name="touch">True for touch attacks, false for normal attacks. Default is false.</param>
        /// <returns>The armor class value, or -255 on error. Returns flat-footed AC if versus is invalid.</returns>
        public int GetArmorClassVersus(uint attacked, uint versus, bool touch = false)
        {
            // Mock implementation - in real tests, this would calculate armor class versus
            return 10;
        }

        /// <summary>
        /// Moves a creature to limbo (a special area for temporary storage).
        /// </summary>
        /// <param name="creature">The creature object to move. Must be a valid creature.</param>
        public void JumpToLimbo(uint creature)
        {
            // Mock implementation - in real tests, this would move creature to limbo
        }

        /// <summary>
        /// Sets the critical hit multiplier modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="modifier">The modifier to apply to the critical hit multiplier. Can be positive or negative.</param>
        /// <param name="hand">The hand to apply the modifier to: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="persist">Whether the modifier should persist to the .bic file if applicable. Default is false.</param>
        /// <param name="baseItemType">The base item type to apply the modifier to. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        public void SetCriticalMultiplierModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            // Mock implementation - in real tests, this would track critical multiplier modifier
        }

        /// <summary>
        /// Retrieves the critical hit multiplier modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="hand">The hand to query: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="baseItemType">The base item type to query. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <returns>The current critical hit multiplier modifier for the creature.</returns>
        public int GetCriticalMultiplierModifier(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            // Mock implementation - in real tests, this would track critical multiplier modifier
            return 0;
        }

        /// <summary>
        /// Sets the critical hit multiplier override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="override">The override value to set for the critical hit multiplier. Must be positive.</param>
        /// <param name="hand">The hand to apply the override to: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="persist">Whether the override should persist to the .bic file if applicable. Default is false.</param>
        /// <param name="baseItemType">The base item type to apply the override to. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        public void SetCriticalMultiplierOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            // Mock implementation - in real tests, this would track critical multiplier override
        }

        /// <summary>
        /// Retrieves the critical hit multiplier override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="hand">The hand to query: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="baseItemType">The base item type to query. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <returns>The current critical hit multiplier override for the creature.</returns>
        public int GetCriticalMultiplierOverride(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            // Mock implementation - in real tests, this would track critical multiplier override
            return 0;
        }

        /// <summary>
        /// Sets the critical range modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="modifier">The modifier to apply to the critical range. Can be positive or negative.</param>
        /// <param name="hand">The hand to apply the modifier to: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="persist">Whether the modifier should persist to the .bic file if applicable. Default is false.</param>
        /// <param name="baseItemType">The base item type to apply the modifier to. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        public void SetCriticalRangeModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            // Mock implementation - in real tests, this would track critical range modifier
        }

        /// <summary>
        /// Retrieves the critical range modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="hand">The hand to query: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="baseItemType">The base item type to query. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <returns>The current critical range modifier for the creature.</returns>
        public int GetCriticalRangeModifier(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            // Mock implementation - in real tests, this would track critical range modifier
            return 0;
        }

        /// <summary>
        /// Sets the critical range override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="override">The override value to set for the critical range. Must be positive.</param>
        /// <param name="hand">The hand to apply the override to: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="persist">Whether the override should persist to the .bic file if applicable. Default is false.</param>
        /// <param name="baseItemType">The base item type to apply the override to. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        public void SetCriticalRangeOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            // Mock implementation - in real tests, this would track critical range override
        }

        /// <summary>
        /// Retrieves the critical range override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="hand">The hand to query: 0 for all attacks, 1 for Mainhand, 2 for Offhand. Default is 0.</param>
        /// <param name="baseItemType">The base item type to query. Use BaseItem.Invalid for all types. Default is Invalid.</param>
        /// <returns>The current critical range override for the creature.</returns>
        public int GetCriticalRangeOverride(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid)
        {
            // Mock implementation - in real tests, this would track critical range override
            return 0;
        }

        /// <summary>
        /// Adds an associate to the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="associate">The associate object to add. Must be a valid creature.</param>
        /// <param name="associateType">The associate type. See associate types in the game constants.</param>
        public void AddAssociate(uint creature, uint associate, int associateType)
        {
            // Mock implementation - in real tests, this would track associates
        }

        /// <summary>
        /// Retrieves the walk animation ID of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The walk animation ID.</returns>
        public int GetWalkAnimation(uint creature)
        {
            // Mock implementation - in real tests, this would track walk animation
            return 0;
        }

        /// <summary>
        /// Sets the walk animation of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="animation">The animation ID to set. Must be a valid animation ID.</param>
        public void SetWalkAnimation(uint creature, int animation)
        {
            // Mock implementation - in real tests, this would track walk animation
        }

        /// <summary>
        /// Sets the attack roll override for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="roll">The roll value to override with. Must be between 1 and 20.</param>
        /// <param name="modifier">The modifier value to apply. Can be positive or negative.</param>
        public void SetAttackRollOverride(uint creature, int roll, int modifier)
        {
            // Mock implementation - in real tests, this would track attack roll override
        }

        /// <summary>
        /// Sets whether the creature can parry all attacks.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="parry">True if the creature can parry all attacks, false otherwise.</param>
        public void SetParryAllAttacks(uint creature, bool parry)
        {
            // Mock implementation - in real tests, this would track parry all attacks
        }

        /// <summary>
        /// Retrieves whether the creature has no permanent death.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>True if the creature has no permanent death, false otherwise.</returns>
        public bool GetNoPermanentDeath(uint creature)
        {
            // Mock implementation - in real tests, this would track permanent death status
            return false;
        }

        /// <summary>
        /// Sets whether the creature has no permanent death.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="noPermanentDeath">True if the creature has no permanent death, false otherwise.</param>
        public void SetNoPermanentDeath(uint creature, bool noPermanentDeath)
        {
            // Mock implementation - in real tests, this would track permanent death status
        }

        /// <summary>
        /// Computes a safe location for the creature near the specified position.
        /// </summary>
        /// <param name="creature">The creature object to find a safe location for. Must be a valid creature.</param>
        /// <param name="position">The position to search around. Must be a valid 3D position.</param>
        /// <param name="radius">The radius to search within. Default is 20.0f.</param>
        /// <param name="walkStraightLineRequired">Whether the creature must be able to walk in a straight line to the location. Default is true.</param>
        /// <returns>A safe location vector, or the original position if no safe location is found.</returns>
        public Vector3 ComputeSafeLocation(uint creature, Vector3 position, float radius = 20.0f, bool walkStraightLineRequired = true)
        {
            // Mock implementation - in real tests, this would compute safe location
            return position;
        }

        /// <summary>
        /// Performs a perception update on the creature for the target creature.
        /// </summary>
        /// <param name="creature">The creature object to update perception for. Must be a valid creature.</param>
        /// <param name="targetCreature">The target creature to check perception against. Must be a valid creature.</param>
        public void DoPerceptionUpdateOnCreature(uint creature, uint targetCreature)
        {
            // Mock implementation - in real tests, this would update perception
        }

        /// <summary>
        /// Retrieves the personal space value of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The personal space value in meters.</returns>
        public float GetPersonalSpace(uint creature)
        {
            // Mock implementation - in real tests, this would track personal space
            return 1.0f;
        }

        /// <summary>
        /// Sets the personal space value of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="personalSpace">The personal space value to set in meters. Must be non-negative.</param>
        public void SetPersonalSpace(uint creature, float personalSpace)
        {
            // Mock implementation - in real tests, this would track personal space
        }

        /// <summary>
        /// Retrieves the creature personal space value.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature personal space value in meters.</returns>
        public float GetCreaturePersonalSpace(uint creature)
        {
            // Mock implementation - in real tests, this would track creature personal space
            return 1.0f;
        }

        /// <summary>
        /// Sets the creature personal space value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="creaturePersonalSpace">The creature personal space value to set in meters. Must be non-negative.</param>
        public void SetCreaturePersonalSpace(uint creature, float creaturePersonalSpace)
        {
            // Mock implementation - in real tests, this would track creature personal space
        }

        /// <summary>
        /// Retrieves the height of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The height value in meters.</returns>
        public float GetHeight(uint creature)
        {
            // Mock implementation - in real tests, this would track height
            return 1.8f;
        }

        /// <summary>
        /// Sets the height of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="height">The height value to set in meters. Must be positive.</param>
        public void SetHeight(uint creature, float height)
        {
            // Mock implementation - in real tests, this would track height
        }

        /// <summary>
        /// Retrieves the hit distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The hit distance value in meters.</returns>
        public float GetHitDistance(uint creature)
        {
            // Mock implementation - in real tests, this would track hit distance
            return 1.5f;
        }

        /// <summary>
        /// Sets the hit distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="hitDistance">The hit distance value to set in meters. Must be positive.</param>
        public void SetHitDistance(uint creature, float hitDistance)
        {
            // Mock implementation - in real tests, this would track hit distance
        }

        /// <summary>
        /// Retrieves the preferred attack distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The preferred attack distance value in meters.</returns>
        public float GetPreferredAttackDistance(uint creature)
        {
            // Mock implementation - in real tests, this would track preferred attack distance
            return 2.0f;
        }

        /// <summary>
        /// Sets the preferred attack distance of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="preferredAttackDistance">The preferred attack distance value to set in meters. Must be positive.</param>
        public void SetPreferredAttackDistance(uint creature, float preferredAttackDistance)
        {
            // Mock implementation - in real tests, this would track preferred attack distance
        }

        /// <summary>
        /// Retrieves the armor check penalty of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The armor check penalty value.</returns>
        public int GetArmorCheckPenalty(uint creature)
        {
            // Mock implementation - in real tests, this would track armor check penalty
            return 0;
        }

        /// <summary>
        /// Retrieves the shield check penalty of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The shield check penalty value.</returns>
        public int GetShieldCheckPenalty(uint creature)
        {
            // Mock implementation - in real tests, this would track shield check penalty
            return 0;
        }

        /// <summary>
        /// Sets the bypass effect immunity for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="immunityType">The immunity type to bypass. See immunity types in game constants.</param>
        /// <param name="chance">The chance percentage to bypass the immunity (0-100). Default is 100.</param>
        /// <param name="persist">Whether the setting should persist to the .bic file. Default is false.</param>
        public void SetBypassEffectImmunity(uint creature, int immunityType, int chance = 100, bool persist = false)
        {
            // Mock implementation - in real tests, this would track bypass effect immunity
        }

        /// <summary>
        /// Retrieves the bypass effect immunity for the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="immunityType">The immunity type to query. See immunity types in game constants.</param>
        /// <returns>The bypass effect immunity chance value (0-100).</returns>
        public int GetBypassEffectImmunity(uint creature, int immunityType)
        {
            // Mock implementation - in real tests, this would track bypass effect immunity
            return 0;
        }

        /// <summary>
        /// Retrieves the creature's number of bonus spells for a specific class and spell level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="multiClass">The character class position, starting at 0. Must be between 0 and 2.</param>
        /// <param name="spellLevel">The spell level to query, 0 to 9.</param>
        /// <returns>The number of bonus spells for the specified class and spell level.</returns>
        public int GetNumberOfBonusSpells(uint creature, int multiClass, int spellLevel)
        {
            // Mock implementation - in real tests, this would track bonus spells
            return 0;
        }

        /// <summary>
        /// Modifies the creature's number of bonus spells for a specific class and spell level.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="multiClass">The character class position, starting at 0. Must be between 0 and 2.</param>
        /// <param name="spellLevel">The spell level to modify, 0 to 9.</param>
        /// <param name="delta">The value to change the number of bonus spells by. Can be positive or negative.</param>
        public void ModifyNumberBonusSpells(uint creature, int multiClass, int spellLevel, int delta)
        {
            // Mock implementation - in real tests, this would track bonus spells
        }

        /// <summary>
        /// Sets a caster level modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class that this modifier will apply to. See ClassType enum for available classes.</param>
        /// <param name="modifier">The modifier to apply to the caster level. Can be positive or negative.</param>
        /// <param name="persist">Whether the modifier should persist to the .bic file if applicable. Default is false.</param>
        public void SetCasterLevelModifier(uint creature, ClassType classId, int modifier, bool persist = false)
        {
            // Mock implementation - in real tests, this would track caster level modifier
        }

        /// <summary>
        /// Gets the current caster level modifier for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="classId">The creature caster class. See ClassType enum for available classes.</param>
        /// <returns>The current caster level modifier for the creature.</returns>
        public int GetCasterLevelModifier(uint creature, ClassType classId)
        {
            // Mock implementation - in real tests, this would track caster level modifier
            return 0;
        }

        /// <summary>
        /// Sets a caster level override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class that this override will apply to. See ClassType enum for available classes.</param>
        /// <param name="casterLevel">The caster level override to apply. Must be positive.</param>
        /// <param name="persist">Whether the override should persist to the .bic file if applicable. Default is false.</param>
        public void SetCasterLevelOverride(uint creature, ClassType classId, int casterLevel, bool persist = false)
        {
            // Mock implementation - in real tests, this would track caster level override
        }

        /// <summary>
        /// Gets the current caster level override for the creature.
        /// </summary>
        /// <param name="creature">The target creature object to query. Must be a valid creature.</param>
        /// <param name="classId">The creature caster class. See ClassType enum for available classes.</param>
        /// <returns>The current caster level override for the creature, or -1 if not set.</returns>
        public int GetCasterLevelOverride(uint creature, ClassType classId)
        {
            // Mock implementation - in real tests, this would track caster level override
            return -1;
        }

        /// <summary>
        /// Retrieves the remaining spell slots for the creature's innate casting ability.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="classId">The class ID to check. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to check. Must be between 0 and 9.</param>
        /// <returns>The number of remaining spell slots for the specified class and level.</returns>
        public int GetRemainingSpellSlots(uint creature, ClassType classId, int level)
        {
            // Mock implementation - in real tests, this would track spell slots
            return 0;
        }

        /// <summary>
        /// Sets the remaining spell slots for the creature's innate casting ability.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class ID to modify. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to modify. Must be between 0 and 9.</param>
        /// <param name="slots">The number of spell slots to set. Must be non-negative.</param>
        public void SetRemainingSpellSlots(uint creature, ClassType classId, int level, int slots)
        {
            // Mock implementation - in real tests, this would track spell slots
        }

        /// <summary>
        /// Removes a spell from the creature's spellbook for a specific class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class ID to modify. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to modify. Must be between 0 and 9.</param>
        /// <param name="spellId">The spell ID to remove. Must be a valid spell ID.</param>
        public void RemoveKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            // Mock implementation - in real tests, this would track known spells
        }

        /// <summary>
        /// Adds a spell to the creature's spellbook for a specific class.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class ID to modify. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to modify. Must be between 0 and 9.</param>
        /// <param name="spellId">The spell ID to add. Must be a valid spell ID.</param>
        public void AddKnownSpell(uint creature, ClassType classId, int level, int spellId)
        {
            // Mock implementation - in real tests, this would track known spells
        }

        /// <summary>
        /// Retrieves the maximum number of spell slots for the creature's class and level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="classId">The class ID to check. See ClassType enum for available classes.</param>
        /// <param name="level">The spell level to check. Must be between 0 and 9.</param>
        /// <returns>The maximum number of spell slots for the specified class and level.</returns>
        public int GetMaxSpellSlots(uint creature, ClassType classId, int level)
        {
            // Mock implementation - in real tests, this would calculate max spell slots
            return 0;
        }

        /// <summary>
        /// Sets the creature's movement rate.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="movementRate">The new movement rate value. Must be between 0.1 and 10.0.</param>
        public void SetMovementRate(uint creature, float movementRate)
        {
            GetCreatureData(creature).MovementRate = Math.Max(0.1f, Math.Min(10.0f, movementRate));
        }

        /// <summary>
        /// Gets the creature's movement rate.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's movement rate.</returns>
        public float GetMovementRate(uint creature)
        {
            return GetCreatureData(creature).MovementRate;
        }

        /// <summary>
        /// Retrieves the maximum hit points for the creature at a specific level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="level">The level to check. Must be a positive integer.</param>
        /// <returns>The maximum hit points for the specified level.</returns>
        public int GetMaxHitPointsByLevel(uint creature, int level)
        {
            // Mock implementation - in real tests, this would track hit points by level
            return 100;
        }

        /// <summary>
        /// Sets the maximum hit points for the creature at a specific level.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="level">The level to modify. Must be a positive integer.</param>
        /// <param name="value">The maximum hit points to set for the specified level. Must be non-negative.</param>
        public void SetMaxHitPointsByLevel(uint creature, int level, int value)
        {
            // Mock implementation - in real tests, this would track hit points by level
        }

        /// <summary>
        /// Sets the creature's base movement rate.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="rate">The movement rate to set. See MovementRate enum for available rates.</param>
        public void SetMovementRate(uint creature, MovementRateType rate)
        {
            // Mock implementation - in real tests, this would set movement rate type
        }

        /// <summary>
        /// Retrieves the creature's current movement rate factor.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The movement rate factor. Base value is 1.0.</returns>
        public float GetMovementRateFactor(uint creature)
        {
            // Mock implementation - in real tests, this would track movement rate factor
            return 1.0f;
        }

        /// <summary>
        /// Sets the creature's current movement rate factor.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="factor">The movement rate factor to set. Base value is 1.0.</param>
        public void SetMovementRateFactor(uint creature, float factor)
        {
            // Mock implementation - in real tests, this would track movement rate factor
        }

        /// <summary>
        /// Sets the creature's size.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="size">The new size value. See CreatureSize enum for available sizes.</param>
        public void SetSize(uint creature, CreatureSizeType size)
        {
            GetCreatureData(creature).Size = size;
        }

        /// <summary>
        /// Gets the creature's size.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's size.</returns>
        public CreatureSizeType GetSize(uint creature)
        {
            return GetCreatureData(creature).Size;
        }

        /// <summary>
        /// Restores all feat uses for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        public void RestoreFeats(uint creature)
        {
            // Mock implementation - in real tests, this would restore feat uses
        }

        /// <summary>
        /// Restores all special ability uses for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        public void RestoreSpecialAbilities(uint creature)
        {
            // Mock implementation - in real tests, this would restore special ability uses
        }

        /// <summary>
        /// Restores uses for all items carried by the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        public void RestoreItems(uint creature)
        {
            // Mock implementation - in real tests, this would restore item uses
        }


        /// <summary>
        /// Sets the creature's race.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="race">The new race value. See RacialType enum for available races.</param>
        public void SetRace(uint creature, RacialType race)
        {
            GetCreatureData(creature).Race = race;
        }

        /// <summary>
        /// Gets the creature's race.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's race.</returns>
        public RacialType GetRace(uint creature)
        {
            return GetCreatureData(creature).Race;
        }

        /// <summary>
        /// Retrieves the creature's remaining unspent skill points.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The number of remaining skill points available for spending.</returns>
        public int GetSkillPointsRemaining(uint creature)
        {
            // Mock implementation - in real tests, this would track skill points
            return 0;
        }

        /// <summary>
        /// Sets the creature's remaining unspent skill points.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="skillpoints">The number of skill points to set. Must be non-negative.</param>
        public void SetSkillPointsRemaining(uint creature, int skillpoints)
        {
            // Mock implementation - in real tests, this would track skill points
        }

        /// <summary>
        /// Sets the creature's racial type.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="racialtype">The racial type to set. See RacialType enum for available types.</param>
        public void SetRacialType(uint creature, RacialType racialtype)
        {
            GetCreatureData(creature).Race = racialtype;
        }

        /// <summary>
        /// Sets the creature's gender.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="gender">The new gender value. See Gender enum for available genders.</param>
        public void SetGender(uint creature, GenderType gender)
        {
            GetCreatureData(creature).Gender = gender;
        }

        /// <summary>
        /// Gets the creature's gender.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's gender.</returns>
        public GenderType GetGender(uint creature)
        {
            return GetCreatureData(creature).Gender;
        }

        /// <summary>
        /// Sets the creature's alignment.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="alignment">The new alignment value. See Alignment enum for available alignments.</param>
        public void SetAlignment(uint creature, AlignmentType alignment)
        {
            GetCreatureData(creature).Alignment = alignment;
        }

        /// <summary>
        /// Gets the creature's alignment.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's alignment.</returns>
        public AlignmentType GetAlignment(uint creature)
        {
            return GetCreatureData(creature).Alignment;
        }

        /// <summary>
        /// Sets the creature's raw good/evil alignment value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="value">The good/evil alignment value. Typically ranges from -100 (evil) to +100 (good).</param>
        public void SetAlignmentGoodEvil(uint creature, int value)
        {
            // Mock implementation - in real tests, this would track good/evil alignment
        }

        /// <summary>
        /// Sets the creature's raw law/chaos alignment value.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="value">The law/chaos alignment value. Typically ranges from -100 (chaotic) to +100 (lawful).</param>
        public void SetAlignmentLawChaos(uint creature, int value)
        {
            // Mock implementation - in real tests, this would track law/chaos alignment
        }


        /// <summary>
        /// Sets the creature's deity.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="deity">The new deity value. See Deity enum for available deities.</param>
        public void SetDeity(uint creature, int deity)
        {
            GetCreatureData(creature).Deity = deity;
        }

        /// <summary>
        /// Gets the creature's deity.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's deity.</returns>
        public int GetDeity(uint creature)
        {
            return GetCreatureData(creature).Deity;
        }

        /// <summary>
        /// Sets the creature's appearance.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="appearance">The new appearance value. See AppearanceType enum for available appearances.</param>
        public void SetAppearance(uint creature, AppearanceType appearance)
        {
            GetCreatureData(creature).Appearance = appearance;
        }

        /// <summary>
        /// Gets the creature's appearance.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's appearance.</returns>
        public AppearanceType GetAppearance(uint creature)
        {
            return GetCreatureData(creature).Appearance;
        }

        /// <summary>
        /// Sets the creature's portrait.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="portrait">The new portrait value. Must be between 0 and 255.</param>
        public void SetPortrait(uint creature, int portrait)
        {
            GetCreatureData(creature).Portrait = Math.Max(0, Math.Min(255, portrait));
        }

        /// <summary>
        /// Gets the creature's portrait.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's portrait.</returns>
        public int GetPortrait(uint creature)
        {
            return GetCreatureData(creature).Portrait;
        }

        /// <summary>
        /// Sets the creature's name.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="name">The new name value.</param>
        public void SetName(uint creature, string name)
        {
            GetCreatureData(creature).Name = name ?? "";
        }

        /// <summary>
        /// Gets the creature's name.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's name.</returns>
        public string GetName(uint creature)
        {
            return GetCreatureData(creature).Name;
        }

        /// <summary>
        /// Sets the creature's tag.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="tag">The new tag value.</param>
        public void SetTag(uint creature, string tag)
        {
            GetCreatureData(creature).Tag = tag ?? "";
        }

        /// <summary>
        /// Gets the creature's tag.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's tag.</returns>
        public string GetTag(uint creature)
        {
            return GetCreatureData(creature).Tag;
        }

        /// <summary>
        /// Sets the creature's resref.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="resRef">The new resref value.</param>
        public void SetResRef(uint creature, string resRef)
        {
            GetCreatureData(creature).ResRef = resRef ?? "";
        }

        /// <summary>
        /// Gets the creature's resref.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's resref.</returns>
        public string GetResRef(uint creature)
        {
            return GetCreatureData(creature).ResRef;
        }

        /// <summary>
        /// Sets the creature's description.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="description">The new description value.</param>
        public void SetDescription(uint creature, string description)
        {
            GetCreatureData(creature).Description = description ?? "";
        }

        /// <summary>
        /// Gets the creature's description.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's description.</returns>
        public string GetDescription(uint creature)
        {
            return GetCreatureData(creature).Description;
        }

        /// <summary>
        /// Sets the creature's position.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="position">The new position value.</param>
        public void SetPosition(uint creature, Vector3 position)
        {
            GetCreatureData(creature).Position = position;
        }

        /// <summary>
        /// Gets the creature's position.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's position.</returns>
        public Vector3 GetPosition(uint creature)
        {
            return GetCreatureData(creature).Position;
        }

        /// <summary>
        /// Sets the creature's facing.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="facing">The new facing value.</param>
        public void SetFacing(uint creature, float facing)
        {
            GetCreatureData(creature).Facing = facing;
        }

        /// <summary>
        /// Gets the creature's facing.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's facing.</returns>
        public float GetFacing(uint creature)
        {
            return GetCreatureData(creature).Facing;
        }

        /// <summary>
        /// Sets the creature's area.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="area">The new area value.</param>
        public void SetArea(uint creature, uint area)
        {
            GetCreatureData(creature).Area = area;
        }

        /// <summary>
        /// Gets the creature's area.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's area.</returns>
        public uint GetArea(uint creature)
        {
            return GetCreatureData(creature).Area;
        }

        /// <summary>
        /// Sets the creature's level.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="level">The new level value. Must be between 1 and 255.</param>
        public void SetLevel(uint creature, int level)
        {
            GetCreatureData(creature).Level = Math.Max(1, Math.Min(255, level));
        }

        /// <summary>
        /// Gets the creature's level.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's level.</returns>
        public int GetLevel(uint creature)
        {
            return GetCreatureData(creature).Level;
        }

        /// <summary>
        /// Adds the specified number of levels of the given class to the creature, bypassing all validation.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="classId">The class ID to add levels in. See ClassType enum for available classes.</param>
        /// <param name="count">The number of levels to add. Must be positive. Default is 1.</param>
        public void LevelUp(uint creature, ClassType classId, int count = 1)
        {
            // Mock implementation - in real tests, this would add levels
        }

        /// <summary>
        /// Removes the specified number of levels from the creature, starting with the most recent levels.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="count">The number of levels to remove. Must be positive. Default is 1.</param>
        public void LevelDown(uint creature, int count = 1)
        {
            // Mock implementation - in real tests, this would remove levels
        }

        /// <summary>
        /// Sets the creature's challenge rating.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="fCR">The challenge rating to set. Must be non-negative.</param>
        public void SetChallengeRating(uint creature, float fCR)
        {
            // Mock implementation - in real tests, this would set challenge rating
        }

        /// <summary>
        /// Sets the creature's experience points.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="experience">The new experience points value. Must be between 0 and 2147483647.</param>
        public void SetExperience(uint creature, int experience)
        {
            GetCreatureData(creature).Experience = Math.Max(0, experience);
        }

        /// <summary>
        /// Gets the creature's experience points.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's experience points.</returns>
        public int GetExperience(uint creature)
        {
            return GetCreatureData(creature).Experience;
        }

        /// <summary>
        /// Sets the creature's gold.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="gold">The new gold value. Must be between 0 and 2147483647.</param>
        public void SetGold(uint creature, int gold)
        {
            GetCreatureData(creature).Gold = Math.Max(0, gold);
        }

        /// <summary>
        /// Gets the creature's gold.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's gold.</returns>
        public int GetGold(uint creature)
        {
            return GetCreatureData(creature).Gold;
        }

        /// <summary>
        /// Retrieves the creature's current movement type.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's current movement type. See MovementType enum for possible values.</returns>
        public MovementType GetMovementType(uint creature)
        {
            // Mock implementation - in real tests, this would track movement type
            return MovementType.Walk;
        }

        /// <summary>
        /// Sets the maximum movement rate a creature can have while walking (not running).
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="fWalkRate">The walk rate cap. Setting to -1.0 removes the cap. Default is 2000.0 (base human walk speed).</param>
        public void SetWalkRateCap(uint creature, float fWalkRate)
        {
            // Mock implementation - in real tests, this would set walk rate cap
        }


        /// <summary>
        /// Sets the creature's special ability.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">The special ability to modify.</param>
        public void SetSpecialAbility(uint creature, SpecialAbilitySlot ability)
        {
            GetCreatureData(creature).SpecialAbilities.Add(ability);
        }

        /// <summary>
        /// Gets the creature's special abilities.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's special abilities.</returns>
        public List<SpecialAbilitySlot> GetSpecialAbilities(uint creature)
        {
            return new List<SpecialAbilitySlot>(GetCreatureData(creature).SpecialAbilities);
        }

        /// <summary>
        /// Removes the creature's special ability.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">The special ability to remove.</param>
        public void RemoveSpecialAbility(uint creature, SpecialAbilitySlot ability)
        {
            GetCreatureData(creature).SpecialAbilities.Remove(ability);
        }

        /// <summary>
        /// Retrieves a special ability from the creature's special ability list by index.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="index">The index of the special ability. Must be between 0 and GetSpecialAbilityCount() - 1.</param>
        /// <returns>A SpecialAbilitySlot struct containing the special ability information.</returns>
        public SpecialAbilitySlot GetSpecialAbility(uint creature, int index)
        {
            var creatureData = GetCreatureData(creature);
            if (index >= 0 && index < creatureData.SpecialAbilities.Count)
            {
                return creatureData.SpecialAbilities[index];
            }
            return new SpecialAbilitySlot();
        }

        /// <summary>
        /// Retrieves the total number of special abilities possessed by the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The total number of special abilities in the creature's ability list.</returns>
        public int GetSpecialAbilityCount(uint creature)
        {
            return GetCreatureData(creature).SpecialAbilities.Count;
        }

        /// <summary>
        /// Adds a special ability to the creature's special ability list.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="ability">A SpecialAbilitySlot struct containing the ability information to add.</param>
        public void AddSpecialAbility(uint creature, SpecialAbilitySlot ability)
        {
            GetCreatureData(creature).SpecialAbilities.Add(ability);
        }

        /// <summary>
        /// Removes a special ability from the creature's special ability list by index.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="index">The index of the special ability to remove. Must be between 0 and GetSpecialAbilityCount() - 1.</param>
        public void RemoveSpecialAbility(uint creature, int index)
        {
            var creatureData = GetCreatureData(creature);
            if (index >= 0 && index < creatureData.SpecialAbilities.Count)
            {
                creatureData.SpecialAbilities.RemoveAt(index);
            }
        }

        /// <summary>
        /// Modifies a special ability at the specified index in the creature's special ability list.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="index">The index of the special ability to modify. Must be between 0 and GetSpecialAbilityCount() - 1.</param>
        /// <param name="ability">A SpecialAbilitySlot struct containing the new ability information.</param>
        public void SetSpecialAbility(uint creature, int index, SpecialAbilitySlot ability)
        {
            var creatureData = GetCreatureData(creature);
            if (index >= 0 && index < creatureData.SpecialAbilities.Count)
            {
                creatureData.SpecialAbilities[index] = ability;
            }
        }

        /// <summary>
        /// Sets the creature's devastating critical data.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="data">The devastating critical data to set.</param>
        public void SetDevastatingCriticalData(uint creature, DevastatingCriticalData data)
        {
            GetCreatureData(creature).DevastatingCriticalData = data;
        }

        /// <summary>
        /// Gets the creature's devastating critical data.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The creature's devastating critical data.</returns>
        public DevastatingCriticalData GetDevastatingCriticalData(uint creature)
        {
            return GetCreatureData(creature).DevastatingCriticalData;
        }

        /// <summary>
        /// Retrieves the total number of feats known by the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <returns>The total number of feats in the creature's feat list.</returns>
        public int GetFeatCount(uint creature)
        {
            return GetCreatureData(creature).Feats.Count;
        }

        /// <summary>
        /// Retrieves a specific feat from the creature's overall feat list by index.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="index">The index of the feat in the creature's feat list. Must be between 0 and GetFeatCount() - 1.</param>
        /// <returns>The feat type at the specified index.</returns>
        public FeatType GetFeatByIndex(uint creature, int index)
        {
            var creatureData = GetCreatureData(creature);
            if (index >= 0 && index < creatureData.Feats.Count)
            {
                return creatureData.Feats[index];
            }
            return FeatType.Invalid;
        }

        /// <summary>
        /// Determines whether the creature meets all requirements to take a specific feat.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat type to check requirements for. See FeatType enum for available feats.</param>
        /// <returns>True if the creature meets all requirements to take the feat, false otherwise.</returns>
        public bool GetMeetsFeatRequirements(uint creature, FeatType feat)
        {
            // Mock implementation - in real tests, this would check feat prerequisites
            return true;
        }

        /// <summary>
        /// Retrieves the creature's highest attack bonus based on its own stats.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="isMelee">True to get melee/unarmed attack bonus, false to get ranged attack bonus. Default is true.</param>
        /// <param name="isTouchAttack">Whether this is a touch attack. Default is false.</param>
        /// <param name="isOffhand">Whether this is an offhand attack. Default is false.</param>
        /// <param name="includeBaseAttackBonus">Whether to include base attack bonus. Default is true.</param>
        /// <returns>The attack bonus value.</returns>
        public int GetAttackBonus(uint creature, bool isMelee = true, bool isTouchAttack = false,
            bool isOffhand = false, bool includeBaseAttackBonus = true)
        {
            // Mock implementation - in real tests, this would calculate attack bonus
            return 0;
        }

        /// <summary>
        /// Retrieves the highest level version of a feat possessed by the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat ID to check. Must be a valid feat ID.</param>
        /// <returns>The highest level of the feat possessed by the creature.</returns>
        public int GetHighestLevelOfFeat(uint creature, int feat)
        {
            // Mock implementation - in real tests, this would track feat levels
            return 0;
        }

        /// <summary>
        /// Retrieves the remaining uses of a feat for the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat type to check. See FeatType enum for available feats.</param>
        /// <returns>The number of remaining uses for the specified feat.</returns>
        public int GetFeatRemainingUses(uint creature, FeatType feat)
        {
            // Mock implementation - in real tests, this would track feat uses
            return 0;
        }

        /// <summary>
        /// Retrieves the total uses of a feat for the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="feat">The feat type to check. See FeatType enum for available feats.</param>
        /// <returns>The total number of uses for the specified feat.</returns>
        public int GetFeatTotalUses(uint creature, FeatType feat)
        {
            // Mock implementation - in real tests, this would track feat uses
            return 0;
        }

        /// <summary>
        /// Sets the remaining uses of a feat for the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="feat">The feat type to modify. See FeatType enum for available feats.</param>
        /// <param name="uses">The number of remaining uses to set. Must be non-negative.</param>
        public void SetFeatRemainingUses(uint creature, FeatType feat, int uses)
        {
            // Mock implementation - in real tests, this would track feat uses
        }

        /// <summary>
        /// Retrieves the total effect bonus for the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="bonusType">The bonus type to calculate. See BonusType enum for available types. Default is Attack.</param>
        /// <param name="target">The target object for the bonus calculation. Use OBJECT_INVALID for general bonuses. Default is OBJECT_INVALID.</param>
        /// <param name="isElemental">Whether this is an elemental bonus. Default is false.</param>
        /// <param name="isForceMax">Whether to force maximum bonus calculation. Default is false.</param>
        /// <param name="saveType">The save type for saving throw bonuses. Use -1 for general bonuses. Default is -1.</param>
        /// <param name="saveSpecificType">The specific save type for saving throw bonuses. Use -1 for general bonuses. Default is -1.</param>
        /// <param name="skill">The skill type for skill bonuses. Use NWNSkillType.Invalid for general bonuses. Default is Invalid.</param>
        /// <param name="abilityScore">The ability score for ability bonuses. Use -1 for general bonuses. Default is -1.</param>
        /// <param name="isOffhand">Whether this is an offhand bonus. Default is false.</param>
        /// <returns>The total effect bonus value.</returns>
        public int GetTotalEffectBonus(uint creature, BonusType bonusType = BonusType.Attack,
            uint target = OBJECT_INVALID, bool isElemental = false,
            bool isForceMax = false, int saveType = -1, int saveSpecificType = -1, NWNSkillType skill = NWNSkillType.Invalid,
            int abilityScore = -1, bool isOffhand = false)
        {
            // Mock implementation - in real tests, this would calculate total effect bonus
            return 0;
        }

        /// <summary>
        /// Sets the original first or last name of the creature.
        /// </summary>
        /// <param name="creature">The creature object to modify. Must be a valid creature.</param>
        /// <param name="name">The name to set. Cannot be null or empty.</param>
        /// <param name="isLastName">True to set the last name, false to set the first name.</param>
        public void SetOriginalName(uint creature, string name, bool isLastName)
        {
            // Mock implementation - in real tests, this would track original names
        }

        /// <summary>
        /// Retrieves the original first or last name of the creature.
        /// </summary>
        /// <param name="creature">The creature object to query. Must be a valid creature.</param>
        /// <param name="isLastName">True to get the last name, false to get the first name.</param>
        /// <returns>The original name of the creature.</returns>
        public string GetOriginalName(uint creature, bool isLastName)
        {
            // Mock implementation - in real tests, this would track original names
            return "";
        }

        // Helper methods for testing
        /// <summary>
        /// Resets all mock data to default values for testing.
        /// </summary>
        public void Reset()
        {
            _creatureData.Clear();
        }

        /// <summary>
        /// Gets the creature data for the specified creature, creating it if it doesn't exist.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The creature data for the specified creature.</returns>
        private CreatureData GetCreatureData(uint creature)
        {
            if (!_creatureData.TryGetValue(creature, out var data))
            {
                data = new CreatureData();
                _creatureData[creature] = data;
            }
            return data;
        }

        /// <summary>
        /// Gets the creature data for testing verification.
        /// </summary>
        /// <param name="creature">The creature object.</param>
        /// <returns>The creature data for the specified creature.</returns>
        public CreatureData GetCreatureDataForTesting(uint creature)
        {
            return GetCreatureData(creature);
        }

        // Helper classes
        public class CreatureData
        {
            public List<FeatType> Feats { get; set; } = new();
            public Dictionary<int, List<FeatType>> FeatsByLevel { get; set; } = new();
            public Dictionary<AbilityType, int> AbilityScores { get; set; } = new();
            public Dictionary<NWNSkillType, int> SkillRanks { get; set; } = new();
            public Dictionary<ClassType, int> ClassLevels { get; set; } = new();
            public int HitPoints { get; set; } = 100;
            public int MaxHitPoints { get; set; } = 100;
            public int ArmorClass { get; set; } = 10;
            public int BaseAttackBonus { get; set; } = 0;
            public Dictionary<SavingThrowType, int> SavingThrowBonuses { get; set; } = new();
            public int SpellResistance { get; set; } = 0;
            public float MovementRate { get; set; } = 1.0f;
            public CreatureSizeType Size { get; set; } = CreatureSizeType.Medium;
            public RacialType Race { get; set; } = RacialType.Human;
            public GenderType Gender { get; set; } = GenderType.Male;
            public AlignmentType Alignment { get; set; } = AlignmentType.Neutral;
            public int Deity { get; set; } = 0;
            public AppearanceType Appearance { get; set; } = AppearanceType.Human;
            public int Portrait { get; set; } = 0;
            public string Name { get; set; } = "";
            public string Tag { get; set; } = "";
            public string ResRef { get; set; } = "";
            public string Description { get; set; } = "";
            public Vector3 Position { get; set; } = Vector3.Zero;
            public float Facing { get; set; } = 0.0f;
            public uint Area { get; set; } = OBJECT_INVALID;
            public int Level { get; set; } = 1;
            public int Experience { get; set; } = 0;
            public int Gold { get; set; } = 0;
            public List<SpecialAbilitySlot> SpecialAbilities { get; set; } = new();
            public DevastatingCriticalData DevastatingCriticalData { get; set; } = new();
        }

        // Constants
        private const uint OBJECT_INVALID = 0x7F000000;
    }
}
