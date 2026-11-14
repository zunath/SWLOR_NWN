namespace SWLOR.Game.Server.Service.PerkService
{
    public enum PerkGroupType
    {
        Invalid = 0,
        Player = 1,
        Beast = 2,
        // Droid instructions with no player rank (the industrial protocols); never offered to players or beasts.
        Droid = 3,
    }
}
