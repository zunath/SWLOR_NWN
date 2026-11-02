using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A crafting style (Overclock, Safe Mode) is a Buff the player chose in a craft window. Cleanses, purges, stat-based removals
    /// and the clear on death must leave it; only a full reset takes it. The headless half is <c>CraftingStyleStatusEffectTests</c>.
    /// </summary>
    public static class CraftingStyleStatusEngineTests
    {
        private static int LoadBoost(uint creature) => Stat.GetStatAdjustment(creature, StatType.CircuitLoadBoostPercent);

        [EngineTest("Every bulk status removal leaves a crafting style and takes the ordinary effects beside it", Category = "CraftingStyle", TimeoutSeconds = 30f)]
        public static async Task BulkRemovalsLeaveCraftingStyles(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();

            StatusEffect.ApplyPermanentStatusEffect(typeof(OverclockStatusEffect), npc, npc);
            StatusEffect.ApplyPermanentStatusEffect(typeof(SafeModeStatusEffect), npc, npc);
            ctx.Assert(StatusEffect.HasStatusEffect<OverclockStatusEffect>(npc), "Overclock applies");
            ctx.Assert(StatusEffect.HasStatusEffect<SafeModeStatusEffect>(npc), "Safe Mode applies");
            var withStyles = LoadBoost(npc);
            ctx.AssertEqual(CircuitPerkRules.OverclockLoadBoostPercent + CircuitPerkRules.SafeModeLoadBoostPercent, withStyles,
                "the styles carry their Load boost through the stat service");

            void StylesRemain(string after)
            {
                ctx.Assert(StatusEffect.HasStatusEffect<OverclockStatusEffect>(npc), $"Overclock remains after {after}");
                ctx.Assert(StatusEffect.HasStatusEffect<SafeModeStatusEffect>(npc), $"Safe Mode remains after {after}");
                ctx.AssertEqual(withStyles, LoadBoost(npc), $"the styles still adjust the stat after {after}");
            }

            // Rifle Pacification's Neutralizing Shot: takes the first beneficial effect, which is the ordinary buff here.
            ctx.Assert(StatusEffect.ApplyStatusEffect<RegenerativeHealingStatusEffect>(npc, npc, 60f), "an ordinary buff applies");
            ctx.Assert(StatusEffect.RemoveFirstBeneficialCombatStatusEffect(npc, false), "the purge takes the ordinary buff");
            ctx.Assert(!StatusEffect.HasStatusEffect<RegenerativeHealingStatusEffect>(npc), "the ordinary buff is gone");
            ctx.Assert(!StatusEffect.RemoveFirstBeneficialCombatStatusEffect(npc, false), "the purge finds nothing else to take");
            StylesRemain("a beneficial-effect purge");

            ctx.Assert(StatusEffect.ApplyStatusEffect<RegenerativeHealingStatusEffect>(npc, npc, 60f), "the ordinary buff applies again");
            ctx.Assert(StatusEffect.RemoveFirstStatusEffectByCategory(npc, StatusEffectCategory.Buff, false), "the category removal takes the ordinary buff");
            ctx.Assert(!StatusEffect.RemoveFirstStatusEffectByCategory(npc, StatusEffectCategory.Buff, false), "the category removal finds no other Buff");
            StylesRemain("a Buff category removal");

            ctx.Assert(StatusEffect.ApplyStatusEffect<PoisonStatusEffect>(npc, npc, 60f), "a cleanseable debuff applies");
            StatusEffect.RemoveCleanseableStatusEffects(npc, StatusEffectCleanseType.Purify, false);
            ctx.Assert(!StatusEffect.HasStatusEffect<PoisonStatusEffect>(npc), "the cleanse takes the debuff");
            StylesRemain("a cleanse");

            ctx.Assert(StatusEffect.ApplyStatusEffect<PoisonStatusEffect>(npc, npc, 60f), "the debuff applies again");
            ctx.Assert(StatusEffect.RemoveFirstCleanseableStatusEffect(npc, StatusEffectCleanseType.Purify, false), "the single cleanse takes the debuff");
            ctx.Assert(!StatusEffect.RemoveFirstCleanseableStatusEffect(npc, StatusEffectCleanseType.Purify, false), "the single cleanse finds nothing else");
            StylesRemain("a single cleanse");

            StatusEffect.RemoveStatusEffectsWithStat(npc, StatType.CircuitLoadBoostPercent, false);
            StylesRemain("a removal by stat");
            StatusEffect.RemoveStatusEffectsWithNegativeStat(npc, StatType.CircuitLoadBoostPercent, false);
            StylesRemain("a removal by negative stat");

            ctx.Assert(StatusEffect.ApplyStatusEffect<RegenerativeHealingStatusEffect>(npc, npc, 60f), "the ordinary buff applies a third time");
            StatusEffect.RemoveAllStatusEffects(npc);
            ctx.Assert(!StatusEffect.HasStatusEffect<RegenerativeHealingStatusEffect>(npc), "remove-all takes the ordinary buff");
            StylesRemain("remove-all");

            StatusEffect.RemoveAllStatusEffects(npc, includeCraftingStyles: true);
            ctx.Assert(!StatusEffect.HasStatusEffect<OverclockStatusEffect>(npc), "a full reset takes Overclock");
            ctx.Assert(!StatusEffect.HasStatusEffect<SafeModeStatusEffect>(npc), "a full reset takes Safe Mode");
            ctx.AssertEqual(0, LoadBoost(npc), "a full reset takes the stat too");
        }

        [EngineTest("Circuit styles stay exclusive and survive a purge", Category = "CraftingStyle", TimeoutSeconds = 30f)]
        public static async Task CircuitStylesStayExclusiveThroughPurges(EngineTestContext ctx)
        {
            var npc = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            ctx.SetNPCPerkLevel(npc, PerkType.OverclockStyle, 1);
            ctx.SetNPCPerkLevel(npc, PerkType.SafeModeStyle, 1);

            CircuitStyles.Select(npc, CircuitStyle.Overclock);
            ctx.AssertEqual(CircuitStyle.Overclock, CircuitStyles.Active(npc), "Overclock is selected");
            StatusEffect.RemoveFirstBeneficialCombatStatusEffect(npc, false);
            StatusEffect.RemoveAllStatusEffects(npc);
            ctx.AssertEqual(CircuitStyle.Overclock, CircuitStyles.Active(npc), "Overclock survives a purge and remove-all");

            CircuitStyles.Select(npc, CircuitStyle.SafeMode);
            ctx.AssertEqual(CircuitStyle.SafeMode, CircuitStyles.Active(npc), "Safe Mode replaces Overclock");
            ctx.Assert(!StatusEffect.HasStatusEffect<OverclockStatusEffect>(npc), "only one style is active");
            StatusEffect.RemoveFirstStatusEffectByCategory(npc, StatusEffectCategory.Buff, false);
            ctx.AssertEqual(CircuitStyle.SafeMode, CircuitStyles.Active(npc), "Safe Mode survives a category removal");

            CircuitStyles.Select(npc, CircuitStyle.Overclock);
            ctx.AssertEqual(CircuitStyle.Overclock, CircuitStyles.Active(npc), "Overclock replaces Safe Mode again");
            ctx.Assert(!StatusEffect.HasStatusEffect<SafeModeStatusEffect>(npc), "still only one style is active");

            // An NPC's perk level of 0 falls back to the perk's maximum, so a refund cannot be simulated; an owned style must stay.
            CircuitStyles.RemoveUnowned(npc);
            ctx.AssertEqual(CircuitStyle.Overclock, CircuitStyles.Active(npc), "an owned style stays when unowned styles are cleared");

            // The craft window and a perk refund remove a style by its own class.
            StatusEffect.RemoveStatusEffect<OverclockStatusEffect>(npc);
            ctx.AssertEqual(CircuitStyle.None, CircuitStyles.Active(npc), "removing a style by its class still works");

            CircuitStyles.Select(npc, CircuitStyle.SafeMode);
            CircuitStyles.Select(npc, CircuitStyle.None);
            ctx.AssertEqual(CircuitStyle.None, CircuitStyles.Active(npc), "the craft window can clear the style");
        }
    }
}
