namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One change a component trait makes (see <see cref="ShipTraitStat"/> for the units).</summary>
    public class ShipTraitEffect
    {
        public ShipTraitStat Stat { get; }
        public int Value { get; }

        public ShipTraitEffect(ShipTraitStat stat, int value)
        {
            Stat = stat;
            Value = value;
        }
    }
}
