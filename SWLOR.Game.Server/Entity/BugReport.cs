using System;

namespace SWLOR.Game.Server.Entity
{
    public class BugReport: EntityBase
    {
        public Guid? SenderPlayerID { get; set; }
        public string SenderName { get; set; }
        public string CDKey { get; set; }
        public string Text { get; set; }
        public string AreaResref { get; set; }
        public double SenderLocationX { get; set; }
        public double SenderLocationY { get; set; }
        public double SenderLocationZ { get; set; }
        public double SenderLocationOrientation { get; set; }
        public override string KeyPrefix => "BugReport";
    }
}
