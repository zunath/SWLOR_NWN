using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for alignment and reputation
        private readonly Dictionary<uint, AlignmentData> _creatureAlignment = new();
        private readonly Dictionary<string, ReputationData> _reputationData = new();

        private class AlignmentData
        {
            public int LawChaosValue { get; set; } = 50;
            public int GoodEvilValue { get; set; } = 50;
            public AlignmentType LawChaosAlignment => LawChaosValue switch
            {
                < 33 => AlignmentType.Lawful,
                > 66 => AlignmentType.Chaotic,
                _ => AlignmentType.Neutral
            };
            public AlignmentType GoodEvilAlignment => GoodEvilValue switch
            {
                < 33 => AlignmentType.Evil,
                > 66 => AlignmentType.Good,
                _ => AlignmentType.Neutral
            };
        }

        public class ReputationData
        {
            public ReputationType Type { get; set; } = ReputationType.Neutral;
            public bool Decays { get; set; } = false;
            public float DurationSeconds { get; set; } = 180.0f;
            public float RemainingTime { get; set; } = 180.0f;
        }

        public enum ReputationType
        {
            Neutral,
            Friend,
            Enemy
        }

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
        public void AdjustAlignment(uint oSubject, AlignmentType nAlignment, int nShift,
            bool bAllPartyMembers = true)
        {
            if (!_creatureAlignment.ContainsKey(oSubject))
                _creatureAlignment[oSubject] = new AlignmentData();

            var data = _creatureAlignment[oSubject];
            
            switch (nAlignment)
            {
                case AlignmentType.Lawful:
                    data.LawChaosValue = Math.Max(0, Math.Min(100, data.LawChaosValue + nShift));
                    break;
                case AlignmentType.Chaotic:
                    data.LawChaosValue = Math.Max(0, Math.Min(100, data.LawChaosValue - nShift));
                    break;
                case AlignmentType.Good:
                    data.GoodEvilValue = Math.Max(0, Math.Min(100, data.GoodEvilValue + nShift));
                    break;
                case AlignmentType.Evil:
                    data.GoodEvilValue = Math.Max(0, Math.Min(100, data.GoodEvilValue - nShift));
                    break;
            }
        }

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) representing the creature's Law/Chaos alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment value for</param>
        /// <returns>An integer between 0 and 100 (100=law, 0=chaos). Returns -1 if the creature is not valid</returns>
        public int GetLawChaosValue(uint oCreature) => 
            _creatureAlignment.GetValueOrDefault(oCreature, new AlignmentData()).LawChaosValue;

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) representing the creature's Good/Evil alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment value for</param>
        /// <returns>An integer between 0 and 100 (100=good, 0=evil). Returns -1 if the creature is not valid</returns>
        public int GetGoodEvilValue(uint oCreature) => 
            _creatureAlignment.GetValueOrDefault(oCreature, new AlignmentData()).GoodEvilValue;

        /// <summary>
        /// Returns an ALIGNMENT_* constant representing the creature's law/chaos alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment for</param>
        /// <returns>An ALIGNMENT_* constant. Returns -1 if the creature is not valid</returns>
        public AlignmentType GetAlignmentLawChaos(uint oCreature) => 
            _creatureAlignment.GetValueOrDefault(oCreature, new AlignmentData()).LawChaosAlignment;

        /// <summary>
        /// Returns an ALIGNMENT_* constant representing the creature's good/evil alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment for</param>
        /// <returns>An ALIGNMENT_* constant. Returns -1 if the creature is not valid</returns>
        public AlignmentType GetAlignmentGoodEvil(uint oCreature) => 
            _creatureAlignment.GetValueOrDefault(oCreature, new AlignmentData()).GoodEvilAlignment;

        /// <summary>
        /// Clears all personal feelings that the source has about the target.
        /// </summary>
        /// <param name="oTarget">The target to clear personal feelings about</param>
        /// <param name="oSource">The source whose personal feelings to clear (default: OBJECT_SELF)</param>
        public void ClearPersonalReputation(uint oTarget, uint oSource = OBJECT_INVALID) 
        {
            var key = $"{oTarget}|{oSource}";
            _reputationData.Remove(key);
        }

        /// <summary>
        /// Makes the source temporarily friendly towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to befriend</param>
        /// <param name="oSource">The source to make friendly (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the friendship decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary friendship lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Friendship will only be in effect as long as (faction reputation + total personal reputation) >= REPUTATION_TYPE_FRIEND.</remarks>
        public void SetIsTemporaryFriend(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f) 
        {
            var key = $"{oTarget}|{oSource}";
            _reputationData[key] = new ReputationData
            {
                Type = ReputationType.Friend,
                Decays = bDecays,
                DurationSeconds = fDurationInSeconds,
                RemainingTime = fDurationInSeconds
            };
        }

        /// <summary>
        /// Makes the source temporarily hostile towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to make hostile</param>
        /// <param name="oSource">The source to make hostile (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the enmity decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary enmity lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Enmity will only be in effect as long as (faction reputation + total personal reputation) <= REPUTATION_TYPE_ENEMY.</remarks>
        public void SetIsTemporaryEnemy(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f) 
        {
            var key = $"{oTarget}|{oSource}";
            _reputationData[key] = new ReputationData
            {
                Type = ReputationType.Enemy,
                Decays = bDecays,
                DurationSeconds = fDurationInSeconds,
                RemainingTime = fDurationInSeconds
            };
        }

        /// <summary>
        /// Makes the source temporarily neutral towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to make neutral</param>
        /// <param name="oSource">The source to make neutral (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the neutrality decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary neutrality lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Neutrality will only be in effect as long as (faction reputation + total personal reputation) > REPUTATION_TYPE_ENEMY and (faction reputation + total personal reputation) < REPUTATION_TYPE_FRIEND.</remarks>
        public void SetIsTemporaryNeutral(uint oTarget, uint oSource = OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f) 
        {
            var key = $"{oTarget}|{oSource}";
            _reputationData[key] = new ReputationData
            {
                Type = ReputationType.Neutral,
                Decays = bDecays,
                DurationSeconds = fDurationInSeconds,
                RemainingTime = fDurationInSeconds
            };
        }

        // Helper methods for testing

        public int GetReputation(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            var key = $"{oTarget}|{oSource}";
            var data = _reputationData.GetValueOrDefault(key, new ReputationData { Type = ReputationType.Neutral });
            return (int)data.Type; // Return the reputation type as int
        }

    }
}
