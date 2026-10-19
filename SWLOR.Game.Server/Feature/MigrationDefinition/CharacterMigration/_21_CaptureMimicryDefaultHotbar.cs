using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Records the quickbar positions of the active Mimicry build in that build, as <see cref="Mimicry.CaptureLoadoutHotbar"/>
/// does. The quickbar lives in the character file, so the record learns it from there.
/// </summary>
internal sealed class _21_CaptureMimicryDefaultHotbar : ICharacterMigration
{
    private const int TotalHotBarSlots = 36;

    public int Version => 21;

    public void Migrate(SavedCharacter character)
    {
        var record = character.Record;
        var active = record.MimicryLoadouts?.FirstOrDefault(x => x.Id == record.ActiveMimicryLoadoutId);
        if (!character.AppliesRecordEffects || active == null || active.HotbarSlots.Count > 0 || active.Techniques.Count == 0 ||
            !MimicryLoadouts.Matches(active.Techniques, record.EquippedTechniques))
            return;

        var slots = character.QuickBar;
        for (var slot = 0; slot < TotalHotBarSlots && slot < slots.Count; slot++)
        {
            if (slots[slot].Type != QuickBarSlotType.Feat)
                continue;
            var feat = QuickCast.GetOriginalFeat((FeatType)slots[slot].IntParameter);
            if (active.Techniques.Contains(feat))
                active.HotbarSlots[slot] = feat;
        }
    }
}
