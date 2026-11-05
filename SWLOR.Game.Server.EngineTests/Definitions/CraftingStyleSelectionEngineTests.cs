using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// The crafter's selected style is kept on the player record and applied again after a death, a login and a perk refund.
    /// The headless half is <c>CraftingStyleSelectionTests</c>. The fixture is a headless player with a record, not a connected
    /// client, so the logout, login and death events themselves are not raised: they are stood in for by removing the effect
    /// and calling the restore their handler or the revive hook calls.
    /// </summary>
    public static class CraftingStyleSelectionEngineTests
    {
        private static PerkType Selected(string id) =>
            CraftingStyleSelection.Selected(DB.Get<Player>(id), SkillType.Engineering);

        private static async Task<PlayerAbilityFixture> OwningBothStylesAsync(EngineTestContext ctx)
        {
            var player = await PlayerAbilityFixture.CreateAsync(ctx);
            player.Update(record =>
            {
                record.Perks[PerkType.OverclockStyle] = 1;
                record.Perks[PerkType.SafeModeStyle] = 1;
            });
            return player;
        }

        [EngineTest("Selecting a Circuit style in the window records it on the player, one style at a time", Category = "CraftingStyle", TimeoutSeconds = 30f)]
        public static async Task SelectingAStyleRecordsIt(EngineTestContext ctx)
        {
            using var player = await OwningBothStylesAsync(ctx);
            ctx.AssertEqual(PerkType.Invalid, Selected(player.Id), "a new crafter has no selection");

            CircuitStyles.Select(player.Creature, CircuitStyle.Overclock);
            ctx.AssertEqual(CircuitStyle.Overclock, CircuitStyles.Active(player.Creature), "Overclock is active");
            ctx.AssertEqual(PerkType.OverclockStyle, Selected(player.Id), "Overclock is recorded");

            CircuitStyles.Select(player.Creature, CircuitStyle.SafeMode);
            ctx.AssertEqual(CircuitStyle.SafeMode, CircuitStyles.Active(player.Creature), "Safe Mode replaces it");
            ctx.AssertEqual(PerkType.SafeModeStyle, Selected(player.Id), "the record follows the change, with one style per skill");

            CircuitStyles.Select(player.Creature, CircuitStyle.None);
            ctx.AssertEqual(CircuitStyle.None, CircuitStyles.Active(player.Creature), "None clears the effect");
            ctx.AssertEqual(PerkType.Invalid, Selected(player.Id), "None clears the record");
        }

        [EngineTest("A selected Circuit style comes back when a revive settles", Category = "CraftingStyle", TimeoutSeconds = 30f)]
        public static async Task SelectedStyleSurvivesDeath(EngineTestContext ctx)
        {
            using var player = await OwningBothStylesAsync(ctx);
            var creature = player.Creature;
            CircuitStyles.Select(creature, CircuitStyle.SafeMode);

            // Death strips every effect (an engine behavior verified on a killed creature); the fixture cannot be killed, so the
            // strip is stood in for. Every revive path (respawn, subdual, Resuscitation, a DM raise) ends in this hook.
            StatusEffect.RemoveAllStatusEffects(creature, includeCraftingStyles: true);
            ctx.AssertEqual(CircuitStyle.None, CircuitStyles.Active(creature), "the style effect is gone");
            ctx.AssertEqual(PerkType.SafeModeStyle, Selected(player.Id), "the selection is still on the record");

            await ctx.ExecuteInCreatureContextAsync(creature, () => PlayerTemporaryEffects.RestoreGhostAfterRevive(creature));
            await ctx.WaitUntilAsync(() => CircuitStyles.Active(creature) == CircuitStyle.SafeMode, 5f, "the revive to restore the selected style");
            ctx.Assert(!StatusEffect.HasEffect<OverclockStatusEffect>(creature), "only the selected style is restored");
            ctx.AssertEqual(PerkType.SafeModeStyle, Selected(player.Id), "the record is unchanged");
        }

        [EngineTest("A selected Circuit style is applied again on login, only while its perk is owned", Category = "CraftingStyle", TimeoutSeconds = 30f)]
        public static async Task SelectedStyleReturnsOnLogin(EngineTestContext ctx)
        {
            using var player = await OwningBothStylesAsync(ctx);
            var creature = player.Creature;
            CircuitStyles.Select(creature, CircuitStyle.Overclock);

            // Logging out drops the non-persistent effect; the record stays.
            StatusEffect.RemoveAllStatusEffects(creature, includeCraftingStyles: true);
            ctx.AssertEqual(CircuitStyle.None, CircuitStyles.Active(creature), "the style effect does not outlive the session");
            await ctx.ExecuteInCreatureContextAsync(creature, () => CraftingStyleSelection.Restore(creature));
            ctx.AssertEqual(CircuitStyle.Overclock, CircuitStyles.Active(creature), "login applies the selected style");
            ctx.Assert(!StatusEffect.HasEffect<SafeModeStatusEffect>(creature), "and not the other one");

            // A second login while the style is already active leaves it alone.
            var effect = StatusEffect.GetStatusEffect(creature, typeof(OverclockStatusEffect));
            await ctx.ExecuteInCreatureContextAsync(creature, () => CraftingStyleSelection.Restore(creature));
            ctx.Assert(ReferenceEquals(effect, StatusEffect.GetStatusEffect(creature, typeof(OverclockStatusEffect))),
                "restoring an active style does not stack or replace it");

            // The perk was refunded while the effect was gone (a refund by a character upgrade, say): nothing comes back.
            StatusEffect.RemoveAllStatusEffects(creature, includeCraftingStyles: true);
            player.Update(record => record.Perks.Remove(PerkType.OverclockStyle));
            await ctx.ExecuteInCreatureContextAsync(creature, () => CraftingStyleSelection.Restore(creature));
            ctx.AssertEqual(CircuitStyle.None, CircuitStyles.Active(creature), "a style whose perk is no longer owned is not applied");
            ctx.AssertEqual(PerkType.Invalid, Selected(player.Id), "and its stale selection is dropped");
        }

        [EngineTest("Refunding a Circuit style perk removes the style and forgets the selection", Category = "CraftingStyle", TimeoutSeconds = 30f)]
        public static async Task RefundedStyleIsForgotten(EngineTestContext ctx)
        {
            using var player = await OwningBothStylesAsync(ctx);
            var creature = player.Creature;
            CircuitStyles.Select(creature, CircuitStyle.SafeMode);

            // Refunding the other style leaves the selection alone.
            player.Update(record => record.Perks.Remove(PerkType.OverclockStyle));
            CircuitStyles.RemoveUnowned(creature);
            ctx.AssertEqual(CircuitStyle.SafeMode, CircuitStyles.Active(creature), "an owned selected style stays");
            ctx.AssertEqual(PerkType.SafeModeStyle, Selected(player.Id), "and stays selected");

            // This is the refund trigger of both style perks, run after the perk has left the record.
            player.Update(record => record.Perks.Remove(PerkType.SafeModeStyle));
            CircuitStyles.RemoveUnowned(creature);
            ctx.AssertEqual(CircuitStyle.None, CircuitStyles.Active(creature), "the refunded style is removed");
            ctx.AssertEqual(PerkType.Invalid, Selected(player.Id), "and forgotten");

            await ctx.ExecuteInCreatureContextAsync(creature, () => CraftingStyleSelection.Restore(creature));
            ctx.AssertEqual(CircuitStyle.None, CircuitStyles.Active(creature), "nothing comes back at the next login");
        }
    }
}
