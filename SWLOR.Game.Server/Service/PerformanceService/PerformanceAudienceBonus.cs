namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>A performer's own audience bonus: the skill XP percent (after SOC scaling) and the XP debt percent cleared each minute.</summary>
    public sealed record PerformanceAudienceBonus(int XpPercent, int DebtPercent);
}
