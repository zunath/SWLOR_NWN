namespace SWLOR.Game.Server.ValueObject
{
    public class AbilityResistanceResult
    {
        public int DC { get; set; }
        public int Roll { get; set; }

        public bool IsResisted => Roll < DC;
        public int Delta => Roll - DC;

    }
}
