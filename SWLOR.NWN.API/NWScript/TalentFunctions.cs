using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   * Returns TRUE if tTalent is valid.
        /// </summary>
        public static bool GetIsTalentValid(Talent tTalent)
        {
            return global::NWN.Core.NWScript.GetIsTalentValid(tTalent) != 0;
        }

        /// <summary>
        ///   Get the type (TALENT_TYPE_*) of tTalent.
        /// </summary>
        public static TalentType GetTypeFromTalent(Talent tTalent)
        {
            return (TalentType)global::NWN.Core.NWScript.GetTypeFromTalent(tTalent);
        }

        /// <summary>
        ///   Get the ID of tTalent.  This could be a SPELL_*, FEAT_* or SKILL_*.
        /// </summary>
        public static int GetIdFromTalent(Talent tTalent)
        {
            return global::NWN.Core.NWScript.GetIdFromTalent(tTalent);
        }

        /// <summary>
        ///   Create a Spell Talent.
        ///   - nSpell: SPELL_*
        /// </summary>
        public static Talent TalentSpell(Spell nSpell)
        {
            return global::NWN.Core.NWScript.TalentSpell((int)nSpell);
        }

        /// <summary>
        ///   Create a Feat Talent.
        ///   - nFeat: FEAT_*
        /// </summary>
        public static Talent TalentFeat(FeatType nFeat)
        {
            return global::NWN.Core.NWScript.TalentFeat((int)nFeat);
        }

        /// <summary>
        ///   Create a Skill Talent.
        ///   - nSkill: SKILL_*
        /// </summary>
        public static Talent TalentSkill(NWNSkillType nSkill)
        {
            return global::NWN.Core.NWScript.TalentSkill((int)nSkill);
        }

        /// <summary>
        ///   Determine whether oCreature has tTalent.
        /// </summary>
        public static bool GetCreatureHasTalent(Talent tTalent, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCreatureHasTalent(tTalent, oCreature) != 0;
        }

        /// <summary>
        ///   Get a random talent of oCreature, within nCategory.
        ///   - nCategory: TALENT_CATEGORY_*
        ///   - oCreature
        /// </summary>
        public static Talent GetCreatureTalentRandom(TalentCategory nCategory, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCreatureTalentRandom((int)nCategory, oCreature);
        }

        /// <summary>
        ///   Get the best talent (i.e. closest to nCRMax without going over) of oCreature,
        ///   within nCategory.
        ///   - nCategory: TALENT_CATEGORY_*
        ///   - nCRMax: Challenge Rating of the talent
        ///   - oCreature
        /// </summary>
        public static Talent GetCreatureTalentBest(TalentCategory nCategory, int nCRMax,
            uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCreatureTalentBest((int)nCategory, nCRMax, oCreature);
        }
    }
}