using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// A mayor's step-down, kept on the city from the day the interim holder is named until the snap election ends.
    /// The mayor stays <see cref="WorldProperty.OwnerPlayerId"/> throughout, so the city's level keeps following their rank.
    /// </summary>
    public class CityStepDown
    {
        public CityStepDown()
        {
            InterimHolderPlayerId = string.Empty;
            Stage = CityStepDownStage.Pending;
        }

        /// <summary>The Acting Mayor or officer the mayor named to hold the seat until the snap election ends.</summary>
        public string InterimHolderPlayerId { get; set; }

        public CityStepDownStage Stage { get; set; }

        /// <summary>When the mayor named the interim holder. It also tells one step-down from the next.</summary>
        public DateTime NamedUtc { get; set; }

        /// <summary>The time by which the interim holder must accept, or the step-down is cancelled.</summary>
        public DateTime AcceptDeadlineUtc { get; set; }
    }
}
