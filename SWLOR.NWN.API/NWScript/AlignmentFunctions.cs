using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Adjust the alignment of oSubject.
        ///   - oSubject
        ///   - nAlignment:
        ///   -> ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_GOOD/ALIGNMENT_EVIL: oSubject's
        ///   alignment will be shifted in the direction specified
        ///   -> ALIGNMENT_ALL: nShift will be added to oSubject's law/chaos and
        ///   good/evil alignment values
        ///   -> ALIGNMENT_NEUTRAL: nShift is applied to oSubject's law/chaos and
        ///   good/evil alignment values in the direction which is towards neutrality.
        ///   e.g. If oSubject has a law/chaos value of 10 (i.e. chaotic) and a
        ///   good/evil value of 80 (i.e. good) then if nShift is 15, the
        ///   law/chaos value will become (10+15)=25 and the good/evil value will
        ///   become (80-25)=55
        ///   Furthermore, the shift will at most take the alignment value to 50 and
        ///   not beyond.
        ///   e.g. If oSubject has a law/chaos value of 40 and a good/evil value of 70,
        ///   then if nShift is 15, the law/chaos value will become 50 and the
        ///   good/evil value will become 55
        ///   - nShift: this is the desired shift in alignment
        ///   - bAllPartyMembers: when TRUE the alignment shift of oSubject also has a
        ///   diminished affect all members of oSubject's party (if oSubject is a Player).
        ///   When FALSE the shift only affects oSubject.
        ///   * No return value
        /// </summary>
        public static void AdjustAlignment(uint oSubject, Alignment nAlignment, int nShift,
            bool bAllPartyMembers = true)
        {
            global::NWN.Core.NWScript.AdjustAlignment(oSubject, (int)nAlignment, nShift, bAllPartyMembers ? 1 : 0);
        }

        /// <summary>
        ///   Get an integer between 0 and 100 (inclusive) to represent oCreature's
        ///   Law/Chaos alignment
        ///   (100=law, 0=chaos)
        ///   * Return value if oCreature is not a valid creature: -1
        /// </summary>
        public static int GetLawChaosValue(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetLawChaosValue(oCreature);
        }

        /// <summary>
        ///   Get an integer between 0 and 100 (inclusive) to represent oCreature's
        ///   Good/Evil alignment
        ///   (100=good, 0=evil)
        ///   * Return value if oCreature is not a valid creature: -1
        /// </summary>
        public static int GetGoodEvilValue(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetGoodEvilValue(oCreature);
        }

        /// <summary>
        ///   Return an ALIGNMENT_* constant to represent oCreature's law/chaos alignment
        ///   * Return value if oCreature is not a valid creature: -1
        /// </summary>
        public static Alignment GetAlignmentLawChaos(uint oCreature)
        {
            return (Alignment)global::NWN.Core.NWScript.GetAlignmentLawChaos(oCreature);
        }

        /// <summary>
        ///   Return an ALIGNMENT_* constant to represent oCreature's good/evil alignment
        ///   * Return value if oCreature is not a valid creature: -1
        /// </summary>
        public static Alignment GetAlignmentGoodEvil(uint oCreature)
        {
            return (Alignment)global::NWN.Core.NWScript.GetAlignmentGoodEvil(oCreature);
        }

        /// <summary>
        ///   Clear all personal feelings that oSource has about oTarget.
        /// </summary>
        public static void ClearPersonalReputation(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.ClearPersonalReputation(oTarget, oSource);
        }

        /// <summary>
        ///   oSource will temporarily be friends towards oTarget.
        ///   bDecays determines whether the personal reputation value decays over time
        ///   fDurationInSeconds is the length of time that the temporary friendship lasts
        ///   Make oSource into a temporary friend of oTarget using personal reputation.
        ///   - oTarget
        ///   - oSource
        ///   - bDecays: If this is TRUE, the friendship decays over fDurationInSeconds;
        ///   otherwise it is indefinite.
        ///   - fDurationInSeconds: This is only used if bDecays is TRUE, it is how long
        ///   the friendship lasts.
        ///   Note: If bDecays is TRUE, the personal reputation amount decreases in size
        ///   over fDurationInSeconds. Friendship will only be in effect as long as
        ///   (faction reputation + total personal reputation) >= REPUTATION_TYPE_FRIEND.
        /// </summary>
        public static void SetIsTemporaryFriend(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f)
        {
            global::NWN.Core.NWScript.SetIsTemporaryFriend(oTarget, oSource, bDecays ? 1 : 0, fDurationInSeconds);
        }

        /// <summary>
        ///   Make oSource into a temporary enemy of oTarget using personal reputation.
        ///   - oTarget
        ///   - oSource
        ///   - bDecays: If this is TRUE, the enmity decays over fDurationInSeconds;
        ///   otherwise it is indefinite.
        ///   - fDurationInSeconds: This is only used if bDecays is TRUE, it is how long
        ///   the enmity lasts.
        ///   Note: If bDecays is TRUE, the personal reputation amount decreases in size
        ///   over fDurationInSeconds. Enmity will only be in effect as long as
        ///   (faction reputation + total personal reputation) <= REPUTATION_TYPE_ENEMY.
        /// </summary>
        public static void SetIsTemporaryEnemy(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f)
        {
            global::NWN.Core.NWScript.SetIsTemporaryEnemy(oTarget, oSource, bDecays ? 1 : 0, fDurationInSeconds);
        }

        /// <summary>
        ///   Make oSource temporarily neutral to oTarget using personal reputation.
        ///   - oTarget
        ///   - oSource
        ///   - bDecays: If this is TRUE, the neutrality decays over fDurationInSeconds;
        ///   otherwise it is indefinite.
        ///   - fDurationInSeconds: This is only used if bDecays is TRUE, it is how long
        ///   the neutrality lasts.
        ///   Note: If bDecays is TRUE, the personal reputation amount decreases in size
        ///   over fDurationInSeconds. Neutrality will only be in effect as long as
        ///   (faction reputation + total personal reputation) > REPUTATION_TYPE_ENEMY and
        ///   (faction reputation + total personal reputation) < REPUTATION_TYPE_FRIEND.
        /// </summary>
        public static void SetIsTemporaryNeutral(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f)
        {
            global::NWN.Core.NWScript.SetIsTemporaryNeutral(oTarget, oSource, bDecays ? 1 : 0, fDurationInSeconds);
        }
    }
}