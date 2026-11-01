using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Self-buffs a crew member takes from a station (<see cref="SpaceStanceRules"/>): stances and limited weapon cycle
    /// effects sit on the ship with the crew member as their source, their shot modifiers reach that crew member's
    /// station weapons, and they end when the crew member leaves the seat, as their Formation does.
    /// </summary>
    public static partial class Space
    {
        /// <summary>Whether a status on a ship is a self-buff its taker owns: a stance or a limited weapon cycle effect.</summary>
        private static bool IsOwnedSelfBuff(IStatusEffect effect) =>
            effect.SourceType == StatusEffectSourceType.Stance || effect is ILimitedWeaponCycleStatusEffect;

        private static IEnumerable<IStatusEffect> GetOwnedSelfBuffs(uint ship) =>
            StatusEffect.GetCreatureStatusEffects(ship).GetAllEffects().Where(IsOwnedSelfBuff);

        /// <summary>
        /// A shot stat for the firing ship's shot: the stat of the creature whose stats the shot reads, with a crew
        /// member's self-buff on the ship moved from the pilot's shots to that crew member's own. A station shot (a crew
        /// member's, or a droid's with no stat source) also takes the ship's other statuses, which cover the whole ship:
        /// an enemy's debuff (Disabling Shot, Break Formation) or a commander's aura. A pilot's shot already reads them.
        /// </summary>
        public static int GetShotStatAdjustment(uint ship, uint shotSource, StatType stat)
        {
            var value = Stat.GetStatAdjustment(shotSource, stat);
            if (!GetIsObjectValid(ship))
                return value;

            foreach (var buff in GetOwnedSelfBuffs(ship))
            {
                if (buff.StatGroup.Stats.TryGetValue(stat, out var amount))
                    value += SpaceStanceRules.ShotAdjustment(ship, shotSource, buff.Source, amount);
            }

            if (shotSource != ship)
                value += GetShipWideAdjustment(ship, shotSource, stat);

            return value;
        }

        /// <summary>
        /// The part of a station shot's stat that covers the whole firing ship: its statuses other than owned stances
        /// (an enemy's debuff, a commander's aura) and its temporary modifiers (a Wild Space system). A status the shot's
        /// source also carries is left out, so a command order that lands on the ship and on each crew member counts once.
        /// </summary>
        public static int GetShipWideAdjustment(uint ship, uint shotSource, StatType stat)
        {
            var sourceStatuses = GetIsObjectValid(shotSource)
                ? StatusEffect.GetCreatureStatusEffects(shotSource).GetAllEffects().Select(x => x.GetType()).ToHashSet()
                : new HashSet<System.Type>();

            var value = TemporaryStatModifier.GetStatAdjustment(ship, stat);
            foreach (var effect in StatusEffect.GetCreatureStatusEffects(ship).GetAllEffects())
            {
                if (!IsOwnedSelfBuff(effect) &&
                    !sourceStatuses.Contains(effect.GetType()) &&
                    effect.StatGroup.Stats.TryGetValue(stat, out var amount))
                {
                    value += amount;
                }
            }

            return value;
        }

        /// <summary>Ends the stances and self-buffs a crew member took on a ship from its station. Leaving the seat calls it.</summary>
        public static void RemoveUserSelfBuffs(uint ship, uint user)
        {
            if (!GetIsObjectValid(ship) || ship == user)
                return;

            foreach (var buff in GetOwnedSelfBuffs(ship).Where(x => x.Source == user).ToList())
                StatusEffect.RemoveStatusEffect(ship, buff.GetType(), user, false);
        }
    }
}
