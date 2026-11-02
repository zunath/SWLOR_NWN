namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// Why an election was opened. Values are stored on <see cref="Entity.Election"/>, so they never change.
    /// </summary>
    public enum ElectionKind
    {
        Invalid = 0,

        /// <summary>The scheduled election that opens when the quiet period after the previous election ends.</summary>
        Regular = 1,

        /// <summary>Opened after the city ignored its mayor's duties (Neglect and recall).</summary>
        Recall = 2,

        /// <summary>Opened at once after the citizens voted No in a confidence vote on the sitting mayor.</summary>
        SnapConfidence = 3,

        /// <summary>Opened when the mayor steps down and names an interim holder.</summary>
        SnapStepDown = 4,
    }
}
