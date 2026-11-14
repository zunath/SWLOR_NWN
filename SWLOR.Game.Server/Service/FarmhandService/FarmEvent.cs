namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>One event a farmhand's replay applied: what happened, to which animal or plot, and when.</summary>
    public sealed class FarmEvent
    {
        public FarmEvent(DateTime timeUtc, FarmEventType type, string subjectId, string detail = "")
        {
            TimeUtc = timeUtc;
            Type = type;
            SubjectId = subjectId;
            Detail = detail;
        }

        public DateTime TimeUtc { get; }

        public FarmEventType Type { get; }

        /// <summary>The id of the <c>Livestock</c> record or the garden plot's <c>GardenPlot</c> record.</summary>
        public string SubjectId { get; }

        /// <summary>For a collection, the items it put in the hopper.</summary>
        public string Detail { get; }
    }
}
