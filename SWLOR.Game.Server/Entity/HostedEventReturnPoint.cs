namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// Where a player joined an event from, so /leaveevent can send them back.
    /// </summary>
    public class HostedEventReturnPoint
    {
        public string EventId { get; set; }
        public string EventName { get; set; }
        public string AreaResref { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Facing { get; set; }

        public HostedEventReturnPoint()
        {
            EventId = string.Empty;
            EventName = string.Empty;
            AreaResref = string.Empty;
        }
    }
}
