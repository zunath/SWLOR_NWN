using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat;

/// <summary>Tracks the Twin Pistol capstone mastery trials inside the trial area.</summary>
public interface ITwinPistolMasteryService
{
    void RecordOrdinaryHit(uint actor, uint target, bool companion);

    void RecordAbility(uint actor, bool anyHit, PerkType perk, int cadenceSpent);

    void ClearTrialState(uint actor);

    void RecordTargetChange(uint actor, uint target);
}
