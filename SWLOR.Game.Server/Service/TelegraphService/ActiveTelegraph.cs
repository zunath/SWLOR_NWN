namespace SWLOR.Game.Server.Service.TelegraphService
{
    public class ActiveTelegraph
    {
        public uint Area { get; set; }
        public TelegraphData Data { get; set; }
        public int Start { get; set; }
        public int End { get; set; }

        public ActiveTelegraph(uint area, int start, int end, TelegraphData data)
        {
            Area = area;
            Start = start;
            End = end;
            Data = data;
        }
    }
}
