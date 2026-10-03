using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DoubleWeaponEngineTests
    {
        private static uint _observedAttacker = OBJECT_INVALID;
        private static readonly List<(uint Weapon, CombatDamageType Type, int Damage, DateTime Time)> _hits = new();
        private static readonly List<(uint Weapon, DateTime Time)> _itemHits = new();

        [EngineTest("Double weapon staggered ends consume a queued ability once for the same item", Category = "DoubleWeapon", TimeoutSeconds = 30f)]
        public static async Task StaggeredQueuedAbility(EngineTestContext ctx)
        {
            var (attacker, target, weapon) = await CreateFixture(ctx, "trn_saberstaff_1");
            var ability = Ability.GetAbilityDetail(FeatType.ForceSheath1);
            var originalImpact = ability.ImpactAction;
            var impactCount = 0;
            try
            {
                ctx.Assert(originalImpact != null, "Force Sheath has a real damage payload");
                ability.ImpactAction = (activator, selectedTarget, level, location) =>
                {
                    if (activator == attacker)
                        impactCount++;
                    originalImpact(activator, selectedTarget, level, location);
                };
                Observe(attacker);
                Combat.SetAutoAttackHitResolutionOverride(true);
                Combat.SetAbilityHitResolutionOverride(true);
                Ability.ClearLastCompletedAbilityImpactSummary(attacker);
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    ctx.SetNPCResources(attacker, 100, 100);
                    ctx.SetNPCPerkLevel(attacker, PerkType.ForceSheath, 1);
                    CreaturePlugin.AddFeat(attacker, FeatType.ForceSheath1);
                    ctx.Assert(UsePerkFeat.TryUseAbility(attacker, target, FeatType.ForceSheath1, GetLocation(target)),
                        "Force Sheath queues through normal activation");
                    ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(attacker), "The ability waits for a landed weapon hit");
                    ActionAttack(target);
                });
                await ctx.WaitUntilAsync(() => _itemHits.Count >= 2, 6f, "both staggered ends' item hits");
                ctx.AssertEqual(2, _itemHits.Count, "The first cycle delivers exactly two item-hit callbacks");
                ctx.Assert(_itemHits.All(hit => hit.Weapon == weapon), "Both callbacks refer to the same equipped item");
                ctx.Assert((_itemHits[1].Time - _itemHits[0].Time).TotalMilliseconds >= 1000,
                    "The second callback waits for the first end's complete swing");
                ctx.AssertEqual(1, impactCount, "Only one end invokes the queued ability's real damage payload");
                ctx.Assert(!UsePerkFeat.HasQueuedWeaponAbility(attacker), "The first end consumes the queued ability");
                var summary = Ability.GetLastCompletedAbilityImpactSummary(attacker);
                ctx.Assert(summary != null && summary.ImpactedTargetCount == 1 && summary.AttributedDamage > 0,
                    "The consumed ability actually damages its selected target");
                ctx.AssertEqual(1, _hits.Count, "The queued ability replaces one ordinary weapon roll");
                ctx.AssertEqual(weapon, _hits[0].Weapon, "The other end retains the same weapon profile");
                ctx.AssertEqual(CombatDamageType.Ice, _hits[0].Type, "The other end retains ordinary elemental damage");
                ctx.Assert(_hits[0].Damage > 0, "The other end still deals damage after the ability is consumed");
            }
            finally
            {
                ability.ImpactAction = originalImpact;
                UsePerkFeat.ClearQueuedAbility(attacker);
                Ability.ClearLastCompletedAbilityImpactSummary(attacker);
                Stop(attacker);
                ResetObservation();
            }
        }

        [EngineTest("Double weapon equipment changes and cancellation prevent a stale second end", Category = "DoubleWeapon", TimeoutSeconds = 60f)]
        public static async Task InterruptedSecondEnd(EngineTestContext ctx)
        {
            foreach (var resref in new[] { "b_twinblade", "trn_saberstaff_1" })
            foreach (var interruption in new[] { "weapon swap", "cancel" })
            {
                var (attacker, target, weapon) = await CreateFixture(ctx, resref);
                var replacement = OBJECT_INVALID;
                try
                {
                    if (interruption == "weapon swap")
                    {
                        await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                            replacement = CreateItemOnObject(resref, attacker));
                        await ctx.WaitFrameAsync();
                        ctx.Assert(GetIsObjectValid(replacement), "Replacement double weapon exists before the attack");
                    }

                    Observe(attacker);
                    Combat.SetAutoAttackHitResolutionOverride(true);
                    AssignCommand(attacker, () => ActionAttack(target));
                    await ctx.WaitUntilAsync(() => _hits.Count > 0, 5f, "the first end's damage roll");
                    ctx.AssertEqual(1, _hits.Count, "The second end is still pending");
                    await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                    {
                        if (interruption == "weapon swap")
                        {
                            // RunUnequip also needs an inventory destination. Use the native
                            // equipment operations so storage/action timing cannot prevent
                            // this fixture from changing equipment during a pending swing.
                            var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
                                .GetGameObject(attacker).AsNWSCreature();
                            var oldItem = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
                                .GetGameObject(weapon).AsNWSItem();
                            var newItem = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp
                                .GetGameObject(replacement).AsNWSItem();
                            var attackIndex = native.m_pcCombatRound.m_nCurrentAttack;
                            ctx.Assert(native.UnequipItem(oldItem, 0, 0) != 0, "The first double weapon is unequipped");
                            ctx.Assert(native.EquipItem((uint)global::NWN.Native.API.EquipmentSlot.RightHand, newItem, 1, 0, 0) != 0,
                                "A different double weapon is equipped immediately");
                            ctx.AssertEqual(replacement, GetItemInSlot(InventorySlot.RightHand, attacker),
                                "The right hand now contains the replacement item");
                            ctx.AssertEqual(replacement, EquipmentPredicates.GetOffhandAttackWeapon(attacker),
                                "Both ends now refer to the replacement item");
                            ctx.AssertEqual(attackIndex, native.m_pcCombatRound.m_nCurrentAttack,
                                "Equipment replacement does not advance the pending attack index");
                            ctx.Assert(WeaponAttackAnimation.HasQueuedAttack(native, target) && WeaponAttackAnimation.IsPlaying(native),
                                "The queued attack and animation remain active while equipment identity changes");
                        }
                        else
                        {
                            WeaponAttackCycle.CancelPendingHand(attacker);
                        }
                    });
                    await ctx.DelaySecondsAsync(1.3f);
                    ctx.AssertEqual(1, _hits.Count(hit => hit.Weapon == weapon),
                        $"{resref}: no delayed hit from the original item after {interruption}");
                    ctx.AssertEqual(1, _itemHits.Count(hit => hit.Weapon == weapon),
                        $"{resref}: no stale second-end on-hit payload after {interruption}");
                }
                finally
                {
                    Stop(attacker);
                    ResetObservation();
                }
            }
        }

        [EngineTest("NPC double weapon retains authored damage and delay with its two-end cycle", Category = "DoubleWeapon", TimeoutSeconds = 30f)]
        public static async Task NpcAuthoredDamageBudget(EngineTestContext ctx)
        {
            var (attacker, target, weapon) = await CreateFixture(ctx, "vnpcmsentguard", configureWeapon: false);
            var pairedCycle = 0;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    ctx.AssertEqual(53, GetPropertyTotal(weapon, ItemPropertyType.DMG), "The Sentinel's authored DMG budget is preserved");
                    var delayUnits = GetPropertyTotal(weapon, ItemPropertyType.Delay) * 10;
                    ctx.AssertEqual(290, delayUnits, "The Sentinel's explicit delay is preserved");
                    ctx.AssertEqual(0, Combat.CalculateOffhandAttackDelayReduction(attacker), "An untrained NPC has no Dual Wield reduction");
                    var formerSingleCycle = Combat.CalculateEffectiveAttackDelay(
                        Combat.CalculateAttackDelayMilliseconds(delayUnits, 0, 0, 0));
                    pairedCycle = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(attacker));
                    ctx.AssertEqual(2 * formerSingleCycle, pairedCycle,
                        "Two full-damage ends pay twice the former single-roll cycle");
                    ctx.Assert(Math.Abs(2.0 * 53 / pairedCycle - 53.0 / formerSingleCycle) < 0.0001,
                        "Two ends preserve the NPC's former authored damage per second");
                });

                Observe(attacker);
                Combat.SetAutoAttackHitResolutionOverride(true);
                AssignCommand(attacker, () => ActionAttack(target));
                await ctx.WaitUntilAsync(() => _hits.Count >= 2, 6f, "the NPC's two striking ends");
                ctx.AssertEqual(2, _hits.Count, "The NPC gets exactly two rolls in its opening cycle");
                ctx.Assert(_hits.All(hit => hit.Weapon == weapon && hit.Damage > 0),
                    "Both ends use the actual NPC weapon and deal damage");
                ctx.Assert((_hits[1].Time - _hits[0].Time).TotalMilliseconds >= 1000,
                    "The NPC's damage rolls remain staggered");
                var firstHit = _hits[0].Time;
                await ctx.WaitUntilAsync(() => _hits.Count >= 3, 8f, "the NPC's next paired cycle");
                var cycleGap = (_hits[2].Time - firstHit).TotalMilliseconds;
                ctx.Assert(cycleGap >= pairedCycle - 100, "The native attack loop honors the doubled cycle delay");
                ctx.Log($"Sentinel DMG 53 / Delay 290: two native rolls per {pairedCycle}ms cycle; observed next cycle {cycleGap:0}ms.");
            }
            finally
            {
                Stop(attacker);
                ResetObservation();
            }
        }

        private static async Task<(uint Attacker, uint Target, uint Weapon)> CreateFixture(
            EngineTestContext ctx, string resref, bool configureWeapon = true)
        {
            var attacker = ctx.SpawnCreature("nw_bandit001", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.WaitFrameAsync();
            var weapon = await ctx.EquipItemAsync(attacker, resref, InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                if (configureWeapon)
                {
                    BiowareXP2.IPSafeAddItemProperty(weapon, ItemPropertyCustom(ItemPropertyType.DMG, -1, 20),
                        0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
                    BiowareXP2.IPSafeAddItemProperty(weapon, ItemPropertyCustom(ItemPropertyType.Delay, -1, 29),
                        0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
                    BiowareXP2.IPSafeAddItemProperty(weapon, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Ice),
                        0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
                }
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 20000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 90f);
                TemporaryStatModifier.Add(target, StatType.MeleeDeflection, -100, 90f);
                ctx.MakeHostile(target);
                ctx.Assert(EquipmentPredicates.HasDualWield(attacker), "The fixture uses an eligible double weapon");
                ctx.AssertEqual(weapon, EquipmentPredicates.GetOffhandAttackWeapon(attacker),
                    "The second striking end aliases the right-hand item");
            });
            return (attacker, target, weapon);
        }

        private static int GetPropertyTotal(uint weapon, ItemPropertyType type)
        {
            var total = 0;
            for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
                if (GetItemPropertyType(ip) == type)
                    total += GetItemPropertyCostTableValue(ip);
            return total;
        }

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void ObserveDamage()
        {
            if (OBJECT_SELF != _observedAttacker)
                return;
            _hits.Add((StringToObject(EventsPlugin.GetEventData("WEAPON")),
                (CombatDamageType)int.Parse(EventsPlugin.GetEventData("DAMAGE_TYPE")),
                int.Parse(EventsPlugin.GetEventData("DAMAGE")), DateTime.UtcNow));
        }

        [NWNEventHandler(ScriptName.OnItemHit)]
        public static void ObserveItemHit()
        {
            if (OBJECT_SELF == _observedAttacker)
                _itemHits.Add((GetSpellCastItem(), DateTime.UtcNow));
        }

        private static void Observe(uint attacker)
        {
            _observedAttacker = attacker;
            _hits.Clear();
            _itemHits.Clear();
        }

        private static void Stop(uint attacker)
        {
            if (GetIsObjectValid(attacker))
                AssignCommand(attacker, () => ClearAllActions());
            WeaponAttackCycle.CancelPendingHand(attacker);
        }

        private static void ResetObservation()
        {
            Combat.SetAutoAttackHitResolutionOverride(null);
            Combat.SetAbilityHitResolutionOverride(null);
            _observedAttacker = OBJECT_INVALID;
            _hits.Clear();
            _itemHits.Clear();
        }
    }
}
