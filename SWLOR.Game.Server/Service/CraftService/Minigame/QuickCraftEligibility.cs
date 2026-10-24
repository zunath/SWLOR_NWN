namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>Whether a player may quick craft a recipe, and the reason to show when they may not.</summary>
public sealed record QuickCraftEligibility(bool IsEligible, string Reason)
{
    public static readonly QuickCraftEligibility Eligible = new(true, string.Empty);

    public static QuickCraftEligibility Refused(string reason) => new(false, reason);
}
