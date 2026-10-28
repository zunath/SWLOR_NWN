namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>A space area hidden signatures appear in, with its tier and how many it holds at once.</summary>
    public class ScanAreaDetail
    {
        public string AreaResref { get; init; }
        public int Tier { get; init; }
        public int Signatures { get; init; }
    }
}
