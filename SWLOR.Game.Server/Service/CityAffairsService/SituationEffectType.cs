namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>What a <see cref="SituationEffect"/> does when a choice or an ignored outcome applies.</summary>
    public enum SituationEffectType
    {
        Invalid = 0,
        TreasuryChange = 1,
        Modifier = 2,
        FollowUp = 3,

        /// <summary>An NPC wave attacks the city's corrals for 30 minutes.</summary>
        StartHunt = 4,

        /// <summary>Every animal in the city gives no products for 72 hours.</summary>
        LivestockNoProducts = 5
    }
}
