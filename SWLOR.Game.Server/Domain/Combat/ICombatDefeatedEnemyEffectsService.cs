using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What a kill sets off: the effects for the killer, the effects for the recent damagers of the defeated enemy, and the
    /// poison spread of an ability that spreads its poison from the target.
    /// </summary>
    public interface ICombatDefeatedEnemyEffectsService
    {
        void ApplyDefeatedEnemyEffects(uint creature, uint defeated = OBJECT_INVALID);

        void ApplyRecentDamagerDefeatedEnemyEffects(uint defeated);

        void SpreadPoisonFromTarget(uint activator, uint target);
    }
}
