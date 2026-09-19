using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using Skill = SWLOR.Game.Server.Service.Skill;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DualWieldEngineTests
    {
        private static uint _observedAttacker = OBJECT_INVALID;
        private static readonly List<(uint Weapon, CombatDamageType Type, DateTime Time)> _hits = new();
        private static readonly List<uint> _itemHits = new();

        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void ObserveItemHit()
        {
            if (OBJECT_SELF == _observedAttacker)
                _itemHits.Add(GetSpellCastItem());
        }

        [EngineTest("Dual wield mapped melee weapons retain both hands and isolated accuracy", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task MappedWeaponsParticipate(EngineTestContext ctx)
        {
            var (attacker, target, main, off) = await CreatePair(ctx);
            try
            {
                Combat.SetAutoAttackHitResolutionOverride(true);
                _observedAttacker = attacker;
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    foreach (var type in new[] { BaseItem.Kama, BaseItem.Club, BaseItem.LightFlail,
                                 BaseItem.LightHammer, BaseItem.MorningStar, BaseItem.WarHammer })
                    {
                        ItemPlugin.SetBaseItemType(main, type);
                        ctx.Assert(EquipmentPredicates.HasDualWield(attacker), $"Equipped {type} participates in dual wield");
                        AssertAccuracyCountsOnce(ctx, attacker, main, EquipmentSlot.RightHand);
                    }
                });
                _hits.Clear();
                AssignCommand(attacker, () => ActionAttack(target));
                await ctx.WaitUntilAsync(() => _hits.Count >= 2, 10f, "a paired attack with the mapped warhammer");
                ctx.AssertEqual(main, _hits[0].Weapon, "Mapped main hand attacks");
                ctx.AssertEqual(off, _hits[1].Weapon, "Mapped main hand does not hide the off hand");
            }
            finally
            {
                AssignCommand(attacker, () => ClearAllActions());
                ResetObservation();
            }
        }

        [EngineTest("Dual wield accuracy filtering includes creature attack weapons", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task NaturalWeaponAccuracyCountsOnce(EngineTestContext ctx)
        {
            var attacker = ctx.SpawnCreature("kath_hound");
            await ctx.WaitFrameAsync();
            var weapon = GetItemInSlot(InventorySlot.CreatureRight, attacker);
            ctx.Assert(GetIsObjectValid(weapon), "Creature blueprint provides an equipped natural weapon");
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
                    if (GetItemPropertyType(ip) == ItemPropertyType.AccuracyBonus)
                        RemoveItemProperty(weapon, ip);
                ctx.AssertEqual(weapon, GetItemInSlot(InventorySlot.CreatureRight, attacker), "Natural weapon is equipped");
                foreach (var type in Item.CreatureBaseItemTypes)
                {
                    ItemPlugin.SetBaseItemType(weapon, type);
                    AssertAccuracyCountsOnce(ctx, attacker, weapon, EquipmentSlot.CreatureWeaponRight);
                }
            });
        }

        private static void AssertAccuracyCountsOnce(EngineTestContext ctx, uint attacker, uint weapon, EquipmentSlot slot)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
            var nativeWeapon = native.m_pInventory.GetItemInSlot((uint)slot);
            var baseline = Stat.GetAccuracyNative(native, nativeWeapon);
            var scriptBaseline = Stat.GetAccuracy(attacker, weapon, AbilityType.Invalid,
                Skill.GetSkillTypeByBaseItem(GetBaseItemType(weapon)));
            var accuracy = ItemPropertyCustom(ItemPropertyType.AccuracyBonus, -1, 5);
            AddItemProperty(DurationType.Permanent, accuracy, weapon);
            ctx.AssertEqual(baseline + 5, Stat.GetAccuracyNative(native, nativeWeapon), $"{GetBaseItemType(weapon)} native ACC counts once");
            ctx.AssertEqual(scriptBaseline + 5, Stat.GetAccuracy(attacker, weapon, AbilityType.Invalid,
                Skill.GetSkillTypeByBaseItem(GetBaseItemType(weapon))), $"{GetBaseItemType(weapon)} script ACC counts once");
            RemoveItemProperty(weapon, accuracy);
        }

        [EngineTest("Dual wield queued placeable batch spends each matching attack charge", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task QueuedPlaceableBatchConsumesCharges(EngineTestContext ctx)
        {
            var (attacker, _, _, _) = await CreatePair(ctx);
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                var target = CreateObject(ObjectType.Placeable, "mando_crate", ctx.GetArenaLocation(1f));
                ctx.Track(target);
                ctx.Assert(GetIsObjectValid(target), "Damageable placeable fixture exists");
                SetPlotFlag(target, false);
                ObjectPlugin.SetMaxHitPoints(target, 20000);
                ApplyEffectToObject(DurationType.Instant, EffectHeal(20000), target);
                ctx.SetNPCResources(attacker, 100, 100);
                ctx.SetNPCPerkLevel(attacker, PerkType.PathogenStrike, 1);
                CreaturePlugin.AddFeat(attacker, FeatType.PathogenStrike1);
                ctx.Assert(UsePerkFeat.TryUseAbility(attacker, target, FeatType.PathogenStrike1, GetLocation(target)), "Placeable ability queues");
                StatusEffect.ApplyStatusEffect(attacker, attacker,
                    new LimitedHasteStatusEffect(30, 10, SkillType.Vibroknife, EffectIconType.Haste, null), 60f);
                var active = (LimitedHasteStatusEffect)StatusEffect.GetStatusEffect(attacker, typeof(LimitedHasteStatusEffect));
                ResolveCycle(attacker, target, 6);
                ctx.AssertEqual(5, active.RemainingAttacks, "Five ordinary rolls spend charges before the queued impact callback");
            });
            await ctx.WaitUntilAsync(() => !UsePerkFeat.HasQueuedWeaponAbility(attacker), 5f, "the queued placeable hit to consume");
            var remaining = (LimitedHasteStatusEffect)StatusEffect.GetStatusEffect(attacker, typeof(LimitedHasteStatusEffect));
            ctx.AssertEqual(4, remaining.RemainingAttacks, "The queued impact spends the sixth and final batch charge");
            AssignCommand(attacker, () => ClearAllActions());
        }

        [EngineTest("Dual wield reservation accepts ammunition for queued ranged attacks", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task QueuedRangedAbilityAcceptsProjectile(EngineTestContext ctx)
        {
            var attacker = ctx.SpawnCreature("nw_bandit001", -2f);
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            var launcher = await ctx.EquipItemAsync(attacker, "npc_eco_rifle", InventorySlot.RightHand);
            // Unlimited-ammunition rifles may equip their own stack during equip processing.
            var ammunition = GetItemInSlot(InventorySlot.Bolts, attacker);
            if (!GetIsObjectValid(ammunition))
                ammunition = await ctx.EquipItemAsync(attacker, "nw_wambo001", InventorySlot.Bolts);
            ctx.ApplyStandardOnHitProperty(ammunition);
            try
            {
                Combat.SetAutoAttackHitResolutionOverride(true);
                _observedAttacker = attacker;
                _itemHits.Clear();
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    // Require the projectile to deliver the callback rather than letting the
                    // launcher's own property accidentally mask a broken reservation check.
                    for (var ip = GetFirstItemProperty(launcher); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(launcher))
                        if (GetItemPropertyType(ip) == ItemPropertyType.OnHitCastSpell)
                            RemoveItemProperty(launcher, ip);
                    SetItemStackSize(ammunition, 50);
                    ctx.SuppressNPCNaturalRegen(target);
                    Stat.SetNPCMaxHitPoints(target, 20000, true);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
                    ctx.MakeHostile(target);
                    ctx.SetNPCResources(attacker, 100, 100);
                    ctx.SetNPCPerkLevel(attacker, PerkType.Headshot, 1);
                    CreaturePlugin.AddFeat(attacker, FeatType.Headshot1);
                    ctx.Assert(UsePerkFeat.TryUseAbility(attacker, target, FeatType.Headshot1, GetLocation(target)), "Headshot queues");
                    ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(attacker), "Headshot is pending before attacking");
                    ActionAttack(target);
                });
                await ctx.WaitUntilAsync(() => !UsePerkFeat.HasQueuedWeaponAbility(attacker), 15f, "Headshot to consume through its projectile");
                ctx.Assert(_itemHits.Contains(ammunition), "Equipped ammunition delivered item_on_hit");
            }
            finally
            {
                AssignCommand(attacker, () => ClearAllActions());
                ResetObservation();
            }
        }

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

        [EngineTest("Dual wield temporary off-hand no-delay proc grants a matching bonus once", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task TemporaryOffHandNoDelayUsesTheOffHand(EngineTestContext ctx)
        {
            var (attacker, target, main, off) = await CreatePair(ctx, "nw_wswls001");
            try
            {
                _hits.Clear();
                _observedAttacker = attacker;
                Combat.SetAutoAttackHitResolutionOverride(true);
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    // 140 delay units produce the minimum effective delay without any proc.
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, 14), main);
                    ctx.AssertEqual(Combat.MinimumAttackDelayMilliseconds,
                        Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(attacker)),
                        "Unbuffed attacks are already at the minimum effective delay");
                    Combat.ClearAttackSwingDebt(attacker);
                    Combat.GrantNextAutoAttackNoDelay(attacker, SkillType.Vibroblade, 30);
                    ctx.Assert(!Combat.HasTemporaryNextAutoAttackNoDelay(attacker, SkillType.Vibroknife), "Proc excludes the main-hand skill");
                    ctx.Assert(Combat.HasTemporaryNextAutoAttackNoDelay(attacker, SkillType.Vibroblade), "Proc is armed for the off-hand skill");
                    ActionAttack(target);
                });
                await ctx.WaitUntilAsync(() => _hits.Count >= 10, 15f, "the proc's six-roll batch followed by four ordinary rolls");
                var firstBatch = _hits.Take(6).ToList();
                ctx.Assert((firstBatch.Last().Time - firstBatch.First().Time).TotalMilliseconds < 500, "Proc batch resolves in one animation");
                ctx.AssertEqual(3, firstBatch.Count(hit => hit.Weapon == main), "Proc batch main-hand rolls");
                ctx.AssertEqual(3, firstBatch.Count(hit => hit.Weapon == off && hit.Type == CombatDamageType.Poison), "Proc grants the extra off-hand roll");
                var nextBatch = _hits.Skip(6).Take(4).ToList();
                ctx.AssertEqual(2, nextBatch.Count(hit => hit.Weapon == off), "Next batch returns to ordinary off-hand cadence");
                ctx.Assert((nextBatch.First().Time - firstBatch.First().Time).TotalMilliseconds >= Combat.BaseAttackDelayMilliseconds,
                    "The proc retains the shared time gate");
                ctx.Assert(!Combat.HasTemporaryNextAutoAttackNoDelay(attacker, SkillType.Vibroblade), "The one-shot proc was consumed");
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
            var (attacker, target, main, _) = await CreatePair(ctx, "nw_wswls001");
            try
            {
                Combat.SetAutoAttackHitResolutionOverride(false);
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, 30), main);
                    var unbuffedDelay = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(attacker));
                    var effect = new LimitedHasteStatusEffect(40, 4, SkillType.Vibroblade, EffectIconType.Haste, null);
                    ctx.Assert(StatusEffect.ApplyStatusEffect(attacker, attacker, effect, 60f), "Off-hand haste applies");
                    ctx.AssertEqual(SkillType.Vibroblade, WeaponAttackTiming.GetTimingSkill(attacker),
                        "The off-hand's haste participates in the shared timer");
                    var sheet = new CharacterSheetViewModel();
                    typeof(CharacterSheetViewModel).GetField("_target", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sheet, attacker);
                    var delayInfo = ((string Value, string Tooltip))typeof(CharacterSheetViewModel)
                        .GetMethod("GetAttackDelayInfo", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(sheet, null)!;
                    var expectedDelay = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(attacker, 40)) / 1000f;
                    ctx.Assert(expectedDelay < unbuffedDelay / 1000f, "The display fixture has measurably faster off-hand haste");
                    ctx.AssertEqual($"{expectedDelay:0.##}s", delayInfo.Value, "Character sheet uses the off-hand timing effect");
                    var active = (LimitedHasteStatusEffect)StatusEffect.GetStatusEffect(attacker, typeof(LimitedHasteStatusEffect));
                    var budget = WeaponAttackTiming.GetLimitedBudget(attacker, SkillType.Vibroblade, SkillType.Vibroknife, SkillType.Vibroblade, false);
                    ctx.AssertEqual(false, budget.MainHand, "The timing charge excludes the knife");
                    ctx.AssertEqual(true, budget.OffHand, "The timing charge includes the blade");
                    var attacks = Combat.ConsumeAttacksPerSwing(attacker, 1000, 1750, false, 1750, 4, 0, 2, budget);
                    ResolveCycle(attacker, target, attacks);
                    ctx.AssertEqual(3, active.RemainingAttacks, "The main-hand knife does not consume the blade's charge");
                    budget = WeaponAttackTiming.GetLimitedBudget(attacker, SkillType.Vibroblade, SkillType.Vibroknife, SkillType.Vibroblade, false);
                    attacks = Combat.ConsumeAttacksPerSwing(attacker, 1000, 1750, false, 1750, 3, 0, 2, budget);
                    ctx.AssertEqual(4, attacks, "Fractional haste progress schedules two pairs with only two matching charges");
                    ResolveCycle(attacker, target, attacks);
                    ctx.AssertEqual(1, active.RemainingAttacks, "Both pairs consume only their blade charges");
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
            _itemHits.Clear();
        }
    }
}
