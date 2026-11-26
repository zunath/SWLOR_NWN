namespace SWLOR.Game.Server.Service.TelegraphService
{
    public class ActiveTelegraph
    {
        public uint Area { get; set; }
        public TelegraphData Data { get; set; }
        public int Start { get; set; }
        public int End { get; set; }

        /// <summary>When the shape starts filling, on the server's fill clock (milliseconds).</summary>
        public long FillStartMilliseconds { get; set; }

        /// <summary>How long the shape takes to fill, in milliseconds. Zero for shapes that never fill.</summary>
        public int FillDurationMilliseconds { get; set; }

        public ActiveTelegraph(uint area, int start, int end, TelegraphData data)
        {
            Area = area;
            Start = start;
            End = end;
            Data = data;
        }
    }
}
