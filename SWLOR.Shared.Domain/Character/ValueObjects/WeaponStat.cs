namespace SWLOR.Shared.Domain.Character.ValueObjects
{
    public class WeaponStat
    {
        public int DMG { get; set; }
        public int Delay { get; set; }
        public uint Item { get; set; } = OBJECT_INVALID;
    }
}
