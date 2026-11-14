using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class SavedAnimationMappingsEngineTests
    {
        private const string Category = "SavedAnimationMappings";

        // Mappings a character saved mid-playback brings back with no session state to release them.
        private static readonly (string Source, string Replacement)[] Orphans =
        {
            ("1hslashl", AuthoredAnimation.DoubleStrike.Name), // queued weapon ability swing
            ("2hstab", AuthoredAnimation.DoubleStrike.Name),
            ("custom1lp", AuthoredAnimation.SnapRushTechnique.Name), // immediate ability clip
            ("run", "sw_whir_guar"), // weapon repositioning travel
            ("run_swordr", NamedAnimationPlayback.NoHoldName),
            (PistolAnimationRemap.SlingAttackAnimation, PistolAnimationRemap.FormerPistolAttackAnimation),
        };

        [EngineTest("Login clears animation mappings a saved character brought back", Category = Category, TimeoutSeconds = 60f)]
        public static async Task LoginClearsOrphanedMappings(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                var saved = KatarAnimationRemapPlan.Pairs.Concat(Orphans).ToArray();
                Install(player, saved);
                SetLocalString(player, "_NAMED_ANIMATION_PLAYBACK", "saved-playback");
                ctx.Assert(NamedAnimation.IsPlaybackActive(player), "The saved playback token is present");
                ctx.AssertEqual(saved.Select(mapping => mapping.Source).Distinct().Count(), Read(player).Count, "The orphaned mappings are installed");

                SavedAnimationMappings.RestoreOnLogin(player);
                ctx.AssertEqual(0, Read(player).Count, "Empty hands keep no saved mapping");
                ctx.Assert(!NamedAnimation.IsPlaybackActive(player), "Login releases the saved playback token");
            });
        }

        [EngineTest("Login keeps only the equipment remaps the current gear needs", Category = Category, TimeoutSeconds = 60f)]
        public static async Task LoginReappliesEquipmentRemaps(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;

            await ctx.EquipItemAsync(player, "t_katar", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                Install(player, Orphans);
                SavedAnimationMappings.RestoreOnLogin(player);
                var mappings = Read(player);
                ctx.AssertEqual(KatarAnimationRemapPlan.Pairs.Count, mappings.Count, "A katar keeps only its unarmed remap");
                ctx.Assert(KatarAnimationRemapPlan.Pairs.All(pair => mappings.GetValueOrDefault(pair.Source) == pair.Replacement),
                    "Every katar key, including the saved swing clip's, uses its unarmed animation");
            });

            var katar = GetItemInSlot(InventorySlot.RightHand, player);
            AssignCommand(player, () => ActionUnequipItem(katar));
            await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.RightHand, player) == OBJECT_INVALID, 5f, "the katar unequip");
            await ctx.EquipItemAsync(player, "b_pistol", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                Install(player, Orphans.Concat(KatarAnimationRemapPlan.Pairs));
                SavedAnimationMappings.RestoreOnLogin(player);
                var mappings = Read(player);
                ctx.AssertEqual(1, mappings.Count, $"A pistol keeps only its attack remap ({string.Join(", ", mappings.Keys)})");
                ctx.AssertEqual(PistolAnimationRemap.FormerPistolAttackAnimation,
                    mappings.GetValueOrDefault(PistolAnimationRemap.SlingAttackAnimation), "The pistol attack remap is reapplied");
            });
        }

        [EngineTest("Login restores a selected rifle profile after saved named playback", Category = Category, TimeoutSeconds = 60f)]
        public static async Task LoginRestoresRifleAfterSavedPlayback(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                SetCreatureAppearanceType(player, AppearanceType.Human);
                SetGender(player, Gender.Male);
                SetPhenoType(PhenoType.Normal, player);
                var armor = GetItemInSlot(InventorySlot.Chest, player);
                foreach (var part in new[] { CreaturePart.RightHand, CreaturePart.LeftHand })
                {
                    SetCreatureBodyPart(part, 1, player);
                    if (GetIsObjectValid(armor))
                        EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)part, 0);
                }
                if (GetIsObjectValid(armor))
                {
                    EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
                    EquippedItemAppearance.Refresh(player, armor);
                }
            });
            var rifle = await ctx.EquipItemAsync(player, "b_rifle", InventorySlot.RightHand);
            await ctx.WaitUntilAsync(() => Read(player).GetValueOrDefault(NamedAnimationPlayback.HoldCarrierSource, "") !=
                NamedAnimationPlayback.NoHoldName, 5f, "the existing equipment hold clear");
            var field = typeof(RifleSupportAnimation).GetField("_catalog", BindingFlags.Static | BindingFlags.NonPublic)!;
            var previous = field.GetValue(null);
            var poses = new RifleSupportPoses("rf_fixture_i", "rf_fixture_r", "rf_fixture_s");
            try
            {
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                {
                    foreach (var part in new[] { AppearanceWeapon.Top, AppearanceWeapon.Bottom })
                    {
                        EquippedItemAppearance.Set(rifle, ItemAppearanceType.WeaponModel, (int)part, 1);
                        EquippedItemAppearance.Set(rifle, ItemAppearanceType.WeaponColor, (int)part, 1);
                    }
                    var handModel = typeof(WeaponGripHandRenderer).GetMethod("GetHandModel", BindingFlags.Static | BindingFlags.NonPublic)!;
                    var weaponPart = typeof(WeaponFit).GetMethod("GetPart", BindingFlags.Static | BindingFlags.NonPublic)!;
                    var rows = new[]
                    {
                        ((string)handModel.Invoke(null, new object[] { player, CreaturePart.RightHand, "r" })!,
                            (string)handModel.Invoke(null, new object[] { player, CreaturePart.LeftHand, "l" })!,
                            (int)weaponPart.Invoke(null, new object[] { rifle, AppearanceWeapon.Middle })!, poses)
                    };
                    field.SetValue(null, Activator.CreateInstance(field.FieldType, BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new object[] { rows }, null));
                    SetLocalString(player, "_NAMED_ANIMATION_PLAYBACK", "saved-rifle-playback");
                    ReplaceObjectAnimation(player, "xbowr", NamedAnimationPlayback.NoHoldName);
                    ReplaceObjectAnimation(player, "custom1lp", "sw_aimedshot");

                    SavedAnimationMappings.RestoreOnLogin(player);

                    ctx.Assert(!NamedAnimation.IsPlaybackActive(player), "Login discards persisted playback ownership");
                    var mappings = Read(player);
                    ctx.AssertEqual(poses.Idle, mappings.GetValueOrDefault("xbowr"), "The selected idle mapping is reapplied");
                    ctx.AssertEqual(poses.Ready, mappings.GetValueOrDefault("xbowrdy"), "The selected ready mapping is reapplied");
                    ctx.AssertEqual(poses.Shot, mappings.GetValueOrDefault("xbowshot"), "The selected firing mapping is reapplied");
                    ctx.Assert(!mappings.ContainsKey("custom1lp"), "The saved ability mapping is removed");
                });
            }
            finally
            {
                field.SetValue(null, previous);
                await ctx.ExecuteInCreatureContextAsync(player, () => RifleSupportAnimation.Apply(player));
            }
        }

        private static void Install(uint creature, IEnumerable<(string Source, string Replacement)> mappings)
        {
            foreach (var (source, replacement) in mappings)
                ReplaceObjectAnimation(creature, source, replacement);
        }

        private static Dictionary<string, string> Read(uint creature) =>
            WeaponAttackAnimation.ReadAnimationReplacements(
                NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSObject());
    }
}
