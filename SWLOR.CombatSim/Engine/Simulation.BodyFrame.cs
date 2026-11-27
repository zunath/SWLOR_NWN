using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Scenario;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Engine;
public sealed partial class Simulation
{
    /// <summary>
    /// A combatant uses only what its body can train. An astromech has Utility Systems and Armaments in place of
    /// First Aid, Staff and Rifle, and no weapon, Force, Mimicry or beast lines; a humanoid has none of the astromech
    /// lines. A scenario that mixes them would measure a character the game cannot build.
    /// </summary>
    private void RequireOpenToBodyFrame(CombatantSpec spec)
    {
        var frame = Enum.Parse<BodyFrame>(spec.BodyFrame);

        foreach (var use in spec.Abilities)
        {
            if (Enum.TryParse<FeatType>(use.Feat, out var feat) &&
                _catalog.Perks.TryGetGrantingPerk(feat, out var perk, out _) &&
                !AstromechMirrors.IsOpenTo(perk.Category, frame))
            {
                throw new InvalidDataException(
                    $"'{spec.Name}' has the {frame} body and cannot use {use.Feat}: the {perk.Name} perk is closed to it.");
            }
        }

        foreach (var entry in spec.Perks)
        {
            if (!Enum.TryParse<PerkType>(entry.Split(':')[0].Trim(), true, out var type) || !Enum.IsDefined(type))
                continue;

            var perk = _catalog.Perks.Get(type);
            if (!AstromechMirrors.IsOpenTo(perk.Category, frame))
            {
                throw new InvalidDataException(
                    $"'{spec.Name}' has the {frame} body and cannot own {perk.Name}: the perk is closed to it.");
            }
        }
    }
}
