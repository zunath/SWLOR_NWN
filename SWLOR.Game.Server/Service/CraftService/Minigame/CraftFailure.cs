using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>What a broken craft costs, decided by <see cref="CraftOutcomeRules.RollFailure"/>.</summary>
/// <param name="EnhancementsKept">For each filled enhancement slot in order, true when the enhancement comes back.</param>
/// <param name="ComponentsKept">For each committed component stack in order, true when the stack comes back.</param>
/// <param name="Xp">The XP a failed craft still grants.</param>
public sealed record CraftFailure(
    IReadOnlyList<bool> EnhancementsKept,
    IReadOnlyList<bool> ComponentsKept,
    int Xp);
