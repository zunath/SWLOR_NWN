using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXCreature
    {
        void AddFeat(NWCreature creature, int feat);
        void AddFeatByLevel(NWCreature creature, int feat, int level);
        void RemoveFeat(NWCreature creature, int feat);
        int GetKnowsFeat(NWCreature creature, int feat);
        int GetFeatCountByLevel(NWCreature creature, int level);
        int GetFeatByLevel(NWCreature creature, int level, int index);
        int GetFeatCount(NWCreature creature);
        int GetFeatByIndex(NWCreature creature, int index);
        int GetMeetsFeatRequirements(NWCreature creature, int feat);
        SpecialAbilitySlot GetSpecialAbility(NWCreature creature, int index);
        int GetSpecialAbilityCount(NWCreature creature);
        void AddSpecialAbility(NWCreature creature, SpecialAbilitySlot ability);
        void RemoveSpecialAbility(NWCreature creature, int index);
        void SetSpecialAbility(NWCreature creature, int index, SpecialAbilitySlot ability);
        int GetClassByLevel(NWCreature creature, int level);
        void SetBaseAC(NWCreature creature, int ac);
        int GetBaseAC(NWCreature creature);
        void SetAbilityScore(NWCreature creature, int ability, int value);
        void SetRawAbilityScore(NWCreature creature, int ability, int value);
        int GetRawAbilityScore(NWCreature creature, int ability);
        void ModifyRawAbilityScore(NWCreature creature, int ability, int modifier);
        MemorizedSpellSlot GetMemorizedSpell(NWCreature creature, int classId, int level, int index);
        int GetMemorizedSpellCountByLevel(NWCreature creature, int classId, int level);
        void SetMemorizedSpell(NWCreature creature, int classId, int level, int index, MemorizedSpellSlot spell);
        int GetRemainingSpellSlots(NWCreature creature, int classId, int level);
        void SetRemainingSpellSlots(NWCreature creature, int classId, int level, int slots);
        int GetKnownSpell(NWCreature creature, int classId, int level, int index);
        int GetKnownSpellCount(NWCreature creature, int classId, int level);
        void RemoveKnownSpell(NWCreature creature, int classId, int level, int spellId);
        void AddKnownSpell(NWCreature creature, int classId, int level, int spellId);
        int GetMaxSpellSlots(NWCreature creature, int classId, int level);
        int GetMaxHitPointsByLevel(NWCreature creature, int level);
        void SetMaxHitPointsByLevel(NWCreature creature, int level, int value);
        void SetMovementRate(NWCreature creature, int rate);
        void SetAlignmentGoodEvil(NWCreature creature, int value);
        void SetAlignmentLawChaos(NWCreature creature, int value);
        int GetClericDomain(NWCreature creature, int index);
        void SetClericDomain(NWCreature creature, int index, int domain);
        int GetWizardSpecialization(NWCreature creature);
        void SetWizardSpecialization(NWCreature creature, int school);
        int GetSoundset(NWCreature creature);
        void SetSoundset(NWCreature creature, int soundset);
        void SetSkillRank(NWCreature creature, int skill, int rank);
        void SetClassByPosition(NWCreature creature, int position, int classId);
        void SetBaseAttackBonus(NWCreature creature, int bab);
        int GetAttacksPerRound(NWCreature creature, int bBaseAPR);
        void SetGender(NWCreature creature, int gender);
        void RestoreFeats(NWCreature creature);
        void RestoreSpecialAbilities(NWCreature creature);
        void RestoreSpells(NWCreature creature, int level);
        void RestoreItems(NWCreature creature);
        void SetSize(NWCreature creature, int size);
        int GetSkillPointsRemaining(NWCreature creature);
        void SetSkillPointsRemaining(NWCreature creature, int skillpoints);
        void SetRacialType(NWCreature creature, int racialtype);
        int GetMovementType(NWCreature creature);
        void SetWalkRateCap(NWCreature creature, float fWalkRate);
    }

}
