namespace SWLOR.Game.Server.Service.CityTreasuryService
{
    /// <summary>What moved a city's treasury, for its treasury history.</summary>
    public enum CityTreasuryEntryType
    {
        Invalid = 0,
        Deposit = 1,
        Withdrawal = 2,
        CitizenTaxes = 3,
        StoreSalesTax = 4,
        ExchangeTariffs = 5,
        TransportationTax = 6,
        ShipyardFees = 7,
        UpkeepPaid = 8,
        EdictEnacted = 9,
        SituationGain = 10,
        SituationCost = 11,
        BountyPosted = 12,
        BountyReturned = 13,
        BountyCollected = 14,
        ProjectStarted = 15,
        ProjectReturned = 16,
        ProjectCompleted = 17,
        TournamentPrizePosted = 18,
        TournamentPrizeReturned = 19,
        TournamentPrizeAwarded = 20,
        TournamentFees = 21,
    }
}
