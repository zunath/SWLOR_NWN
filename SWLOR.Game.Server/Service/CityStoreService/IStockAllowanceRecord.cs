using System;

namespace SWLOR.Game.Server.Service.CityStoreService
{
    /// <summary>What a weekly allowance record keeps, whether it belongs to a city store or to a traveler.</summary>
    public interface IStockAllowanceRecord
    {
        /// <summary>The start of the week the sold count belongs to.</summary>
        DateTime WeekStartUtc { get; set; }

        /// <summary>How many were sold in that week.</summary>
        int Sold { get; set; }
    }
}
