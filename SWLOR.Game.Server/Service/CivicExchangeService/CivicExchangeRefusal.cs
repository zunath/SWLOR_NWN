namespace SWLOR.Game.Server.Service.CivicExchangeService
{
    /// <summary>Why the Civic Exchange does not sell a citizen an item.</summary>
    public enum CivicExchangeRefusal
    {
        /// <summary>Nothing stands in the way.</summary>
        None = 0,

        /// <summary>The item is not on the Exchange's list.</summary>
        UnknownItem = 1,

        /// <summary>The Cantina has not reached the level that stocks the item.</summary>
        CantinaTooLow = 2,

        /// <summary>The CD key has bought its crates for the week.</summary>
        WeeklyLimit = 3,

        /// <summary>The citizen has less Available XP than the price.</summary>
        NotEnoughXp = 4
    }
}
