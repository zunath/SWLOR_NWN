using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// Runs against native PC creatures and disposable Redis. Window actions are invoked
/// on the server view model; this does not simulate client clicks or client rendering.
/// </summary>
public static class MimicryLoadoutEngineTests
{
    private const FeatType Rush = FeatType.SnapRushTechnique;
    private const FeatType Wave = FeatType.DreadWaveTechnique;
    private const FeatType Trait = FeatType.IronCarapaceTechnique;

    [EngineTest("Legacy equipped techniques migrate to Default and survive Redis reload", Category = "MimicryLoadouts", TimeoutSeconds = 30f)]
    public static async Task DefaultMigration(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        Prepare(fixture);
        // Records saved before saved builds existed deserialize without either field.
        var legacy = ReadPersisted(fixture.Id);
        legacy.MimicryLoadouts = null;
        legacy.ActiveMimicryLoadoutId = null;
        legacy.EquippedTechniques = new List<FeatType> { Rush, Trait };
        DB.Set(legacy);
        CreaturePlugin.AddFeat(player, Rush);
        PlayerPlugin.SetQuickBarSlot(player, 17, PlayerQuickBarSlot.UseFeat(Rush));

        var migration = new _27_UpdatePlayerRecords();
        migration.Migrate();
        var record = ReadPersisted(fixture.Id);
        var migrated = record.MimicryLoadouts.Single();
        ctx.AssertEqual("Default", migrated.Name, "the legacy build receives the standard name");
        ctx.AssertEqual(migrated.Id, record.ActiveMimicryLoadoutId, "the migrated build is active");
        AssertSet(ctx, new[] { Rush, Trait }, migrated.Techniques, "migration preserves equipped techniques");
        migration.Migrate();
        record = ReadPersisted(fixture.Id);
        ctx.AssertEqual(1, record.MimicryLoadouts.Count, "migration retry does not duplicate Default");
        ctx.AssertEqual(migrated.Id, record.MimicryLoadouts[0].Id, "migration retry keeps the original Default");

        new _21_CaptureMimicryDefaultHotbar().Migrate(player);
        ctx.Assert(ReadPersisted(fixture.Id).MimicryLoadouts[0].HotbarSlots.TryGetValue(17, out var captured) && captured == Rush,
            "first login records the migrated build's quickbar positions");

        // A Combat Analyzer refund clears technique shortcuts before any build switch.
        Mimicry.UnequipAllTechniques(player);
        ctx.Assert(!IsFeat(player, 17, Rush), "refund removes the technique shortcut");
        record = ReadPersisted(fixture.Id);
        ctx.AssertEqual(0, record.EquippedTechniques.Count, "refund unequips every technique");
        AssertSet(ctx, new[] { Rush, Trait }, record.MimicryLoadouts[0].Techniques, "refund does not rewrite the saved Default");
        MimicryLoadouts.Save(record, null, "Other", new[] { Wave, Trait }, out var other, out _);
        DB.Set(record);

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            Apply(ctx, player, other.Id, Wave, Trait);
            record = ReadPersisted(fixture.Id);
            AssertSet(ctx, new[] { Wave, Trait }, record.EquippedTechniques, $"cycle {cycle}: Other is persisted as equipped");
            ctx.AssertEqual(other.Id, record.ActiveMimicryLoadoutId, $"cycle {cycle}: Other is persisted as active");
            ctx.Assert(GetHasFeat(Wave, player) && !GetHasFeat(Rush, player), $"cycle {cycle}: native feats follow Other");

            Apply(ctx, player, migrated.Id, Rush, Trait);
            record = ReadPersisted(fixture.Id);
            AssertSet(ctx, new[] { Rush, Trait }, record.EquippedTechniques, $"cycle {cycle}: Default is persisted as equipped");
            ctx.AssertEqual(migrated.Id, record.ActiveMimicryLoadoutId, $"cycle {cycle}: Default is persisted as active");
            ctx.AssertEqual(2, record.MimicryLoadouts.Count, $"cycle {cycle}: no builds are added or lost");
            ctx.Assert(GetHasFeat(Rush, player) && !GetHasFeat(Wave, player), $"cycle {cycle}: native feats follow Default");
            AssertSlot(ctx, player, 17, Rush);
        }
        ctx.SetResultDetail("Legacy record with no saved builds; Default name, active ID, retry, first-login quickbar capture, refund before switching, and two apply/save/Redis-reload cycles verified.");
    }

    [EngineTest("Save, apply, unsaved apply and reopened technique window preserve their separate state", Category = "MimicryLoadouts", TimeoutSeconds = 30f)]
    public static async Task SaveApplyReopen(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        Prepare(fixture);
        var template = Gui.GetWindowTemplate(GuiWindowType.Techniques);
        ctx.AssertEqual(0, template.LayoutFindings.Count, "the production Techniques window has no layout warnings");
        var model = new TechniquesViewModel();
        Bind(model, fixture.Creature);
        AddToPreview(model, Rush);
        model.OnClickSaveAs()();
        model.ModalInputText = "Field kit";
        model.OnInputModalConfirmClick()();
        await ctx.WaitFrameAsync();
        var saved = DB.Get<Player>(fixture.Id).MimicryLoadouts.Single(x => x.Name == "Field kit");
        AssertSet(ctx, new[] { Rush }, saved.Techniques, "Save As persists its preview");
        ctx.AssertEqual(0, DB.Get<Player>(fixture.Id).EquippedTechniques.Count, "saving does not equip");
        ctx.Assert(!GetHasFeat(Rush, fixture.Creature), "saving does not grant the native feat");
        model.OnClickApply()();
        ctx.Assert(GetHasFeat(Rush, fixture.Creature), "Apply grants the native technique feat");
        AddToPreview(model, Trait);
        model.OnClickApply()();
        await ctx.WaitFrameAsync();
        var record = DB.Get<Player>(fixture.Id);
        AssertSet(ctx, new[] { Rush, Trait }, record.EquippedTechniques, "unsaved Apply persists the actual equipped set");
        AssertSet(ctx, new[] { Rush }, record.MimicryLoadouts.Single(x => x.Id == saved.Id).Techniques, "Apply does not overwrite the saved preset");
        model.OnWindowClosed()();
        Bind(model, fixture.Creature);
        AssertSet(ctx, new[] { Rush, Trait }, Preview(model), "cached window reopen shows the applied build");
        ctx.Assert(model.ActiveLoadoutText.Contains("Modified"), "the saved preset and equipped build are distinguished");
        var fresh = new TechniquesViewModel();
        Bind(fresh, fixture.Creature);
        AssertSet(ctx, new[] { Rush, Trait }, Preview(fresh), "a fresh window after reconnect or restart shows the actual equipped build");
        fresh.OnClickSave()();
        await ctx.WaitFrameAsync();
        AssertSet(ctx, new[] { Rush, Trait }, DB.Get<Player>(fixture.Id).MimicryLoadouts.Single(x => x.Id == saved.Id).Techniques, "Save updates the selected preset after reopening");
        ctx.SetResultDetail("Production view-model Save As/Save/Apply handlers, Redis persistence, native feat and cached/fresh reopen checked. Client input/rendering excluded.");
    }

    [EngineTest("Manage renames or deletes the selected build in the toolbar, and Cancel leaves it unchanged", Category = "MimicryLoadouts", TimeoutSeconds = 30f)]
    public static async Task ManageBuild(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var (first, _) = Prepare(fixture);
        Apply(ctx, fixture.Creature, first, Rush, Trait);
        var model = new TechniquesViewModel();
        Bind(model, fixture.Creature);
        AssertBar(ctx, model, TechniquesViewModel.BuildBarPartial, "the window opens on the build bar");

        model.OnClickManage()();
        AssertBar(ctx, model, TechniquesViewModel.ManageBarPartial, "Manage swaps in the manage bar");
        ctx.AssertEqual("First", model.BuildNameText, "the name field starts with the selected build's name");
        model.BuildNameText = "Discarded";
        model.OnClickCancelManage()();
        AssertBar(ctx, model, TechniquesViewModel.BuildBarPartial, "Cancel returns to the build bar");
        await ctx.WaitFrameAsync();
        ctx.AssertEqual("First", ReadPersisted(first, fixture.Id).Name, "Cancel keeps the saved name");

        model.OnClickManage()();
        model.BuildNameText = "second";
        model.OnClickRenameBuild()();
        AssertBar(ctx, model, TechniquesViewModel.ManageBarPartial, "a rejected name keeps the manage bar open");
        ctx.Assert(model.ManageText.Contains("already uses"), "the manage bar explains a duplicate name");
        model.BuildNameText = "  ";
        model.OnClickRenameBuild()();
        ctx.AssertEqual($"Use 1-{MimicryLoadouts.MaxNameLength} characters, no line breaks.", model.ManageText, "the manage bar explains an empty name");
        model.BuildNameText = "Tank kit";
        model.OnClickRenameBuild()();
        await ctx.WaitFrameAsync();
        AssertBar(ctx, model, TechniquesViewModel.BuildBarPartial, "Rename returns to the build bar");
        ctx.AssertEqual("Tank kit", ReadPersisted(first, fixture.Id).Name, "Rename persists the new name");
        ctx.Assert(model.LoadoutOptions.Any(x => x.Label == "Tank kit"), "the dropdown lists the new name");

        model.OnClickManage()();
        model.OnClickDeleteBuild()();
        AssertBar(ctx, model, TechniquesViewModel.DeleteBarPartial, "Delete asks for confirmation in the toolbar");
        ctx.Assert(!model.DeleteBuildText.Contains("Unsaved"), "a clean preview needs no unsaved-changes warning");
        RemoveFromPreview(model, Trait);
        ctx.Assert(model.DeleteBuildText.Contains("Unsaved changes are lost."), "editing the preview during the confirmation adds the warning");
        model.OnClickCancelManage()();
        AssertBar(ctx, model, TechniquesViewModel.BuildBarPartial, "Cancel closes the delete confirmation");
        await ctx.WaitFrameAsync();
        ctx.Assert(ReadPersisted(fixture.Id).MimicryLoadouts.Any(x => x.Id == first), "Cancel keeps the build");

        model.OnClickManage()();
        model.OnClickDeleteBuild()();
        model.OnClickConfirmDeleteBuild()();
        await ctx.WaitFrameAsync();
        AssertBar(ctx, model, TechniquesViewModel.BuildBarPartial, "a confirmed delete returns to the build bar");
        var record = ReadPersisted(fixture.Id);
        ctx.Assert(record.MimicryLoadouts.All(x => x.Id != first), "Delete removes the build");
        AssertSet(ctx, new[] { Rush, Trait }, record.EquippedTechniques, "Delete keeps the equipped techniques");

        model.OnClickManage()();
        model.OnClickDeleteBuild()();
        model.OnClickConfirmDeleteBuild()();
        model.OnClickManage()();
        ctx.Assert(!model.CanDeleteBuild, "the last saved build cannot be deleted");
        model.OnClickCancelManage()();
        AssertBar(ctx, model, TechniquesViewModel.BuildBarPartial, "Cancel closes the manage bar");
        ctx.SetResultDetail("Production view-model Manage handlers: in-window bar swaps, Cancel from both steps, rejected and accepted rename, confirmed delete, last-build guard, and Redis persistence. Client rendering excluded.");
    }

    [EngineTest("Revert re-equips the saved build when applied changes modified it, and otherwise resets only the preview", Category = "MimicryLoadouts", TimeoutSeconds = 30f)]
    public static async Task RevertModifiedBuild(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var (first, _) = Prepare(fixture);
        var player = fixture.Creature;
        Apply(ctx, player, first, Rush, Trait);
        var model = new TechniquesViewModel();
        Bind(model, player);

        RemoveFromPreview(model, Trait);
        ctx.Assert(model.CanRevert, "an unsaved preview can be reverted");
        model.OnClickRevert()();
        ctx.AssertEqual("Revert the preview to the saved build?", model.ModalPromptText, "an unmodified equipped build only resets the preview");
        model.OnModalConfirmClick()();
        AssertSet(ctx, new[] { Rush, Trait }, Preview(model), "Revert restores the saved preview");

        // Snap Rush is active (a native feat); traits are never granted as feats.
        RemoveFromPreview(model, Rush);
        model.OnClickApply()();
        ctx.Assert(!GetHasFeat(Rush, player), "applying the change revokes the removed technique");
        AddToPreview(model, Rush);
        ctx.Assert(model.ActiveLoadoutText.EndsWith("(Modified)"), "applying an unsaved change marks the equipped build as modified");
        ctx.Assert(model.CanRevert, "Revert stays available while the equipped build is modified, even when the preview matches the save");
        model.OnClickRevert()();
        ctx.Assert(model.ModalPromptText.Contains("re-equips"), "the confirmation says the saved build is re-equipped");
        model.OnModalConfirmClick()();
        await ctx.WaitFrameAsync();
        var record = ReadPersisted(fixture.Id);
        AssertSet(ctx, new[] { Rush, Trait }, record.EquippedTechniques, "Revert re-equips the saved build");
        ctx.AssertEqual(first, record.ActiveMimicryLoadoutId, "Revert keeps the build active");
        ctx.Assert(GetHasFeat(Rush, player), "Revert grants the restored native feat");
        ctx.AssertEqual("Equipped: First", model.ActiveLoadoutText, "Revert clears the modified flag");
        ctx.Assert(!model.CanRevert, "nothing is left to revert");
        ctx.SetResultDetail("Production view-model Revert: preview-only revert, then applied-unsaved change reverted with re-equip, Redis state, native feat, (Modified) flag and Revert availability.");
    }

    [EngineTest("Technique build changes preserve native quickbar layouts and unrelated shortcuts", Category = "MimicryLoadouts", TimeoutSeconds = 30f)]
    public static async Task HotbarRoundTrip(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var (first, second) = Prepare(fixture);
        var player = fixture.Creature;
        Apply(ctx, player, first, Rush, Trait);
        for (var slot = 0; slot < 36; slot++)
            PlayerPlugin.SetQuickBarSlot(player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
        PlayerPlugin.SetQuickBarSlot(player, 0, PlayerQuickBarSlot.UseFeat(Rush));
        PlayerPlugin.SetQuickBarSlot(player, 17, PlayerQuickBarSlot.UseFeat(Rush));
        var unrelated = PlayerQuickBarSlot.UseSkill(0);
        PlayerPlugin.SetQuickBarSlot(player, 5, unrelated);
        Apply(ctx, player, second, Wave, Trait);
        ctx.Assert(!GetHasFeat(Rush, player) && GetHasFeat(Wave, player), "switching replaces active native feats");
        ctx.AssertEqual(QuickBarSlotType.Empty, PlayerPlugin.GetQuickBarSlot(player, 17).ObjectType, "outgoing shortcuts are removed");
        Apply(ctx, player, first, Rush, Trait);
        AssertSlot(ctx, player, 0, Rush);
        AssertSlot(ctx, player, 17, Rush);
        ctx.AssertEqual(QuickBarSlotType.Skill, PlayerPlugin.GetQuickBarSlot(player, 5).ObjectType, "unrelated shortcut survives round trip");
        ctx.Assert(!Enumerable.Range(0, 36).Any(slot => IsFeat(player, slot, Trait)), "passive traits never create quickbar shortcuts");
        Apply(ctx, player, first, Wave, Trait); // Applied without saving the preset.
        Apply(ctx, player, second, Wave, Trait);
        await ctx.WaitFrameAsync();
        ctx.Assert(DB.Get<Player>(fixture.Id).MimicryLoadouts.Single(x => x.Id == first).HotbarSlots.ContainsKey(17), "a modified active build cannot erase the original saved layout");
        PlayerPlugin.SetQuickBarSlot(player, 17, unrelated);
        Apply(ctx, player, first, Rush, Trait);
        ctx.AssertEqual(QuickBarSlotType.Skill, PlayerPlugin.GetQuickBarSlot(player, 17).ObjectType, "a conflicting unrelated shortcut has priority");
        ctx.Assert(Enumerable.Range(0, 12).Any(slot => IsFeat(player, slot, Rush)), "the conflicting technique still receives an available fallback slot");
        ctx.SetResultDetail("Native NWNX quickbar: duplicate slots, outgoing cleanup, restoration, traits, modified presets and occupied-cell fallback verified.");
    }

    [EngineTest("Combat blocks Apply atomically while Save remains available", Category = "MimicryLoadouts", TimeoutSeconds = 45f)]
    public static async Task CombatRestrictions(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var (first, second) = Prepare(fixture);
        var player = fixture.Creature;
        Apply(ctx, player, first, Rush, Trait);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), player, 120f);
        var enemy = ctx.SpawnCreature("nw_bandit001", 1f);
        await ctx.DelaySecondsAsync(1f);
        ctx.MakeHostile(enemy);
        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(5000), enemy, 120f);
        AssignCommand(player, () => ActionAttack(enemy));
        AssignCommand(enemy, () => ActionAttack(player));
        await ctx.WaitUntilAsync(() => GetIsInCombat(player), 15f, "the native player to enter combat");
        var before = JsonConvert.SerializeObject(DB.Get<Player>(fixture.Id));
        var slots = Enumerable.Range(0, 36).Select(slot => PlayerPlugin.GetQuickBarSlot(player, slot)).ToArray();
        ctx.Assert(!Mimicry.ApplyLoadout(player, second, new[] { Wave, Trait }, out var error) && error.Contains("combat"), "native combat denies Apply");
        ctx.AssertEqual(before, JsonConvert.SerializeObject(DB.Get<Player>(fixture.Id)), "combat denial leaves the persisted record unchanged");
        ctx.Assert(GetHasFeat(Rush, player) && !GetHasFeat(Wave, player), "combat denial leaves native feats unchanged");
        for (var slot = 0; slot < 36; slot++)
            ctx.AssertEqual(JsonConvert.SerializeObject(slots[slot]), JsonConvert.SerializeObject(PlayerPlugin.GetQuickBarSlot(player, slot)), $"combat denial preserves quickbar slot {slot}");
        var model = new TechniquesViewModel();
        Bind(model, player);
        AddToPreview(model, Wave);
        model.OnClickSave()();
        await ctx.WaitFrameAsync();
        ctx.Assert(DB.Get<Player>(fixture.Id).MimicryLoadouts.Single(x => x.Id == first).Techniques.Contains(Wave), "Save works during combat");
        AssertSet(ctx, new[] { Rush, Trait }, DB.Get<Player>(fixture.Id).EquippedTechniques, "combat Save does not equip the preview");
        AssignCommand(player, () => ClearAllActions());
        AssignCommand(enemy, () => ClearAllActions());
        DestroyObject(enemy);
        await ctx.WaitUntilAsync(() => !GetIsInCombat(player), 15f, "the native player to leave combat");
        Apply(ctx, player, second, Wave, Trait);
        ctx.SetResultDetail("Real hostile combat: Apply denial is atomic across DB/feats/36 quickbar cells; Save succeeds; Apply succeeds after leaving combat.");
    }

    [EngineTest("Actual technique cooldown survives switching away and back and expires normally", Category = "MimicryLoadouts", TimeoutSeconds = 60f)]
    public static async Task CooldownRoundTrip(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var (first, second) = Prepare(fixture);
        var player = fixture.Creature;
        Apply(ctx, player, first, Rush, Trait);
        var activated = false;
        await ctx.ExecuteInCreatureContextAsync(player, () => activated = UsePerkFeat.TryUseAbility(player, player, Rush, GetLocation(player)));
        ctx.Assert(activated, $"the equipped technique activates: {Ability.GetLastActivationDenialReason()}");
        await ctx.WaitUntilAsync(() => DB.Get<Player>(fixture.Id).RecastTimes.ContainsKey(RecastGroup.SnapRush), 5f, "the actual technique to start its cooldown");
        var expires = DB.Get<Player>(fixture.Id).RecastTimes[RecastGroup.SnapRush];
        ctx.Assert(expires > DateTime.UtcNow, "activation creates a future cooldown");
        await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect<Hasten1StatusEffect>(player), 5f, "Snap Rush to apply its effect");
        Apply(ctx, player, first, Rush, Trait);
        ctx.Assert(StatusEffect.HasStatusEffect<Hasten1StatusEffect>(player), "reapplying a shared technique preserves its active effect");
        Apply(ctx, player, second, Wave, Trait);
        ctx.Assert(!StatusEffect.HasStatusEffect<Hasten1StatusEffect>(player), "removing a technique revokes its source-owned active effect");
        ctx.Assert(!Ability.CanUseAbility(player, player, Rush, 1, GetLocation(player)) && Ability.GetLastActivationDenialReason().Contains("not equipped"), "removed techniques cannot be used through a stale shortcut");
        Apply(ctx, player, first, Rush, Trait);
        await ctx.WaitFrameAsync();
        ctx.AssertEqual(expires, DB.Get<Player>(fixture.Id).RecastTimes[RecastGroup.SnapRush], "switching does not reset or extend the cooldown");
        ctx.Assert(!Ability.CanUseAbility(player, player, Rush, 1, GetLocation(player)) && Ability.GetLastActivationDenialReason().StartsWith("This ability can be used in "), "re-equipped techniques remain blocked by their real cooldown");
        await ctx.WaitUntilAsync(() => DateTime.UtcNow > expires, 35f, "the original cooldown to expire");
        ctx.Assert(Ability.CanUseAbility(player, player, Rush, 1, GetLocation(player)), $"normal use becomes available after expiry: {Ability.GetLastActivationDenialReason()}");
        ctx.SetResultDetail("Snap Rush activated through TryUseAbility; original cooldown timestamp and activation denials verified across switches through natural expiry.");
    }

    [EngineTest("Invalid builds fail before changing equipped state", Category = "MimicryLoadouts", TimeoutSeconds = 30f)]
    public static async Task InvalidApplyIsAtomic(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var (first, second) = Prepare(fixture);
        Apply(ctx, fixture.Creature, first, Rush, Trait);
        foreach (var invalid in new[] { new[] { Rush, Rush }, new[] { (FeatType)int.MaxValue } })
            ctx.Assert(!Mimicry.ApplyLoadout(fixture.Creature, second, invalid, out _), "duplicate or unavailable techniques are denied");
        fixture.Update(record => record.LearnedTechniques.Remove(Wave));
        ctx.Assert(!Mimicry.ApplyLoadout(fixture.Creature, second, new[] { Wave }, out _), "unlearned techniques are denied");
        fixture.Update(record => { record.LearnedTechniques[Wave] = DateTime.UtcNow; record.Skills[SkillType.Mimicry].Rank = 0; });
        ctx.Assert(!Mimicry.ApplyLoadout(fixture.Creature, second, new[] { Wave }, out _), "insufficient rank is denied");
        fixture.Update(record => { record.Skills[SkillType.Mimicry].Rank = 50; record.Perks.Clear(); });
        ctx.Assert(!Mimicry.ApplyLoadout(fixture.Creature, second, new[] { Wave }, out _), "insufficient slot budget is denied");
        await ctx.WaitFrameAsync();
        AssertSet(ctx, new[] { Rush, Trait }, DB.Get<Player>(fixture.Id).EquippedTechniques, "all invalid applies preserve the equipped set");
        ctx.AssertEqual(first, DB.Get<Player>(fixture.Id).ActiveMimicryLoadoutId, "all invalid applies preserve the active ID");
        ctx.Assert(GetHasFeat(Rush, fixture.Creature) && !GetHasFeat(Wave, fixture.Creature), "all invalid applies preserve native feats");
    }

    private static (string First, string Second) Prepare(PlayerAbilityFixture fixture)
    {
        var record = DB.Get<Player>(fixture.Id);
        record.Perks[PerkType.AnalyzerMemory] = 2;
        foreach (var feat in new[] { Rush, Wave, Trait }) record.LearnedTechniques[feat] = DateTime.UtcNow;
        MimicryLoadouts.EnsureStartingBuild(record);
        MimicryLoadouts.Save(record, null, "First", new[] { Rush, Trait }, out var first, out _);
        MimicryLoadouts.Save(record, null, "Second", new[] { Wave, Trait }, out var second, out _);
        DB.Set(record);
        return (first.Id, second.Id);
    }

    // Reads Redis directly; DB.Get returns the cached instance that DB.Set stored.
    private static Player ReadPersisted(string id)
        => JsonConvert.DeserializeObject<Player>(DB.GetRawJson<Player>(id));

    private static MimicryLoadout ReadPersisted(string loadoutId, string playerId)
        => ReadPersisted(playerId).MimicryLoadouts.Single(x => x.Id == loadoutId);

    // Manage must swap only the build bar; the window root stays on the main view instead of a modal.
    private static void AssertBar(EngineTestContext ctx, TechniquesViewModel model, string expected, string message)
    {
        var baseType = typeof(GuiViewModelBase<TechniquesViewModel, TechniquesPayload>);
        var root = (string)baseType.GetField("_rootPartial", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);
        var groups = (Dictionary<string, string>)baseType.GetField("_groupLayouts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);
        var layouts = Gui.GetWindowTemplate(GuiWindowType.Techniques).PartialViewLayouts;
        ctx.AssertEqual("%%WINDOW_MAIN%%", root, $"{message}: the main view stays on screen");
        ctx.AssertEqual(layouts[expected], groups[TechniquesViewModel.BuildBarElement], message);
    }

    private static void Apply(EngineTestContext ctx, uint player, string id, params FeatType[] feats)
        => ctx.Assert(Mimicry.ApplyLoadout(player, id, feats, out var error), $"apply succeeds: {error}");

    private static void Bind(TechniquesViewModel model, uint player)
    {
        model.Geometry ??= Gui.GetWindowTemplate(GuiWindowType.Techniques).InitialGeometry;
        model.Bind(player, 0, model.Geometry, GuiWindowType.Techniques, null, OBJECT_INVALID);
    }

    private static List<FeatType> Preview(TechniquesViewModel model)
        => (List<FeatType>)typeof(TechniquesViewModel).GetField("_draft", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(model);

    private static void AddToPreview(TechniquesViewModel model, FeatType feat)
    {
        var field = typeof(TechniquesViewModel).GetField("_unequippedFeats", BindingFlags.Instance | BindingFlags.NonPublic);
        var index = ((List<FeatType>)field.GetValue(model)).IndexOf(feat);
        if (index < 0) throw new InvalidOperationException($"Technique {feat} is absent from the learned list.");
        typeof(TechniquesViewModel).GetField("_selectedUnequippedIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, index);
        model.OnClickEquip()();
    }

    private static void RemoveFromPreview(TechniquesViewModel model, FeatType feat)
    {
        var field = typeof(TechniquesViewModel).GetField("_equippedFeats", BindingFlags.Instance | BindingFlags.NonPublic);
        var index = ((List<FeatType>)field.GetValue(model)).IndexOf(feat);
        if (index < 0) throw new InvalidOperationException($"Technique {feat} is absent from the preview.");
        typeof(TechniquesViewModel).GetField("_selectedEquippedIndex", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(model, index);
        model.OnClickUnequip()();
    }

    private static bool IsFeat(uint player, int slot, FeatType feat)
    {
        var entry = PlayerPlugin.GetQuickBarSlot(player, slot);
        return entry.ObjectType == QuickBarSlotType.Feat && entry.INTParam1 == (int)feat;
    }

    private static void AssertSlot(EngineTestContext ctx, uint player, int slot, FeatType feat)
        => ctx.Assert(IsFeat(player, slot, feat), $"native quickbar slot {slot} restores {feat}");

    private static void AssertSet(EngineTestContext ctx, IEnumerable<FeatType> expected, IEnumerable<FeatType> actual, string message)
        => ctx.Assert(expected.ToHashSet().SetEquals(actual), message);
}
