namespace SWLOR.Game.Server.NWN.Enum
{
    public enum DamageType
    {
        Bludgeoning = 1,
        Piercing = 2,
        Slashing = 4,
        Magical = 8,
        Acid = 16,
        Cold = 32,
        Divine = 64,
        Electrical = 128,
        Fire = 256,
        Negative = 512,
        Positive = 1024,
        Sonic = 2048,

        // The base weapon damage is the base damage delivered by the weapon before
        // any additional types of damage (e.g. fire) have been added.
        BaseWeapon = 4096
    }
}