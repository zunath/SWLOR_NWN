using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// A Power Glove's equipment stats now count once per hand (<see cref="PowerGloveEquipment.GetEquipmentStatCopies"/>),
/// so one glove grants what two identical Katars do. A character wearing a glove before this holds only one hand's
/// share on the record; this adds the other hand's share.
/// </summary>
internal sealed class _25_CountPowerGloveStatsPerHand : ICharacterMigration
{
    public int Version => 25;

    public void Migrate(SavedCharacter character)
    {
        var glove = character.GetEquipped(InventorySlot.RightHand);
        if (glove?.BaseItem != BaseItem.PowerGlove)
            return;
        character.ApplyOneHandItemStats(glove, true);
        character.RefreshCombatReadinessEquipment();
    }
}
