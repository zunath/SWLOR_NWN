namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>Where a situation is in its life. Values are saved with the situations and never change.</summary>
    public enum CitySituationState
    {
        Invalid = 0,

        /// <summary>A follow-up waiting for its due date.</summary>
        Queued = 1,

        /// <summary>Waiting for an official's decision.</summary>
        Open = 2,

        /// <summary>An official chose.</summary>
        Decided = 3,

        /// <summary>Nobody decided in time, so its ignored outcome applied.</summary>
        Expired = 4
    }
}
