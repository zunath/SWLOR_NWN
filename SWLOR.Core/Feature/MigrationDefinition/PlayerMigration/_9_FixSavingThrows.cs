using SWLOR.Core.NWNX;
using SWLOR.Core.NWScript.Enum;

namespace SWLOR.Core.Feature.MigrationDefinition.PlayerMigration
{
    public class _9_FixSavingThrows: PlayerMigrationBase
    {
        public override int Version => 9;
        public override void Migrate(uint player)
        {
            // Addresses a problem with player saving throws being incorrect due to changes in the 2DA files.
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Fortitude, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Will, 0);
            CreaturePlugin.SetBaseSavingThrow(player, SavingThrow.Reflex, 0);
        }
    }
}
