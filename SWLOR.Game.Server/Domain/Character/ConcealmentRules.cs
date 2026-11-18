using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Whether a creature is hidden from other players, read straight from the engine: no state and no services, so the status
    /// effect service can ask it without reaching up to Combat. <c>StealthService</c> answers its own concealment reads from the
    /// same rule (its invisibility read keeps the native pre-check in front of it).
    /// </summary>
    public static class ConcealmentRules
    {
        /// <summary>
        /// Returns whether the creature is hidden from other players: in stealth or invisible. Signals
        /// that would tell nearby players such a creature is there go only to the creature itself.
        /// </summary>
        public static bool IsConcealed(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   (GetActionMode(creature, ActionMode.Stealth) || HasEffect(creature, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility));
        }

        /// <summary>Returns whether the creature carries an applied effect of one of the two script types.</summary>
        public static bool HasEffect(uint creature, EffectTypeScript effectType, EffectTypeScript otherEffectType)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                if (type == effectType || type == otherEffectType)
                    return true;
            }

            return false;
        }
    }
}
