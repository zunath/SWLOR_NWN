namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>
    /// How far a mayor's step-down has come. Values are saved on the city and never change.
    /// </summary>
    public enum CityStepDownStage
    {
        Invalid = 0,

        /// <summary>The mayor named an interim holder, who has not yet accepted. The mayor keeps every power.</summary>
        Pending = 1,

        /// <summary>
        /// The interim holder accepted and the snap election opened. The mayor stepped down and has no powers; the
        /// interim holder has the Acting Mayor's powers until the election ends.
        /// </summary>
        Accepted = 2
    }
}
