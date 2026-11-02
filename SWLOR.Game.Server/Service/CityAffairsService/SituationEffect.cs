namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>One thing a choice, or the ignored outcome, does to the city.</summary>
    public sealed class SituationEffect
    {
        private SituationEffect(SituationEffectType type)
        {
            Type = type;
        }

        public SituationEffectType Type { get; }

        /// <summary>Credits added to the treasury, or taken from it when negative.</summary>
        public int Credits { get; private init; }

        public CityModifierType Modifier { get; private init; }

        public SituationType FollowUp { get; private init; }

        /// <summary>Days until the follow-up is due.</summary>
        public int FollowUpDelayDays { get; private init; }

        /// <summary>The follow-up arrives with a one in this many chance. 1 means always.</summary>
        public int FollowUpOneIn { get; private init; }

        /// <summary>The follow-up carries the secret Gazette entry this decision posted, so it can expose it.</summary>
        public bool FollowUpCarriesSecret { get; private init; }

        public static SituationEffect TreasuryChange(int credits)
        {
            return new SituationEffect(SituationEffectType.TreasuryChange) { Credits = credits };
        }

        public static SituationEffect AddModifier(CityModifierType modifier)
        {
            return new SituationEffect(SituationEffectType.Modifier) { Modifier = modifier };
        }

        /// <summary>Calls a hunt: an NPC wave attacks the city's corrals until it is driven off or 30 minutes pass.</summary>
        public static SituationEffect CallHunt()
        {
            return new SituationEffect(SituationEffectType.StartHunt);
        }

        /// <summary>Every animal in the city gives no products for 72 hours. Crating keeps the penalty.</summary>
        public static SituationEffect LivestockLoseProducts()
        {
            return new SituationEffect(SituationEffectType.LivestockNoProducts);
        }

        public static SituationEffect QueueFollowUp(SituationType followUp, int delayDays, int oneIn = 1, bool carriesSecret = false)
        {
            return new SituationEffect(SituationEffectType.FollowUp)
            {
                FollowUp = followUp,
                FollowUpDelayDays = delayDays,
                FollowUpOneIn = oneIn < 1 ? 1 : oneIn,
                FollowUpCarriesSecret = carriesSecret
            };
        }
    }
}
