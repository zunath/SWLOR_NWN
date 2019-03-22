using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXCreature
    {
        void AddFeat(NWCreature creature, int feat);
        void AddFeatByLevel(NWCreature creature, int feat, int level);
        void AddKnownSpell(NWCreature creature, int classId, int level, int spellId);
        void AddSpecialAbility(NWCreature creature, SpecialAbilitySlot ability);
        int GetAttacksPerRound(NWCreature creature, int bBaseAPR);
        int GetBaseAC(NWCreature creature);
        int GetBaseSavingThrow(NWCreature creature, int which);
        int GetClassByLevel(NWCreature creature, int level);
        int GetClericDomain(NWCreature creature, int index);
        int GetFeatByIndex(NWCreature creature, int index);
        int GetFeatByLevel(NWCreature creature, int level, int index);
        int GetFeatCount(NWCreature creature);
        int GetFeatCountByLevel(NWCreature creature, int level);
        int GetKnownSpell(NWCreature creature, int classId, int level, int index);
        int GetKnownSpellCount(NWCreature creature, int classId, int level);
        int GetKnowsFeat(NWCreature creature, int feat);
        int GetMaxHitPointsByLevel(NWCreature creature, int level);
        int GetMaxSpellSlots(NWCreature creature, int classId, int level);
        int GetMeetsFeatRequirements(NWCreature creature, int feat);
        MemorizedSpellSlot GetMemorizedSpell(NWCreature creature, int classId, int level, int index);
        int GetMemorizedSpellCountByLevel(NWCreature creature, int classId, int level);
        int GetMovementType(NWCreature creature);
        int GetRawAbilityScore(NWCreature creature, int ability);
        int GetRemainingSpellSlots(NWCreature creature, int classId, int level);
        void ClearMemorisedKnownSpells(NWCreature creature, int classId, int spellId);
        int GetSkillPointsRemaining(NWCreature creature);
        int GetSoundset(NWCreature creature);
        SpecialAbilitySlot GetSpecialAbility(NWCreature creature, int index);
        int GetSpecialAbilityCount(NWCreature creature);
        int GetWizardSpecialization(NWCreature creature);
        void LevelDown(NWCreature creature, int count = 1);
        void LevelUp(NWCreature creature, int @class, int count = 1);
        void ModifyRawAbilityScore(NWCreature creature, int ability, int modifier);
        void RemoveFeat(NWCreature creature, int feat);
        void RemoveKnownSpell(NWCreature creature, int classId, int level, int spellId);
        void RemoveSpecialAbility(NWCreature creature, int index);
        void RestoreFeats(NWCreature creature);
        void RestoreItems(NWCreature creature);
        void RestoreSpecialAbilities(NWCreature creature);
        void RestoreSpells(NWCreature creature, int level);
        void SetAlignmentGoodEvil(NWCreature creature, int value);
        void SetAlignmentLawChaos(NWCreature creature, int value);
        void SetBaseAC(NWCreature creature, int ac);
        void SetBaseAttackBonus(NWCreature creature, int bab);
        void SetBaseSavingThrow(NWCreature creature, int which, int value);
        void SetChallengeRating(NWCreature creature, float fCR);
        void SetClassByPosition(NWCreature creature, int position, int classId);
        void SetClericDomain(NWCreature creature, int index, int domain);
        void SetCorpseDecayTime(NWCreature creature, int nDecayTime);
        void SetGender(NWCreature creature, int gender);
        void SetGold(NWCreature creature, int gold);
        void SetMaxHitPointsByLevel(NWCreature creature, int level, int value);
        void SetMemorizedSpell(NWCreature creature, int classId, int level, int index, MemorizedSpellSlot spell);
        void SetMovementRate(NWCreature creature, int rate);
        void SetRacialType(NWCreature creature, int racialtype);
        void SetRawAbilityScore(NWCreature creature, int ability, int value);
        void SetRemainingSpellSlots(NWCreature creature, int classId, int level, int slots);
        void SetSize(NWCreature creature, int size);
        void SetSkillPointsRemaining(NWCreature creature, int skillpoints);
        void SetSkillRank(NWCreature creature, int skill, int rank);
        void SetSoundset(NWCreature creature, int soundset);
        void SetSpecialAbility(NWCreature creature, int index, SpecialAbilitySlot ability);
        void SetWalkRateCap(NWCreature creature, float fWalkRate);
        void SetWizardSpecialization(NWCreature creature, int school);
        int GetAttackBonus(NWCreature creature, int isMelee = -1, int isTouchAttack = FALSE, int isOffhand = FALSE, int includeBaseAttackBonus = TRUE);
        int GetFeatRemainingUses(NWCreature creature, int feat);
        int GetFeatTotalUses(NWCreature creature, int feat);
        void SetFeatRemainingUses(NWCreature creature, int feat, int uses);

        int GetTotalEffectBonus(NWCreature creature,
            CreatureBonusType bonusType,
            NWObject target,
            int isElemental = 0,
            int isForceMax = 0,
            int savetype = -1,
            int saveSpecificType = -1,
            int skill = -1,
            int abilityScore = -1,
            int isOffhand = FALSE);
    }

}
