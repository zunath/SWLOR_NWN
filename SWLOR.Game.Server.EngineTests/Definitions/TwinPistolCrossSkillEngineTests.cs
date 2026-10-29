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
    // No perk grants this stat after the Cadence rework; a temporary modifier keeps its dual-wield gating covered.
    private const string PairedOrdinaryGroup = "PAIRED_ORDINARY_TEST";

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
                    var bonus = PairedRangedTraits.BeginAbility(actor, false, false);
                    PairedRangedTraits.CompleteAbility(actor, bonus, false, 0, 0, 6, 25, 12);
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
                ctx.AssertEqual(6, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                    "The landed queued replacement consumes none of the six ordinary Twin Suns attempts");
                ctx.AssertEqual(SkillType.Rifle, summary.SkillType, "Cross-trained Headshot retains its original skill");
                ctx.Assert(summary.AttributedDamage > 0 && GetCurrentHitPoints(target) < 20000,
                    "The replacement actually damages the placeable");
                amounts.Add(summary.AttributedDamage);
                _queuedActor = OBJECT_INVALID;
                _queuedTarget = OBJECT_INVALID;
                PairedRangedTraits.ClearActor(actor);
                DestroyObject(actor);
                DestroyObject(target);
                await ctx.WaitFrameAsync();
            }
            ctx.AssertEqual(amounts[0], amounts[1], "Akimbo's ordinary-only penalty does not leak into the same fixed-seed queued replacement");
        }
        finally
        {
            if (GetIsObjectValid(_queuedActor)) PairedRangedTraits.ClearActor(_queuedActor);
            _queuedActor = OBJECT_INVALID;
            _queuedTarget = OBJECT_INVALID;
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
        }
    }

    [EngineTest("Twin Pistol native placeable attacks apply captured ordinary bonuses before global stance costs", Category = "TwinPistolCrossSkill", TimeoutSeconds = 45f)]
    public static async Task NativePlaceableOrdinaryModifiers(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;
        player.Update(record =>
        {
            record.Perks.Clear();
            record.Skills[SkillType.Pistol].Rank = 50;
            record.Skills[SkillType.TwinPistol].Rank = 50;
        });
        var weapon = await ctx.EquipItemAsync(actor, "b_pistol", InventorySlot.RightHand);
        var target = OBJECT_INVALID;
        var baseline = Array.Empty<int>();
        Combat.SetAutoAttackHitResolutionOverride(true);
        try
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                target = CreateObject(ObjectType.Placeable, "mando_crate", ctx.GetArenaLocation(2f));
                ctx.Track(target);
                ctx.Assert(GetIsObjectValid(target) && GetObjectType(target) == ObjectType.Placeable,
                    "The ordinary damage target is a real placeable");
                SetPlotFlag(target, false);
                SetHardness(0, target);
                ObjectPlugin.SetMaxHitPoints(target, 20000);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(20000), target);
                var damageProperties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
                for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
                    if (GetItemPropertyType(ip) == ItemPropertyType.DMG) damageProperties.Add(ip);
                foreach (var ip in damageProperties) RemoveItemProperty(weapon, ip);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 80), weapon);
            });
            // Native passive-property removal completes after the setup script returns.
            await ctx.WaitUntilAsync(() =>
            {
                var nativeWeapon = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(weapon).AsNWSItem();
                var damageProperties = new List<int>();
                for (var index = 0; index < nativeWeapon.m_lstPassiveProperties.Count; index++)
                {
                    var ip = nativeWeapon.GetPassiveProperty(index);
                    if (ip?.m_nPropertyName == (ushort)ItemPropertyType.DMG) damageProperties.Add(ip.m_nCostTableValue);
                }
                return damageProperties.Count == 1 && damageProperties[0] == 80 && Item.GetDMG(weapon) == 80;
            }, 3f, "the replacement DMG property to be the only native weapon damage property");
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ctx.AssertEqual(CombatDamageType.Physical, WeaponDamage.GetDamageType(weapon),
                    "The native return value contains all of this fixture's weapon damage");

                baseline = Sample(64, 0, false);
                ctx.Assert(baseline.All(damage => damage > 20), "The seeded baseline stays clear of minimum-damage rounding");
                TemporaryStatModifier.Replace(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment, 10, 120f, PairedOrdinaryGroup);
                ctx.AssertEqual(10, Stat.GetStatAdjustment(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment),
                    "The paired ordinary damage stat contributes ten percent");
                // The paired ordinary damage stat offsets the paired firing cycle, so a single pistol captures none of it.
                AssertDamage(Sample(64, 0, false), 0, 0, false, "Paired ordinary damage on one pistol");

                TemporaryStatModifier.Consume(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment, PairedOrdinaryGroup);
                GrantTwinSuns(12);
                AssertDamage(Sample(8, 0, true), 0, 0, true, "Twin Suns");
                ctx.Assert(StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor) == null,
                    "The sixth ordinary attempt exhausts the Twin Suns status");

                TemporaryStatModifier.Replace(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment, 10, 120f, PairedOrdinaryGroup);
                StatusEffect.ApplyPermanentStatusEffect<AkimboStanceStatusEffect>(actor, actor);
                GrantTwinSuns(12);
                AssertDamage(Sample(8, 0, true), 0, -30, true, "Akimbo with one-pistol handling and Twin Suns");
                StatusEffect.RemoveStatusEffect<AkimboStanceStatusEffect>(actor);
                StatusEffect.ApplyPermanentStatusEffect<CrossfireStanceStatusEffect>(actor, actor);
                GrantTwinSuns(12);
                AssertDamage(Sample(8, 0, true), 0, -20, true, "Crossfire with one-pistol handling and Twin Suns");
                StatusEffect.RemoveStatusEffect<CrossfireStanceStatusEffect>(actor);
                TemporaryStatModifier.Consume(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment, PairedOrdinaryGroup);

                // Use the same production grant/expiry path with a short fixture lifetime.
                GrantTwinSuns(1);
                AssertDamage(Sample(1, 0, true), 0, 0, true, "Twin Suns before expiry");
                ctx.AssertEqual(5, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                    "One roll and its repeated damage query leave five attempts before expiry");
            });
            await ctx.DelaySecondsAsync(1.25f);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                PairedRangedTraits.Reconcile(actor);
                ctx.Assert(StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor) == null,
                    "Time expiry discards the five unspent attempts");
                AssertDamage(Sample(8, 0, false), 0, 0, false, "Twin Suns after expiry");
                ClearAllActions();
            });
            ctx.SetResultDetail("Real damageable placeable; 64 matched-seed native attempts for baseline/handling, six-charge exhaustion, repeated damage queries, both stance costs, and shortened-lifetime expiry. Queued replacement is covered separately.");
        }
        finally
        {
            StatusEffect.RemoveStatusEffect<AkimboStanceStatusEffect>(actor);
            StatusEffect.RemoveStatusEffect<CrossfireStanceStatusEffect>(actor);
            OrdinaryAttackTraits.ClearActor(actor);
            PairedRangedTraits.ClearActor(actor);
            RangedWeaponAttackContext.Clear(actor);
            Combat.SetAutoAttackHitResolutionOverride(null);
            if (GetIsObjectValid(actor)) AssignCommand(actor, () => ClearAllActions());
        }

        void GrantTwinSuns(int seconds)
        {
            var bonus = PairedRangedTraits.BeginAbility(actor, false, false);
            PairedRangedTraits.CompleteAbility(actor, bonus, false, 0, 0, 6, 25, seconds);
            ctx.AssertEqual(6, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                "The production activation completion grants six ordinary attempts");
        }

        int[] Sample(int attempts, int handlingPercent, bool twinSuns)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
            var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
            native.m_pcCombatRound.StartCombatRound(target);
            ctx.SeedRandom(561089);
            var damage = new int[attempts * 2];
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                native.m_pcCombatRound.m_nCurrentAttack = 0;
                native.m_pcCombatRound.GetAttack(0).m_bRangedAttack = 1;
                native.ResolveAttackRoll(defender);
                var roll = RangedWeaponAttackContext.GetRoll(actor, target, 0);
                ctx.Assert(roll is { Ordinary: true, Completed: false } && roll.Weapon == weapon,
                    "The real native resolver captures this ordinary weapon attempt");
                ctx.AssertEqual(handlingPercent + (twinSuns && attempt < 6 ? 25 : 0), roll.Bonuses.DamagePercentAdjustment,
                    "The attempt captures handling and only its own remaining Twin Suns charge");
                var remaining = twinSuns ? Math.Max(0, 5 - attempt) : 0;
                for (var query = 0; query < 2; query++)
                {
                    damage[attempt * 2 + query] = native.m_pStats.GetDamageRoll(defender, 0, 0, 0, 0, 0);
                    ctx.Assert(roll.Completed, "The production native damage resolver completes the ordinary roll");
                    ctx.AssertEqual(damage[attempt * 2 + query], roll.Damage,
                        "The recorded ordinary physical damage equals the native result");
                    ctx.AssertEqual(remaining, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                        "Repeated damage queries cannot consume another attempted-shot charge");
                }
            }
            return damage;
        }

        void AssertDamage(int[] actual, int handlingPercent, int stancePercent, bool twinSuns, string scenario)
        {
            for (var index = 0; index < actual.Length; index++)
            {
                var bonus = handlingPercent + (twinSuns && index / 2 < 6 ? 25 : 0);
                var outgoing = Math.Min(baseline[index] * 2,
                    baseline[index] + (int)Math.Ceiling(baseline[index] * (bonus / 100f)));
                var expected = (int)Math.Floor(outgoing * ((100d + stancePercent) / 100d));
                ctx.AssertEqual(expected, actual[index],
                    $"{scenario} native query {index} applies the captured additive bonus before the global stance cost");
            }
        }
    }

    [EngineTest("Twin Pistol stance damage costs and Haste reach native saber and rifle attacks without paired handling", Category = "TwinPistolCrossSkill", TimeoutSeconds = 180f)]
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
                    TemporaryStatModifier.Replace(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment, 10, 120f, PairedOrdinaryGroup);
                    ctx.AssertEqual(10, Stat.GetStatAdjustment(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment), "The paired ordinary damage stat is present outside pistol equipment");
                    var handling = Sample();
                    // The paired ordinary damage stat needs two weapons; a single saber or rifle captures none of it.
                    ctx.Assert(Math.Abs(handling - baseline) < .01,
                        $"The single {resref} gains no paired ordinary damage bonus ({baseline:0.00}->{handling:0.00})");
                    TemporaryStatModifier.Consume(actor, StatType.PairedRangedOrdinaryDamagePercentAdjustment, PairedOrdinaryGroup);
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
        var loadouts = new (string Main, string Off, int Impacts, bool Lethal)[]
        {
            ("b_pistol", null, 1, false), ("b_pistol", "ec_shield", 1, false),
            ("b_longsword", null, 1, false), ("b_longsword", "b_longsword", 2, false),
            ("b_staff", null, 1, false), ("b_rifle", null, 1, false),
            ("saber_train_1", null, 1, false), ("saber_train_1", "saber_train_1", 2, false),
            ("trn_saberstaff_1", null, 1, false), ("b_twinpistol", null, 2, false),
            ("b_twinpistol", null, 2, true),
        };
        Combat.SetAbilityHitResolutionOverride(true);
        Combat.SetAutoAttackHitResolutionOverride(false);
        var presentationActor = OBJECT_INVALID;
        try
        {
            foreach (var (mainResref, offResref, impacts, killingShot) in loadouts)
            {
                var actor = ctx.SpawnCreature("civilian");
                presentationActor = actor;
                var target = ctx.SpawnCreature("nw_rat001", 2f);
                await ctx.EquipItemAsync(actor, mainResref, InventorySlot.RightHand);
                if (offResref != null) await ctx.EquipItemAsync(actor, offResref, InventorySlot.LeftHand);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    ctx.SetNPCResources(actor, 100, 100);
                    ctx.SetNPCPerkLevel(actor, PerkType.PairedVolley, 1);
                    CreaturePlugin.AddFeat(actor, FeatType.PairedVolley1);
                    ctx.SuppressNPCNaturalRegen(target);
                    Stat.SetNPCMaxHitPoints(target, killingShot ? 1 : 20000, true);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 120f);
                    ctx.MakeHostile(target);
                    _observedTarget = target;
                    _damageEvents = 0;
                    Ability.ClearLastCompletedAbilityImpactSummary(actor);
                    if (killingShot)
                    {
                        PairedRangedPresentation.Release(actor);
                        PairedRangedPresentation.Prepare(actor);
                    }
                    ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.PairedVolley1, GetLocation(target)),
                        $"Paired Volley accepts {mainResref}/{offResref}: {Ability.GetLastActivationDenialReason()}");
                });
                await ctx.WaitUntilAsync(() => (killingShot ? GetIsDead(target) : _damageEvents >= impacts) &&
                    Ability.GetLastCompletedAbilityImpactSummary(actor)?.AttributedDamage > 0,
                    8f, $"the captured {mainResref}/{offResref} weapon impacts (lethal={killingShot})");
                await ctx.DelaySecondsAsync(.4f);
                if (!killingShot)
                    ctx.AssertEqual(impacts, _damageEvents, $"{mainResref}/{offResref} contributes exactly one impact per equipped attack weapon");
                var firingSources = 0;
                for (var source = GetFirstObjectInArea(ctx.Arena, ObjectType.Placeable); GetIsObjectValid(source);
                     source = GetNextObjectInArea(ctx.Arena, ObjectType.Placeable))
                    if (GetTag(source) == PairedRangedPresentation.MuzzleTag &&
                        GetResRef(source) == PairedRangedPresentation.MuzzleBlueprint &&
                        GetPosition(source).Z - GetPosition(actor).Z > 1f)
                        firingSources++;
                ctx.AssertEqual(mainResref == "b_twinpistol" ? 2 : 0, firingSources,
                    "The real ability shows each fired pistol including a killing shot, while cross-skilled weapons create no paired muzzle effects");
                if (killingShot)
                {
                    ctx.Assert(GetIsDead(target), "The lethal ability still presents both fired barrels after its damage flush");
                    for (var nth = 1; ; nth++)
                    {
                        var corpse = GetNearestObject(ObjectType.Placeable, target, nth);
                        if (!GetIsObjectValid(corpse) || GetDistanceBetween(target, corpse) > 10f) break;
                        if (GetLocalObject(corpse, Loot.CorpseBodyVariable) != target) continue;
                        ctx.Track(corpse);
                        break;
                    }
                }
                ctx.Log($"Ability presentation {mainResref}/{offResref}: lethal={killingShot}, damageCallbacks={_damageEvents}, " +
                    $"attributedDamage={Ability.GetLastCompletedAbilityImpactSummary(actor).AttributedDamage}, raisedSources={firingSources}");
                ctx.AssertEqual(SkillType.TwinPistol, Ability.GetLastCompletedAbilityImpactSummary(actor).SkillType,
                    "Cross-trained equipment does not redirect the purchased ability's skill");
                await ctx.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
                _observedTarget = OBJECT_INVALID;
                PairedRangedPresentation.Release(actor);
                DestroyObject(actor);
                DestroyObject(target);
                presentationActor = OBJECT_INVALID;
                await ctx.WaitFrameAsync();
            }
        }
        finally
        {
            PairedRangedPresentation.Release(presentationActor);
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
