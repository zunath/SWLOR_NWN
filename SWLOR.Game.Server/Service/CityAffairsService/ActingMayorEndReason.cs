namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// Why an Acting Mayor's term ended (Player City Revamp, "Acting mayor and stepping down").
    /// </summary>
    public enum ActingMayorEndReason
    {
        Invalid = 0,

        /// <summary>The mayor ended the term, or named someone else.</summary>
        MayorEnded = 1,

        /// <summary>The 30 days passed.</summary>
        TermElapsed = 2,

        /// <summary>The Acting Mayor left the city.</summary>
        LeftCity = 3,

        /// <summary>An election seated a mayor.</summary>
        ElectionSeatedMayor = 4,

        /// <summary>The mayor stepped down, so the interim holder took over.</summary>
        MayorSteppedDown = 5
    }
}
