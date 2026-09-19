using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using DurationType = SWLOR.NWN.API.NWScript.Enum.DurationType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using AbilityType = SWLOR.NWN.API.NWScript.Enum.AbilityType;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DualWieldEngineTests
    {
        private static uint _observedAttacker = OBJECT_INVALID;
        private static readonly List<(uint Weapon, CombatDamageType Type, DateTime Time)> _hits = new();

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void ObserveWeaponDamage()
        {
            if (OBJECT_SELF != _observedAttacker)
                return;
            _hits.Add((StringToObject(EventsPlugin.GetEventData("WEAPON")),
                (CombatDamageType)int.Parse(EventsPlugin.GetEventData("DAMAGE_TYPE")), DateTime.UtcNow));
        }

        [EngineTest("Dual wield weapon accuracy stays on its own hand and counts once", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task WeaponAccuracyDoesNotLeak(EngineTestContext ctx)
        {
            var attacker = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            var main = await ctx.EquipItemAsync(attacker, "nw_wswss001", InventorySlot.RightHand);
            var off = await ctx.EquipItemAsync(attacker, "nw_wswss001", InventorySlot.LeftHand);
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
                var mainWeapon = native.m_pInventory.GetItemInSlot((uint)EquipmentSlot.RightHand);
                var offWeapon = native.m_pInventory.GetItemInSlot((uint)EquipmentSlot.LeftHand);
                var mainBaseline = Stat.GetAccuracyNative(native, mainWeapon);
                var offBaseline = Stat.GetAccuracyNative(native, offWeapon);
                var mainScriptBaseline = Stat.GetAccuracy(attacker, main, AbilityType.Invalid, SkillType.Vibroknife);
                var offScriptBaseline = Stat.GetAccuracy(attacker, off, AbilityType.Invalid, SkillType.Vibroknife);

                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.AccuracyBonus, -1, 5), main);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.AccuracyBonus, -1, 10), off);
                ctx.AssertEqual(mainBaseline + 5, Stat.GetAccuracyNative(native, mainWeapon), "Main-hand ACC counts once, excluding off-hand ACC");
                ctx.AssertEqual(offBaseline + 10, Stat.GetAccuracyNative(native, offWeapon), "Off-hand ACC counts once, excluding main-hand ACC");
                ctx.AssertEqual(mainScriptBaseline + 5, Stat.GetAccuracy(attacker, main, AbilityType.Invalid, SkillType.Vibroknife), "Character-sheet main-hand accuracy agrees");
                ctx.AssertEqual(offScriptBaseline + 10, Stat.GetAccuracy(attacker, off, AbilityType.Invalid, SkillType.Vibroknife), "Character-sheet off-hand accuracy agrees");

                ApplyEffectToObject(DurationType.Temporary, EffectAccuracyIncrease(2), attacker, 30f);
                ctx.AssertEqual(mainBaseline + 15, Stat.GetAccuracyNative(native, mainWeapon), "Ordinary attack buffs still affect main-hand accuracy");
                ctx.AssertEqual(offBaseline + 20, Stat.GetAccuracyNative(native, offWeapon), "Ordinary attack buffs still affect off-hand accuracy");
            });
        }

        [EngineTest("Dual wield native batches retain both weapons through six rolls", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task NativeBatchesUseBothWeapons(EngineTestContext ctx)
        {
            var (attacker, target, main, off) = await CreatePair(ctx);
            try
            {
                Combat.SetAutoAttackHitResolutionOverride(true);
                _observedAttacker = attacker;
                foreach (var cycles in new[] { 1, 3 })
                {
                    _hits.Clear();
                    await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                    {
                        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
                        var round = native.m_pcCombatRound;
                        round.StartCombatRound(target);
                        var count = WeaponAttackCycle.PrepareDualWieldAttacks(round, cycles * 2);
                        StatusEffect.BeginNativeAttackSwing(attacker);
                        try { native.ResolveAttack(target, count, Combat.BaseAttackDelayMilliseconds); }
                        finally { StatusEffect.EndNativeAttackSwing(attacker); }
                    });
                    ctx.AssertEqual(cycles * 2, _hits.Count, "Every scheduled hand deals its own damage");
                    ctx.AssertEqual(cycles, _hits.Count(hit => hit.Weapon == main && hit.Type == CombatDamageType.Ice), "Cold main-hand rolls");
                    ctx.AssertEqual(cycles, _hits.Count(hit => hit.Weapon == off && hit.Type == CombatDamageType.Poison), "Poison off-hand rolls");
                    await ctx.DelaySecondsAsync(2f);
                }
            }
            finally { ResetObservation(); }
        }

        [EngineTest("Dual wield commanded attacks resolve both hands on one shared cadence", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task CommandedAttacksUseBothHands(EngineTestContext ctx)
        {
            var (attacker, target, main, off) = await CreatePair(ctx);
            try
            {
                Combat.SetAutoAttackHitResolutionOverride(true);
                _hits.Clear();
                _observedAttacker = attacker;
                AssignCommand(attacker, () => ActionAttack(target));
                await ctx.WaitUntilAsync(() => _hits.Count >= 6, 30f, "three real dual-wield attack cycles");
                for (var cycle = 0; cycle < 3; cycle++)
                {
                    var first = _hits[cycle * 2];
                    var second = _hits[cycle * 2 + 1];
                    ctx.AssertEqual(main, first.Weapon, "Each cycle starts with the main hand");
                    ctx.AssertEqual(off, second.Weapon, "Each cycle includes the off hand");
                    ctx.AssertEqual(CombatDamageType.Ice, first.Type, "Main-hand damage retains its type");
                    ctx.AssertEqual(CombatDamageType.Poison, second.Type, "Off-hand damage retains its type");
                    ctx.Assert((second.Time - first.Time).TotalMilliseconds < 500, "Both hands share one delay gate");
                    if (cycle > 0)
                        ctx.Assert((first.Time - _hits[(cycle - 1) * 2].Time).TotalMilliseconds >= Combat.BaseAttackDelayMilliseconds,
                            "Paired attacks respect the animation floor between cycles");
                }
            }
            finally
            {
                AssignCommand(attacker, () => ClearAllActions());
                ResetObservation();
            }
        }

        [EngineTest("Dual wield misses consume one limited attack charge per hand", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task MissesConsumeChargesForBothHands(EngineTestContext ctx)
        {
            var (attacker, target, _, _) = await CreatePair(ctx);
            try
            {
                _hits.Clear();
                _observedAttacker = attacker;
                Combat.SetAutoAttackHitResolutionOverride(false);
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    var effect = new LimitedHasteStatusEffect(30, 4, SkillType.Vibroknife, EffectIconType.Haste, null);
                    ctx.Assert(StatusEffect.ApplyStatusEffect(attacker, attacker, effect, 60f), "Limited Haste applies");
                    var active = (LimitedHasteStatusEffect)StatusEffect.GetStatusEffect(attacker, typeof(LimitedHasteStatusEffect));
                    ResolveCycle(attacker, target);
                    ctx.AssertEqual(2, active.RemainingAttacks, "Both missed weapon attempts consume a charge");
                    ctx.AssertEqual(0, _hits.Count, "Missed hands do not publish damaging hits");
                });
            }
            finally { ResetObservation(); }
        }

        [EngineTest("Dual wield queued ability replaces one hand while the other still attacks", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task QueuedAbilityLeavesTheOtherHandAvailable(EngineTestContext ctx)
        {
            var (attacker, target, main, off) = await CreatePair(ctx);
            try
            {
                _hits.Clear();
                _observedAttacker = attacker;
                Combat.SetAutoAttackHitResolutionOverride(true);
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    ctx.SetNPCResources(attacker, 100, 100);
                    ctx.SetNPCPerkLevel(attacker, PerkType.PathogenStrike, 1);
                    CreaturePlugin.AddFeat(attacker, FeatType.PathogenStrike1);
                    ctx.Assert(UsePerkFeat.TryUseAbility(attacker, target, FeatType.PathogenStrike1, GetLocation(target)), "Pathogen Strike queues through normal activation");
                    ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(attacker), "Pathogen Strike is waiting for a landed hit");
                    ResolveCycle(attacker, target, 6);
                    ctx.AssertEqual(3, _hits.Count(hit => hit.Weapon == off && hit.Type == CombatDamageType.Poison), "All off-hand rolls still deal ordinary poison hits");
                    ctx.AssertEqual(2, _hits.Count(hit => hit.Weapon == main), "Only the first main-hand auto-attack is replaced");
                });
                await ctx.WaitUntilAsync(() => !UsePerkFeat.HasQueuedWeaponAbility(attacker), 5f, "the queued ability to consume once");
            }
            finally
            {
                AssignCommand(attacker, () => ClearAllActions());
                ResetObservation();
            }
        }

        private static void ResolveCycle(uint attacker, uint target, int attacks = 2)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
            var round = native.m_pcCombatRound;
            round.StartCombatRound(target);
            var count = WeaponAttackCycle.PrepareDualWieldAttacks(round, attacks);
            StatusEffect.BeginNativeAttackSwing(attacker);
            try { native.ResolveAttack(target, count, Combat.BaseAttackDelayMilliseconds); }
            finally { StatusEffect.EndNativeAttackSwing(attacker); }
        }

        [EngineTest("Mixed dual wield uses off-hand haste and consumes only matching charges", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task OffHandTimingEffectsParticipate(EngineTestContext ctx)
        {
            var (attacker, target, _, _) = await CreatePair(ctx, "nw_wswls001");
            try
            {
                Combat.SetAutoAttackHitResolutionOverride(false);
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    var effect = new LimitedHasteStatusEffect(40, 4, SkillType.Vibroblade, EffectIconType.Haste, null);
                    ctx.Assert(StatusEffect.ApplyStatusEffect(attacker, attacker, effect, 60f), "Off-hand haste applies");
                    ctx.AssertEqual(SkillType.Vibroblade, WeaponAttackCycle.SelectTimingSkill(attacker, SkillType.Vibroknife, SkillType.Vibroblade),
                        "The off-hand's haste participates in the shared timer");
                    var active = (LimitedHasteStatusEffect)StatusEffect.GetStatusEffect(attacker, typeof(LimitedHasteStatusEffect));
                    ResolveCycle(attacker, target);
                    ctx.AssertEqual(3, active.RemainingAttacks, "The main-hand knife does not consume the blade's charge");
                });
            }
            finally { ResetObservation(); }
        }

        private static async Task<(uint Attacker, uint Target, uint Main, uint Off)> CreatePair(EngineTestContext ctx, string offResref = "nw_wswss001")
        {
            var attacker = ctx.SpawnCreature("nw_bandit001", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.WaitFrameAsync();
            var main = await ctx.EquipItemAsync(attacker, "nw_wswss001", InventorySlot.RightHand);
            var off = await ctx.EquipItemAsync(attacker, offResref, InventorySlot.LeftHand);
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                ConfigureWeapon(main, CombatDamageType.Ice);
                ConfigureWeapon(off, CombatDamageType.Poison);
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 20000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 90f);
                TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 90f);
                ctx.MakeHostile(target);
            });
            return (attacker, target, main, off);
        }

        private static void ConfigureWeapon(uint weapon, CombatDamageType damageType)
        {
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 20), weapon);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)damageType), weapon);
        }

        private static void ResetObservation()
        {
            Combat.SetAutoAttackHitResolutionOverride(null);
            _observedAttacker = OBJECT_INVALID;
            _hits.Clear();
        }
    }
}
