using NWN.Native.API;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// What a deflection sets off (part 6 of the stat split; until then a member of <c>IStatService</c>): the restores,
    /// the temporary boosts, the recast reduction and the next-ability bonuses a defender's deflection stats grant.
    /// </summary>
    public interface ICombatDeflectionReactionService
    {
        void ApplyDeflectionEffectsNative(CNWSCreature creature, DeflectionSource source);
    }
}
