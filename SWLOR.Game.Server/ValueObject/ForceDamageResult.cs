namespace SWLOR.Game.Server.ValueObject
{
    public class ForceDamageResult
    {
        public int Damage { get; set; }
        public ForceResistanceResult Resistance { get; set; }
        public int ItemBonus { get; set; }
    }
}
