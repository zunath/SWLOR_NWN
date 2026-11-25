#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// The seams several parts call, and the one list every seam writes to. The perk read is called by the XP code (BM-1) and the incubator (BM-4); the
// stables menu (BM-3) and the incubator each open a window. The two parts that have their own double (BM-2, BM-4) write to this list and read
// these perk levels through Record, RecordWindow and PerkLevels.
public sealed partial class TestBeastMastery
{
    public Dictionary<(uint Creature, PerkType Perk), int> PerkLevels { get; } = new();
    public List<string> Calls { get; } = new();
    public GuiPayloadBase? Payload { get; private set; }

    protected override int GetPerkLevel(uint creature, PerkType perkType) =>
        PerkLevels.GetValueOrDefault((creature, perkType));

    protected override void TogglePlayerWindow(uint player, GuiWindowType windowType, GuiPayloadBase payload, uint tetherObject) =>
        RecordWindow(player, windowType, payload, tetherObject);

    internal void Record(string call) => Calls.Add(call);

    internal void RecordWindow(uint player, GuiWindowType windowType, GuiPayloadBase payload, uint tetherObject)
    {
        Calls.Add($"Window {player} {windowType} {tetherObject}");
        Payload = payload;
    }
}
