using System;

namespace SWLOR.Game.Server.Entity
{
    public class BugReport: EntityBase
    {
        [Indexed]
        public Guid? SenderPlayerId { get; set; }
        [Indexed]
        public string SenderName { get; set; }
        [Indexed]
        public string CDKey { get; set; }
        [Indexed]
        public string Text { get; set; }
        [Indexed]
        public string AreaResref { get; set; }
        public double SenderLocationX { get; set; }
        public double SenderLocationY { get; set; }
        public double SenderLocationZ { get; set; }
        public double SenderLocationOrientation { get; set; }
    }
}
