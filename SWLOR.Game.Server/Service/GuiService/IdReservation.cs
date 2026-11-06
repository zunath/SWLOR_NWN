namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// The range of GUI ids a system reserved.
    /// </summary>
    public class IdReservation
    {
        public int Count { get; set; }
        public int StartId { get; set; }
        public int EndId { get; set; }
    }
}
