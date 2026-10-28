using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;
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
                ctx.AssertEqual(saved.Select(mapping => mapping.Source).Distinct().Count(), Read(player).Count, "The orphaned mappings are installed");

                SavedAnimationMappings.RestoreOnLogin(player);
                ctx.AssertEqual(0, Read(player).Count, "Empty hands keep no saved mapping");
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
