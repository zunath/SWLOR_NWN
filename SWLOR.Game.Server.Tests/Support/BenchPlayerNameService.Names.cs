using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seams the display name and known name code calls: the script-context hook and the disguise and descriptor reads.</summary>
public sealed partial class BenchPlayerNameService
{
    public Action? ScriptContextEnd { get; private set; }
    public int ContextSubscriptions { get; private set; }
    public Dictionary<uint, PlayerDisguise> Disguises { get; } = new();
    public Dictionary<uint, string> Descriptors { get; } = new();
    public HashSet<uint> ScrambledAccounts { get; } = new();
    public Dictionary<string, string> DescriptorsById { get; } = new();

    protected override void SubscribeScriptContextEnd(Action handler)
    {
        ContextSubscriptions++;
        ScriptContextEnd = handler;
    }

    public override PlayerDisguise GetActiveDisguise(uint player) => Disguises.GetValueOrDefault(player)!;

    public override string GetDisplayDescriptor(uint player) => Descriptors.GetValueOrDefault(player, "Tall Stranger");

    public override string GetIdentityKey(uint player) =>
        Disguises.TryGetValue(player, out var disguise) ? "disguise:" + disguise.Id : "id-of-" + player;

    public override bool ShouldScrambleAccountName(uint player) => ScrambledAccounts.Contains(player);

    public override string GetUnknownDisplayNameByPlayerId(string targetPlayerId) =>
        DescriptorsById.GetValueOrDefault(targetPlayerId, "Someone");
}
