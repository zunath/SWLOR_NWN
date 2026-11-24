#nullable enable
using SWLOR.Game.Server.Domain.Companions;

namespace SWLOR.Game.Server.Tests.Support;

// Part BM-3, BeastEventService: the companion control calls, the stat restore, the rest effect and the returned item of the event handlers.
public sealed partial class TestBeastMastery
{
    public bool ConversationHandled { get; set; }

    protected override void ClearCompanionControl(uint companion) => Calls.Add($"Clear {companion}");

    protected override void InitializeCompanionControl(uint companion) => Calls.Add($"Initialize {companion}");

    protected override void ProcessCompanionCombatRound(uint companion) => Calls.Add($"CombatRound {companion}");

    protected override void ProcessCompanionHeartbeat(uint companion) => Calls.Add($"Heartbeat {companion}");

    protected override bool HandleCompanionConversation(uint companion)
    {
        Calls.Add($"Conversation {companion}");
        return ConversationHandled;
    }

    protected override void RegisterDefensiveThreat(uint companion, uint threat) =>
        Calls.Add($"Threat {companion} {threat}");

    protected override void RecordBeastDeath(uint master, string beastId, uint killer) =>
        Calls.Add($"RecordDeath {master} {beastId} {killer}");

    protected override void RestoreBeastStats() => Calls.Add("RestoreBeastStats");

    protected override void LoadNPCStats() => Calls.Add("LoadNPCStats");

    protected override void ApplyCreatureMovementRate(uint creature) => Calls.Add($"MovementRate {creature}");

    protected override void ApplyRestStatusEffect(uint beast) => Calls.Add($"Rest {beast}");

    protected override void ReturnItem(uint owner, uint item) => Calls.Add($"ReturnItem {owner} {item}");
}
