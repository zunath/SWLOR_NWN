namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The status effect a meal applies. A player's temporary food HP lasts only while it does, so the maximum HP rebuild asks
    /// for it by this capability. Character owns the capability, so the stat code names no status effect definition.
    /// </summary>
    public interface IFoodStatusEffect : IStatusEffect
    {
    }
}
