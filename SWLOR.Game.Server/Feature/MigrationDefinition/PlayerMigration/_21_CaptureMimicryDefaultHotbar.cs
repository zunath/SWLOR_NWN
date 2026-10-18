using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;

/// <summary>
/// Records the quickbar positions of the build created by server migration 27. The quickbar lives in
/// the character file, so this runs on the character's first login after that migration.
/// </summary>
public sealed class _21_CaptureMimicryDefaultHotbar : PlayerMigrationBase
{
    public override int Version => 21;

    public override void Migrate(uint player)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        var active = dbPlayer?.MimicryLoadouts?.FirstOrDefault(x => x.Id == dbPlayer.ActiveMimicryLoadoutId);
        if (active == null || active.HotbarSlots.Count > 0 || active.Techniques.Count == 0 ||
            !MimicryLoadouts.Matches(active.Techniques, dbPlayer.EquippedTechniques))
            return;

        Mimicry.CaptureLoadoutHotbar(player, active);
        DB.Set(dbPlayer);
    }
}
