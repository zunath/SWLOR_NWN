using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using AttackBonus = SWLOR.NWN.API.NWScript.Enum.AttackBonus;
using DamagePower = SWLOR.NWN.API.NWScript.Enum.DamagePower;
using AbilityType = SWLOR.NWN.API.NWScript.Enum.AbilityType;
using DurationType = SWLOR.NWN.API.NWScript.Enum.DurationType;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;
using DamageType = SWLOR.NWN.API.NWScript.Enum.DamageType;
using Ability = SWLOR.Game.Server.Service.Ability;
using StatusEffect = SWLOR.Game.Server.Service.StatusEffect;
using NumericsVector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class TwinPistolEngineTests
    {
        private static uint _observed = OBJECT_INVALID;
        private static readonly List<(uint Weapon, CombatDamageType Type, int Damage)> Hits = new();
        private static readonly List<long> HitTimes = new();

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void ObserveDamage()
        {
            if (OBJECT_SELF == _observed)
            {
                Hits.Add((StringToObject(EventsPlugin.GetEventData("WEAPON")),
                    (CombatDamageType)int.Parse(EventsPlugin.GetEventData("DAMAGE_TYPE")),
                    int.Parse(EventsPlugin.GetEventData("DAMAGE"))));
                HitTimes.Add(Environment.TickCount64);
            }
        }

        [EngineTest("Twin Pistol ordinary pair keeps each gun's damage accuracy and type", Category = "TwinPistol", TimeoutSeconds = 45f)]
        public static async Task SeparateProfiles(EngineTestContext ctx)
        {
            var (actor, target, main, companion) = await CreatePair(ctx);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    var mainAccuracy = Stat.GetAccuracy(actor, main, AbilityType.Agility, SkillType.TwinPistol);
                    var companionAccuracy = Stat.GetAccuracy(actor, companion, AbilityType.Agility, SkillType.TwinPistol);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 7), main);
                    AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 19), companion);
                    ctx.AssertEqual(mainAccuracy + 7, Stat.GetAccuracy(actor, main, AbilityType.Agility, SkillType.TwinPistol), "Main ACC counts only its own property");
                    ctx.AssertEqual(companionAccuracy + 19, Stat.GetAccuracy(actor, companion, AbilityType.Agility, SkillType.TwinPistol), "Companion ACC counts only its own property");
                    ctx.Assert(WeaponShotProfile.Capture(actor, main, SkillType.TwinPistol).Damage !=
                               WeaponShotProfile.Capture(actor, companion, SkillType.TwinPistol).Damage,
                        "The fixture has independently budgeted gun damage");
                });
                Observe(actor);
                Combat.SetAutoAttackHitResolutionOverride(true);
                AssignCommand(actor, () => ActionAttack(target));
                await ctx.WaitUntilAsync(() => Hits.Count >= 2, 8f, "both ordinary gun impacts");
                ctx.AssertEqual(main, Hits[0].Weapon, "Native launcher fires first");
                ctx.AssertEqual(companion, Hits[1].Weapon, "Simulated shot owns the companion");
                ctx.AssertEqual(CombatDamageType.Physical, Hits[0].Type, "Main damage stays physical");
                ctx.AssertEqual(CombatDamageType.Ice, Hits[1].Type, "Companion damage keeps its own element");
                await ctx.DelaySecondsAsync(.7f);
                ctx.AssertEqual(2, Hits.Count, "One unhasted cycle has exactly two real shots");
            }
            finally { Stop(actor); }
        }

        [EngineTest("Twin Pistol companion can hit after the native launcher misses", Category = "TwinPistol", TimeoutSeconds = 45f)]
        public static async Task IndependentMiss(EngineTestContext ctx)
        {
            var (actor, target, main, companion) = await CreatePair(ctx);
            try
            {
                Observe(actor);
                Combat.SetAutoAttackHitResolutionOverride(false);
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                var initial = native.m_pcCombatRound.m_nCurrentAttack;
                AssignCommand(actor, () => ActionAttack(target));
                await ctx.WaitUntilAsync(() => native.m_pcCombatRound.m_nCurrentAttack != initial, 6f, "the native main-hand miss");
                Combat.SetAutoAttackHitResolutionOverride(true);
                await ctx.WaitUntilAsync(() => Hits.Any(x => x.Weapon == companion), 3f, "the independent companion hit");
                ctx.AssertEqual(0, Hits.Count(x => x.Weapon == main), "A main-hand miss contributes no copied damage");
                ctx.AssertEqual(1, Hits.Count(x => x.Weapon == companion), "Companion rolls and lands independently");
            }
            finally { Stop(actor); }
        }

        [EngineTest("Twin Pistol finite ammunition caps ordinary and hasted logical shot budgets", Category = "TwinPistol", TimeoutSeconds = 90f)]
        public static Task FiniteAmmunition(EngineTestContext ctx) => VerifyFiniteAmmunition(ctx, false);

        [EngineTest("Twin Pistol legacy pistol conversion preserves finite ordinary and hasted ammunition budgets", Category = "TwinPistol", TimeoutSeconds = 90f)]
        public static Task LegacyFiniteAmmunition(EngineTestContext ctx) => VerifyFiniteAmmunition(ctx, true);

        [EngineTest("Twin Pistol finite launched native shot keeps original damage and ammo after stop or swaps", Category = "TwinPistol", TimeoutSeconds = 75f)]
        public static async Task LaunchedFiniteShot(EngineTestContext ctx)
        {
            foreach (var interruption in new[] { "stop", "ammo swap", "weapon swap" })
            {
                using var player = await PlayerAbilityFixture.CreateAsync(ctx);
                var actor = player.Creature;
                player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 50);
                var ammunition = await ctx.EquipItemAsync(actor, "blaster_bullets", InventorySlot.Bullets);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    SetItemCursedFlag(ammunition, false);
                    SetItemStackSize(ammunition, 3);
                });
                var pair = await CreatePair(ctx, finiteAmmunition: true, actorOverride: actor);
                var replacement = OBJECT_INVALID;
                var ammunitionBag = OBJECT_INVALID;
                if (interruption == "ammo swap")
                {
                    await ctx.ExecuteInCreatureContextAsync(actor, () => ammunitionBag = CreateItemOnObject("bag_b", actor));
                    await ctx.WaitFrameAsync();
                    ctx.Assert(GetIsObjectValid(ammunitionBag), "An owned container keeps the original ammo stack separate from the replacement");
                }
                var hpBefore = GetCurrentHitPoints(pair.Target);
                var started = Environment.TickCount64;
                try
                {
                    Observe(actor);
                    Combat.SetAutoAttackHitResolutionOverride(true);
                    AssignCommand(actor, () => ActionAttack(pair.Target));
                    await ctx.WaitUntilAsync(() => Hits.Any(x => x.Weapon == pair.Main), 7f, "the original native main shot to launch");
                    var originalDamage = Hits.First(x => x.Weapon == pair.Main).Damage;
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        ClearAllActions();
                        ReplaceWeaponProperty(pair.Main, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 200));
                        if (interruption == "ammo swap")
                        {
                            CreaturePlugin.RunUnequip(actor, ammunition);
                            ctx.Assert(ItemPlugin.MoveTo(ammunition, ammunitionBag, true), "The original stack remains distinct in owned storage");
                        }
                        if (interruption == "weapon swap") CreaturePlugin.RunUnequip(actor, pair.Main);
                    });
                    if (interruption == "ammo swap")
                    {
                        replacement = await ctx.EquipItemAsync(actor, "blaster_bullets", InventorySlot.Bullets);
                        ctx.Assert(replacement != ammunition, "The replacement is a different original engine item, not a merged stack");
                        await ctx.ExecuteInCreatureContextAsync(actor, () =>
                        {
                            SetItemCursedFlag(replacement, false);
                            SetItemStackSize(replacement, 9);
                        });
                    }
                    if (interruption == "weapon swap") await ctx.EquipItemAsync(actor, "b_longsword", InventorySlot.RightHand);
                    await ctx.WaitUntilAsync(() => GetIsObjectValid(ammunition) && GetItemStackSize(ammunition) == 2,
                        7f, "the launched native projectile to debit its original finite stack");
                    await ctx.WaitUntilAsync(() => GetCurrentHitPoints(pair.Target) < hpBefore, 7f, "the launched native projectile's actual impact");
                    await ctx.DelaySecondsAsync(.7f);
                    ctx.AssertEqual(1, Hits.Count, "Stop and swaps cancel the pending companion and all new ordinary rolls");
                    ctx.AssertEqual(originalDamage, hpBefore - GetCurrentHitPoints(pair.Target), "The launched projectile keeps its original captured damage");
                    ctx.AssertEqual(actor, GetItemPossessor(ammunition), "The original finite ammunition stays owned");
                    ctx.AssertEqual(2, GetItemStackSize(ammunition), "Exactly the launched main bullet is consumed");
                    if (GetIsObjectValid(replacement)) ctx.AssertEqual(9, GetItemStackSize(replacement), "Delayed native consumption never touches replacement ammunition");
                    ctx.Log($"Finite launch {interruption}: elapsedMs={Environment.TickCount64 - started}, originalDamage={originalDamage}, " +
                        $"actualDamage={hpBefore - GetCurrentHitPoints(pair.Target)}, originalAmmo={GetItemStackSize(ammunition)}, " +
                        $"replacementAmmo={(GetIsObjectValid(replacement) ? GetItemStackSize(replacement) : 0)}");
                }
                catch
                {
                    var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                    ctx.Log($"Finite launch failure: mode={interruption}, elapsedMs={Environment.TickCount64 - started}, pc={GetIsPC(actor)}, " +
                        $"nativeBase={(int)GetBaseItemType(pair.Main)}, shots={Hits.Count}, magicalBullets={native.m_bMagicalBulletsEquipped}, " +
                        $"originalAmmo={ammunition:X}/{(GetIsObjectValid(ammunition) ? GetItemStackSize(ammunition) : 0)}, " +
                        $"originalOwner={GetItemPossessor(ammunition):X}, replacementAmmo={replacement:X}/{(GetIsObjectValid(replacement) ? GetItemStackSize(replacement) : 0)}, " +
                        $"equippedAmmo={GetItemInSlot(InventorySlot.Bullets, actor):X}, actualDamage={hpBefore - GetCurrentHitPoints(pair.Target)}");
                    throw;
                }
                finally
                {
                    await ctx.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
                    Stop(actor);
                    player.Dispose();
                    DestroyObject(actor);
                    DestroyObject(pair.Target);
                    await ctx.WaitFrameAsync();
                }
            }
        }

        private static async Task VerifyFiniteAmmunition(EngineTestContext ctx, bool legacyInput)
        {
            foreach (var (quantity, hasted) in new[] { (0, false), (1, false), (2, false), (2, true), (4, true), (5, true) })
            {
                using var player = await PlayerAbilityFixture.CreateAsync(ctx);
                var actor = player.Creature;
                player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 50);
                var ammunition = await ctx.EquipItemAsync(actor, "blaster_bullets", InventorySlot.Bullets);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    SetItemCursedFlag(ammunition, false);
                    SetItemStackSize(ammunition, Math.Max(1, quantity));
                });
                var pair = await CreatePair(ctx, finiteAmmunition: true, actorOverride: actor);
                var target = pair.Target;
                var main = pair.Main;
                var companion = pair.Companion;
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        for (var property = GetFirstItemProperty(main); GetIsItemPropertyValid(property); property = GetNextItemProperty(main))
                            if (GetItemPropertyType(property) == ItemPropertyType.UnlimitedAmmunition)
                                RemoveItemProperty(main, property);
                        CreaturePlugin.RunUnequip(actor, main);
                    });
                    await ctx.WaitFrameAsync();
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        if (GetIsObjectValid(ammunition))
                        {
                            // This blueprint also supplies generated ammunition. This fixture
                            // explicitly owns a normal removable finite stack instead.
                            SetItemCursedFlag(ammunition, false);
                            SetItemStackSize(ammunition, Math.Max(1, quantity));
                            ctx.Assert(!GetItemCursedFlag(ammunition), "Finite ammunition is a real removable item");
                        }
                        if (legacyInput)
                        {
                            ItemPlugin.SetBaseItemType(main, BaseItem.Pistol);
                            ctx.AssertEqual(BaseItem.Pistol, GetBaseItemType(main), "The compatibility fixture starts with real legacy base-item data");
                            ctx.Assert(PistolBaseItemCompatibility.Normalize(main), "The actual compatibility converter repairs the legacy input");
                            ctx.AssertEqual(actor, GetItemPossessor(main), "Compatibility conversion preserves the original pistol and owner");
                            if (GetIsObjectValid(ammunition))
                            {
                                ctx.AssertEqual(actor, GetItemPossessor(ammunition), "Conversion preserves the original finite ammunition owner");
                                ctx.AssertEqual(Math.Max(1, quantity), GetItemStackSize(ammunition), "Conversion preserves the finite ammunition quantity");
                            }
                        }
                        ctx.Assert(CreaturePlugin.RunEquip(actor, main, InventorySlot.RightHand), "The finite main equips beside its real ammunition");
                        ctx.AssertEqual(BaseItem.Sling, GetBaseItemType(main), "Ordinary paired attacks use the canonical native launcher after compatibility conversion");
                        var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                        ctx.Assert(GetIsPC(actor) && native.m_bPlayerCharacter != 0, "Native player ammunition consumption is exercised");
                        ctx.AssertEqual(0, native.m_bMagicalBulletsEquipped, "Finite fixture has no generated unlimited bullets");
                        if (quantity == 0)
                        {
                            CreaturePlugin.RunUnequip(actor, ammunition);
                            DestroyObject(ammunition);
                        }
                        if (hasted)
                        {
                            TemporaryStatModifier.Add(actor, StatType.AttackDelayReductionPercent, 35, 30f, "TWIN_AMMO_HASTE");
                            var haste = Stat.GetStatAdjustment(actor, StatType.AttackDelayReductionPercent);
                            ctx.AssertEqual(35, haste, "The real PC hasted control has the intended global speed adjustment");
                            var delay = Enumerable.Range(1, 600).First(cost =>
                            {
                                var effective = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelayMilliseconds(cost * 10, cost * 10,
                                    haste, Combat.CalculateOffhandAttackDelayReduction(actor)));
                                return Combat.CalculateAttackSwingDelay(effective) == Combat.BaseAttackDelayMilliseconds &&
                                       Combat.CalculateAttacksPerSwing(effective, 0, out _) == (quantity <= 2 ? 2 : 1);
                            });
                            ReplaceWeaponProperty(main, ItemPropertyType.Delay, ItemPropertyCustom(ItemPropertyType.Delay, -1, delay));
                            ReplaceWeaponProperty(companion, ItemPropertyType.Delay, ItemPropertyCustom(ItemPropertyType.Delay, -1, delay));
                            var effectiveDelay = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(actor));
                            ctx.Log($"Finite haste setup: pc={GetIsPC(actor)}, quantity={quantity}, delayCost={delay}, haste={haste}, " +
                                $"offhandReduction={Combat.CalculateOffhandAttackDelayReduction(actor)}, effectiveMs={effectiveDelay}, " +
                                $"swingMs={Combat.CalculateAttackSwingDelay(effectiveDelay)}");
                            ctx.AssertEqual(quantity <= 2 ? 4 : 2, Combat.CalculateAttacksPerSwing(effectiveDelay, 0, out _) * 2,
                                "The hasted batch has its requested logical-shot budget before finite ammunition limits");
                            ctx.AssertEqual(Combat.BaseAttackDelayMilliseconds, Combat.CalculateAttackSwingDelay(effectiveDelay),
                                "Hasted finite tests use the production native swing floor");
                        }
                    });
                    if (quantity == 0)
                        await ctx.WaitUntilAsync(() => !GetIsObjectValid(ammunition), 5f, "the zero-ammunition fixture to remove its temporary equip prerequisite");
                    Observe(actor);
                    Combat.SetAutoAttackHitResolutionOverride(true);
                    var started = Environment.TickCount64;
                    var hpBefore = GetCurrentHitPoints(target);
                    AssignCommand(actor, () => ActionAttack(target));
                    if (quantity > 0)
                    {
                        try
                        {
                            await ctx.WaitUntilAsync(() => Hits.Count >= quantity, 7f,
                                $"{quantity} affordable logical shots (nativeBase={(int)GetBaseItemType(main)}, hasted={hasted})");
                            await ctx.WaitUntilAsync(() =>
                            {
                                ctx.Assert(Hits.Count <= quantity, "Delayed native debits cannot allow ammunition to be reused by later cycles");
                                return !GetIsObjectValid(ammunition) || GetItemStackSize(ammunition) == 0;
                            }, 7f, "the original player ammunition stack's native delayed debit to drain");
                        }
                        catch
                        {
                            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                            ctx.Log($"Finite ammunition failure: nativeBase={(int)GetBaseItemType(main)}, start={quantity}, " +
                                $"hasted={hasted}, pc={GetIsPC(actor)}, elapsedMs={Environment.TickCount64 - started}, landed={Hits.Count}, attack={native.m_pcCombatRound.m_nCurrentAttack}, " +
                                $"paired={TwinPistolEquipment.IsActivePair(actor)}, bullets={GetItemInSlot(InventorySlot.Bullets, actor):X}, " +
                                $"arrows={GetItemInSlot(InventorySlot.Arrows, actor):X}, " +
                                $"remaining={(GetIsObjectValid(ammunition) ? GetItemStackSize(ammunition) : 0)}");
                            throw;
                        }
                    }
                    await ctx.DelaySecondsAsync(1f);
                    ctx.AssertEqual(quantity, Hits.Count, $"{quantity} bullets permit exactly {quantity} shots (hasted={hasted}, legacyInput={legacyInput})");
                    ctx.AssertEqual(quantity / 2, Hits.Count(x => x.Weapon == companion), "Each launched companion consumes its own available bullet");
                    ctx.Assert(!GetIsObjectValid(ammunition) || GetItemStackSize(ammunition) == 0,
                        $"No spent finite ammunition is duplicated or refunded (start={quantity}, hasted={hasted}, legacyInput={legacyInput}, " +
                        $"remaining={(GetIsObjectValid(ammunition) ? GetItemStackSize(ammunition) : 0)})");
                    if (quantity >= 4)
                    {
                        var mainTimes = Hits.Select((hit, index) => (hit, time: HitTimes[index])).Where(x => x.hit.Weapon == main).Select(x => x.time).ToArray();
                        ctx.Assert(mainTimes.Last() - mainTimes.First() >= Combat.BaseAttackDelayMilliseconds - 50,
                            "Four and five bullets cover at least two real native-floor cycles");
                    }
                    ctx.Assert(quantity == 0 ? GetCurrentHitPoints(target) == hpBefore : GetCurrentHitPoints(target) < hpBefore,
                        "Real projectile impacts require an affordable finite shot");
                    ctx.Log($"Finite PC ammo: start={quantity}, hasted={hasted}, legacyInput={legacyInput}, nativeBase={(int)GetBaseItemType(main)}, " +
                        $"elapsedMs={Environment.TickCount64 - started}, logicalShots={Hits.Count}, companionShots={Hits.Count(x => x.Weapon == companion)}, " +
                        $"actualDamage={hpBefore - GetCurrentHitPoints(target)}, remaining={(GetIsObjectValid(ammunition) ? GetItemStackSize(ammunition) : 0)}");
                }
                finally
                {
                    await ctx.ExecuteInCreatureContextAsync(actor, () => ClearAllActions());
                    Stop(actor);
                    player.Dispose();
                    DestroyObject(actor);
                    DestroyObject(target);
                    await ctx.WaitFrameAsync();
                }
            }
        }

        [EngineTest("Twin Pistol unlimited ammunition does not create duplicate real stacks", Category = "TwinPistol", TimeoutSeconds = 45f)]
        public static async Task UnlimitedAmmunition(EngineTestContext ctx)
        {
            var (actor, target, _, companion) = await CreatePair(ctx);
            try
            {
                var ammo = GetItemInSlot(InventorySlot.Bullets, actor);
                var quantity = GetIsObjectValid(ammo) ? GetItemStackSize(ammo) : 0;
                Observe(actor);
                Combat.SetAutoAttackHitResolutionOverride(true);
                AssignCommand(actor, () => ActionAttack(target));
                await ctx.WaitUntilAsync(() => Hits.Any(x => x.Weapon == companion), 8f, "the unlimited companion shot");
                ctx.AssertEqual(ammo, GetItemInSlot(InventorySlot.Bullets, actor), "The generated ammunition source is reused");
                if (GetIsObjectValid(ammo))
                    ctx.AssertEqual(quantity, GetItemStackSize(ammo), "Unlimited shots do not debit a finite stack");
            }
            finally { Stop(actor); }
        }

        [EngineTest("Twin Pistol queued cross-trained ability replaces one landed roll once", Category = "TwinPistol", TimeoutSeconds = 45f)]
        public static async Task QueuedAbilityOnce(EngineTestContext ctx)
        {
            var (actor, target, main, companion) = await CreatePair(ctx);
            try
            {
                Observe(actor);
                Combat.SetAutoAttackHitResolutionOverride(true);
                Combat.SetAbilityHitResolutionOverride(true);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    ctx.SetNPCResources(actor, 100, 100);
                    ctx.SetNPCPerkLevel(actor, PerkType.Headshot, 1);
                    CreaturePlugin.AddFeat(actor, FeatType.Headshot1);
                    ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.Headshot1, GetLocation(target)), "Cross-trained Headshot queues through the normal feat pipeline");
                    ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(actor), "The replacement waits for a landed weapon roll");
                    ActionAttack(target);
                });
                await ctx.WaitUntilAsync(() => !UsePerkFeat.HasQueuedWeaponAbility(actor), 10f, "one queued ability consumption");
                await ctx.WaitUntilAsync(() => Hits.Any(x => x.Weapon == companion), 4f, "the remaining ordinary companion roll");
                var summary = Ability.GetLastCompletedAbilityImpactSummary(actor);
                ctx.Assert(summary != null && summary.SkillType == SkillType.Rifle && summary.AttributedDamage > 0,
                    "The queued ability keeps its captured cross-trained skill and real impact");
                ctx.AssertEqual(0, Hits.Count(x => x.Weapon == main), "The replaced main-hand roll contributes no ordinary damage");
                ctx.AssertEqual(1, Hits.Count(x => x.Weapon == companion), "The other budgeted roll remains an ordinary shot");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Stop(actor);
            }
        }

        [EngineTest("Twin Pistol companion queued hit keeps the main gun profile and original skill credit", Category = "TwinPistol", TimeoutSeconds = 45f)]
        public static async Task CompanionQueuedAbility(EngineTestContext ctx)
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx, -.5f);
            var actor = player.Creature;
            player.Update(record =>
            {
                record.Skills[SkillType.TwinPistol].Rank = 50;
                record.Skills[SkillType.Rifle].Rank = 50;
                record.Perks[PerkType.Headshot] = 1;
            });
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            var main = await ctx.EquipItemAsync(actor, "b_pistol", InventorySlot.RightHand);
            var companion = await ctx.EquipItemAsync(actor, "tp_comp_proto", InventorySlot.LeftHand);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    ReplaceWeaponProperty(main, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 10));
                    ReplaceWeaponProperty(companion, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 30));
                    ReplaceWeaponProperty(companion, ItemPropertyType.WeaponDamageType,
                        ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Ice));
                    ctx.SuppressNPCNaturalRegen(target);
                    Stat.SetNPCMaxHitPoints(target, 20000, true);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 30f);
                    ctx.MakeHostile(target);
                    TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 30f, "TWIN_QUEUED");
                    CreaturePlugin.AddFeat(actor, FeatType.Headshot1);
                    Ability.ClearLastCompletedAbilityImpactSummary(actor);
                    var bonus = PairedRangedTraits.BeginAbility(actor, false, false);
                    PairedRangedTraits.CompleteAbility(actor, bonus, false, 0f, false, 0, 0, 6, 25, 12);
                    ctx.AssertEqual(6, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                        "The fixture starts with six ordinary attempted-shot charges");
                });
                Observe(actor);
                Combat.SetAutoAttackHitResolutionOverride(false);
                Combat.SetAbilityHitResolutionOverride(true);
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                var initial = native.m_pcCombatRound.m_nCurrentAttack;
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    ctx.Assert(UsePerkFeat.TryUseAbility(actor, target, FeatType.Headshot1, GetLocation(target)),
                        "The cross-trained ability queues for the first landed hand");
                    ActionAttack(target);
                });
                await ctx.WaitUntilAsync(() => native.m_pcCombatRound.m_nCurrentAttack != initial, 6f, "the queued main-hand miss");
                ctx.Assert(UsePerkFeat.HasQueuedWeaponAbility(actor), "A miss leaves the queued replacement readied");
                ctx.AssertEqual(5, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                    "The ordinary miss consumes one attempted-shot charge while leaving the replacement queued");
                Combat.SetAutoAttackHitResolutionOverride(true);
                await ctx.WaitUntilAsync(() => !UsePerkFeat.HasQueuedWeaponAbility(actor), 4f, "the companion consumes the queued replacement once");
                var summary = Ability.GetLastCompletedAbilityImpactSummary(actor);
                ctx.Assert(summary != null && summary.SkillType == SkillType.Rifle && summary.AttributedDamage > 0,
                    "The companion-triggered activation retains the purchased Rifle ability identity");
                ctx.AssertEqual(WeaponDamage.GetEffectiveDMG(actor, main), summary.TriggeringWeaponDamage ?? -1,
                    "Cross-training captures the real main pistol's damage even when the companion lands first");
                ctx.AssertEqual(CombatDamageType.Physical, summary.TriggeringWeaponDamageType ?? CombatDamageType.Force,
                    "The Ice companion cannot retype the captured main-hand ability");
                ctx.AssertEqual(0, Hits.Count, "The main miss and replaced companion grant no ordinary damage");
                ctx.AssertEqual(5, StatusEffect.GetStatusEffect<TwinSunsStatusEffect>(actor)?.Attempts ?? 0,
                    "The landed queued replacement consumes no additional ordinary-shot charge");
                var tracker = (Dictionary<uint, Dictionary<uint, Dictionary<SkillType, int>>>)typeof(CombatPoint)
                    .GetField("_creatureCombatPointTracker", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                var points = tracker[target][actor];
                ctx.AssertEqual(3, points.GetValueOrDefault(SkillType.Rifle), "One queued ability awards its original skill contribution once");
                ctx.AssertEqual(0, points.GetValueOrDefault(SkillType.TwinPistol), "A replaced roll awards no extra Twin Pistol contribution");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Stop(actor);
            }
        }

        [EngineTest("Twin Pistol pending companion cancels on stop gear death and target changes", Category = "TwinPistol", TimeoutSeconds = 120f)]
        public static async Task Cancellation(EngineTestContext ctx)
        {
            foreach (var interruption in new[] { "stop", "gear", "attacker death", "target death", "target change" })
            {
                var (actor, target, _, companion) = await CreatePair(ctx);
                try
                {
                    Observe(actor);
                    Combat.SetAutoAttackHitResolutionOverride(true);
                    AssignCommand(actor, () => ActionAttack(target));
                    await ctx.WaitUntilAsync(() => Hits.Count >= 1, 6f, "main-hand resolution before the companion launch");
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        switch (interruption)
                        {
                            case "stop": ClearAllActions(); break;
                            case "gear": CreaturePlugin.RunUnequip(actor, companion); break;
                            case "attacker death": ApplyEffectToObject(DurationType.Instant, EffectDeath(), actor); break;
                            case "target death": ApplyEffectToObject(DurationType.Instant, EffectDeath(), target); break;
                            case "target change": ClearAllActions(); ActionAttack(ctx.SpawnCreature("nw_rat001", 2f)); break;
                        }
                    });
                    await ctx.DelaySecondsAsync(.8f);
                    ctx.AssertEqual(0, Hits.Count(x => x.Weapon == companion), $"Pending companion is cancelled after {interruption}");
                }
                finally { Stop(actor); }
            }
        }

        [EngineTest("Twin Pistol uses ranged deflection and physical Guard separately", Category = "TwinPistol", TimeoutSeconds = 60f)]
        public static async Task DeflectionAndGuard(EngineTestContext ctx)
        {
            var (actor, target, _, companion) = await CreatePair(ctx, "nw_bandit001");
            await ctx.ExecuteInCreatureContextAsync(target, () =>
            {
                ClearAllActions();
                var existing = GetItemInSlot(InventorySlot.RightHand, target);
                if (GetIsObjectValid(existing)) CreaturePlugin.RunUnequip(target, existing);
                var blade = CreateItemOnObject("nw_wswss001", target);
                CreaturePlugin.RunEquip(target, blade, InventorySlot.RightHand);
                ctx.AssertEqual(blade, GetItemInSlot(InventorySlot.RightHand, target),
                    "The paralyzed defender immediately equips its deflection weapon");
            });
            try
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    TemporaryStatModifier.Add(target, StatType.RangedDeflection, 200, 30f, "TWIN_DEFLECT");
                    TemporaryStatModifier.Add(target, StatType.RangedDeflectionChanceCap, 100, 30f, "TWIN_DEFLECT");
                });
                Observe(actor);
                Combat.SetAutoAttackHitResolutionOverride(true);
                AssignCommand(actor, () => ActionAttack(target));
                await ctx.DelaySecondsAsync(2f);
                ctx.AssertEqual(0, Hits.Count, "Ranged deflection negates both independent shots");
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    ClearAllActions();
                    TemporaryStatModifier.Add(target, StatType.RangedDeflection, -300, 30f, "TWIN_NO_DEFLECT");
                    TemporaryStatModifier.Add(target, StatType.Guard, 100, 30f, "TWIN_GUARD");
                    ReplaceWeaponProperty(companion, ItemPropertyType.WeaponDamageType,
                        ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical));
                    ActionAttack(target);
                });
                await ctx.WaitUntilAsync(() => Hits.Any(x => x.Weapon == companion), 8f, "a physical companion shot through Guard");
                ctx.Assert(Combat.HasRecentGuardedHit(target, 10f), "Guard reduces landed physical damage instead of negating the roll");
                ctx.Assert(Hits.All(x => x.Damage > 0), "Guard leaves damage on the landed shot");
            }
            finally { Stop(actor); }
        }

        [EngineTest("Twin Pistol physical and elemental native immunity mitigate each hand once", Category = "TwinPistol", TimeoutSeconds = 75f)]
        public static async Task NativeImmunity(EngineTestContext ctx)
        {
            foreach (var type in new[] { CombatDamageType.Physical, CombatDamageType.Ice })
            {
                var (actor, target, main, companion) = await CreatePair(ctx);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        ReplaceWeaponProperty(main, ItemPropertyType.WeaponDamageType,
                            ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)type));
                        ReplaceWeaponProperty(companion, ItemPropertyType.WeaponDamageType,
                            ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)type));
                        var immunities = type == CombatDamageType.Physical
                            ? new[] { DamageType.Slashing, DamageType.Bludgeoning, DamageType.Piercing }
                            : new[] { DamageType.Cold };
                        foreach (var immunity in immunities)
                            ApplyEffectToObject(DurationType.Temporary, EffectDamageImmunityIncrease(immunity, 100), target, 30f);
                    });
                    Observe(actor);
                    Combat.SetAutoAttackHitResolutionOverride(true);
                    var hp = GetCurrentHitPoints(target);
                    var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                    var initial = native.m_pcCombatRound.m_nCurrentAttack;
                    AssignCommand(actor, () => ActionAttack(target));
                    await ctx.WaitUntilAsync(() => native.m_pcCombatRound.m_nCurrentAttack != initial, 6f, "an ordinary paired cycle against immunity");
                    await ctx.DelaySecondsAsync(1.2f);
                    ctx.AssertEqual(hp, GetCurrentHitPoints(target), $"Native {type} immunity negates both independently resolved hands");
                    if (type == CombatDamageType.Physical)
                        ctx.AssertEqual(0, Hits.Count, "Physical immunity resolves before damage-stage effects");
                }
                finally { Stop(actor); }
            }
        }

        [EngineTest("Twin Pistol physical companion owns its immunity flags and penetration scope", Category = "TwinPistol", TimeoutSeconds = 60f)]
        public static async Task IndependentPhysicalMitigation(EngineTestContext ctx)
        {
            foreach (var penetration in new[] { false, true })
            {
                var (actor, target, main, companion) = await CreatePair(ctx);
                try
                {
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        ReplaceWeaponProperty(companion, ItemPropertyType.WeaponDamageType,
                            ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical));
                        if (penetration)
                        {
                            ApplyEffectToObject(DurationType.Temporary, EffectAccuracyIncrease(5, AttackBonus.OnHand), actor, 30f);
                            ApplyEffectToObject(DurationType.Temporary, EffectDamageReduction(9999, DamagePower.PlusOne), target, 30f);
                        }
                        else
                            ApplyEffectToObject(DurationType.Temporary, EffectDamageImmunityIncrease(DamageType.Piercing, 100), target, 30f);
                    });
                    Observe(actor);
                    Combat.SetAutoAttackHitResolutionOverride(true);
                    var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
                    var initial = native.m_pcCombatRound.m_nCurrentAttack;
                    AssignCommand(actor, () => ActionAttack(target));
                    await ctx.WaitUntilAsync(() => native.m_pcCombatRound.m_nCurrentAttack != initial, 6f, "the independently mitigated main roll");
                    await ctx.DelaySecondsAsync(1.1f);
                    ctx.AssertEqual(penetration ? 1 : 0, Hits.Count(x => x.Weapon == main),
                        "The real launcher preserves its native Piercing flag and main-hand penetration");
                    ctx.AssertEqual(penetration ? 0 : 1, Hits.Count(x => x.Weapon == companion),
                        "Physical companion mitigation uses its captured Slashing type and its own hand's penetration");
                    if (penetration)
                    {
                        await ctx.ExecuteInCreatureContextAsync(actor, () =>
                        {
                            ClearAllActions();
                            ApplyEffectToObject(DurationType.Temporary, EffectAccuracyIncrease(5, AttackBonus.OffHand), actor, 30f);
                            ActionAttack(target);
                        });
                        await ctx.WaitUntilAsync(() => Hits.Any(x => x.Weapon == companion), 6f,
                            "the companion's own native penetration buff bypasses reduction");
                    }
                }
                finally { Stop(actor); }
            }
        }

        [EngineTest("Twin Pistol damage discount excludes native immunity and periodic damage", Category = "TwinPistol", TimeoutSeconds = 45f)]
        public static async Task DamageDiscountEligibility(EngineTestContext ctx)
        {
            var (actor, source, _, _) = await CreatePair(ctx);
            try
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    TemporaryStatModifier.Add(actor, StatType.PairedRangedDirectDamageNextAbilityStaminaReduction, 2, 30f);
                    TemporaryStatModifier.Add(actor, StatType.PairedRangedDirectDamageCostWindowSeconds, 10, 30f);
                    TemporaryStatModifier.Add(actor, StatType.PairedRangedDirectDamageCostCooldownSeconds, 10, 30f);
                    ApplyEffectToObject(DurationType.Temporary, EffectDamageImmunityIncrease(DamageType.Cold, 100), actor, 30f);
                    Combat.ApplyDamageDealtEffects(source, actor, 10, SkillType.TwinPistol, CombatDamageType.Ice);
                    ctx.AssertEqual(0, PairedRangedTraits.GetAbilityStaminaCostAdjustment(actor),
                        "A pre-mitigation elemental event cannot grant the discount through native immunity");
                    Combat.ApplyDamageDealtEffects(source, actor, 10, SkillType.TwinPistol, CombatDamageType.Physical,
                        CombatDamageDeliveryType.DamageOverTime);
                    ctx.AssertEqual(0, PairedRangedTraits.GetAbilityStaminaCostAdjustment(actor),
                        "Positive periodic damage cannot grant a direct-damage discount");
                    Combat.ApplyDamageDealtEffects(source, actor, 10, SkillType.TwinPistol, CombatDamageType.Physical,
                        CombatDamageDeliveryType.Triggered);
                    ctx.AssertEqual(0, PairedRangedTraits.GetAbilityStaminaCostAdjustment(actor),
                        "Triggered or reflected damage cannot grant the discount");
                    Combat.ApplyDamageDealtEffects(source, actor, 10, SkillType.TwinPistol, CombatDamageType.Physical);
                    ctx.AssertEqual(-2, PairedRangedTraits.GetAbilityStaminaCostAdjustment(actor),
                        "The same damage hook grants exactly one discount for positive direct hostile damage");
                });
            }
            finally { Stop(actor); }
        }

        [EngineTest("Twin Pistol Slide Rush and Grapple finish movement through the real feat pipeline", Category = "TwinPistolMovement", TimeoutSeconds = 120f)]
        public static async Task MovementAbilityPipeline(EngineTestContext ctx)
        {
            foreach (var movement in new[] { TwinPistolMovementType.Retreat, TwinPistolMovementType.FreeDestination, TwinPistolMovementType.Approach })
            {
                var (actor, target, _, _) = await CreatePair(ctx);
                try
                {
                    var origin = GetPosition(actor);
                    SWLOR.NWN.API.Engine.Location destination = null;
                    NumericsVector3 enemyPosition = default;
                    var found = false;
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        ClearAllActions();
                        for (var direction = 0; direction < 16 && !found; direction++)
                        {
                            var angle = direction * MathF.PI / 8;
                            var unit = new NumericsVector3(MathF.Cos(angle), MathF.Sin(angle), 0);
                            var distance = movement == TwinPistolMovementType.Approach ? 3.5f : 4f;
                            var end = origin + unit * distance;
                            end.Z = GetGroundHeight(Location(ctx.Arena, end, 0));
                            var travel = Location(ctx.Arena, end, 0);
                            if (!string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor), travel, 2f, 6f))) continue;
                            enemyPosition = movement switch
                            {
                                TwinPistolMovementType.Retreat => origin - unit * 2f,
                                TwinPistolMovementType.Approach => origin + unit * 5f,
                                _ => origin + unit * 2f + new NumericsVector3(-unit.Y, unit.X, 0) * 1.2f
                            };
                            enemyPosition.Z = GetGroundHeight(Location(ctx.Arena, enemyPosition, 0));
                            if (!string.IsNullOrEmpty(WeaponRepositioning.Validate(actor, GetLocation(actor),
                                    Location(ctx.Arena, enemyPosition, 0), 0f, 6f))) continue;
                            destination = travel;
                            found = true;
                        }
                        ctx.Assert(found, $"The arena provides a reachable {movement} lane and a legal hostile target position");
                        ctx.SetNPCResources(actor, 100, 100);
                        TemporaryStatModifier.Add(actor, StatType.PairedRangedMomentumMaximum, 3, 60f);
                        TemporaryStatModifier.Add(actor, StatType.PairedRangedMomentumDurationSeconds, 12, 60f);
                    });
                    // The controlled target is paralyzed; queued JumpToLocation cannot relocate it.
                    ObjectPlugin.SetPosition(target, enemyPosition);
                    await ctx.WaitFrameAsync();
                    ctx.Assert(NumericsVector3.Distance(enemyPosition, GetPosition(target)) < .05f,
                        "The stationary target is at the authored relative position before validating movement");
                    var feat = movement switch
                    {
                        TwinPistolMovementType.Retreat => FeatType.BlasterSlide1,
                        TwinPistolMovementType.Approach => FeatType.GrappleShot1,
                        _ => FeatType.PistolRush1
                    };
                    var perk = movement switch
                    {
                        TwinPistolMovementType.Retreat => PerkType.BlasterSlide,
                        TwinPistolMovementType.Approach => PerkType.GrappleShot,
                        _ => PerkType.PistolRush
                    };
                    Combat.SetAutoAttackHitResolutionOverride(false);
                    Combat.SetAbilityHitResolutionOverride(true);
                    await ctx.ExecuteInCreatureContextAsync(actor, () =>
                    {
                        ctx.SetNPCPerkLevel(actor, perk, 1);
                        CreaturePlugin.AddFeat(actor, feat);
                        Ability.ClearLastCompletedAbilityImpactSummary(actor);
                        var activated = UsePerkFeat.TryUseAbility(actor,
                            movement == TwinPistolMovementType.FreeDestination ? OBJECT_INVALID : target,
                            feat, destination);
                        ctx.Assert(activated, $"{feat} prepayment validation: {Ability.GetLastActivationDenialReason()}; " +
                            $"actor={GetPosition(actor)}, target={GetPosition(target)}, requested={GetPositionFromLocation(destination)}, " +
                            $"hostile={GetIsReactionTypeHostile(target, actor)}, objectLOS={LineOfSightObject(actor, target)}, " +
                            $"vectorLOS={LineOfSightVector(GetPosition(actor), GetPosition(target))}");
                        ctx.Assert(TwinPistolAbility.HasActiveActivation(actor), "The cast captures its weapon and trait state");
                        TwinPistolCombatLifecycle.Reconcile();
                        ctx.Assert(TwinPistolAbility.HasActiveActivation(actor),
                            "Out-of-combat reconciliation preserves an authorized cast before its first hostile impact");
                    });
                    await ctx.WaitUntilAsync(() => !WeaponRepositioning.IsMoving(actor) &&
                        Ability.GetLastCompletedAbilityImpactSummary(actor)?.AttributedDamage > 0,
                        10f, "confirmed arrival followed by the captured paired impacts");
                    ctx.Assert(NumericsVector3.Distance(origin, GetPosition(actor)) >= 3f,
                        "The normal post-cast attack resume did not cancel the movement action");
                    var summary = Ability.GetLastCompletedAbilityImpactSummary(actor);
                    ctx.Assert(summary.SkillType == SkillType.TwinPistol && summary.AttributedDamage > 0,
                        "Both actual relocation and the paid Twin Pistol attack completed");
                    if (movement != TwinPistolMovementType.Approach)
                        ctx.AssertEqual(1, StatusEffect.GetStatusEffect<MomentumStatusEffect>(actor)?.Charges ?? 0,
                            "A landed moving activation grants one Momentum rather than one per gun");
                }
                finally
                {
                    TwinPistolAbility.Cancel(actor);
                    Combat.SetAbilityHitResolutionOverride(null);
                    Stop(actor);
                    // Finished controlled actors must not occupy the next relative-movement lane.
                    DestroyObject(actor);
                    DestroyObject(target);
                    await ctx.WaitFrameAsync();
                }
            }
        }

        private static async Task<(uint Actor, uint Target, uint Main, uint Companion)> CreatePair(EngineTestContext ctx, string targetBlueprint = "nw_rat001", bool finiteAmmunition = false, uint actorOverride = OBJECT_INVALID)
        {
            ctx.SeedRandom(20261013);
            var actor = GetIsObjectValid(actorOverride) ? actorOverride : ctx.SpawnCreature("nw_bandit001", -.5f);
            var target = ctx.SpawnCreature(targetBlueprint, 1f);
            await ctx.WaitFrameAsync();
            var main = OBJECT_INVALID;
            if (finiteAmmunition)
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    main = CreateItemOnObject("b_pistol", actor);
                    ctx.Assert(GetIsObjectValid(main), "The finite fixture owns its main pistol before first equip");
                    var unlimited = new List<ItemProperty>();
                    for (var ip = GetFirstItemProperty(main); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(main))
                        if (GetItemPropertyType(ip) == ItemPropertyType.UnlimitedAmmunition) unlimited.Add(ip);
                    foreach (var ip in unlimited) RemoveItemProperty(main, ip);
                });
                await ctx.WaitFrameAsync();
                ctx.ApplyStandardOnHitProperty(main);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    var existing = GetItemInSlot(InventorySlot.RightHand, actor);
                    if (GetIsObjectValid(existing)) CreaturePlugin.RunUnequip(actor, existing);
                    ClearAllActions();
                    ActionEquipItem(main, InventorySlot.RightHand);
                });
                await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, actor) == main, 5f,
                    "the finite pistol to equip without ever generating unlimited ammunition");
            }
            else main = await ctx.EquipItemAsync(actor, "b_pistol", InventorySlot.RightHand);
            var companion = await ctx.EquipItemAsync(actor, "tp_comp_proto", InventorySlot.LeftHand);
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ReplaceWeaponProperty(main, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 10));
                ReplaceWeaponProperty(companion, ItemPropertyType.DMG, ItemPropertyCustom(ItemPropertyType.DMG, -1, 30));
                ReplaceWeaponProperty(main, ItemPropertyType.WeaponDamageType,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Physical));
                ReplaceWeaponProperty(companion, ItemPropertyType.WeaponDamageType,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Ice));
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 20000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 120f);
                TemporaryStatModifier.Add(target, StatType.RangedDeflection, -100, 120f, "TWIN_FIXTURE");
                ctx.MakeHostile(target);
                ctx.Assert(TwinPistolEquipment.IsActivePair(actor), "Rank-zero fixture activates the new paired skill");
            });
            return (actor, target, main, companion);
        }

        private static void ReplaceWeaponProperty(uint weapon, ItemPropertyType type, ItemProperty replacement)
        {
            var old = new List<ItemProperty>();
            for (var property = GetFirstItemProperty(weapon); GetIsItemPropertyValid(property); property = GetNextItemProperty(weapon))
                if (GetItemPropertyType(property) == type) old.Add(property);
            foreach (var property in old) RemoveItemProperty(weapon, property);
            AddItemProperty(DurationType.Permanent, replacement, weapon);
        }

        private static void Observe(uint actor)
        {
            _observed = actor;
            Hits.Clear();
            HitTimes.Clear();
        }

        private static void Stop(uint actor)
        {
            if (GetIsObjectValid(actor)) AssignCommand(actor, () => ClearAllActions());
            PairedRangedAttackCycle.Cancel(actor);
            Combat.SetAutoAttackHitResolutionOverride(null);
            _observed = OBJECT_INVALID;
            Hits.Clear();
            HitTimes.Clear();
        }
    }
}
