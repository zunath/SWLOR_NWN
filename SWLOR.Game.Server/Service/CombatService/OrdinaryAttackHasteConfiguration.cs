namespace SWLOR.Game.Server.Service.CombatService;

public readonly record struct OrdinaryAttackHasteConfiguration(
    int HastePerStack, int MaximumStacks, int GrantCooldownSeconds, int DurationSeconds);
