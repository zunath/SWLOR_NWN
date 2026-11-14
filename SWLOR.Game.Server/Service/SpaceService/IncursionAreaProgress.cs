namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>How far the defenders got in one area of a running incursion. Saved with the incursion.</summary>
    public class IncursionAreaProgress
    {
        public int WavesCleared { get; set; }
        public bool CommandDestroyed { get; set; }
    }
}
