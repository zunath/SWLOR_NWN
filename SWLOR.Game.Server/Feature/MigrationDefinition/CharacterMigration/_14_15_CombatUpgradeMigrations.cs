using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Converts the combat upgrade's item properties (resistances, weapon damage types, combat readiness names,
/// pistol base items and ammunition, accuracy) and rebuilds the record's combat readiness and accuracy totals.
/// </summary>
internal sealed class _14_MigrateResistanceItemProperties : ICharacterMigration
{
    public int Version => 14;

    public void Migrate(SavedCharacter character)
    {
        SavedItemResistanceMigration.MigrateCharacter(character);
        SavedWeaponDamageTypeMigration.MigrateCharacter(character);
        SavedCombatUpgradeItemMigrations.MigrateCombatReadiness(character);
        SavedCombatUpgradeItemMigrations.MigratePistols(character);
        SavedCombatUpgradeItemMigrations.MigrateAccuracy(character);
    }
}

/// <summary>
/// Levels characters below 40 native levels, removes retired instruction items, recalibrates legacy sabers and
/// resets native feats to the basic set plus the feats owned perks grant. The record gains a rebuild token.
/// </summary>
internal sealed class _15_RemoveObsoleteCombatInstructionDiscs : ICharacterMigration
{
    public int Version => 15;

    public void Migrate(SavedCharacter character)
    {
        if (CharacterRecalculation.EnsureNativeLevels(character))
        {
            CharacterRecalculation.InitializeSkills(character);
            character.SetBaseSavingThrows(0);
            character.AdjustPlayerMaxHP(0);
        }
        SavedObsoleteItemMigration.MigrateCharacter(character);
        SavedLegacySaberMigration.MigrateCharacter(character);
        CharacterRecalculation.ResetFeatsToBaseline(character);
        CharacterRecalculation.RestorePlayerFeats(character);
    }

    public void MigratePlayerData(Player player)
    {
        if (!player.Currencies.ContainsKey(CurrencyType.RebuildToken))
            player.Currencies[CurrencyType.RebuildToken] = 0;
        player.Currencies[CurrencyType.RebuildToken]++;
    }
}
