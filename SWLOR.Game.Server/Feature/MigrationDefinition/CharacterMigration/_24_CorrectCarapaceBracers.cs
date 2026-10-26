using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// The release's single player migration. It removes the Carapace Bracer's and Carapace Wraps' retired Poison
/// protection and gives droid parts and controllers the Unarmed and Twin Pistol skills
/// (<see cref="DroidWeaponSkillMigration"/>) in the combined inventory traversal, and it converts the crafting perks
/// to skill ranks (<see cref="CraftingPerkConversion"/>): the retired perks leave the character's feats, and the
/// record gets their SP back. The record gains one optional rebuild token for the whole release.
/// </summary>
internal sealed class _24_CorrectCarapaceBracers : ICharacterMigration
{
    public int Version => 24;

    public void Migrate(SavedCharacter character)
    {
        CharacterLootRevision.Migrate(character, updateArtwork: true);
        CraftingPerkConversion.RemoveGrantedFeats(character);
    }

    public void MigratePlayerData(Player player)
    {
        CraftingPerkConversion.RefundRetiredPerks(player);
        RebuildTokenGrant.Grant(player);
        player.UpgradeNotices ??= new List<string>();
        player.UpgradeNotices.Add("You received 1 Rebuild Token. Rebuilding your character is optional.");
    }
}
