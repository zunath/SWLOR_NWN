#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// The seams several parts call, and the one list every seam writes to. The perk read is called by the XP code (BM-1) and the incubator (BM-4); the
// feat sync by the spawn (BM-2) and the stat application (BM-1); the stables menu (BM-3) and the incubator each open a window.
public sealed partial class TestBeastMastery
{
    public Dictionary<(uint Creature, PerkType Perk), int> PerkLevels { get; } = new();
    public List<string> Calls { get; } = new();
    public GuiPayloadBase? Payload { get; private set; }

    protected override int GetPerkLevel(uint creature, PerkType perkType) =>
        PerkLevels.GetValueOrDefault((creature, perkType));

    protected override void SyncGrantedFeats(uint creature, PerkType perkType, int perkLevel, bool addByLevel) =>
        Calls.Add($"SyncGrantedFeats {creature} {perkType} {perkLevel} {addByLevel}");

    protected override void TogglePlayerWindow(uint player, GuiWindowType windowType, GuiPayloadBase payload, uint tetherObject)
    {
        Calls.Add($"Window {player} {windowType} {tetherObject}");
        Payload = payload;
    }
}
