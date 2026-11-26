#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Part BM-3, <see cref="BeastEventService"/>, over a <see cref="TestBeastMastery"/> (the IDs and the player beast test) with the companion
/// control calls, the stat restore, the rest effect, the returned item and the window answered by the test: each is written to the list the
/// host records its seams in.
/// </summary>
public sealed class TestBeastEventService : BeastEventService
{
    private readonly TestBeastMastery _host;

    public TestBeastEventService(TestBeastMastery host, IDBService db)
        : base(db, FacadeForwarder.For<ICompanionControlService>(), FacadeForwarder.For<ICreatureStatService>(), FacadeForwarder.For<IStatusEffectApplicationService>(), FacadeForwarder.For<IItemService>(), FacadeForwarder.For<IGuiService>(), host)
    {
        _host = host;
    }

    public bool ConversationHandled { get; set; }

    protected override void ClearCompanionControl(uint companion) => _host.Record($"Clear {companion}");

    protected override void InitializeCompanionControl(uint companion) => _host.Record($"Initialize {companion}");

    protected override void ProcessCompanionCombatRound(uint companion) => _host.Record($"CombatRound {companion}");

    protected override void ProcessCompanionHeartbeat(uint companion) => _host.Record($"Heartbeat {companion}");

    protected override bool HandleCompanionConversation(uint companion)
    {
        _host.Record($"Conversation {companion}");
        return ConversationHandled;
    }

    protected override void RegisterDefensiveThreat(uint companion, uint threat) =>
        _host.Record($"Threat {companion} {threat}");

    protected override void RecordBeastDeath(uint master, string beastId, uint killer) =>
        _host.Record($"RecordDeath {master} {beastId} {killer}");

    protected override void RestoreBeastStats() => _host.Record("RestoreBeastStats");

    protected override void LoadNPCStats() => _host.Record("LoadNPCStats");

    protected override void ApplyCreatureMovementRate(uint creature) => _host.Record($"MovementRate {creature}");

    protected override void ApplyRestStatusEffect(uint beast) => _host.Record($"Rest {beast}");

    protected override void ReturnItem(uint owner, uint item) => _host.Record($"ReturnItem {owner} {item}");

    protected override void TogglePlayerWindow(uint player, GuiWindowType windowType, GuiPayloadBase payload, uint tetherObject) =>
        _host.RecordWindow(player, windowType, payload, tetherObject);
}
