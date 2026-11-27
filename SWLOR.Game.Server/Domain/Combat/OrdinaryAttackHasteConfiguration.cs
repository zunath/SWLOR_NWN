namespace SWLOR.Game.Server.Domain.Combat;

public readonly record struct OrdinaryAttackHasteConfiguration(
    int HastePerStack, int MaximumStacks, int GrantCooldownSeconds, int DurationSeconds);
