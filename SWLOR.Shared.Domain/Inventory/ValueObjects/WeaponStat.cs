namespace SWLOR.Shared.Domain.Inventory.ValueObjects
{
    public class WeaponStat
    {
        public int DMG { get; set; }
        public int Delay { get; set; }
        public uint Item { get; set; } = OBJECT_INVALID;
    }
}
