using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NWN.Native.API;
using Ability = SWLOR.Game.Server.Service.Ability;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class QuickCastEngineTests
{
    [EngineTest("Settings targeting applies only on Save and casts through the NUI button handlers", Category = "QuickCast", TimeoutSeconds = 40f)]
    public static async Task SettingsTargeting(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        QuickCast.ResetSession(player);
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        SettingsViewModel LoadModel()
        {
            var model = new SettingsViewModel();
            typeof(GuiViewModelBase<SettingsViewModel, GuiPayloadBase>).GetProperty("Player", flags).SetValue(model, player);
            foreach (var method in new[] { "LoadGeneralView", "LoadIdentityView", "LoadChatView", "LoadTargetingView" })
                typeof(SettingsViewModel).GetMethod(method, flags).Invoke(model, null);
            return model;
        }

        // Settings Save updates the vitals window as well. Install the same player-window
        // instances as module entry, without inventing a connected client's NUI state.
        var windows = (Dictionary<string, Dictionary<GuiWindowType, GuiPlayerWindow>>)typeof(Gui)
            .GetField("_playerWindows", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var playerWindows = new Dictionary<GuiWindowType, GuiPlayerWindow>();
        foreach (var type in new[] { GuiWindowType.PlayerStatus, GuiWindowType.PlayerStatusPortrait, GuiWindowType.PlayerStatusPortraitSpace })
        {
            var template = Gui.GetWindowTemplate(type);
            var window = template.CreatePlayerWindowAction();
            window.ViewModel.Geometry = template.InitialGeometry;
            playerWindows.Add(type, window);
        }
        windows.Add(fixture.Id, playerWindows);
        try
        {
            var empty = LoadModel();
            ctx.AssertEqual(-1, empty.SelectedSelfCastFeat, "no known self abilities has an explicit empty option");
            ctx.AssertEqual(-1, empty.SelectedEnemyCastFeat, "no known enemy abilities has an explicit empty option");
            empty.SelfQuickCastEnabled = true;
            empty.EnemyQuickCastEnabled = true;
            await ctx.ExecuteInCreatureContextAsync(player, () => empty.OnSave()());
            ctx.Assert(!empty.CanSelfQuickCast && !empty.CanEnemyQuickCast, "empty lists cannot cast even when modes are applied");
            QuickCast.ResetSession(player);

            fixture.Update(record => record.Perks[PerkType.Innervate] = 1);
            CreaturePlugin.AddFeat(player, FeatType.Innervate1);
            ctx.SetResources(player, 100, 100);
            ObjectPlugin.SetCurrentHitPoints(player, 1);
            var model = LoadModel();
            ctx.Assert(!model.SelfQuickCastEnabled && !model.EnemyQuickCastEnabled, "Settings starts with both modes off");
            ctx.Assert(model.SelfCastAbilities.Any(entry => entry.Value == (int)FeatType.Innervate1), "known support ability appears in the selector");
            model.SelectedSelfCastFeat = (int)FeatType.Innervate1;
            model.SelfQuickCastEnabled = true;
            model.OnSelfQuickCast()();
            ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.Self) && !model.CanSelfQuickCast,
                "unsaved checkbox cannot enable a cast");
            ctx.AssertEqual(1, GetCurrentHitPoints(player), "unsaved cast does not heal");
            ctx.Assert(!LoadModel().SelfQuickCastEnabled, "discarding a draft leaves applied modes unchanged");
            await ctx.ExecuteInCreatureContextAsync(player, () => model.OnSave()());
            ctx.Assert(model.CanSelfQuickCast, "Save enables the chosen self-cast button");
            ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy), "Save keeps the other mode off");
            ctx.Assert(LoadModel().SelfQuickCastEnabled, "reopening Settings restores the applied mode during this login");
            ctx.Assert(GetObjectType(OBJECT_SELF) != ObjectType.Creature, "NUI callback starts outside creature context");
            model.OnSelfQuickCast()();
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(player) > 1 && !Activity.IsBusy(player), 8f, "Settings button self-heal");
            ctx.AssertEqual(97, Stat.GetCurrentFP(player), "Settings button uses the normal resource cost");
            model.SelectedSelfCastFeat = int.MaxValue;
            ctx.Assert(!model.CanSelfQuickCast, "a forged combo selection cannot enable the cast button");
            model.SelfQuickCastEnabled = false;
            await ctx.ExecuteInCreatureContextAsync(player, () => model.OnSave()());
            ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.Self), "Save applies disabling the mode");
            fixture.Update(record => record.Perks[PerkType.ForceDrain] = 1);
            CreaturePlugin.AddFeat(player, FeatType.ForceDrain1);
            var enemy = ctx.SpawnCreature("nw_bandit001", 2f);
            await ctx.DelaySecondsAsync(1f);
            ctx.MakeHostile(enemy);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 60f);
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), enemy, 60f);
            var enemyModel = LoadModel();
            enemyModel.SelectedEnemyCastFeat = (int)FeatType.ForceDrain1;
            enemyModel.EnemyQuickCastEnabled = true;
            await ctx.ExecuteInCreatureContextAsync(player, () => enemyModel.OnSave()());
            enemyModel.OnEnemyQuickCast()();
            await ctx.WaitUntilAsync(() => enemyModel.TargetingFeedback.Contains("Attack an enemy first"), 3f,
                "Settings enemy button rejects a missing attack target");
            await ctx.ExecuteInCreatureContextAsync(player, () => ActionAttack(enemy));
            await ctx.WaitUntilAsync(() => GetAttackTarget(player) == enemy, 10f, "current enemy for Settings button");
            Ability.ClearLastCompletedAbilityImpactSummary(player);
            enemyModel.OnEnemyQuickCast()();
            await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(player) != null && !Activity.IsBusy(player), 10f,
                "Settings enemy button impact");
            ctx.AssertEqual("Force Drain I", Ability.GetLastCompletedAbilityImpactSummary(player).Ability.Name,
                "Settings enemy button follows the normal impact pipeline");
            ctx.AssertEqual(93, Stat.GetCurrentFP(player), "both Settings buttons charge normal resource costs");
            fixture.Update(record =>
            {
                record.Perks[PerkType.RousingShout] = 3;
                record.Skills[SkillType.Leadership].Rank = 50;
            });
            CreaturePlugin.AddFeat(player, FeatType.RousingShout3);
            ctx.SetResources(player, 100, 100);
            var shoutModel = LoadModel();
            ctx.Assert(shoutModel.SelfCastAbilities.Any(entry => entry.Value == (int)FeatType.RousingShout3),
                "Rousing Shout is offered as a self-legal support ability");
            shoutModel.SelectedSelfCastFeat = (int)FeatType.RousingShout3;
            shoutModel.SelfQuickCastEnabled = true;
            await ctx.ExecuteInCreatureContextAsync(player, () => shoutModel.OnSave()());
            var beforeShoutHP = GetCurrentHitPoints(player);
            shoutModel.OnSelfQuickCast()();
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(player) > beforeShoutHP && !Activity.IsBusy(player), 8f,
                "Settings Rousing Shout self-support impact");
            ctx.AssertEqual(90, Stat.GetCurrentStamina(player), "Rousing Shout III charges its normal stamina cost");
            QuickCast.ResetSession(player);
            var nextLogin = LoadModel();
            ctx.Assert(!nextLogin.SelfQuickCastEnabled && !nextLogin.EnemyQuickCastEnabled, "fresh login restores both defaults");
        }
        finally
        {
            await ctx.ExecuteInCreatureContextAsync(player, () => ClearAllActions(true));
            foreach (var type in playerWindows.Keys)
                Gui.ClosePlayerWindow(player, type);
            windows.Remove(fixture.Id);
            QuickCast.ResetSession(player);
        }
    }

    [EngineTest("Quick casting respects native incapacitation and permits rooted casts", Category = "QuickCast", TimeoutSeconds = 60f)]
    public static async Task IncapacitationGuards(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        SetPlotFlag(player, false);
        fixture.Update(record => record.Perks[PerkType.Innervate] = 1);
        CreaturePlugin.AddFeat(player, FeatType.Innervate1);
        fixture.Update(record => record.Perks[PerkType.ForceDrain] = 1);
        CreaturePlugin.AddFeat(player, FeatType.ForceDrain1);
        ctx.SetResources(player, 100, 100);
        ObjectPlugin.SetCurrentHitPoints(player, 1);
        QuickCast.ResetSession(player);
        QuickCast.SetEnabled(player, QuickCastTargetType.Self, true);
        QuickCast.SetEnabled(player, QuickCastTargetType.CurrentEnemy, true);
        var controls = new (string Name, Func<Effect> Create)[]
        {
            ("stun", EffectStunned),
            ("daze", EffectDazed),
            ("sleep", EffectSleep),
            ("paralysis", EffectParalyze),
            ("cutscene paralysis", EffectCutsceneParalyze),
            ("petrification", EffectPetrify),
            ("knockdown", EffectKnockdown),
            ("confusion", EffectConfused),
            ("fear", EffectFrightened),
            ("domination", EffectDominated),
            ("cutscene domination", EffectCutsceneDominated),
            ("turning", EffectTurned)
        };
        const string effectTag = "QUICK_CAST_CONTROL_TEST";
        try
        {
            foreach (var control in controls)
            {
                ApplyEffectToObject(DurationType.Temporary,
                    TagEffect(IgnoreEffectImmunity(control.Create()), effectTag), player, 60f);
                await ctx.WaitFrameAsync();
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
                var types = new List<int>();
                foreach (var effect in native.m_appliedEffects)
                    types.Add(effect.GetScriptEffectType(1));
                ctx.Log($"{control.Name}: native effect types {string.Join(",", types)}");
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.Self, "Innervate I", out var error) &&
                               error.Contains("incapacitated"), $"{control.Name} blocks direct self casting");
                    ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out error) &&
                               error.Contains("incapacitated"), $"{control.Name} blocks direct enemy casting before target resolution");
                    ctx.AssertEqual(1, GetCurrentHitPoints(player), $"{control.Name} does not heal");
                    ctx.AssertEqual(100, Stat.GetCurrentFP(player), $"{control.Name} does not spend FP");
                    ctx.Assert(!Activity.IsBusy(player), $"{control.Name} does not start an activation");
                    ctx.Assert(!Recast.IsOnRecastDelay(player, Ability.GetAbilityDetail(FeatType.Innervate1).RecastGroup).Item1,
                        $"{control.Name} does not start recast");
                });
                RemoveEffectByTag(player, effectTag);
                await ctx.WaitFrameAsync();
            }

            ApplyEffectToObject(DurationType.Temporary, TagEffect(EffectCutsceneImmobilize(), effectTag), player, 60f);
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(QuickCast.TryCast(player, QuickCastTargetType.Self, "Innervate I", out var error),
                    $"roots still permit self casting: {error}"));
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(player) > 1 && !Activity.IsBusy(player), 8f,
                "a rooted player's permitted self-heal");
            ctx.AssertEqual(97, Stat.GetCurrentFP(player), "a rooted cast uses the ordinary FP cost");
        }
        finally
        {
            RemoveEffectByTag(player, effectTag);
            QuickCast.ResetSession(player);
        }
    }

    [EngineTest("Optional self-cast uses native player ownership, healing, resources and recast", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task SelfCastGuardsAndActivation(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record => record.Perks[PerkType.Innervate] = 1);
        CreaturePlugin.AddFeat(player, FeatType.Innervate1);
        ctx.SetResources(player, 100, 100);
        ObjectPlugin.SetCurrentHitPoints(player, 1);
        QuickCast.ResetSession(player);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.Self, "Innervate I", out var error) && error.Contains("off"), "self casting is off by default");
                QuickCast.SetEnabled(player, QuickCastTargetType.Self, true);
                ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy), "self casting does not enable enemy casting");
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.Self, "Benevolence I", out _), "unknown native feats are denied");
                ctx.Assert(QuickCast.TryCast(player, QuickCastTargetType.Self, "Innervate I", out error), $"known self support activates: {error}; {Ability.GetLastActivationDenialReason()}");
            });
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(player) > 1 && !Activity.IsBusy(player), 8f, "self-cast healing to complete");
            ctx.AssertEqual(97, Stat.GetCurrentFP(player), "the ordinary Innervate resource cost is charged");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.Self, "Innervate I", out _), "the ordinary recast prevents repeated activation");
                ctx.Assert(Ability.GetLastActivationDenialReason().Contains("can be used in"), "recast denial comes from the regular ability pipeline");
            });
            QuickCast.ResetSession(player);
            ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.Self), "a fresh session returns to off");
        }
        finally { QuickCast.ResetSession(player); }
        ctx.SetResultDetail("Native PC known-feat guard, separate off-by-default modes, actual Innervate self-heal, FP cost and recast checked. Settings-window rendering is verified separately with the native client.");
    }

    [EngineTest("Optional enemy-cast resolves an actual current attack target", Category = "QuickCast", TimeoutSeconds = 40f)]
    public static async Task CurrentEnemyGuardsAndActivation(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record => record.Perks[PerkType.ForceDrain] = 1);
        CreaturePlugin.AddFeat(player, FeatType.ForceDrain1);
        ctx.SetResources(player, 100, 100);
        var enemy = ctx.SpawnCreature("nw_bandit001", 2f);
        await ctx.DelaySecondsAsync(1f);
        ctx.MakeHostile(enemy);
        ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 60f);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), enemy, 60f);
        QuickCast.ResetSession(player);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out var error) && error.Contains("off"), "enemy casting is off by default");
                QuickCast.SetEnabled(player, QuickCastTargetType.CurrentEnemy, true);
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out error) && error.Contains("Attack an enemy first"), "missing attack target is denied without entering targeting mode");
                ActionAttack(enemy);
            });
            await ctx.WaitUntilAsync(() => GetAttackTarget(player) == enemy, 10f, "player to acquire the native current enemy");
            Ability.ClearLastCompletedAbilityImpactSummary(player);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out var error), $"current-enemy cast activates: {error}; {Ability.GetLastActivationDenialReason()}"));
            await ctx.WaitUntilAsync(() => Ability.GetLastCompletedAbilityImpactSummary(player) != null && !Activity.IsBusy(player), 10f, "the current-enemy cast to resolve");
            ctx.AssertEqual("Force Drain I", Ability.GetLastCompletedAbilityImpactSummary(player).Ability.Name, "the chosen ability completed through the regular impact pipeline");
            ctx.AssertEqual(96, Stat.GetCurrentFP(player), "the ordinary hostile-cast resource cost is charged");
            QuickCast.SetEnabled(player, QuickCastTargetType.CurrentEnemy, false);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.Assert(!QuickCast.TryCast(player, QuickCastTargetType.CurrentEnemy, "Force Drain I", out var error) && error.Contains("off"), "turning the mode off blocks existing shortcuts"));
        }
        finally
        {
            await ctx.ExecuteInCreatureContextAsync(player, () => ClearAllActions(true));
            QuickCast.ResetSession(player);
        }
        ctx.SetResultDetail("Native PC current attack target, default-off/missing-target denial, real Force Drain impact, FP cost and disable guard checked. Settings-window rendering is verified separately with the native client.");
    }

}
