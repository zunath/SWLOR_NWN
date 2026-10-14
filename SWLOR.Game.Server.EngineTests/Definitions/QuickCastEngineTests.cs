using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using NWN.Native.API;
using Ability = SWLOR.Game.Server.Service.Ability;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class QuickCastEngineTests
{
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

    [EngineTest("Character exports omit quick-cast bindings through repeated saves and fresh logins", Category = "QuickCast", TimeoutSeconds = 60f)]
    public static Task CharacterExportBindings(EngineTestContext ctx) => VerifySavedBindings(ctx, characterExport: true);

    [EngineTest("Stored quickbar snapshots omit quick-cast bindings through repeated saves and fresh logins", Category = "QuickCast", TimeoutSeconds = 60f)]
    public static Task QuickbarSnapshotBindings(EngineTestContext ctx) => VerifySavedBindings(ctx, characterExport: false);

    private static async Task VerifySavedBindings(EngineTestContext ctx, bool characterExport)
    {
        using var original = await PlayerAbilityFixture.CreateAsync(ctx);
        using var firstLogin = await PlayerAbilityFixture.CreateAsync(ctx, 3f);
        using var secondLogin = await PlayerAbilityFixture.CreateAsync(ctx, 6f);
        using var legacy = await PlayerAbilityFixture.CreateAsync(ctx, 9f);
        var fixtures = new[] { original, firstLogin, secondLogin, legacy };
        var command = $"/castself {(int)FeatType.Benevolence1}";
        try
        {
            foreach (var fixture in fixtures)
            {
                CreaturePlugin.AddFeat(fixture.Creature, FeatType.Benevolence1);
                CreaturePlugin.AddFeat(fixture.Creature, FeatType.Benevolence2);
                QuickCast.ResetSession(fixture.Creature);
            }
            var source = original;
            foreach (var receiver in new[] { firstLogin, secondLogin })
            {
                QuickCast.SetEnabled(source.Creature, QuickCastTargetType.Self, true);
                foreach (var slot in new[] { 1, 2, 36 })
                {
                    PlayerPlugin.SetQuickBarSlot(source.Creature, slot - 1, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                    ctx.Assert(QuickCast.TryBind(source.Creature, slot, QuickCastTargetType.Self, "Benevolence I", out _),
                        $"save-cycle binding {slot} succeeds");
                }
                PlayerPlugin.SetQuickBarSlot(source.Creature, 1, PlayerQuickBarSlot.UseSkill(0));
                PlayerPlugin.SetQuickBarSlot(source.Creature, 2, PlayerQuickBarSlot.Command("Manual", command));
                await ctx.WaitFrameAsync();
                ReloadSavedQuickbar(ctx, source.Creature, receiver.Creature, characterExport, () =>
                {
                    ctx.Assert(QuickCast.IsBoundCommand(PlayerPlugin.GetQuickBarSlot(source.Creature, 0), command),
                        "export restores the live first-slot command");
                    ctx.Assert(QuickCast.IsBoundCommand(PlayerPlugin.GetQuickBarSlot(source.Creature, 35), command),
                        "export restores the live last-slot command");
                    ctx.Assert(QuickCast.TryUnbind(source.Creature, 1, out _), "unbind after the saved snapshot succeeds");
                    ctx.Assert(QuickCast.TryBind(source.Creature, 36, QuickCastTargetType.Self, "Benevolence II", out _),
                        "rebind after the saved snapshot succeeds");
                    QuickCast.ResetSession(source.Creature);
                });
                ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(receiver.Creature, 0).ObjectType,
                    "the pre-unbind snapshot contains no temporary first-slot command");
                ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(receiver.Creature, 35).ObjectType,
                    "the pre-rebind snapshot contains no temporary last-slot command");
                ctx.Assert(!QuickCast.IsEnabled(receiver.Creature, QuickCastTargetType.Self),
                    "a fresh player identity has no process-local opt-in");
                QuickCast.ResetSession(receiver.Creature);
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(receiver.Creature, 0).ObjectType,
                    "a fresh login leaves the saved first slot empty");
                ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(receiver.Creature, 35).ObjectType,
                    "a fresh login leaves the saved last slot empty");
                ctx.AssertEqual(QuickBarSlotType.Skill, PlayerPlugin.GetQuickBarSlot(receiver.Creature, 1).ObjectType,
                    "login cleanup preserves a later manual edit");
                ctx.Assert(QuickCast.IsBoundCommand(PlayerPlugin.GetQuickBarSlot(receiver.Creature, 2), command),
                    "login cleanup preserves the identical command in an unowned slot");
                source = receiver;
            }

            PlayerPlugin.SetQuickBarSlot(legacy.Creature, 0, PlayerQuickBarSlot.UseFeat(FeatType.Benevolence1));
            PlayerPlugin.SetQuickBarSlot(legacy.Creature, 1, PlayerQuickBarSlot.Command("Manual", command));
            ReloadSavedQuickbar(ctx, legacy.Creature, secondLogin.Creature, characterExport);
            QuickCast.ResetSession(secondLogin.Creature);
            ctx.AssertEqual(QuickBarSlotType.Feat, PlayerPlugin.GetQuickBarSlot(secondLogin.Creature, 0).ObjectType,
                "an ordinary legacy feat shortcut is preserved");
            ctx.Assert(QuickCast.IsBoundCommand(PlayerPlugin.GetQuickBarSlot(secondLogin.Creature, 1), command),
                "an unowned legacy command is preserved");
        }
        finally
        {
            foreach (var fixture in fixtures)
                QuickCast.ResetSession(fixture.Creature);
        }
        ctx.SetResultDetail($"Two {(characterExport ? "native BIC" : "Redis quickbar snapshot")} save/reload cycles with fresh player identities. Saved snapshots exclude temporary bindings even after subsequent unbind/rebind/reset; live commands are restored after saving, and manual edits and unowned commands survive.");
    }

    private static void ReloadSavedQuickbar(
        EngineTestContext ctx, uint source, uint receiver, bool characterExport, Action afterExport = null)
    {
        if (!characterExport)
        {
            var playerId = GetObjectUUID(source);
            var player = DB.Get<Player>(playerId);
            player.SerializedHotBar = CreaturePlugin.SerializeQuickbar(source);
            DB.Set(player);
            afterExport?.Invoke();
            var saved = JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(playerId));
            ctx.Assert(CreaturePlugin.DeserializeQuickbar(receiver, saved.SerializedHotBar),
                "load the independently persisted quickbar snapshot through the normal space API");
            return;
        }
        using var file = new CResGFF();
        using var root = new CResStruct();
        using var fileType = new CExoString("BIC ");
        using var format = new CExoString("V2.0");
        ctx.Assert(file.CreateGFFFile(root, fileType, format) != 0, "create a native character export");
        var server = NWNXLib.g_pAppManager.m_pServerExoApp;
        ctx.Assert(server.GetGameObject(source).AsNWSCreature().SaveCreature(file, root, 0, 0, 1, 0) != 0,
            "save the quickbar through the native character export path");
        afterExport?.Invoke();
        server.GetGameObject(receiver).AsNWSCreature().LoadQuickButtons(file, root);
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
        ctx.SetResultDetail("Native PC known-feat guard, separate off-by-default modes, actual Innervate self-heal, FP cost and recast checked. Client hotkey/cursor behavior requires connected-client verification.");
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
        ctx.SetResultDetail("Native PC current attack target, default-off/missing-target denial, real Force Drain impact, FP cost and disable guard checked. Client hotkey/cursor behavior requires connected-client verification.");
    }

    [EngineTest("Quick-cast native quickbar bindings require empty slots and preserve later edits", Category = "QuickCast", TimeoutSeconds = 30f)]
    public static async Task QuickbarRoundTrip(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        CreaturePlugin.AddFeat(player, FeatType.Benevolence1);
        QuickCast.ResetSession(player);
        try
        {
            QuickCast.SetEnabled(player, QuickCastTargetType.Self, true);
            var original = PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty);
            PlayerPlugin.SetQuickBarSlot(player, 0, PlayerQuickBarSlot.UseSkill(0));
            PlayerPlugin.SetQuickBarSlot(player, 35, original);
            ctx.Assert(!QuickCast.TryBind(player, 0, QuickCastTargetType.Self, "Benevolence I", out _), "slot zero is rejected");
            ctx.Assert(!QuickCast.TryBind(player, 37, QuickCastTargetType.Self, "Benevolence I", out _), "slot 37 is rejected");
            ctx.Assert(!QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out var occupiedError) && occupiedError.Contains("empty"), "occupied shortcuts are rejected with empty-slot guidance");
            ctx.AssertEqual(QuickBarSlotType.Skill, PlayerPlugin.GetQuickBarSlot(player, 0).ObjectType, "rejecting a binding preserves the occupied shortcut");
            PlayerPlugin.SetQuickBarSlot(player, 0, original);
            ctx.Assert(QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out _), "explicit first-slot binding succeeds");
            ctx.Assert(QuickCast.IsBoundCommand(PlayerPlugin.GetQuickBarSlot(player, 0), $"/castself {(int)FeatType.Benevolence1}"), "native quickbar contains a cursor-free chat command");
            ctx.Assert(QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out _), "repeated binding retains the original slot");
            ctx.Assert(QuickCast.TryUnbind(player, 1, out _), "unbind succeeds");
            ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(player, 0).ObjectType, "unbind restores the originally empty slot");
            ctx.Assert(QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out _), "a later explicit rebind succeeds");
            PlayerPlugin.SetQuickBarSlot(player, 0, PlayerQuickBarSlot.UseFeat(FeatType.Benevolence1));
            ctx.Assert(!QuickCast.TryBind(player, 1, QuickCastTargetType.Self, "Benevolence I", out _), "rebinding cannot overwrite a later manual edit");
            ctx.Assert(QuickCast.TryBind(player, 36, QuickCastTargetType.Self, "Benevolence I", out _), "last slot binds without off-by-one errors");
            QuickCast.ResetSession(player);
            ctx.AssertEqual(QuickBarSlotType.Feat, PlayerPlugin.GetQuickBarSlot(player, 0).ObjectType, "session reset preserves a player's subsequent manual edit");
            ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(player, 35).ObjectType, "session reset clears an unchanged bound slot");
            ctx.Assert(!QuickCast.IsEnabled(player, QuickCastTargetType.Self), "session reset disables the mode");
        }
        finally { QuickCast.ResetSession(player); }
        ctx.SetResultDetail("Native quickbar command slots 1 and 36, invalid bounds, occupied-slot rejection, rebinding, empty-slot restoration, later manual edits and session reset verified. Headless fixture does not click the client's hotbar.");
    }
}
