using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NWNXLib = NWN.Native.API.NWNXLib;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class KatarAnimationRemapEngineTests
    {
        private const string Category = "KatarAnimationRemap";

        [EngineTest("A character saved with the katar remap and no katar gets its one-handed animations back", Category = Category, TimeoutSeconds = 90f)]
        public static async Task SavedRemapIsRemovedForOneHandedWeapon(EngineTestContext ctx)
        {
            byte[] characterFile = null;
            Player record;
            using (var fixture = await PlayerAbilityFixture.CreateAsync(ctx))
            {
                var player = fixture.Creature;
                var katar = await ctx.EquipItemAsync(player, "t_katar", InventorySlot.RightHand);
                await ctx.WaitUntilAsync(() => HasFullRemap(player), 5f, "the katar remap on equip");

                // The unequip event still sees the katar in the slot, so an emptied hand keeps the
                // unarmed animations, which is correct for fists.
                AssignCommand(player, () => ActionUnequipItem(katar));
                await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, player) == OBJECT_INVALID, 5f, "the katar unequip");
                await ctx.WaitFrameAsync();
                ctx.Assert(HasFullRemap(player), "Empty hands keep the unarmed animations");

                await ctx.ExecuteInCreatureContextAsync(player, () => characterFile = SavedCharacterMigration.SaveFile(player));
                record = SavedCharacterMigration.CopyRecord(DB.Get<Player>(fixture.Id));
            }

            var saved = System.Text.Encoding.ASCII.GetString(characterFile);
            ctx.Assert(saved.Contains("AnimationReplace") && saved.Contains("nwreadyr"),
                "The character file keeps the remap after the katar is gone");

            using var login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, characterFile, record, 3f);
            var character = login.Creature;
            // The server vault restores saved mappings when it loads the character after a restart;
            // this harness load does not, so install them as that login does. No session state comes back.
            await ctx.ExecuteInCreatureContextAsync(character, () =>
            {
                foreach (var (source, replacement) in KatarAnimationRemapPlan.Pairs)
                    ReplaceObjectAnimation(character, source, replacement);
            });
            ctx.Assert(HasFullRemap(character), "The loaded character has the saved remap and no katar");

            await ctx.ExecuteInCreatureContextAsync(character, () => KatarAnimationRemap.SynchronizeOnLogin(character));
            ctx.AssertEqual(0, KatarMappings(character).Length, "Login removes the saved remap before any equipment change");

            // Equipment changes catch saved mappings on their own as well.
            await ctx.ExecuteInCreatureContextAsync(character, () =>
            {
                foreach (var (source, replacement) in KatarAnimationRemapPlan.Pairs)
                    ReplaceObjectAnimation(character, source, replacement);
            });

            await ctx.EquipItemAsync(character, "nw_wswls001", InventorySlot.RightHand);
            await ctx.WaitFrameAsync();
            var remaining = KatarMappings(character);
            ctx.AssertEqual(0, remaining.Length,
                $"A one-handed weapon uses its own animations, not unarmed ones (left: {string.Join(", ", remaining)})");
        }

        [EngineTest("A queued katar swing returns to the unarmed animations when it ends", Category = Category, TimeoutSeconds = 60f)]
        public static async Task QueuedSwingRestoresKatarRemap(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            await ctx.EquipItemAsync(player, "t_katar", InventorySlot.RightHand);
            await ctx.WaitUntilAsync(() => HasFullRemap(player), 5f, "the katar remap on equip");

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                QueuedAttackAnimation.Begin(player, AuthoredAnimation.DoubleStrike);
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSObject();
                ctx.AssertEqual(AuthoredAnimation.DoubleStrike.Name,
                    WeaponAttackAnimation.ReadAnimationReplacements(native).GetValueOrDefault("1hslashl"),
                    "The queued ability owns the swing while it waits");
                ctx.AssertEqual("nwreadyr", WeaponAttackAnimation.ReadAnimationReplacements(native).GetValueOrDefault("1hreadyr"),
                    "The ready pose stays unarmed");

                QueuedAttackAnimation.Stop(player);
                ctx.Assert(HasFullRemap(player), "The swing keys return to the unarmed animations");

                // A character saved mid swing brings the clip back without its playback.
                ReplaceObjectAnimation(player, "1hslashl", AuthoredAnimation.DoubleStrike.Name);
                KatarAnimationRemap.SynchronizeOnLogin(player);
                ctx.Assert(HasFullRemap(player), "Login replaces a saved queued clip that has no playback");
            });
        }

        private static bool HasFullRemap(uint creature) =>
            KatarMappings(creature).Length == KatarAnimationRemapPlan.Pairs.Count;

        private static string[] KatarMappings(uint creature)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSObject();
            var current = WeaponAttackAnimation.ReadAnimationReplacements(native);
            return KatarAnimationRemapPlan.Pairs
                .Where(pair => current.TryGetValue(pair.Source, out var replacement) && replacement == pair.Replacement)
                .Select(pair => pair.Source)
                .ToArray();
        }
    }
}
