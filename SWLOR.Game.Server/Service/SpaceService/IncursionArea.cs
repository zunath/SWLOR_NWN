namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One space area of an incursion region, fought at its own tier.</summary>
    public class IncursionArea
    {
        public string Resref { get; set; }
        public string Name { get; set; }
        public int Tier { get; set; }
    }
}
