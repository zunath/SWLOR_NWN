namespace SWLOR.Game.Server.Service.CityProjectService
{
    /// <summary>
    /// Where a City Project is. The record doubles as the journal of the steps that move money, so a restart between two
    /// steps repeats the ones that did not finish and none that did. Values are saved with the project and never change.
    /// </summary>
    public enum CityProjectState
    {
        /// <summary>Saved, but the credits are not yet held in escrow. A restart finds it and either finishes or drops it.</summary>
        Starting = 1,

        /// <summary>Holding its credits and taking donations, until it is supplied and built or cancelled.</summary>
        Open = 2,

        /// <summary>Built: its effect is being applied and its credits paid out.</summary>
        Completing = 3,

        /// <summary>Cancelled: its credits are being returned and each donor's materials sent to their overflow store.</summary>
        Cancelling = 4
    }
}
