using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Which of a creature's effects a bulk removal (a cleanse, a purge, a dispel, a "remove effects with this stat" rule or a
    /// clear on death) may take. A crafting style (<see cref="StatusEffectSourceType.CraftingStyle"/>) is a choice the player made
    /// in a craft window, so no bulk removal takes one unless the caller asks for it by passing <c>includeCraftingStyles</c>.
    /// Removing a style by its own class, as the craft window and a perk refund do, never goes through here.
    /// </summary>
    public static class StatusEffectSelectors
    {
        public static bool IsCraftingStyle(IStatusEffect effect) => effect.SourceType == StatusEffectSourceType.CraftingStyle;

        /// <summary>The effects a bulk removal may take: all of them, or all but the crafting styles.</summary>
        public static IEnumerable<IStatusEffect> Removable(IEnumerable<IStatusEffect> effects, bool includeCraftingStyles = false) =>
            includeCraftingStyles ? effects : effects.Where(effect => !IsCraftingStyle(effect));

        public static IEnumerable<IStatusEffect> WithCleanseType(IEnumerable<IStatusEffect> effects, StatusEffectCleanseType cleanseType) =>
            Removable(effects).Where(effect => (effect.CleanseTypes & cleanseType) == cleanseType);

        public static IEnumerable<IStatusEffect> WithCategory(IEnumerable<IStatusEffect> effects, StatusEffectCategory category) =>
            Removable(effects).Where(effect => (effect.Categories & category) == category);

        public static IEnumerable<IStatusEffect> BeneficialInCombat(IEnumerable<IStatusEffect> effects) =>
            Removable(effects).Where(IsBeneficialCombatStatusEffect);

        public static IEnumerable<IStatusEffect> WithStat(IEnumerable<IStatusEffect> effects, StatType statType) =>
            Removable(effects).Where(effect => effect.StatGroup.Stats.TryGetValue(statType, out var value) && value != 0);

        public static IEnumerable<IStatusEffect> WithNegativeStat(IEnumerable<IStatusEffect> effects, StatType statType) =>
            Removable(effects).Where(effect => effect.StatGroup.Stats.TryGetValue(statType, out var value) && value < 0);

        /// <summary>True for the effects a "remove one beneficial effect" rule, such as a purge, may take.</summary>
        public static bool IsBeneficialCombatStatusEffect(IStatusEffect effect)
        {
            if ((effect.Categories & StatusEffectCategory.Buff) == StatusEffectCategory.Buff)
                return true;

            if (effect.PersistsOnLogout ||
                effect.CleanseTypes != StatusEffectCleanseType.None ||
                (effect.Categories & (StatusEffectCategory.Debuff | StatusEffectCategory.Control | StatusEffectCategory.Bleeding)) != 0)
            {
                return false;
            }

            return effect.StatGroup.Abilities.Any(x => x.Value > 0) ||
                   effect.StatGroup.Resists.Any(x => x.Value > 0) ||
                   effect.StatGroup.Stats.Any(x => Stat.IsBeneficialStatAdjustment(x.Key, x.Value));
        }
    }
}
