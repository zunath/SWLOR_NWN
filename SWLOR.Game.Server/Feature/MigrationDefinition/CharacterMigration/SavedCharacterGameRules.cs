using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// The reads of the game's rules that <see cref="SavedCharacterDataStore.Load"/> makes on the main thread before any
/// character is upgraded. They name the Character, Combat and Items services, so they are content that the store
/// (Foundation) calls.
/// </summary>
internal static class SavedCharacterGameRules
{
    /// <summary>These caches fill on first use; fill them before worker threads read them.</summary>
    public static void FillCaches()
    {
        Resistance.IsValidResistanceType(ResistanceType.Fire);
        Resistance.GetAllResistanceTypes();
        CombatDamageCatalog.GetDefenseDamageTypes();
        PerkGrantedFeat.GetAllActiveAbilityFeats(PerkType.Invalid);
    }
}
