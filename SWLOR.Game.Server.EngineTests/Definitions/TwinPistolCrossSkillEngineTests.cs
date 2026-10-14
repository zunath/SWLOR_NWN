using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class TwinPistolCrossSkillEngineTests
{
    private static uint _observedTarget = OBJECT_INVALID;
    private static int _damageEvents;
    private static uint _queuedActor = OBJECT_INVALID;
    private static uint _queuedTarget = OBJECT_INVALID;
    private static int _itemHitCallbacks;
    private static int _ordinaryDamageEvents;

    [NWNEventHandler(ScriptName.OnItemHit)]
    public static void ObserveQueuedCallback()
    {
        if (OBJECT_SELF == _queuedActor && GetSpellTargetObject() == _queuedTarget) _itemHitCallbacks++;
    }

    [NWNEventHandler(ScriptName.OnSWLORDamage)]
    public static void ObserveQueuedOrdinaryLeak()
    {
        if (OBJECT_SELF == _queuedActor) _ordinaryDamageEvents++;
    }

    [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
    public static void ObserveActivatedImpacts()
    {
        if (OBJECT_SELF == _observedTarget && GetTotalDamageDealt() > 0) _damageEvents++;
    }

    [EngineTest("Twin Pistol queued cross-training replaces a damageable placeable hit once without ordinary stance cost", Category = "TwinPistolCrossSkill", TimeoutSeconds = 45f)]
    public static async Task QueuedPlaceableReplacement(EngineTestContext ctx)
    {
        var amounts = new List<int>();
        Combat.SetAutoAttackHitResolutionOverride(true);
        Combat.SetAbilityHitResolutionOverride(true);
        try
        {
            foreach (var stance in new[] { false, true })
            {
                var actor = ctx.SpawnCreature("civilian");
                await ctx.EquipItemAsync(actor, "b_pistol", InventorySlot.RightHand);
                var target = OBJECT_INVALID;
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    target = CreateObject(ObjectType.Placeable, "mando_crate", ctx.GetArenaLocation(2f));
                    ctx.Track(target);
                    ctx.Assert(GetIsObjectValid(target), "A real damageable placeable is available");
                    SetPlotFlag(target, false);
                    ObjectPlugin.SetMaxHitPoints(target, 20000);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(20000), target);
                    ctx.SetNPCResources(actor, 100, 100);
                    ctx.SetNPCPerkLevel(actor, PerkType.Headshot, 1);
                    CreaturePlugin.AddFeat(actor, FeatType.Headshot1);
                    if (stance) StatusEffect.ApplyPermanentStatusEffect<AkimboStanceStatusEffect>(actor, actor);
                    ctx.SeedRandom(94561);
                    _queuedActor = actor;
                    _queuedTarget = target;
                    _itemHitCallbacks = 0;
                    _ordinaryDamageEvents = 0;
                    Ability.ClearLastCompletedAbilityImpactSummary(actor);
                    ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.Headshot1, GetLocation(target)),
                        $"The existing queued weapon ability accepts its damageable target: {Ability.GetLastActivationDenialReason()}");
                    ActionAttack(target);
                });
                await ctx.WaitUntilAsync(() => !UsePerkFeat.HasQueuedWeaponAbility(actor) &&
                    Ability.GetLastCompletedAbilityImpactSummary(actor) != null, 8f, "the native item-hit callback to resolve the queued replacement");
                await ctx.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
                PairedRangedAttackCycle.Cancel(actor);
                await ctx.DelaySecondsAsync(.3f);
                var summary = Ability.GetLastCompletedAbilityImpactSummary(actor);
                ctx.AssertEqual(1, _itemHitCallbacks, "One launched native projectile dispatches its queued item-hit callback once");
                ctx.AssertEqual(0, _ordinaryDamageEvents, "The replaced native roll deals no additional ordinary damage to the placeable");
                ctx.AssertEqual(SkillType.Rifle, summary.SkillType, "Cross-trained Headshot retains its original skill");
                ctx.Assert(summary.AttributedDamage > 0 && GetCurrentHitPoints(target) < 20000,
                    "The replacement actually damages the placeable");
                amounts.Add(summary.AttributedDamage);
                _queuedActor = OBJECT_INVALID;
                _queuedTarget = OBJECT_INVALID;
                DestroyObject(actor);
                DestroyObject(target);
                await ctx.WaitFrameAsync();
            }
            ctx.AssertEqual(amounts[0], amounts[1], "Akimbo's ordinary-only penalty does not leak into the same fixed-seed queued replacement");
        }
        finally
        {
            _queuedActor = OBJECT_INVALID;
            _queuedTarget = OBJECT_INVALID;
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
        }
    }

    [EngineTest("Twin Pistol ordinary bonuses stance damage costs and Haste reach native saber and rifle attacks", Category = "TwinPistolCrossSkill", TimeoutSeconds = 180f)]
    public static async Task MeasuredNativeOrdinaryModifiers(EngineTestContext ctx)
    {
        Combat.SetAutoAttackHitResolutionOverride(true);
        try
        {
            foreach (var resref in new[] { "saber_train_1", "b_rifle" })
            {
                using var player = await PlayerAbilityFixture.CreateAsync(ctx);
                var actor = player.Creature;
                player.Update(record =>
                {
                    record.Skills[SkillType.TwinPistol].Rank = 50;
                    record.Skills[SkillType.Lightsaber].Rank = 50;
                    record.Skills[SkillType.Rifle].Rank = 50;
                });
                var weapon = await ctx.EquipItemAsync(actor, resref, InventorySlot.RightHand);
                var target = ctx.SpawnCreature("nw_rat001", .8f);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
                    for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
                        if (GetItemPropertyType(ip) == ItemPropertyType.DMG) properties.Add(ip);
                    foreach (var ip in properties) RemoveItemProperty(weapon, ip);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 80), weapon);
                    ctx.SuppressNPCNaturalRegen(target);
                    Stat.SetNPCMaxHitPoints(target, 20000, true);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 180f);
                    TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 180f, "CROSS_SKILL_NATIVE");
                    ctx.MakeHostile(target);
                    double Sample()
                    {
                        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                        var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
                        native.m_pcCombatRound.StartCombatRound(target);
                        ctx.SeedRandom(561032);
                        return Enumerable.Range(0, 256).Average(_ =>
                        {
                            native.ResolveAttackRoll(defender);
                            return native.m_pStats.GetDamageRoll(defender, 0, 0, 0, 0, 0);
                        });
                    }
                    var baseline = Sample();
                    player.Update(record => record.Perks[PerkType.PairedHandling] = 3);
                    ctx.AssertEqual(10, Stat.GetStatAdjustment(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment), "The purchased handling trait is present outside pistol equipment");
                    var handling = Sample();
                    ctx.Assert(handling / baseline is > 1.07 and < 1.13,
                        $"{resref} native ordinary attempts capture the universal +10% bonus ({baseline:0.00}->{handling:0.00})");
                    player.Update(record => record.Perks[PerkType.PairedHandling] = 0);
                    StatusEffect.ApplyPermanentStatusEffect<AkimboStanceStatusEffect>(actor, actor);
                    var akimbo = Sample();
                    ctx.Assert(akimbo / baseline is > .67 and < .71,
                        $"The same {resref} pays Akimbo's native ordinary damage cost ({baseline:0.00}->{akimbo:0.00})");
                    StatusEffect.RemoveStatusEffect<AkimboStanceStatusEffect>(actor);
                    StatusEffect.ApplyPermanentStatusEffect<CrossfireStanceStatusEffect>(actor, actor);
                    var crossfire = Sample();
                    ctx.Assert(crossfire / baseline is > .77 and < .81,
                        $"The same {resref} pays Crossfire's native ordinary damage cost ({baseline:0.00}->{crossfire:0.00})");
                    StatusEffect.RemoveStatusEffect<CrossfireStanceStatusEffect>(actor);
                    ClearAllActions();
                    ctx.Log($"{resref}:256 matched-seed native rolls each, baseline={baseline:0.00}, handling={handling:0.00}, Akimbo={akimbo:0.00}, Crossfire={crossfire:0.00}");
                });
                await ctx.DelaySecondsAsync(.75f);
                async Task<int> CountActualAttacks()
                {
                    _observedTarget = target;
                    _damageEvents = 0;
                    AssignCommand(actor, () => ActionAttack(target));
                    await ctx.WaitUntilAsync(() => _damageEvents > 0, 8f, "the first actual native ordinary hit");
                    _damageEvents = 0;
                    await ctx.DelaySecondsAsync(24f);
                    var count = _damageEvents;
                    await ctx.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
                    _observedTarget = OBJECT_INVALID;
                    await ctx.DelaySecondsAsync(.75f);
                    return count;
                }
                var ordinaryCount = await CountActualAttacks();
                await ctx.ExecuteInCreatureContextAsync(actor, () => StatusEffect.ApplyPermanentStatusEffect<AkimboStanceStatusEffect>(actor, actor));
                var hastedCount = await CountActualAttacks();
                ctx.Assert(hastedCount > ordinaryCount, $"The unchanged {resref} actually attacks faster with global Haste ({ordinaryCount}->{hastedCount} in24s)");
                ctx.Log($"{resref}:actual native hit counts in matched24s windows baseline={ordinaryCount}, Akimbo={hastedCount}");
                StatusEffect.RemoveStatusEffect<AkimboStanceStatusEffect>(actor);
                OrdinaryAttackTraits.ClearActor(actor);
                PairedRangedTraits.ClearActor(actor);
                DestroyObject(target);
            }
        }
        finally { _observedTarget = OBJECT_INVALID; Combat.SetAutoAttackHitResolutionOverride(null); }
    }

    [EngineTest("Twin Pistol cross-skill Converging refresh retains its status past the original native expiry", Category = "TwinPistolCrossSkill", TimeoutSeconds = 40f)]
    public static async Task RefreshedOrdinaryStatusKeepsNativeLifetime(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        player.Update(record => { record.Skills[SkillType.Lightsaber].Rank = 50; record.Perks[PerkType.ConvergingFire] = 1; });
        await ctx.EquipItemAsync(actor, "saber_train_1", InventorySlot.RightHand);
        var target = ctx.SpawnCreature("nw_rat001", .8f);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 20000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 40f);
            ctx.MakeHostile(target);
        });
        Combat.SetAutoAttackHitResolutionOverride(true);
        try
        {
            AssignCommand(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => StatusEffect.GetStatusEffect<ConvergingFireStatusEffect>(actor) != null, 18f,
                "three actual ordinary saber hits to grant Converging Fire");
            var identity = StatusEffect.GetStatusEffect<ConvergingFireStatusEffect>(actor).Id;
            await ctx.DelaySecondsAsync(6.7f);
            ctx.AssertEqual(identity, StatusEffect.GetStatusEffect<ConvergingFireStatusEffect>(actor)?.Id,
                "Same-count refresh renews the native icon lifetime without replacing logical trait state");
            ctx.AssertEqual(5, PairedRangedTraits.GetOrdinaryDamagePercentAdjustment(actor, target), "The refreshed same-target ordinary bonus stays active");
        }
        finally
        {
            AssignCommand(actor, () => ClearAllActions());
            PairedRangedTraits.ClearActor(actor);
            Combat.SetAutoAttackHitResolutionOverride(null);
        }
    }

    [EngineTest("Twin Pistol cross-trained attacks capture one impact per weapon and none from shields", Category = "TwinPistolCrossSkill", TimeoutSeconds = 120f)]
    public static async Task CrossTrainedWeaponCounts(EngineTestContext ctx)
    {
        var loadouts = new (string Main, string Off, int Impacts)[]
        {
            ("b_pistol", null, 1), ("b_pistol", "ec_shield", 1),
            ("b_longsword", null, 1), ("b_longsword", "b_longsword", 2),
            ("b_staff", null, 1), ("b_rifle", null, 1),
            ("saber_train_1", null, 1), ("saber_train_1", "saber_train_1", 2),
            ("trn_saberstaff_1", null, 1), ("b_twinpistol", null, 2),
        };
        Combat.SetAbilityHitResolutionOverride(true);
        Combat.SetAutoAttackHitResolutionOverride(false);
        try
        {
            foreach (var (mainResref, offResref, impacts) in loadouts)
            {
                var actor = ctx.SpawnCreature("civilian");
                var target = ctx.SpawnCreature("nw_rat001", 2f);
                await ctx.EquipItemAsync(actor, mainResref, InventorySlot.RightHand);
                if (offResref != null) await ctx.EquipItemAsync(actor, offResref, InventorySlot.LeftHand);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    ctx.SetNPCResources(actor, 100, 100);
                    ctx.SetNPCPerkLevel(actor, PerkType.PairedVolley, 1);
                    CreaturePlugin.AddFeat(actor, FeatType.PairedVolley1);
                    ctx.SuppressNPCNaturalRegen(target);
                    Stat.SetNPCMaxHitPoints(target, 20000, true);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 120f);
                    ctx.MakeHostile(target);
                    _observedTarget = target;
                    _damageEvents = 0;
                    Ability.ClearLastCompletedAbilityImpactSummary(actor);
                    ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.PairedVolley1, GetLocation(target)),
                        $"Paired Volley accepts {mainResref}/{offResref}: {Ability.GetLastActivationDenialReason()}");
                });
                await ctx.WaitUntilAsync(() => _damageEvents >= impacts &&
                    Ability.GetLastCompletedAbilityImpactSummary(actor)?.AttributedDamage > 0,
                    8f, $"the captured {mainResref}/{offResref} weapon impacts");
                await ctx.DelaySecondsAsync(.2f);
                ctx.AssertEqual(impacts, _damageEvents, $"{mainResref}/{offResref} contributes exactly one impact per equipped attack weapon");
                ctx.AssertEqual(SkillType.TwinPistol, Ability.GetLastCompletedAbilityImpactSummary(actor).SkillType,
                    "Cross-trained equipment does not redirect the purchased ability's skill");
                await ctx.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
                _observedTarget = OBJECT_INVALID;
                DestroyObject(actor);
                DestroyObject(target);
                await ctx.WaitFrameAsync();
            }
        }
        finally
        {
            _observedTarget = OBJECT_INVALID;
            Combat.SetAbilityHitResolutionOverride(null);
            Combat.SetAutoAttackHitResolutionOverride(null);
        }
    }

    [EngineTest("Twin Pistol stances retain their global damage cost and earned rhythm across saber rifle and pistol swaps", Category = "TwinPistolCrossSkill", TimeoutSeconds = 90f)]
    public static async Task StancesAndRhythmSurviveWeaponSwaps(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Skills[SkillType.TwinPistol].Rank = 50;
            record.Skills[SkillType.Lightsaber].Rank = 50;
            record.Skills[SkillType.Rifle].Rank = 50;
            record.Perks[PerkType.AkimboStance] = 1;
            record.Perks[PerkType.CrossfireStance] = 1;
        });
        var main = await ctx.EquipItemAsync(actor, "b_twinpistol", InventorySlot.RightHand);
        var companion = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
        var target = ctx.SpawnCreature("nw_rat001", 2f);
        var saber = OBJECT_INVALID;
        var rifle = OBJECT_INVALID;
        var hasteBaseline = Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent);
        var movementBaseline = Stat.GetStatAdjustment(actor, StatType.MovementSpeedPercentAdjustment);
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            saber = CreateItemOnObject("saber_train_1", actor);
            rifle = CreateItemOnObject("b_rifle", actor);
            CreaturePlugin.AddFeat(actor, FeatType.AkimboStance);
            CreaturePlugin.AddFeat(actor, FeatType.CrossfireStance);
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 20000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 90f);
            ctx.MakeHostile(target);
            ctx.Assert(UsePerkFeat.TryUseAbility(actor, actor, FeatType.AkimboStance, GetLocation(actor)),
                $"Akimbo activates through the normal stance pipeline: {Ability.GetLastActivationDenialReason()}");
        });
        Combat.SetAutoAttackHitResolutionOverride(true);
        try
        {
            await ctx.WaitUntilAsync(() => StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) != null,
                5f, "Akimbo stance to apply");
            AssignCommand(actor, () => ActionAttack(target));
            await ctx.WaitUntilAsync(() => (StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor)?.Stacks ?? 0) >= 3,
                24f, "three rhythm stacks earned from actual ordinary hostile hits");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                var ramp = StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor);
                var stacks = ramp.Stacks;
                var totalHaste = Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent);
                CreaturePlugin.RunUnequip(actor, companion);
                CreaturePlugin.RunUnequip(actor, main);
                CreaturePlugin.RunEquip(actor, saber, InventorySlot.RightHand);
                ctx.AssertEqual(BaseItem.Lightsaber, GetBaseItemType(GetItemInSlot(InventorySlot.RightHand, actor)), "The replacement is an actual lightsaber");
                ctx.Assert(StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) != null, "A legal weapon swap retains Akimbo");
                ctx.AssertEqual(stacks, StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor)?.Stacks ?? 0, "Swapping preserves earned rhythm stacks");
                ctx.AssertEqual(totalHaste, Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent), "Haste remains global with the saber");
                ctx.AssertEqual(70, Combat.ApplyOrdinaryAttackDamageMultiplier(actor, 100), "The saber inherits Akimbo's full ordinary damage cost");
                OrdinaryAttackTraits.RecordHit(actor, target, long.MaxValue - 1);
                ctx.AssertEqual(stacks, StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor)?.Stacks ?? 0,
                    "A weapon swap does not reset the two-second rhythm grant cooldown");
                CreaturePlugin.RunUnequip(actor, saber);
                CreaturePlugin.RunEquip(actor, rifle, InventorySlot.RightHand);
                ctx.AssertEqual(70, Combat.ApplyOrdinaryAttackDamageMultiplier(actor, 100), "The rifle inherits the same ordinary damage cost");
                ctx.Assert(UsePerkFeat.TryUseAbility(actor, actor, FeatType.CrossfireStance, GetLocation(actor)),
                    $"Crossfire can activate with a rifle: {Ability.GetLastActivationDenialReason()}");
            });
            await ctx.WaitUntilAsync(() => StatusEffect.GetStatusEffect<CrossfireStanceStatusEffect>(actor) != null,
                5f, "Crossfire to replace Akimbo");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.Assert(StatusEffect.GetStatusEffect<AkimboStanceStatusEffect>(actor) == null &&
                    StatusEffect.GetStatusEffect<OrdinaryAttackHasteStatusEffect>(actor) == null, "Stance exclusivity removes Akimbo and its ramp");
                ctx.AssertEqual(hasteBaseline + 10, Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent), "Crossfire has only its own global Haste");
                ctx.AssertEqual(movementBaseline + 10, Stat.GetStatAdjustment(actor, StatType.MovementSpeedPercentAdjustment), "Crossfire grants global movement speed");
                ctx.AssertEqual(80, Combat.ApplyOrdinaryAttackDamageMultiplier(actor, 100), "Crossfire pays its global ordinary damage cost");
                CreaturePlugin.RunUnequip(actor, rifle);
                ctx.Assert(!GetIsObjectValid(companion), "The earlier generated gun was destroyed with the pair");
                CreaturePlugin.RunEquip(actor, main, InventorySlot.RightHand);
                ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "The original owned pair can be restored");
                ctx.Assert(StatusEffect.GetStatusEffect<CrossfireStanceStatusEffect>(actor) != null, "Restoring the pair preserves the active cross-trained stance");
                ctx.AssertEqual(80, Combat.ApplyOrdinaryAttackDamageMultiplier(actor, 100), "Restoring the pair cannot shed the stance penalty");
                ctx.Assert(!UsePerkFeat.TryUseAbility(actor, actor, FeatType.AkimboStance, GetLocation(actor)), "Equipment changes cannot reset Akimbo's original recast");
                ClearAllActions();
            });
        }
        finally
        {
            Combat.SetAutoAttackHitResolutionOverride(null);
            OrdinaryAttackTraits.ClearActor(actor);
            PairedRangedTraits.ClearActor(actor);
        }
    }
}
