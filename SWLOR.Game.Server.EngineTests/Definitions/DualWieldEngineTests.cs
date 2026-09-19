using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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

        [EngineTest("Dual wield explicit swing variants preserve equipment and queued ability mappings", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task ExplicitSwingVariants(EngineTestContext ctx)
        {
            var (attacker, target, _, _) = await CreatePair(ctx);
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
                ReplaceObjectAnimation(attacker, "1hslashl", "nwslashl");
                ReplaceObjectAnimation(attacker, "2hslashl", "ca_attack");
                var saved = WeaponAttackAnimation.ReadAnimationReplacements(native);
                foreach (var variant in Enumerable.Range(0, 3))
                {
                    var suffix = new[] { "slashl", "slashr", "stab" }[variant];
                    try
                    {
                        WeaponAttackAnimation.WithSwingVariant(native, variant, () =>
                        {
                            var projected = WeaponAttackAnimation.ReadAnimationReplacements(native);
                            ctx.AssertEqual("2w" + suffix, projected["2wslashl"], "The client's repeated main-hand clip is explicitly replaced");
                            ctx.AssertEqual("nw" + suffix, projected["1hslashl"], "Katar keeps its unarmed family");
                            ctx.AssertEqual("ca_attack", projected["2hslashl"], "Queued ability clip is preserved");
                            ctx.Assert(!projected.ContainsKey("2wslasho"), "The off-hand clip remains distinct");
                            throw new InvalidOperationException("test mapping failure");
                        });
                    }
                    catch (InvalidOperationException ex) when (ex.Message == "test mapping failure") { }
                    var restored = WeaponAttackAnimation.ReadAnimationReplacements(native);
                    ctx.AssertEqual(saved.Count, restored.Count, "Temporary variant keys are removed even on failure");
                    ctx.Assert(saved.All(pair => restored.GetValueOrDefault(pair.Key) == pair.Value), "All original mappings are restored");
                }
                var first = ResolveCycle(attacker, target, 2);
                native.SetAnimation(9);
                WeaponAttackAnimation.Capture(native, first, 5900);
                ctx.AssertEqual((ushort)1750, ReadAnimationPacket(native).Duration, "The opening hand has a full animation duration");
            });
            // Deliberately miss the nominal frame boundary. A late observer still receives
            // a complete off-hand animation, never a shortened catch-up burst.
            var deadline = DateTime.UtcNow.AddMilliseconds(1950);
            await ctx.WaitUntilAsync(() => DateTime.UtcNow >= deadline, 4f, "the full main-hand animation");
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
                var packet = ReadAnimationPacket(native);
                ctx.AssertEqual((byte)2, packet.Weapon, "Off hand follows the completed main hand");
                ctx.AssertEqual((ushort)1750, packet.Duration, "Late updates never accelerate the off hand");
            });
        }

        [EngineTest("Dual wield legacy basic vibroblade repair restores matching noncritical damage", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task BasicVibrobladeDamage(EngineTestContext ctx)
        {
            var attacker = ctx.SpawnCreature("nw_bandit001", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.WaitFrameAsync();
            var main = await ctx.EquipItemAsync(attacker, "b_longsword", InventorySlot.RightHand);
            var off = await ctx.EquipItemAsync(attacker, "b_longsword", InventorySlot.LeftHand);
            var nativeMain = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(main).AsNWSItem();
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                using var legacyResref = new CExoString("longsword_b");
                nativeMain.m_sTemplate = legacyResref;
                SetName(main, BasicVibrobladeCompatibility.DisplayName);
                foreach (var property in Properties(main).Where(ip => GetItemPropertyType(ip) == ItemPropertyType.DMG ||
                                                                      GetItemPropertyType(ip) == ItemPropertyType.RequiresSkill))
                    RemoveItemProperty(main, property);
                SetLocalString(main, "_TEST_PRESERVED", "kept");
            });
            await ctx.WaitUntilAsync(() => !Properties(main).Any(ip => GetItemPropertyType(ip) == ItemPropertyType.DMG), 3f, "legacy missing-DMG fixture");
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                ctx.AssertEqual("longsword_b", GetResRef(main), "Fixture uses the retired template from the saved character");
                ctx.Assert(BasicVibrobladeCompatibility.NormalizeInventory(attacker), "Login/inventory repair finds the equipped legacy item");
                ctx.Assert(!BasicVibrobladeCompatibility.NormalizeInventory(attacker), "Repair is idempotent");
                foreach (var item in new[] { main, off })
                {
                    var dmg = Properties(item).Where(ip => GetItemPropertyType(ip) == ItemPropertyType.DMG).ToArray();
                    ctx.AssertEqual(1, dmg.Length, "Exactly one DMG property survives repair");
                    ctx.AssertEqual(5, GetItemPropertyCostTableValue(dmg[0]), "Both basic vibroblades use canonical DMG 5");
                    ctx.AssertEqual(BasicVibrobladeCompatibility.DisplayName, GetName(item), "Basic vibroblade name is retained");
                }
                ctx.AssertEqual("kept", GetLocalString(main, "_TEST_PRESERVED"), "Existing item data is retained");
                SetName(main, "My custom blade");
                BasicVibrobladeCompatibility.Normalize(main);
                ctx.AssertEqual("My custom blade", GetName(main), "Custom names are not replaced");
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
                var defender = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
                native.m_pcCombatRound.StartCombatRound(target);
                WeaponAttackCycle.PrepareDualWieldAttacks(native.m_pcCombatRound, 2);
                var damage = new[] { new List<int>(), new List<int>() };
                for (var hand = 0; hand < 2; hand++)
                for (var sample = 0; sample < 128; sample++)
                {
                    native.m_pcCombatRound.m_nCurrentAttack = (byte)hand;
                    // Explicitly noncritical; bypass random attack outcomes to compare the
                    // same real damage hook for each equipped weapon.
                    damage[hand].Add(native.m_pStats.GetDamageRoll(defender, hand, 0, 0, 0, 0));
                }
                ctx.Assert(Math.Abs(damage[0].Average() - damage[1].Average()) < 2,
                    $"Identical noncritical weapon profiles agree: main {damage[0].Min()}-{damage[0].Max()}, off {damage[1].Min()}-{damage[1].Max()}");
            });
        }

        private static SWLOR.NWN.API.Engine.ItemProperty[] Properties(uint item)
        {
            var result = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
                result.Add(property);
            return result.ToArray();
        }

        [EngineTest("Dual wield fresh animation bursts bypass an unchanged native attack pose", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task FreshAnimationBursts(EngineTestContext ctx)
        {
            var (attacker, target, _, _) = await CreatePair(ctx);
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
                var first = ResolveCycle(attacker, target, 2);
                native.SetAnimation(9);
                // The engine's complete update-difference pass requires a logged-in
                // network client. Exercise our refresh predicate and the real packet
                // writer here, without invoking unrelated native client/session checks.
                WeaponAttackAnimation.Capture(native, first, 5900);
                ctx.Assert(WeaponAttackAnimation.NeedsAnimationUpdate(native, 0xfffffffe),
                    "An unchanged attack pose still gets a fresh burst");
                var packet = ReadAnimationPacket(native);
                ctx.AssertEqual((byte)1, packet.Count, "The real write hook serializes one visual swing");
                ctx.AssertEqual((byte)1, packet.Weapon, "The first fresh burst shows the main hand");
                ctx.Assert((packet.Updates & 0x1000000) != 0, "Swing replacements accompany the attack packet");
                ctx.AssertEqual(0u, ReadAnimationPacket(native, 0).Updates,
                    "Unrelated updates do not prematurely reset the current swing's replacements");
                ctx.Assert(!WeaponAttackAnimation.NeedsAnimationUpdate(native, 0xfffffffe),
                    "A frame already sent is not restarted on every network tick");
                ctx.Assert(WeaponAttackAnimation.NeedsAnimationUpdate(native, 0xfffffffd),
                    "Another observer still receives its own first frame");
                WeaponAttackAnimation.Capture(native, first, 5900);
                ctx.Assert(WeaponAttackAnimation.NeedsAnimationUpdate(native, 0xfffffffe),
                    "The next cycle sends a fresh burst even with the same target and pose");
                native.SetAnimation(1);
                // SetAnimation may translate Ready to Pause for this NPC's ambient state.
                ctx.Assert(native.m_nAnimation != 9, "The native action left its attack pose");
                ctx.Assert(!WeaponAttackAnimation.NeedsAnimationUpdate(native, 0xfffffffe),
                    "Interrupted playback does not request another attack update");
                var interrupted = ReadAnimationPacket(native);
                ctx.AssertEqual((ushort)native.m_nAnimation, interrupted.Animation,
                    "An interrupted attack is not forced back into combat playback");
                ctx.Assert((interrupted.Updates & 0x1000000) != 0, "Interruption restores the real replacement list on the client");
                ctx.AssertEqual(4u, ReadAnimationPacket(native).Updates, "Restoration is sent only once");
            });
        }

        [EngineTest("Dual wield animation packets retain each hand and restore native combat data", Category = "DualWield", TimeoutSeconds = 60f)]
        public static async Task AnimationPacketsPreserveCombat(EngineTestContext ctx)
        {
            var (attacker, target, _, _) = await CreatePair(ctx);
            try
            {
                Combat.SetAutoAttackHitResolutionOverride(true);
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
                    foreach (var count in new[] { 2, 5, 6 })
                    {
                        var first = ResolveCycle(attacker, target, count);
                        var round = native.m_pcCombatRound;
                        var end = round.m_nCurrentAttack;
                        var saved = Enumerable.Range(first, count).Select(i => WeaponAttackAnimation.Roll.Read(round.GetAttack(i))).ToArray();
                        var animation = native.m_nAnimation;
                        var onHand = round.m_nOnHandAttacks;
                        var offHand = round.m_nOffHandAttacks;
                        var ordered = WeaponAttackAnimation.OrderForPlayback(saved);
                        foreach (var roll in ordered)
                        {
                            var duration = WeaponAttackAnimation.SwingDuration;
                            WeaponAttackAnimation.WithProjectedAttack(native, roll, duration, () =>
                            {
                                var packet = ReadAnimationPacket(native);
                                ctx.AssertEqual((ushort)9, packet.Animation, "Packet uses the native attack animation");
                                ctx.AssertEqual((byte)1, packet.Count, "One animation per packet bypasses the native three-attack limit");
                                ctx.AssertEqual(roll.Weapon, packet.Weapon, "Packet retains the intended hand even in a six-roll batch");
                                ctx.AssertEqual((ushort)duration, packet.Duration, "Packet carries the exclusive animation slot");
                            });
                        }
                        try
                        {
                            WeaponAttackAnimation.WithProjectedAttack(native, ordered[0], WeaponAttackAnimation.SwingDuration,
                                () => throw new InvalidOperationException("test serialization failure"));
                        }
                        catch (InvalidOperationException ex) when (ex.Message == "test serialization failure") { }
                        WeaponAttackAnimation.WithProjectedAttack(native, null, 0, () =>
                            ctx.AssertEqual((ushort)1, ReadAnimationPacket(native).Animation, "Playback ends in the ready pose"));
                        ctx.AssertEqual(end, round.m_nCurrentAttack, "Presentation never advances the combat cursor");
                        ctx.AssertEqual(onHand, round.m_nOnHandAttacks, "Main-hand budget is intact");
                        ctx.AssertEqual(offHand, round.m_nOffHandAttacks, "Off-hand budget is intact");
                        ctx.AssertEqual(animation, native.m_nAnimation, "Native action animation is restored");
                        for (var i = 0; i < count; i++)
                        {
                            var actual = WeaponAttackAnimation.Roll.Read(round.GetAttack(first + i));
                            ctx.AssertEqual(saved[i] with { Damage = Array.Empty<short>() },
                                actual with { Damage = Array.Empty<short>() }, "Every native attack field is restored");
                            ctx.Assert(saved[i].Damage.SequenceEqual(actual.Damage), "Every damage component is restored");
                        }
                    }
                });
            }
            finally { ResetObservation(); }
        }

        // Exercise the actual engine serializer, including the two-bit attack count.
        // This client object is local to the test and never connects or sends a packet.
        private static unsafe (ushort Animation, byte Count, byte Weapon, ushort Duration, uint Updates) ReadAnimationPacket(CNWSCreature native, uint updates = 4)
        {
            using var client = new CNWSPlayer(0xfffffffe);
            client.m_oidNWSObject = native.m_idSelf;
            using var last = new CLastUpdateObject();
            using var packetMessage = new NativePacketMessage();
            using var packetReader = new NativePacketMessage();
            var message = packetMessage.Message;
            var reader = packetReader.Message;
            message.CreateWriteMessage(1024, 0xffffffff, 1);
            message.WriteGameObjUpdate_UpdateObject(client, native, last, updates, 0);
            byte* bytes = null;
            uint size = 0;
            message.GetWriteMessage(&bytes, &size);
            if (size == 0) throw new InvalidOperationException("Engine produced no animation packet");
            // GetWriteMessage includes the three-byte transport header; the engine
            // removes it before handing incoming payloads to SetReadMessage.
            reader.SetReadMessage(bytes + 3, size - 3, 0xffffffff, 1);
            reader.ReadCHAR(8);
            reader.ReadBYTE(8, 1);
            reader.ReadOBJECTIDServer();
            var actualUpdates = reader.ReadDWORD(32);
            // The fake observer has no network version record. SatisfiesBuild is false,
            // so NWN writes the replacement flag but omits its version-gated payload.
            // ExplicitSwingVariants separately verifies the projected native clip list.
            if ((actualUpdates & 4) == 0) return (0, 0, 0, 0, actualUpdates);
            reader.ReadFLOAT(1f, 32);
            var animation = reader.ReadWORD(16);
            if (animation != 9) return (animation, 0, 0, 0, actualUpdates);
            var count = reader.ReadBYTE(2, 1);
            reader.ReadOBJECTIDServer();
            reader.ReadWORD(16); // Reaction.
            reader.ReadWORD(16); // Reaction duration.
            var duration = reader.ReadWORD(16);
            reader.ReadBYTE(4, 1); // Hit outcome.
            reader.ReadWORD(16); // Special attack type.
            reader.ReadWORD(9); // Native damage feedback.
            reader.ReadBOOL();
            reader.ReadBOOL();
            return (animation, count, reader.ReadBYTE(4, 1), duration, actualUpdates);
        }

        private sealed unsafe class NativePacketMessage : IDisposable
        {
            private void* _memory;
            public CNWSMessage Message { get; }

            public NativePacketMessage()
            {
                // The pinned 8193.37.17 engine writes a derived-class flag at offset 0x68.
                // NWNX_SWIG_DotNET's new_CNWSMessage allocates only 0x68 bytes. Allocate
                // the engine's full 0x70-byte object here; this is a test-only fixture.
                var construct = (delegate* unmanaged<void*, void>)NativeLibrary.GetExport(
                    NativeLibrary.GetMainProgramHandle(), "_ZN11CNWSMessageC1Ev");
                _memory = NativeMemory.AllocZeroed(0x70);
                construct(_memory);
                Message = CNWSMessage.FromPointer(_memory);
            }

            public void Dispose()
            {
                if (_memory == null) return;
                var destruct = (delegate* unmanaged<void*, void>)NativeLibrary.GetExport(
                    NativeLibrary.GetMainProgramHandle(), "_ZN11CNWSMessageD1Ev");
                destruct(_memory);
                NativeMemory.Free(_memory);
                _memory = null;
                Message.Dispose(); // Non-owning SWIG wrapper.
            }
        }

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

        [EngineTest("Dual wield melee deflection checks every weapon roll", Category = "DualWield", TimeoutSeconds = 60f)]
        public static Task MeleeDeflectionChecksEveryRoll(EngineTestContext ctx) =>
            AssertDeflectionChecksEveryRoll(ctx, false);

        [EngineTest("Dual wield shield deflection checks every weapon roll", Category = "DualWield", TimeoutSeconds = 60f)]
        public static Task ShieldDeflectionChecksEveryRoll(EngineTestContext ctx) =>
            AssertDeflectionChecksEveryRoll(ctx, true);

        private static async Task AssertDeflectionChecksEveryRoll(EngineTestContext ctx, bool useShield)
        {
            var (attacker, _, _, _) = await CreatePair(ctx);
            var defender = ctx.SpawnCreature("nw_bandit001", 1f);
            await ctx.WaitFrameAsync();
            await ctx.EquipItemAsync(defender, "nw_wswss001", InventorySlot.RightHand);
            if (useShield)
                await ctx.EquipItemAsync(defender, "qk_shield", InventorySlot.LeftHand);
            try
            {
                Combat.SetAutoAttackHitResolutionOverride(true);
                _observedAttacker = attacker;
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    ctx.MakeHostile(defender);
                    ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), defender, 60f);
                    TemporaryStatModifier.Add(defender, StatType.MeleeDeflection, 100, 60f);
                    TemporaryStatModifier.Add(defender, StatType.MeleeDeflectionChanceCap, 100, 60f);
                    if (useShield)
                        TemporaryStatModifier.Add(defender, StatType.ShieldDeflection, 100, 60f);
                    var defenderNative = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(defender).AsNWSCreature();
                    ctx.AssertEqual(useShield ? 0 : 100, Stat.GetMeleeDeflectionChanceNative(defenderNative),
                        "Shield replaces melee deflection rather than stacking with it");
                    ctx.AssertEqual(useShield ? Stat.MaximumShieldDeflectionChance : 0,
                        Stat.GetShieldDeflectionChanceNative(defenderNative), "Expected shield deflection chance");

                    var round = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature().m_pcCombatRound;
                    foreach (var attacks in new[] { 2, 6 })
                    {
                        _hits.Clear();
                        // This seed puts the first 32 D100 rolls below the shield's 75% cap.
                        // Resolution is synchronous, so other creatures cannot consume the sequence.
                        Service.Random.SetSeed(7197);
                        var firstAttack = ResolveCycle(attacker, defender, attacks);
                        for (var index = firstAttack; index < firstAttack + attacks; index++)
                            ctx.AssertEqual(2, (int)round.GetAttack(index).m_nAttackResult,
                                $"Roll {index - firstAttack + 1} of {attacks} is independently deflected");
                        ctx.AssertEqual(0, _hits.Count, "Neither hand deals damage through a successful deflection");
                    }
                });
            }
            finally
            {
                Service.Random.ResetSeed();
                AssignCommand(attacker, () => ClearAllActions());
                ResetObservation();
            }
        }

        private static int ResolveCycle(uint attacker, uint target, int attacks = 2)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
            var round = native.m_pcCombatRound;
            round.StartCombatRound(target);
            var firstAttack = round.m_nCurrentAttack;
            var count = WeaponAttackCycle.PrepareDualWieldAttacks(round, attacks);
            StatusEffect.BeginNativeAttackSwing(attacker);
            try { native.ResolveAttack(target, count, Combat.BaseAttackDelayMilliseconds); }
            finally { StatusEffect.EndNativeAttackSwing(attacker); }
            return firstAttack;
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
