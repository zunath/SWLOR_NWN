using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Adjusts the alignment of the specified subject.
        /// </summary>
        /// <param name="oSubject">The subject whose alignment to adjust</param>
        /// <param name="nAlignment">The alignment type to adjust:
        /// - ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_GOOD/ALIGNMENT_EVIL: Subject's alignment will be shifted in the direction specified
        /// - ALIGNMENT_ALL: nShift will be added to subject's law/chaos and good/evil alignment values
        /// - ALIGNMENT_NEUTRAL: nShift is applied to subject's law/chaos and good/evil alignment values in the direction which is towards neutrality</param>
        /// <param name="nShift">The desired shift in alignment. The shift will at most take the alignment value to 50 and not beyond</param>
        /// <param name="bAllPartyMembers">When true, the alignment shift also has a diminished effect on all members of the subject's party (if subject is a Player). When false, the shift only affects the subject (default: true)</param>
        /// <remarks>No return value. For example, if subject has a law/chaos value of 10 (chaotic) and a good/evil value of 80 (good), then if nShift is 15, the law/chaos value will become 25 and the good/evil value will become 55.</remarks>
        public static void AdjustAlignment(uint oSubject, Alignment nAlignment, int nShift,
            bool bAllPartyMembers = true)
        {
            global::NWN.Core.NWScript.AdjustAlignment(oSubject, (int)nAlignment, nShift, bAllPartyMembers ? 1 : 0);
        }

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) representing the creature's Law/Chaos alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment value for</param>
        /// <returns>An integer between 0 and 100 (100=law, 0=chaos). Returns -1 if the creature is not valid</returns>
        public static int GetLawChaosValue(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetLawChaosValue(oCreature);
        }

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) representing the creature's Good/Evil alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment value for</param>
        /// <returns>An integer between 0 and 100 (100=good, 0=evil). Returns -1 if the creature is not valid</returns>
        public static int GetGoodEvilValue(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetGoodEvilValue(oCreature);
        }

        /// <summary>
        /// Returns an ALIGNMENT_* constant representing the creature's law/chaos alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment for</param>
        /// <returns>An ALIGNMENT_* constant. Returns -1 if the creature is not valid</returns>
        public static Alignment GetAlignmentLawChaos(uint oCreature)
        {
            return (Alignment)global::NWN.Core.NWScript.GetAlignmentLawChaos(oCreature);
        }

        /// <summary>
        /// Returns an ALIGNMENT_* constant representing the creature's good/evil alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment for</param>
        /// <returns>An ALIGNMENT_* constant. Returns -1 if the creature is not valid</returns>
        public static Alignment GetAlignmentGoodEvil(uint oCreature)
        {
            return (Alignment)global::NWN.Core.NWScript.GetAlignmentGoodEvil(oCreature);
        }

        /// <summary>
        /// Clears all personal feelings that the source has about the target.
        /// </summary>
        /// <param name="oTarget">The target to clear personal feelings about</param>
        /// <param name="oSource">The source whose personal feelings to clear (default: OBJECT_SELF)</param>
        public static void ClearPersonalReputation(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            if (oSource == OBJECT_INVALID)
                oSource = OBJECT_SELF;
            global::NWN.Core.NWScript.ClearPersonalReputation(oTarget, oSource);
        }

        /// <summary>
        /// Makes the source temporarily friendly towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to befriend</param>
        /// <param name="oSource">The source to make friendly (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the friendship decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary friendship lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Friendship will only be in effect as long as (faction reputation + total personal reputation) >= REPUTATION_TYPE_FRIEND.</remarks>
        public static void SetIsTemporaryFriend(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f)
        {
            if (oSource == OBJECT_INVALID)
                oSource = OBJECT_SELF;
            global::NWN.Core.NWScript.SetIsTemporaryFriend(oTarget, oSource, bDecays ? 1 : 0, fDurationInSeconds);
        }

        /// <summary>
        /// Makes the source temporarily hostile towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to make hostile</param>
        /// <param name="oSource">The source to make hostile (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the enmity decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary enmity lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Enmity will only be in effect as long as (faction reputation + total personal reputation) <= REPUTATION_TYPE_ENEMY.</remarks>
        public static void SetIsTemporaryEnemy(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f)
        {
            if (oSource == OBJECT_INVALID)
                oSource = OBJECT_SELF;
            global::NWN.Core.NWScript.SetIsTemporaryEnemy(oTarget, oSource, bDecays ? 1 : 0, fDurationInSeconds);
        }

        /// <summary>
        /// Makes the source temporarily neutral towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to make neutral</param>
        /// <param name="oSource">The source to make neutral (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the neutrality decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary neutrality lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Neutrality will only be in effect as long as (faction reputation + total personal reputation) > REPUTATION_TYPE_ENEMY and (faction reputation + total personal reputation) < REPUTATION_TYPE_FRIEND.</remarks>
        public static void SetIsTemporaryNeutral(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f)
        {
            if (oSource == OBJECT_INVALID)
                oSource = OBJECT_SELF;
            global::NWN.Core.NWScript.SetIsTemporaryNeutral(oTarget, oSource, bDecays ? 1 : 0, fDurationInSeconds);
        }
    }
}