using System;
using System.Collections.Generic;
using System.Linq;
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

        [EngineTest("Canonical pistol ammunition survives equipment and save cycles", Category = "PistolPresentation")]
        public static Task CanonicalPistolSurvivesEquipmentAndSave(EngineTestContext ctx) => VerifyEquipmentCycles(ctx);

        private static async Task VerifyEquipmentCycles(EngineTestContext ctx)
        {
            var original = ctx.SpawnCreature("nw_bandit001");
            await ctx.WaitFrameAsync();
            await ctx.EquipItemAsync(original, "exchangepistol", InventorySlot.RightHand);
            var serialized = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(original, () => serialized = ObjectPlugin.Serialize(original));
            var creature = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(original,
                () => creature = ObjectPlugin.Deserialize(serialized));
            ctx.Assert(GetIsObjectValid(creature), "Equipment fixture deserializes");
            ctx.Track(creature);
            ObjectPlugin.AddToArea(creature, ctx.Arena, GetPositionFromLocation(ctx.GetArenaLocation(0f, 0f)));
            await ctx.WaitFrameAsync();
            var weapon = GetItemInSlot(InventorySlot.RightHand, creature);
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                ctx.AssertEqual(0, native.m_bMagicalArrowsEquipped, "The pistol generates no arrows");
                ctx.AssertEqual(1, native.m_bMagicalBulletsEquipped, "The pistol generates bullets");
                CreaturePlugin.RunUnequip(creature, weapon);
                ctx.Assert(GetItemInSlot(InventorySlot.RightHand, creature) != weapon, "The pistol can be unequipped");
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
            ctx.Assert(GetIsObjectValid(reloaded), "Pistol save deserializes");
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

        private static async Task VerifyAttack(EngineTestContext ctx, bool shield, bool player = false)
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
            try
            {
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
                    $"pc={GetIsPC(attacker)} attacker={ObjectToString(attacker)} target={ObjectToString(target)} " +
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
