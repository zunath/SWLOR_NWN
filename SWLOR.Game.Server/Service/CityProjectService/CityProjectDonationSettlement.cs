namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>What happens to one donation record, from the record, the donor's character file and the project
    /// (<see cref="CityProjectDonationRules.Settle"/>).</summary>
    public enum CityProjectDonationSettlement
    {
        /// <summary>The character file carries no mark, so the materials never left the donor's inventory: the record goes.</summary>
        Drop = 1,

        /// <summary>The project already holds the donation: the record and the mark go.</summary>
        Forget = 2,

        /// <summary>The materials left the inventory and the project can take all of them: it is applied, then the record and the mark go.</summary>
        Apply = 3,

        /// <summary>The materials left the inventory and the project cannot take them (it is full, closed or gone): they go to the donor's overflow store.</summary>
        Return = 4
    }
}
