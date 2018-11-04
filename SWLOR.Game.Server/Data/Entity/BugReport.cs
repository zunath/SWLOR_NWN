
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BugReports]")]
    public class BugReport: IEntity
    {
        [Key]
        public int BugReportID { get; set; }
        public string SenderPlayerID { get; set; }
        public string CDKey { get; set; }
        public string Text { get; set; }
        public string TargetName { get; set; }
        public string AreaResref { get; set; }
        public double SenderLocationX { get; set; }
        public double SenderLocationY { get; set; }
        public double SenderLocationZ { get; set; }
        public double SenderLocationOrientation { get; set; }
        public System.DateTime DateSubmitted { get; set; }
    }
}
