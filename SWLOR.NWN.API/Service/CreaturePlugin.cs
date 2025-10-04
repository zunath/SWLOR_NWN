using System;
using System.Numerics;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Model;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.Service
{
    public static class CreaturePlugin
    {
        private static ICreaturePluginService _service = new CreaturePluginService();

        internal static void SetService(ICreaturePluginService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <inheritdoc cref="ICreaturePluginService.AddFeat"/>
        public static void AddFeat(uint creature, FeatType feat) => _service.AddFeat(creature, feat);

        /// <inheritdoc cref="ICreaturePluginService.AddFeatByLevel"/>
        public static void AddFeatByLevel(uint creature, FeatType feat, int level) => _service.AddFeatByLevel(creature, feat, level);

        /// <inheritdoc cref="ICreaturePluginService.RemoveFeat"/>
        public static void RemoveFeat(uint creature, FeatType feat) => _service.RemoveFeat(creature, feat);

        /// <inheritdoc cref="ICreaturePluginService.GetKnowsFeat"/>
        public static bool GetKnowsFeat(uint creature, FeatType feat) => _service.GetKnowsFeat(creature, feat);

        /// <inheritdoc cref="ICreaturePluginService.GetFeatCountByLevel"/>
        public static int GetFeatCountByLevel(uint creature, int level) => _service.GetFeatCountByLevel(creature, level);

        /// <inheritdoc cref="ICreaturePluginService.GetFeatByLevel"/>
        public static FeatType GetFeatByLevel(uint creature, int level, int index) => _service.GetFeatByLevel(creature, level, index);

        /// <inheritdoc cref="ICreaturePluginService.GetFeatCount"/>
        public static int GetFeatCount(uint creature) => _service.GetFeatCount(creature);

        /// <inheritdoc cref="ICreaturePluginService.GetFeatByIndex"/>
        public static FeatType GetFeatByIndex(uint creature, int index) => _service.GetFeatByIndex(creature, index);

        /// <inheritdoc cref="ICreaturePluginService.GetMeetsFeatRequirements"/>
        public static bool GetMeetsFeatRequirements(uint creature, FeatType feat) => _service.GetMeetsFeatRequirements(creature, feat);

        /// <inheritdoc cref="ICreaturePluginService.GetSpecialAbility"/>
        public static SpecialAbilitySlot GetSpecialAbility(uint creature, int index) => _service.GetSpecialAbility(creature, index);

        /// <inheritdoc cref="ICreaturePluginService.GetSpecialAbilityCount"/>
        public static int GetSpecialAbilityCount(uint creature) => _service.GetSpecialAbilityCount(creature);

        /// <inheritdoc cref="ICreaturePluginService.AddSpecialAbility"/>
        public static void AddSpecialAbility(uint creature, SpecialAbilitySlot ability) => _service.AddSpecialAbility(creature, ability);

        /// <inheritdoc cref="ICreaturePluginService.RemoveSpecialAbility"/>
        public static void RemoveSpecialAbility(uint creature, int index) => _service.RemoveSpecialAbility(creature, index);

        /// <inheritdoc cref="ICreaturePluginService.SetSpecialAbility"/>
        public static void SetSpecialAbility(uint creature, int index, SpecialAbilitySlot ability) => _service.SetSpecialAbility(creature, index, ability);

        /// <inheritdoc cref="ICreaturePluginService.GetClassByLevel"/>
        public static ClassType GetClassByLevel(uint creature, int level) => _service.GetClassByLevel(creature, level);

        /// <inheritdoc cref="ICreaturePluginService.SetBaseAC"/>
        public static void SetBaseAC(uint creature, int ac) => _service.SetBaseAC(creature, ac);

        /// <inheritdoc cref="ICreaturePluginService.GetBaseAC"/>
        public static int GetBaseAC(uint creature) => _service.GetBaseAC(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetRawAbilityScore"/>
        public static void SetRawAbilityScore(uint creature, AbilityType ability, int value) => _service.SetRawAbilityScore(creature, ability, value);

        /// <inheritdoc cref="ICreaturePluginService.GetRawAbilityScore"/>
        public static int GetRawAbilityScore(uint creature, AbilityType ability) => _service.GetRawAbilityScore(creature, ability);

        /// <inheritdoc cref="ICreaturePluginService.ModifyRawAbilityScore"/>
        public static void ModifyRawAbilityScore(uint creature, AbilityType ability, int modifier) => _service.ModifyRawAbilityScore(creature, ability, modifier);

        /// <inheritdoc cref="ICreaturePluginService.GetPrePolymorphAbilityScore"/>
        public static int GetPrePolymorphAbilityScore(uint creature, AbilityType ability) => _service.GetPrePolymorphAbilityScore(creature, ability);

        /// <inheritdoc cref="ICreaturePluginService.GetRemainingSpellSlots"/>
        public static int GetRemainingSpellSlots(uint creature, ClassType classId, int level) => _service.GetRemainingSpellSlots(creature, classId, level);

        /// <inheritdoc cref="ICreaturePluginService.SetRemainingSpellSlots"/>
        public static void SetRemainingSpellSlots(uint creature, ClassType classId, int level, int slots) => _service.SetRemainingSpellSlots(creature, classId, level, slots);

        /// <inheritdoc cref="ICreaturePluginService.RemoveKnownSpell"/>
        public static void RemoveKnownSpell(uint creature, ClassType classId, int level, int spellId) => _service.RemoveKnownSpell(creature, classId, level, spellId);

        /// <inheritdoc cref="ICreaturePluginService.AddKnownSpell"/>
        public static void AddKnownSpell(uint creature, ClassType classId, int level, int spellId) => _service.AddKnownSpell(creature, classId, level, spellId);

        /// <inheritdoc cref="ICreaturePluginService.GetMaxSpellSlots"/>
        public static int GetMaxSpellSlots(uint creature, ClassType classId, int level) => _service.GetMaxSpellSlots(creature, classId, level);

        /// <inheritdoc cref="ICreaturePluginService.GetMaxHitPointsByLevel"/>
        public static int GetMaxHitPointsByLevel(uint creature, int level) => _service.GetMaxHitPointsByLevel(creature, level);

        /// <inheritdoc cref="ICreaturePluginService.SetMaxHitPointsByLevel"/>
        public static void SetMaxHitPointsByLevel(uint creature, int level, int value) => _service.SetMaxHitPointsByLevel(creature, level, value);

        /// <inheritdoc cref="ICreaturePluginService.SetMovementRate"/>
        public static void SetMovementRate(uint creature, MovementRateType rate) => _service.SetMovementRate(creature, rate);

        /// <inheritdoc cref="ICreaturePluginService.GetMovementRateFactor"/>
        public static float GetMovementRateFactor(uint creature) => _service.GetMovementRateFactor(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetMovementRateFactor"/>
        public static void SetMovementRateFactor(uint creature, float factor) => _service.SetMovementRateFactor(creature, factor);

        /// <inheritdoc cref="ICreaturePluginService.SetAlignmentGoodEvil"/>
        public static void SetAlignmentGoodEvil(uint creature, int value) => _service.SetAlignmentGoodEvil(creature, value);

        /// <inheritdoc cref="ICreaturePluginService.SetAlignmentLawChaos"/>
        public static void SetAlignmentLawChaos(uint creature, int value) => _service.SetAlignmentLawChaos(creature, value);

        /// <inheritdoc cref="ICreaturePluginService.SetSkillRank"/>
        public static void SetSkillRank(uint creature, NWNSkillType skill, int rank) => _service.SetSkillRank(creature, skill, rank);

        /// <inheritdoc cref="ICreaturePluginService.SetClassByPosition"/>
        public static void SetClassByPosition(uint creature, int position, ClassType classId, bool updateLevels = true) => _service.SetClassByPosition(creature, position, classId, updateLevels);

        /// <inheritdoc cref="ICreaturePluginService.SetBaseAttackBonus"/>
        public static void SetBaseAttackBonus(uint creature, int bab) => _service.SetBaseAttackBonus(creature, bab);

        /// <inheritdoc cref="ICreaturePluginService.GetAttacksPerRound"/>
        public static int GetAttacksPerRound(uint creature, bool bBaseAPR) => _service.GetAttacksPerRound(creature, bBaseAPR);

        /// <inheritdoc cref="ICreaturePluginService.RestoreFeats"/>
        public static void RestoreFeats(uint creature) => _service.RestoreFeats(creature);

        /// <inheritdoc cref="ICreaturePluginService.RestoreSpecialAbilities"/>
        public static void RestoreSpecialAbilities(uint creature) => _service.RestoreSpecialAbilities(creature);

        /// <inheritdoc cref="ICreaturePluginService.RestoreItems"/>
        public static void RestoreItems(uint creature) => _service.RestoreItems(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetSize"/>
        public static void SetSize(uint creature, CreatureSizeType creatureSize) => _service.SetSize(creature, creatureSize);

        /// <inheritdoc cref="ICreaturePluginService.GetSkillPointsRemaining"/>
        public static int GetSkillPointsRemaining(uint creature) => _service.GetSkillPointsRemaining(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetSkillPointsRemaining"/>
        public static void SetSkillPointsRemaining(uint creature, int skillpoints) => _service.SetSkillPointsRemaining(creature, skillpoints);

        /// <inheritdoc cref="ICreaturePluginService.SetRacialType"/>
        public static void SetRacialType(uint creature, RacialType racialtype) => _service.SetRacialType(creature, racialtype);

        /// <inheritdoc cref="ICreaturePluginService.GetMovementType"/>
        public static MovementType GetMovementType(uint creature) => _service.GetMovementType(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetWalkRateCap"/>
        public static void SetWalkRateCap(uint creature, float fWalkRate) => _service.SetWalkRateCap(creature, fWalkRate);

        /// <inheritdoc cref="ICreaturePluginService.SetGold"/>
        public static void SetGold(uint creature, int gold) => _service.SetGold(creature, gold);

        /// <inheritdoc cref="ICreaturePluginService.SetCorpseDecayTime"/>
        public static void SetCorpseDecayTime(uint creature, int decayTimeMs) => _service.SetCorpseDecayTime(creature, decayTimeMs);

        /// <inheritdoc cref="ICreaturePluginService.GetBaseSavingThrow"/>
        public static int GetBaseSavingThrow(uint creature, int which) => _service.GetBaseSavingThrow(creature, which);

        /// <inheritdoc cref="ICreaturePluginService.SetBaseSavingThrow"/>
        public static void SetBaseSavingThrow(uint creature, SavingThrowCategoryType which, int value) => _service.SetBaseSavingThrow(creature, which, value);

        /// <inheritdoc cref="ICreaturePluginService.LevelUp"/>
        public static void LevelUp(uint creature, ClassType classId, int count = 1) => _service.LevelUp(creature, classId, count);

        /// <inheritdoc cref="ICreaturePluginService.LevelDown"/>
        public static void LevelDown(uint creature, int count = 1) => _service.LevelDown(creature, count);

        /// <inheritdoc cref="ICreaturePluginService.SetChallengeRating"/>
        public static void SetChallengeRating(uint creature, float fCR) => _service.SetChallengeRating(creature, fCR);

        /// <inheritdoc cref="ICreaturePluginService.GetAttackBonus"/>
        public static int GetAttackBonus(uint creature, bool isMelee = true, bool isTouchAttack = false, bool isOffhand = false, bool includeBaseAttackBonus = true) => 
            _service.GetAttackBonus(creature, isMelee, isTouchAttack, isOffhand, includeBaseAttackBonus);

        /// <inheritdoc cref="ICreaturePluginService.GetHighestLevelOfFeat"/>
        public static int GetHighestLevelOfFeat(uint creature, int feat) => _service.GetHighestLevelOfFeat(creature, feat);

        /// <inheritdoc cref="ICreaturePluginService.GetFeatRemainingUses"/>
        public static int GetFeatRemainingUses(uint creature, FeatType feat) => _service.GetFeatRemainingUses(creature, feat);

        /// <inheritdoc cref="ICreaturePluginService.GetFeatTotalUses"/>
        public static int GetFeatTotalUses(uint creature, FeatType feat) => _service.GetFeatTotalUses(creature, feat);

        /// <inheritdoc cref="ICreaturePluginService.SetFeatRemainingUses"/>
        public static void SetFeatRemainingUses(uint creature, FeatType feat, int uses) => _service.SetFeatRemainingUses(creature, feat, uses);

        /// <inheritdoc cref="ICreaturePluginService.GetTotalEffectBonus"/>
        public static int GetTotalEffectBonus(uint creature, BonusType bonusType = BonusType.Attack, uint target = OBJECT_INVALID, bool isElemental = false, bool isForceMax = false, int saveType = -1, int saveSpecificType = -1, NWNSkillType skill = NWNSkillType.Invalid, int abilityScore = -1, bool isOffhand = false) => 
            _service.GetTotalEffectBonus(creature, bonusType, target, isElemental, isForceMax, saveType, saveSpecificType, skill, abilityScore, isOffhand);

        /// <inheritdoc cref="ICreaturePluginService.SetOriginalName"/>
        public static void SetOriginalName(uint creature, string name, bool isLastName) => _service.SetOriginalName(creature, name, isLastName);

        /// <inheritdoc cref="ICreaturePluginService.GetOriginalName"/>
        public static string GetOriginalName(uint creature, bool isLastName) => _service.GetOriginalName(creature, isLastName);

        /// <inheritdoc cref="ICreaturePluginService.SetSpellResistance"/>
        public static void SetSpellResistance(uint creature, int sr) => _service.SetSpellResistance(creature, sr);

        /// <inheritdoc cref="ICreaturePluginService.SetAnimalCompanionName"/>
        public static void SetAnimalCompanionName(uint creature, string name) => _service.SetAnimalCompanionName(creature, name);

        /// <inheritdoc cref="ICreaturePluginService.SetFamiliarName"/>
        public static void SetFamiliarName(uint creature, string name) => _service.SetFamiliarName(creature, name);

        /// <inheritdoc cref="ICreaturePluginService.GetDisarmable"/>
        public static bool GetDisarmable(uint creature) => _service.GetDisarmable(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetDisarmable"/>
        public static void SetDisarmable(uint creature, bool isDisarmable) => _service.SetDisarmable(creature, isDisarmable);

        /// <inheritdoc cref="ICreaturePluginService.SetDomain"/>
        public static void SetDomain(uint creature, ClassType @class, int index, int domain) => _service.SetDomain(creature, @class, index, domain);

        /// <inheritdoc cref="ICreaturePluginService.SetSpecialization"/>
        public static void SetSpecialization(uint creature, ClassType @class, int school) => _service.SetSpecialization(creature, @class, school);

        /// <inheritdoc cref="ICreaturePluginService.SetFaction"/>
        public static void SetFaction(uint creature, int factionId) => _service.SetFaction(creature, factionId);

        /// <inheritdoc cref="ICreaturePluginService.GetFaction"/>
        public static int GetFaction(uint creature) => _service.GetFaction(creature);

        /// <inheritdoc cref="ICreaturePluginService.GetFlatFooted"/>
        public static bool GetFlatFooted(uint creature) => _service.GetFlatFooted(creature);

        /// <inheritdoc cref="ICreaturePluginService.SerializeQuickbar"/>
        public static string SerializeQuickbar(uint creature) => _service.SerializeQuickbar(creature);

        /// <inheritdoc cref="ICreaturePluginService.DeserializeQuickbar"/>
        public static bool DeserializeQuickbar(uint creature, string serializedQuickbar) => _service.DeserializeQuickbar(creature, serializedQuickbar);

        /// <inheritdoc cref="ICreaturePluginService.SetEncounter"/>
        public static void SetEncounter(uint creature, uint encounter) => _service.SetEncounter(creature, encounter);

        /// <inheritdoc cref="ICreaturePluginService.GetEncounter"/>
        public static uint GetEncounter(uint creature) => _service.GetEncounter(creature);

        /// <inheritdoc cref="ICreaturePluginService.OverrideDamageLevel"/>
        public static void OverrideDamageLevel(uint creature, int damageLevel) => _service.OverrideDamageLevel(creature, damageLevel);

        /// <inheritdoc cref="ICreaturePluginService.GetIsBartering"/>
        public static bool GetIsBartering(uint creature) => _service.GetIsBartering(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetLastItemCasterLevel"/>
        public static void SetLastItemCasterLevel(uint creature, int casterLevel) => _service.SetLastItemCasterLevel(creature, casterLevel);

        /// <inheritdoc cref="ICreaturePluginService.GetLastItemCasterLevel"/>
        public static int GetLastItemCasterLevel(uint creature) => _service.GetLastItemCasterLevel(creature);

        /// <inheritdoc cref="ICreaturePluginService.GetArmorClassVersus"/>
        public static int GetArmorClassVersus(uint attacked, uint versus, bool touch = false) => _service.GetArmorClassVersus(attacked, versus, touch);

        /// <inheritdoc cref="ICreaturePluginService.JumpToLimbo"/>
        public static void JumpToLimbo(uint creature) => _service.JumpToLimbo(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetCriticalMultiplierModifier"/>
        public static void SetCriticalMultiplierModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid) => 
            _service.SetCriticalMultiplierModifier(creature, modifier, hand, persist, baseItemType);

        /// <inheritdoc cref="ICreaturePluginService.GetCriticalMultiplierModifier"/>
        public static int GetCriticalMultiplierModifier(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid) => 
            _service.GetCriticalMultiplierModifier(creature, hand, baseItemType);

        /// <inheritdoc cref="ICreaturePluginService.SetCriticalMultiplierOverride"/>
        public static void SetCriticalMultiplierOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid) => 
            _service.SetCriticalMultiplierOverride(creature, @override, hand, persist, baseItemType);

        /// <inheritdoc cref="ICreaturePluginService.GetCriticalMultiplierOverride"/>
        public static int GetCriticalMultiplierOverride(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid) => 
            _service.GetCriticalMultiplierOverride(creature, hand, baseItemType);

        /// <inheritdoc cref="ICreaturePluginService.SetCriticalRangeModifier"/>
        public static void SetCriticalRangeModifier(uint creature, int modifier, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid) => 
            _service.SetCriticalRangeModifier(creature, modifier, hand, persist, baseItemType);

        /// <inheritdoc cref="ICreaturePluginService.GetCriticalRangeModifier"/>
        public static int GetCriticalRangeModifier(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid) => 
            _service.GetCriticalRangeModifier(creature, hand, baseItemType);

        /// <inheritdoc cref="ICreaturePluginService.SetCriticalRangeOverride"/>
        public static void SetCriticalRangeOverride(uint creature, int @override, int hand = 0, bool persist = false, BaseItemType baseItemType = BaseItemType.Invalid) => 
            _service.SetCriticalRangeOverride(creature, @override, hand, persist, baseItemType);

        /// <inheritdoc cref="ICreaturePluginService.GetCriticalRangeOverride"/>
        public static int GetCriticalRangeOverride(uint creature, int hand = 0, BaseItemType baseItemType = BaseItemType.Invalid) => 
            _service.GetCriticalRangeOverride(creature, hand, baseItemType);

        /// <inheritdoc cref="ICreaturePluginService.AddAssociate"/>
        public static void AddAssociate(uint creature, uint associate, int associateType) => _service.AddAssociate(creature, associate, associateType);

        /// <inheritdoc cref="ICreaturePluginService.GetWalkAnimation"/>
        public static int GetWalkAnimation(uint creature) => _service.GetWalkAnimation(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetWalkAnimation"/>
        public static void SetWalkAnimation(uint creature, int animation) => _service.SetWalkAnimation(creature, animation);

        /// <inheritdoc cref="ICreaturePluginService.SetAttackRollOverride"/>
        public static void SetAttackRollOverride(uint creature, int roll, int modifier) => _service.SetAttackRollOverride(creature, roll, modifier);

        /// <inheritdoc cref="ICreaturePluginService.SetParryAllAttacks"/>
        public static void SetParryAllAttacks(uint creature, bool parry) => _service.SetParryAllAttacks(creature, parry);

        /// <inheritdoc cref="ICreaturePluginService.GetNoPermanentDeath"/>
        public static bool GetNoPermanentDeath(uint creature) => _service.GetNoPermanentDeath(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetNoPermanentDeath"/>
        public static void SetNoPermanentDeath(uint creature, bool noPermanentDeath) => _service.SetNoPermanentDeath(creature, noPermanentDeath);

        /// <inheritdoc cref="ICreaturePluginService.ComputeSafeLocation"/>
        public static Vector3 ComputeSafeLocation(uint creature, Vector3 position, float radius = 20.0f, bool walkStraightLineRequired = true) => 
            _service.ComputeSafeLocation(creature, position, radius, walkStraightLineRequired);

        /// <inheritdoc cref="ICreaturePluginService.DoPerceptionUpdateOnCreature"/>
        public static void DoPerceptionUpdateOnCreature(uint creature, uint targetCreature) => _service.DoPerceptionUpdateOnCreature(creature, targetCreature);

        /// <inheritdoc cref="ICreaturePluginService.GetPersonalSpace"/>
        public static float GetPersonalSpace(uint creature) => _service.GetPersonalSpace(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetPersonalSpace"/>
        public static void SetPersonalSpace(uint creature, float personalSpace) => _service.SetPersonalSpace(creature, personalSpace);

        /// <inheritdoc cref="ICreaturePluginService.GetCreaturePersonalSpace"/>
        public static float GetCreaturePersonalSpace(uint creature) => _service.GetCreaturePersonalSpace(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetCreaturePersonalSpace"/>
        public static void SetCreaturePersonalSpace(uint creature, float creaturePersonalSpace) => _service.SetCreaturePersonalSpace(creature, creaturePersonalSpace);

        /// <inheritdoc cref="ICreaturePluginService.GetHeight"/>
        public static float GetHeight(uint creature) => _service.GetHeight(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetHeight"/>
        public static void SetHeight(uint creature, float height) => _service.SetHeight(creature, height);

        /// <inheritdoc cref="ICreaturePluginService.GetHitDistance"/>
        public static float GetHitDistance(uint creature) => _service.GetHitDistance(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetHitDistance"/>
        public static void SetHitDistance(uint creature, float hitDistance) => _service.SetHitDistance(creature, hitDistance);

        /// <inheritdoc cref="ICreaturePluginService.GetPreferredAttackDistance"/>
        public static float GetPreferredAttackDistance(uint creature) => _service.GetPreferredAttackDistance(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetPreferredAttackDistance"/>
        public static void SetPreferredAttackDistance(uint creature, float preferredAttackDistance) => _service.SetPreferredAttackDistance(creature, preferredAttackDistance);

        /// <inheritdoc cref="ICreaturePluginService.GetArmorCheckPenalty"/>
        public static int GetArmorCheckPenalty(uint creature) => _service.GetArmorCheckPenalty(creature);

        /// <inheritdoc cref="ICreaturePluginService.GetShieldCheckPenalty"/>
        public static int GetShieldCheckPenalty(uint creature) => _service.GetShieldCheckPenalty(creature);

        /// <inheritdoc cref="ICreaturePluginService.SetBypassEffectImmunity"/>
        public static void SetBypassEffectImmunity(uint creature, int immunityType, int chance = 100, bool persist = false) => 
            _service.SetBypassEffectImmunity(creature, immunityType, chance, persist);

        /// <inheritdoc cref="ICreaturePluginService.GetBypassEffectImmunity"/>
        public static int GetBypassEffectImmunity(uint creature, int immunityType) => _service.GetBypassEffectImmunity(creature, immunityType);

        /// <inheritdoc cref="ICreaturePluginService.GetNumberOfBonusSpells"/>
        public static int GetNumberOfBonusSpells(uint creature, int multiClass, int spellLevel) => _service.GetNumberOfBonusSpells(creature, multiClass, spellLevel);

        /// <inheritdoc cref="ICreaturePluginService.ModifyNumberBonusSpells"/>
        public static void ModifyNumberBonusSpells(uint creature, int multiClass, int spellLevel, int delta) => _service.ModifyNumberBonusSpells(creature, multiClass, spellLevel, delta);

        /// <inheritdoc cref="ICreaturePluginService.SetCasterLevelModifier"/>
        public static void SetCasterLevelModifier(uint creature, ClassType classId, int modifier, bool persist = false) => 
            _service.SetCasterLevelModifier(creature, classId, modifier, persist);

        /// <inheritdoc cref="ICreaturePluginService.GetCasterLevelModifier"/>
        public static int GetCasterLevelModifier(uint creature, ClassType classId) => _service.GetCasterLevelModifier(creature, classId);

        /// <inheritdoc cref="ICreaturePluginService.SetCasterLevelOverride"/>
        public static void SetCasterLevelOverride(uint creature, ClassType classId, int casterLevel, bool persist = false) => 
            _service.SetCasterLevelOverride(creature, classId, casterLevel, persist);

        /// <inheritdoc cref="ICreaturePluginService.GetCasterLevelOverride"/>
        public static int GetCasterLevelOverride(uint creature, ClassType classId) => _service.GetCasterLevelOverride(creature, classId);
    }
}
