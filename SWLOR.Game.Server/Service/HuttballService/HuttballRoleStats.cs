namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// A role's bonuses to strength and agility rolls, its movement in squares, and whether it opposes players
    /// moving away from it with strength instead of agility.
    /// </summary>
    public readonly record struct HuttballRoleStats(int Strength, int Agility, int Movement, bool OpposesMovementWithStrength);
}
