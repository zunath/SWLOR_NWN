using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PistolPresentationEngineTests
    {
        private static uint _target = OBJECT_INVALID;
        private static readonly List<(uint Receiver, int Type, uint SpellId, uint Delta, int AttackResult, int Path)> Projectiles = new();

        [EngineTest("Canonical pistol attack without a shield", Category = "PistolPresentation")]
        public static Task CanonicalPistol(EngineTestContext ctx) => VerifyAttack(ctx, false);

        [EngineTest("Canonical pistol attack with a shield", Category = "PistolPresentation")]
        public static Task ShieldedPistol(EngineTestContext ctx) => VerifyAttack(ctx, true);

        [EngineTest("Canonical player pistol attack without a shield", Category = "PistolPresentation")]
        public static Task PlayerCanonicalPistol(EngineTestContext ctx) => VerifyAttack(ctx, false, true);

        [EngineTest("Canonical player pistol attack with a shield", Category = "PistolPresentation")]
        public static Task PlayerShieldedPistol(EngineTestContext ctx) => VerifyAttack(ctx, true, true);

        [EngineTest("Saved legacy player pistol attack without a shield", Category = "PistolPresentation")]
        public static Task MigratedPlayerPistol(EngineTestContext ctx) => VerifyAttack(ctx, false, true, true);

        [EngineTest("Saved legacy player pistol attack with a shield", Category = "PistolPresentation")]
        public static Task MigratedPlayerShieldedPistol(EngineTestContext ctx) => VerifyAttack(ctx, true, true, true);

        [EngineTest("Saved pistol ammunition repair survives equipment and save cycles", Category = "PistolPresentation")]
        public static Task RepairedPistolSurvivesEquipmentAndSave(EngineTestContext ctx) => VerifyEquipmentCycles(ctx, true);

        [EngineTest("Canonical pistol ammunition survives equipment and save cycles", Category = "PistolPresentation")]
        public static Task CanonicalPistolSurvivesEquipmentAndSave(EngineTestContext ctx) => VerifyEquipmentCycles(ctx, false);

        private static async Task VerifyEquipmentCycles(EngineTestContext ctx, bool legacy)
        {
            var original = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            await ctx.EquipItemAsync(original, "exchangepistol", InventorySlot.RightHand);
            var serialized = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(original, () => serialized = ObjectPlugin.Serialize(original));
            var creature = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(original,
                () => creature = ObjectPlugin.Deserialize(legacy ? WithLegacyWeaponBaseItem(serialized) : serialized));
            ctx.Assert(GetIsObjectValid(creature), "Legacy equipment fixture deserializes");
            ctx.Track(creature);
            ObjectPlugin.AddToArea(creature, ctx.Arena, GetPositionFromLocation(ctx.GetArenaLocation(0f, 0f)));
            await ctx.WaitFrameAsync();
            var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                PistolBaseItemCompatibility.Normalize(weapon);
                ctx.AssertEqual(0, native.m_bMagicalArrowsEquipped, "Repair removes legacy generated arrows");
                ctx.AssertEqual(1, native.m_bMagicalBulletsEquipped, "Repair supplies generated bullets");
                CreaturePlugin.RunUnequip(creature, weapon);
                ctx.Assert(GetItemInSlot(InventorySlot.RightHand, creature) != weapon, "Repaired pistol can be unequipped");
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                ctx.AssertEqual(0, native.m_bMagicalBulletsEquipped, "Unequipping clears generated bullets normally");
                CreaturePlugin.RunEquip(creature, weapon, InventorySlot.RightHand);
                ctx.AssertEqual(weapon, GetItemInSlot(InventorySlot.RightHand, creature), "Same pistol re-equips");
                ctx.AssertEqual(0, native.m_bMagicalArrowsEquipped, "Re-equipping keeps arrows clear");
                ctx.AssertEqual(1, native.m_bMagicalBulletsEquipped, "Re-equipping supplies generated bullets");
                serialized = ObjectPlugin.Serialize(creature);
            });
            var reloaded = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(creature, () => reloaded = ObjectPlugin.Deserialize(serialized));
            ctx.Assert(GetIsObjectValid(reloaded), "Repaired pistol save deserializes");
            ctx.Track(reloaded);
            ObjectPlugin.AddToArea(reloaded, ctx.Arena, GetPositionFromLocation(ctx.GetArenaLocation(0f, 0f)));
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(reloaded, () =>
            {
                var reloadedNative = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(reloaded).AsNWSCreature();
                var reloadedWeapon = GetItemInSlot(InventorySlot.RightHand, reloaded);
                ctx.AssertEqual(BaseItem.Sling, GetBaseItemType(reloadedWeapon), "Save retains the canonical pistol base");
                ctx.AssertEqual(0, reloadedNative.m_bMagicalArrowsEquipped, "Canonical save has no generated arrows");
                ctx.AssertEqual(1, reloadedNative.m_bMagicalBulletsEquipped, "Canonical save generates bullets");
                ctx.Assert(!PistolBaseItemCompatibility.Normalize(reloadedWeapon), "Canonical reload needs no further repair");
                CreaturePlugin.RunUnequip(reloaded, reloadedWeapon);
                ctx.Assert(GetItemInSlot(InventorySlot.RightHand, reloaded) != reloadedWeapon,
                    "Reloaded pistol can be unequipped");
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(reloaded, () =>
            {
                var reloadedNative = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(reloaded).AsNWSCreature();
                ctx.AssertEqual(0, reloadedNative.m_bMagicalArrowsEquipped, "Reloaded unequip leaves no generated arrows");
                ctx.AssertEqual(0, reloadedNative.m_bMagicalBulletsEquipped, "Reloaded unequip clears regenerated bullets");
            });
        }

        [EngineTest("Legacy pistol ammo repair preserves a real equipped bullet stack", Category = "PistolPresentation")]
        public static async Task RepairLegacyPistolPreservesEquippedBullets(EngineTestContext ctx)
        {
            const string AmmoMarker = "PISTOL_PRESENTATION_REAL_AMMO";
            ctx.SeedRandom(20260721);
            var originalCreature = ctx.SpawnCreature("civilian");
            await ctx.WaitFrameAsync();
            var originalAmmo = await ctx.EquipItemAsync(originalCreature, "blaster_bullets", InventorySlot.Bullets);
            var originalWeapon = await ctx.EquipItemAsync(originalCreature, "b_longsword", InventorySlot.RightHand);
            var originalQuantity = GetItemStackSize(originalAmmo);

            var serialized = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(originalCreature, () =>
            {
                SetLocalString(originalAmmo, AmmoMarker, "preserve");
                AddItemProperty(DurationType.Permanent, ItemPropertyUnlimitedAmmo(), originalWeapon);
                serialized = ObjectPlugin.Serialize(originalCreature);
            });

            var creature = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(originalCreature,
                () => creature = ObjectPlugin.Deserialize(WithLegacyWeaponBaseItem(serialized)));
            ctx.Assert(GetIsObjectValid(creature), "Legacy pistol ammunition fixture deserializes");
            ctx.Track(creature);
            ObjectPlugin.AddToArea(creature, ctx.Arena, GetPositionFromLocation(ctx.GetArenaLocation(0f, 0f)));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(ctx.Arena, GetArea(creature), "Loaded creature is placed in the test arena");
            await ctx.ExecuteInCreatureContextAsync(originalCreature, () => DestroyObject(originalCreature));
            await ctx.WaitFrameAsync();

            var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
            ctx.Assert(GetIsObjectValid(weapon), "Loaded creature retains its equipped legacy weapon");
            ctx.AssertEqual(BaseItem.Sling, GetBaseItemType(weapon), "Normal acquire handling canonicalizes the saved legacy base");
            var ammo = FindMarkedAmmo(creature, AmmoMarker);
            ctx.Assert(GetIsObjectValid(ammo), "The saved real bullet stack is still present");
            var ammoPointerBeforeRepair = ammo;
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
            var initialAmmoFlags = $"arrows={native.m_bMagicalArrowsEquipped} bullets={native.m_bMagicalBulletsEquipped}";

            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                var needsRepair = native.m_bMagicalArrowsEquipped != 0 && native.m_bMagicalBulletsEquipped == 0;
                ctx.Assert(needsRepair || (native.m_bMagicalArrowsEquipped == 0 && native.m_bMagicalBulletsEquipped != 0),
                    $"Saved pistol has stale or already-repaired ammunition ({initialAmmoFlags}; real ammo={ObjectToString(ammo)})");
                ctx.AssertEqual(needsRepair, PistolBaseItemCompatibility.Normalize(weapon),
                    "The normalizer repairs stale ammunition and leaves an already-repaired load unchanged");
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                ctx.AssertEqual(0, native.m_bMagicalArrowsEquipped, "Repair clears generated arrows");
                ctx.AssertEqual(1, native.m_bMagicalBulletsEquipped, "Repair supplies generated bullets");
                ctx.AssertEqual(creature, GetItemPossessor(ammo), "The real bullet stack remains owned by the creature");
                ctx.AssertEqual(originalQuantity, GetItemStackSize(ammo), "The real bullet stack keeps its complete quantity");
                ctx.AssertEqual(ammoPointerBeforeRepair, FindMarkedAmmo(creature, AmmoMarker),
                    "The repair preserves the original real bullet item");

                var equippedAmmo = GetItemInSlot(InventorySlot.Bullets, creature);
                if (equippedAmmo != ammo)
                {
                    var nativeAmmo = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(ammo).AsNWSItem();
                    ctx.Assert(native.m_pcItemRepository.GetItemInRepository(nativeAmmo) != 0,
                        "A displaced real bullet stack is preserved in the creature's inventory repository");
                }

                ctx.SetResultDetail($"initial=[{initialAmmoFlags}] realAmmo={ObjectToString(ammo)} " +
                                    $"quantity={GetItemStackSize(ammo)} equipped={equippedAmmo == ammo} " +
                                    $"arrows={native.m_bMagicalArrowsEquipped} bullets={native.m_bMagicalBulletsEquipped}");
            });
        }

        private static async Task VerifyAttack(EngineTestContext ctx, bool shield, bool player = false, bool migrateLegacy = false)
        {
            ctx.SeedRandom(20260721);
            var attacker = ctx.SpawnCreature("nw_bandit001", -0.5f, 0f);
            var target = ctx.SpawnCreature("nw_rat001", 1f, 0f);
            ctx.MakeHostile(target);
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(1000), target, 3600f);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneImmobilize(), target, 3600f);
            await ctx.WaitFrameAsync();
            if (shield)
                await ctx.EquipItemAsync(attacker, "byysk_shield002", InventorySlot.LeftHand);
            var weapon = await ctx.EquipItemAsync(attacker, "exchangepistol", InventorySlot.RightHand);
            var savedWeaponBase = GetBaseItemType(weapon);
            if (migrateLegacy)
            {
                savedWeaponBase = BaseItem.Pistol;
                var originalAttacker = attacker;
                var serialized = string.Empty;
                await ctx.ExecuteInCreatureContextAsync(originalAttacker,
                    () => serialized = ObjectPlugin.Serialize(originalAttacker));
                await ctx.ExecuteInCreatureContextAsync(originalAttacker,
                    () => attacker = ObjectPlugin.Deserialize(WithLegacyWeaponBaseItem(serialized)));
                ctx.Assert(GetIsObjectValid(attacker), "Legacy equipped player fixture deserializes");
                ctx.Track(attacker);
                ObjectPlugin.AddToArea(attacker, ctx.Arena, GetPositionFromLocation(ctx.GetArenaLocation(-0.5f, 0f)));
                await ctx.WaitFrameAsync();
                ctx.AssertEqual(ctx.Arena, GetArea(attacker), "Loaded player is placed in the test arena");
                weapon = GetItemInSlot(InventorySlot.RightHand, attacker);
                ctx.Assert(GetIsObjectValid(weapon), "Loaded player retains the equipped pistol");
                ctx.MakeHostile(target);
                await ctx.ExecuteInCreatureContextAsync(originalAttacker,
                    () => DestroyObject(originalAttacker));
                await ctx.WaitFrameAsync();
            }
            ctx.Assert(GetIsObjectValid(weapon), "Fixture has an equipped pistol");
            ctx.AssertEqual("exchangepistol", GetResRef(weapon),
                "Fixture uses its intended pistol blueprint");
            ctx.AssertEqual(BaseItem.Sling, GetBaseItemType(weapon), "Fixture uses its intended pistol carrier");
            ctx.AssertEqual(!shield, GetLocalBool(attacker, "PISTOL_ANIM_REMAP_ACTIVE"), "Fixture uses the correct animation path");
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSCreature();
            ctx.AssertEqual(1, native.GetRangeWeaponEquipped(), "Fixture has a native ranged weapon");
            var wasPlayer = native.m_bPlayerCharacter;
            global::NWN.Native.API.CNWSPlayer client = null;
            string playerId = null;
            var legacyAmmoState = "n/a";
            try
            {
                if (migrateLegacy)
                {
                    await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                    {
                        legacyAmmoState = $"savedBase={(int)savedWeaponBase} loadedBase={(int)GetBaseItemType(weapon)} " +
                                          $"arrows={native.m_bMagicalArrowsEquipped}/bullets={native.m_bMagicalBulletsEquipped}";
                        PistolBaseItemCompatibility.Normalize(weapon);
                    });
                    await ctx.WaitFrameAsync();
                    await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                    {
                        ctx.AssertEqual(BaseItem.Sling, GetBaseItemType(weapon), "Loaded legacy pistol is canonicalized by the normal acquire pipeline");
                        ctx.AssertEqual(0, native.m_bMagicalArrowsEquipped,
                            $"Converted pistol clears generated legacy arrows ({legacyAmmoState}; after bullets={native.m_bMagicalBulletsEquipped})");
                        ctx.AssertEqual(1, native.m_bMagicalBulletsEquipped,
                            $"Converted pistol supplies native bullets ({legacyAmmoState})");
                        ctx.Assert(!PistolBaseItemCompatibility.Normalize(weapon), "Retry leaves the repaired pistol unchanged");
                    });
                }

                if (player)
                {
                    playerId = GetObjectUUID(attacker);
                    var record = new Player(playerId);
                    foreach (var skillType in Enum.GetValues<SkillType>())
                    {
                        if (skillType != SkillType.Invalid)
                            record.Skills[skillType] = new PlayerSkill();
                    }
                    DB.Set(record);

                    native.m_bPlayerCharacter = 1;
                    client = new global::NWN.Native.API.CNWSPlayer(shield ? 0x7fff0101u : 0x7fff0100u);
                    client.SetGameObject(NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(attacker).AsNWSObject());
                    client.m_oidPCObject = attacker;
                    NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Add(client);
                    ctx.Assert(GetIsPC(attacker), "Synthetic player fixture exercises the native PC attack branch");
                    ctx.AssertEqual(1, native.GetRangeWeaponEquipped(), "PC attack uses the native ranged-weapon path");
                    await ctx.WaitUntilAsync(() => native.GetVisibleListElement(target) != null,
                        10f, "the synthetic player's native perception of the target");
                }
                else
                {
                    ctx.Assert(!GetIsPC(attacker), "NPC fixture exercises the skill-based ranged attack branch");
                }

                _target = target;
                Projectiles.Clear();
                AssignCommand(attacker, () => ActionAttack(target));
                try
                {
                    await ctx.WaitUntilAsync(() => Projectiles.Count > 0,
                        20f, "the pistol's native projectile broadcast through the production whitelist");
                }
                catch (EngineTestAssertionException ex)
                {
                    throw new EngineTestAssertionException(
                        $"{ex.Message} (pc={GetIsPC(attacker)}, shield={shield}, action={GetCurrentAction(attacker)}, " +
                        $"targetHP={GetCurrentHitPoints(target)}, distance={GetDistanceBetween(attacker, target)}, " +
                        $"visible={native.GetVisibleListElement(target) != null}, dead={native.GetDead()}, " +
                        $"dying={native.GetIsPCDying()}, aiState={native.m_nAIState}, " +
                        $"ranged={native.GetRangeWeaponEquipped()}, bullets={native.m_bMagicalBulletsEquipped})");
                }
                ctx.Assert(Projectiles.All(p => p.Receiver == attacker), "Projectile callbacks identify the shooter");
                ctx.Assert(Projectiles.All(p => p.Type >= 0 && p.Type <= 5), "Weapon projectiles pass the configured category filter");
                ctx.SetResultDetail($"base={GetBaseItemType(weapon)} shield={shield} remap={GetLocalBool(attacker, "PISTOL_ANIM_REMAP_ACTIVE")} " +
                    $"arrows={native.m_bMagicalArrowsEquipped} bullets={native.m_bMagicalBulletsEquipped} hp={GetCurrentHitPoints(target)} " +
                    $"pc={GetIsPC(attacker)} migratedLegacy={migrateLegacy} legacyAmmo=[{legacyAmmoState}] attacker={ObjectToString(attacker)} target={ObjectToString(target)} " +
                    $"projectiles=[{string.Join(", ", Projectiles.Select(p => $"receiver={ObjectToString(p.Receiver)} type={p.Type} spell={p.SpellId} delta={p.Delta} result={p.AttackResult} path={p.Path}"))}]");
            }
            finally
            {
                AssignCommand(attacker, () => ClearAllActions());
                try
                {
                    await ctx.WaitFrameAsync();
                }
                finally
                {
                    _target = OBJECT_INVALID;
                    Projectiles.Clear();
                    if (client != null)
                    {
                        NWNXLib.g_pAppManager.m_pServerExoApp.GetPlayerList().Remove(client);
                        client.m_oidPCObject = OBJECT_INVALID;
                        client.Dispose();
                    }
                    native.m_bPlayerCharacter = wasPlayer;
                    if (playerId != null)
                        DB.Delete<Player>(playerId);
                }
            }
        }

        private static string WithLegacyWeaponBaseItem(string serialized)
        {
            var data = Convert.FromBase64String(serialized);
            uint Read(int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));
            var structs = (int)Read(8);
            var fields = (int)Read(16);
            var labels = (int)Read(24);
            var fieldIndices = (int)Read(40);
            var listIndices = (int)Read(48);

            int FindField(int node, string name)
            {
                var first = Read(structs + node * 12 + 4);
                var count = Read(structs + node * 12 + 8);
                for (var index = 0; index < count; index++)
                {
                    var fieldIndex = count == 1
                        ? first
                        : Read(fieldIndices + (int)first + (int)index * 4);
                    var field = fields + (int)fieldIndex * 12;
                    var label = Encoding.ASCII.GetString(data, labels + (int)Read(field + 4) * 16, 16)
                        .TrimEnd('\0');
                    if (label == name)
                        return field;
                }

                throw new InvalidOperationException($"Legacy pistol fixture is missing field {name}.");
            }

            var listField = FindField(0, "Equip_ItemList");
            var list = listIndices + (int)Read(listField + 8);
            var rightHandMask = 1u << (int)InventorySlot.RightHand;
            for (var index = 0; index < Read(list); index++)
            {
                var node = (int)Read(list + 4 + (int)index * 4);
                if (Read(structs + node * 12) != rightHandMask)
                    continue;

                BinaryPrimitives.WriteUInt32LittleEndian(
                    data.AsSpan(FindField(node, "BaseItem") + 8, 4),
                    (uint)BaseItem.Pistol);
                return Convert.ToBase64String(data);
            }

            throw new InvalidOperationException("Legacy pistol fixture has no right-hand equipment entry.");
        }

        private static uint FindMarkedAmmo(uint creature, string marker)
        {
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (GetIsObjectValid(item) && GetLocalString(item, marker) == "preserve")
                    return item;
            }

            for (var item = GetFirstItemInInventory(creature);
                 GetIsObjectValid(item);
                 item = GetNextItemInInventory(creature))
            {
                if (GetLocalString(item, marker) == "preserve")
                    return item;
            }

            return OBJECT_INVALID;
        }

        [NWNEventHandler(ScriptName.OnBroadcastSafeProjectileBefore)]
        public static void ObserveProjectile()
        {
            if (_target == OBJECT_INVALID || StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID")) != _target)
                return;
            Projectiles.Add((
                OBJECT_SELF,
                int.Parse(EventsPlugin.GetEventData("PROJECTILE_TYPE")),
                uint.Parse(EventsPlugin.GetEventData("SPELL_ID")),
                uint.Parse(EventsPlugin.GetEventData("DELTA")),
                int.Parse(EventsPlugin.GetEventData("ATTACK_RESULT")),
                int.Parse(EventsPlugin.GetEventData("PROJECTILE_PATH_TYPE"))));
        }
    }
}
