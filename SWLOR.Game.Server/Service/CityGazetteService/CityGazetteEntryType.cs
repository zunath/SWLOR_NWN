namespace SWLOR.Game.Server.Service.CityGazetteService
{
    /// <summary>
    /// What a Gazette entry reports. Values are saved with the entries and never change.
    /// </summary>
    public enum CityGazetteEntryType
    {
        Invalid = 0,

        /// <summary>Anything without a more specific type.</summary>
        Notice = 1,

        /// <summary>Election stages and results.</summary>
        Election = 2,

        /// <summary>Upkeep assessed, paid, overdue, or unpaid past the deadline.</summary>
        Upkeep = 3,

        /// <summary>The city is below its required citizen count.</summary>
        Citizens = 4,

        /// <summary>A city or station was founded.</summary>
        Founding = 5,

        /// <summary>Taxes or fees changed.</summary>
        Tax = 6,

        /// <summary>An official decided a City Affairs situation.</summary>
        Decision = 7,

        /// <summary>An Edict was enacted or repealed.</summary>
        Edict = 8
    }
}
