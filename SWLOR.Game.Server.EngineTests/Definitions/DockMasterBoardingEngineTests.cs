using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class DockMasterBoardingEngineTests
{
    [EngineTest("Hostile dock master blocks boarding on targeting and native combat", Category = "DockMasterBoarding", TimeoutSeconds = 45f)]
    public static async Task CombatBlocksBoarding(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var dockMaster = ctx.SpawnCreature("spc_dockhand", 2f);
        var enemy = ctx.SpawnCreature("nw_bandit001", 1f);
        await ctx.DelaySecondsAsync(1f);
        ctx.MakeHostile(enemy);
        SetLocalBool(dockMaster, ShipManagementViewModel.BoardingOutOfCombatVariable, true);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), player.Creature, 120f);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), enemy, 120f);

        var template = Gui.GetWindowTemplate(GuiWindowType.ShipManagement);
        ctx.AssertEqual(0, template.LayoutFindings.Count, "the production ship window has no layout warnings");
        var model = new ShipManagementViewModel();
        var modelBase = typeof(GuiViewModelBase<ShipManagementViewModel, ShipManagementPayload>);
        modelBase.GetProperty("Player", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, player.Creature);
        modelBase.GetProperty(nameof(model.TetherObject)).SetValue(model, dockMaster);
        // A denied click must return before resolving a selection or loading a ship interior.
        // The production click handler is exercised with no selected ship to catch a missing guard.
        var board = model.OnClickBoardShip();
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                ctx.Assert(!GetIsInCombat(player.Creature), "targeting scenario starts before native combat");
                Enmity.ModifyEnmity(player.Creature, enemy, 100);
                ctx.Assert(Enmity.HasEnmity(player.Creature), "the player is on an enemy's enmity table");
                ctx.Assert(!GetIsInCombat(player.Creature), "the enmity-only scenario does not rely on the native combat flag");
                board();
                ctx.AssertEqual(ctx.Arena, GetArea(player.Creature), "targeting denies boarding without moving the player");
                Enmity.ClearEnmityTable(enemy);
                AssignCommand(enemy, () => ClearAllActions());
            });

            AssignCommand(player.Creature, () => ActionAttack(enemy));
            AssignCommand(enemy, () => ActionAttack(player.Creature));
            await ctx.WaitUntilAsync(() => GetIsInCombat(player.Creature), 15f, "the player enters actual native combat");
            await ctx.ExecuteInCreatureContextAsync(player.Creature, () =>
            {
                Enmity.ClearEnmityTable(enemy);
                ctx.Assert(!Enmity.HasEnmity(player.Creature), "native combat scenario does not rely on enmity");
                ctx.Assert(GetIsInCombat(player.Creature), "the native combat flag is active at the click");
                board();
                ctx.AssertEqual(ctx.Arena, GetArea(player.Creature), "native combat denies boarding without moving the player");
            });
        }
        finally
        {
            Enmity.ClearEnmityTable(enemy);
            AssignCommand(player.Creature, () => ClearAllActions());
            AssignCommand(enemy, () => ClearAllActions());
        }
        ctx.SetResultDetail("Actual ship boarding handler rejects both targeting-only enmity and native combat; ship interior resolution is never reached. Production layout validation is clean.");
    }
}
