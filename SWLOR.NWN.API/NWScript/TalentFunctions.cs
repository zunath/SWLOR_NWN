using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScriptService
    {
        /// <summary>
        /// Checks if the talent is valid.
        /// </summary>
        /// <param name="tTalent">The talent to check</param>
        /// <returns>TRUE if the talent is valid</returns>
        public bool GetIsTalentValid(Talent tTalent)
        {
            return global::NWN.Core.NWScript.GetIsTalentValid(tTalent) != 0;
        }

        /// <summary>
        /// Gets the type of the talent.
        /// </summary>
        /// <param name="tTalent">The talent to get the type for</param>
        /// <returns>The talent type (TALENT_TYPE_*)</returns>
        public TalentType GetTypeFromTalent(Talent tTalent)
        {
            return (TalentType)global::NWN.Core.NWScript.GetTypeFromTalent(tTalent);
        }

        /// <summary>
        /// Gets the ID of the talent.
        /// This could be a SPELL_*, FEAT_* or SKILL_*.
        /// </summary>
        /// <param name="tTalent">The talent to get the ID for</param>
        /// <returns>The ID of the talent</returns>
        public int GetIdFromTalent(Talent tTalent)
        {
            return global::NWN.Core.NWScript.GetIdFromTalent(tTalent);
        }

        /// <summary>
        /// Creates a spell talent.
        /// </summary>
        /// <param name="nSpell">SPELL_* constant</param>
        /// <returns>The created spell talent</returns>
        public Talent TalentSpell(SpellType nSpell)
        {
            return global::NWN.Core.NWScript.TalentSpell((int)nSpell);
        }

        /// <summary>
        /// Creates a feat talent.
        /// </summary>
        /// <param name="nFeat">FEAT_* constant</param>
        /// <returns>The created feat talent</returns>
        public Talent TalentFeat(FeatType nFeat)
        {
            return global::NWN.Core.NWScript.TalentFeat((int)nFeat);
        }

        /// <summary>
        /// Creates a skill talent.
        /// </summary>
        /// <param name="nSkill">SKILL_* constant</param>
        /// <returns>The created skill talent</returns>
        public Talent TalentSkill(NWNSkillType nSkill)
        {
            return global::NWN.Core.NWScript.TalentSkill((int)nSkill);
        }

        /// <summary>
        /// Determines whether the creature has the talent.
        /// </summary>
        /// <param name="tTalent">The talent to check for</param>
        /// <param name="oCreature">The creature to check (defaults to OBJECT_SELF)</param>
        /// <returns>TRUE if the creature has the talent</returns>
        public bool GetCreatureHasTalent(Talent tTalent, uint oCreature = OBJECT_INVALID)
        {
            if (oCreature == OBJECT_INVALID)
                oCreature = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetCreatureHasTalent(tTalent, oCreature) != 0;
        }

        /// <summary>
        /// Gets a random talent of the creature within the specified category.
        /// </summary>
        /// <param name="nCategory">TALENT_CATEGORY_* constant</param>
        /// <param name="oCreature">The creature to get the talent from (defaults to OBJECT_SELF)</param>
        /// <returns>A random talent from the specified category</returns>
        public Talent GetCreatureTalentRandom(TalentCategoryType nCategory, uint oCreature = OBJECT_INVALID)
        {
            if (oCreature == OBJECT_INVALID)
                oCreature = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetCreatureTalentRandom((int)nCategory, oCreature);
        }

        /// <summary>
        /// Gets the best talent (i.e. closest to nCRMax without going over) of the creature within the specified category.
        /// </summary>
        /// <param name="nCategory">TALENT_CATEGORY_* constant</param>
        /// <param name="nCRMax">Challenge Rating of the talent</param>
        /// <param name="oCreature">The creature to get the talent from (defaults to OBJECT_SELF)</param>
        /// <returns>The best talent from the specified category</returns>
        public Talent GetCreatureTalentBest(TalentCategoryType nCategory, int nCRMax,
            uint oCreature = OBJECT_INVALID)
        {
            if (oCreature == OBJECT_INVALID)
                oCreature = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetCreatureTalentBest((int)nCategory, nCRMax, oCreature);
        }
    }
}