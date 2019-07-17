
using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BugReport]")]
    public class BugReport: IEntity
    {
        public BugReport()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public Guid? SenderPlayerID { get; set; }
        public string CDKey { get; set; }
        public string Text { get; set; }
        public string TargetName { get; set; }
        public string AreaResref { get; set; }
        public double SenderLocationX { get; set; }
        public double SenderLocationY { get; set; }
        public double SenderLocationZ { get; set; }
        public double SenderLocationOrientation { get; set; }
        public DateTime DateSubmitted { get; set; }

        public IEntity Clone()
        {
            return new BugReport
            {
                ID = ID,
                SenderPlayerID = SenderPlayerID,
                CDKey = CDKey,
                Text = Text,
                TargetName = TargetName,
                AreaResref = AreaResref,
                SenderLocationX = SenderLocationX,
                SenderLocationY = SenderLocationY,
                SenderLocationZ = SenderLocationZ,
                SenderLocationOrientation = SenderLocationOrientation,
                DateSubmitted = DateSubmitted
            };
        }
    }
}
