using System.Reflection;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using AssociateType = SWLOR.NWN.API.NWScript.Enum.Associate.AssociateType;
using ImmunityType = SWLOR.NWN.API.NWScript.Enum.ImmunityType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class VitalityHitPointsEngineTests
    {
        private const int MissingHP = 75;

        [EngineTest("Cached Vitality is discarded when native player stats are destroyed", Category = "VitalityHitPoints", TimeoutSeconds = 30f)]
        public static Task CacheLifetime(EngineTestContext ctx)
        {
            var states = (System.Collections.IDictionary)typeof(Native.VitalityHitPoints)
                .GetField("PlayerStates", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var originalCount = states.Count;
            for (var cycle = 0; cycle < 3; cycle++)
            {
                using (var creature = new CNWSCreature(OBJECT_INVALID, 0, 1))
                {
                    creature.m_pStats.m_bIsPC = 1;
                    creature.m_pStats.SetCONBase(27, 0);
                    creature.GetMaxHitPoints(1);
                    ctx.AssertEqual(originalCount + 1, states.Count, "the player has one cached state");
                }
                ctx.AssertEqual(originalCount, states.Count, "native stats destruction removes the cached wrappers and score");
            }
            return Task.CompletedTask;
        }

        [EngineTest("Possessed familiars retain the native NPC HP formula", Category = "VitalityHitPoints", TimeoutSeconds = 60f)]
        public static async Task PossessedFamiliar(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(fixture.Creature).AsNWSCreature();
            var associateType = creature.m_nAssociateType;
            // The native familiar branch checks associate type plus client control.
            creature.SetAssociateType((ushort)AssociateType.Familiar);
            try
            {
                ctx.Assert(creature.GetIsPossessedFamiliar() != 0, "fixture uses the actual possessed-familiar branch");
                CreaturePlugin.SetRawAbilityScore(fixture.Creature, AbilityType.Vitality, 26);
                var evenMaximum = creature.GetMaxHitPoints(1);
                CreaturePlugin.SetRawAbilityScore(fixture.Creature, AbilityType.Vitality, 27);
                ctx.AssertEqual(evenMaximum, creature.GetMaxHitPoints(1), "familiar HP must not receive the player odd-score bonus");
            }
            finally { creature.SetAssociateType(associateType); }
        }

        [EngineTest("Every player Vitality point grants 20 HP without healing damage", Category = "VitalityHitPoints", TimeoutSeconds = 60f)]
        public static async Task PermanentScores(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var player = fixture.Creature;
            var baseline = GetMaxHitPoints(player);
            ObjectPlugin.SetCurrentHitPoints(player, baseline - MissingHP);
            foreach (var score in new[] { 11, 12, 26, 27, 28, 27, 26, 10 })
            {
                CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, score);
                ctx.AssertEqual(baseline + (score - 10) * 20, GetMaxHitPoints(player), $"Vitality {score} maximum");
                ctx.AssertEqual(GetMaxHitPoints(player) - MissingHP, ObjectPlugin.GetCurrentHitPoints(player),
                    $"Vitality {score} retains existing damage");
                ctx.AssertEqual((score - 10) / 2, GetAbilityModifier(AbilityType.Vitality, player),
                    "the native Constitution modifier is unchanged");
            }
            var stats = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature().m_pStats;
            stats.SetCONBase(27, 0);
            ctx.AssertEqual(baseline + 340, GetMaxHitPoints(player), "load/level-up setters refresh cached Vitality with HP recalculation disabled");
            ctx.AssertEqual(baseline - MissingHP, ObjectPlugin.GetCurrentHitPoints(player), "the native no-recalculation flag preserves current HP");
            stats.SetCONBase(10, 0);
        }

        [EngineTest("Vitality buffs and debuffs preserve damage and temporary HP", Category = "VitalityHitPoints", TimeoutSeconds = 60f)]
        public static async Task TemporaryScores(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var player = fixture.Creature;
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 27);
            var baseline = GetMaxHitPoints(player);
            ObjectPlugin.SetCurrentHitPoints(player, baseline - MissingHP);
            ctx.Assert(!GetIsImmune(player, ImmunityType.AbilityDecrease), "the player fixture accepts native ability decreases");
            ApplyEffectToObject(DurationType.Temporary, TagEffect(EffectTemporaryHitpoints(100), "vit_test_shield"), player, 600f);
            for (var cycle = 0; cycle < 3; cycle++)
            {
                var buff = EffectAbilityIncrease(AbilityType.Vitality, 1);
                if (cycle == 1)
                    buff = EffectLinkEffects(buff, EffectAbilityIncrease(AbilityType.Might, 2));
                ApplyEffectToObject(DurationType.Temporary, TagEffect(buff, "vit_test_buff"), player, 600f);
                AssertState(ctx, player, 28, baseline + 20, "buff");
                await ctx.ExecuteInCreatureContextAsync(player, () => RemoveTaggedEffect(player, "vit_test_buff"));
                await ctx.WaitUntilAsync(() => GetAbilityScore(player, AbilityType.Vitality) == 27, 5f, "buff removal to finish");
                AssertState(ctx, player, 27, baseline, "buff removed");
                await ctx.ExecuteInCreatureContextAsync(player, () => ApplyEffectToObject(DurationType.Temporary,
                    TagEffect(EffectAbilityDecrease(AbilityType.Vitality, 1), "vit_test_debuff"), player, 600f));
                AssertState(ctx, player, 26, baseline - 20, "debuff");
                await ctx.ExecuteInCreatureContextAsync(player, () => RemoveTaggedEffect(player, "vit_test_debuff"));
                await ctx.WaitUntilAsync(() => GetAbilityScore(player, AbilityType.Vitality) == 27, 5f, "debuff removal to finish");
                AssertState(ctx, player, 27, baseline, "debuff removed");
            }
            ApplyEffectToObject(DurationType.Temporary, EffectAbilityIncrease(AbilityType.Vitality, 1), player, 1f);
            AssertState(ctx, player, 28, baseline + 20, "expiring buff");
            await ctx.WaitUntilAsync(() => GetAbilityScore(player, AbilityType.Vitality) == 27, 5f, "buff to expire naturally");
            AssertState(ctx, player, 27, baseline, "expired buff");
            await ctx.ExecuteInCreatureContextAsync(player, () => RemoveTaggedEffect(player, "vit_test_shield"));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(baseline - MissingHP, GetCurrentHitPoints(player), "temporary pool removes normally");
        }

        [EngineTest("Equipping one Vitality grants 20 HP and unequipping removes it", Category = "VitalityHitPoints", TimeoutSeconds = 60f)]
        public static async Task Equipment(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var player = fixture.Creature;
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 26);
            var baseline = GetMaxHitPoints(player);
            ObjectPlugin.SetCurrentHitPoints(player, baseline - MissingHP);
            var belt = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player, () => belt = CreateItemOnObject("advent_belt", player));
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsObjectValid(belt), "create a real belt");
            ctx.Track(belt);
            for (var property = GetFirstItemProperty(belt); GetIsItemPropertyValid(property); property = GetFirstItemProperty(belt))
                RemoveItemProperty(belt, property);
            AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Vitality, 1), belt);
            for (var cycle = 0; cycle < 2; cycle++)
            {
                await ctx.ExecuteInCreatureContextAsync(player, () => ActionEquipItem(belt, InventorySlot.Belt));
                await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.Belt, player) == belt, 10f, "equip the Vitality belt");
                ctx.AssertEqual(27, GetAbilityScore(player, AbilityType.Vitality), "belt raises Vitality to 27");
                ctx.AssertEqual(baseline + 20, GetMaxHitPoints(player), "belt grants the missing 20 HP");
                ctx.AssertEqual(baseline + 20 - MissingHP, ObjectPlugin.GetCurrentHitPoints(player), "equip retains damage");
                if (cycle == 0)
                    await ctx.ExecuteInCreatureContextAsync(player, () => VerifyReloads(ctx, player));
                await ctx.ExecuteInCreatureContextAsync(player, () => ActionUnequipItem(belt));
                await ctx.WaitUntilAsync(() => GetItemInSlot(InventorySlot.Belt, player) != belt, 10f, "unequip the Vitality belt");
                ctx.AssertEqual(baseline, GetMaxHitPoints(player), "unequip removes the 20 HP");
                ctx.AssertEqual(baseline - MissingHP, ObjectPlugin.GetCurrentHitPoints(player), "unequip retains damage");
            }
        }

        [EngineTest("Odd Vitality HP survives repeated native character save and reload", Category = "VitalityHitPoints", TimeoutSeconds = 60f)]
        public static async Task SaveReload(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            CreaturePlugin.SetRawAbilityScore(fixture.Creature, AbilityType.Vitality, 27);
            ObjectPlugin.SetCurrentHitPoints(fixture.Creature, GetMaxHitPoints(fixture.Creature) - MissingHP);
            await ctx.ExecuteInCreatureContextAsync(fixture.Creature, () => VerifyReloads(ctx, fixture.Creature));
        }

        [EngineTest("Vitality HP retains the native per-level floor and NPC budgets", Category = "VitalityHitPoints", TimeoutSeconds = 60f)]
        public static async Task FloorsAndNPCs(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var player = fixture.Creature;
            for (var level = 1; level <= 40; level++)
                CreaturePlugin.SetMaxHitPointsByLevel(player, level, 1);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 10);
            var baseline = GetMaxHitPoints(player);
            foreach (var score in new[] { 9, 8, 7, 10 })
            {
                CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, score);
                ctx.AssertEqual(baseline, GetMaxHitPoints(player), $"Vitality {score} respects the per-level floor");
            }

            var npc = ctx.SpawnCreature("civilian", 3f);
            await ctx.DelaySecondsAsync(1f);
            CreaturePlugin.SetRawAbilityScore(npc, AbilityType.Vitality, 26);
            Stat.SetNPCMaxHitPoints(npc, 1000, true);
            CreaturePlugin.SetRawAbilityScore(npc, AbilityType.Vitality, 27);
            ctx.AssertEqual(1000, GetMaxHitPoints(npc), "odd Vitality does not change NPC budgets");
            CreaturePlugin.SetRawAbilityScore(npc, AbilityType.Vitality, 28);
            ctx.AssertEqual(1000 + GetHitDice(npc), GetMaxHitPoints(npc), "NPCs retain native Constitution scaling");
        }

        private static async Task<PlayerAbilityFixture> PreparePlayer(EngineTestContext ctx)
        {
            var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            try
            {
                var player = fixture.Creature;
                // The civilian template is plot-protected; ordinary players are not.
                SetPlotFlag(player, false);
                CreaturePlugin.SetClassByPosition(player, 0, SWLOR.NWN.API.NWScript.Enum.ClassType.Standard);
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                    typeof(PlayerInitialization).GetMethod("EnsureNativeLevels", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { player }));
                ctx.AssertEqual(40, GetHitDice(player), "fixture has normal SWLOR native levels");
                for (var level = 1; level <= 40; level++)
                    CreaturePlugin.SetMaxHitPointsByLevel(player, level, 10);
                CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 10);
                return fixture;
            }
            catch
            {
                fixture.Dispose();
                throw;
            }
        }

        private static void AssertState(EngineTestContext ctx, uint player, int vitality, int maximum, string label)
        {
            ctx.AssertEqual(vitality, GetAbilityScore(player, AbilityType.Vitality), $"{label} Vitality");
            ctx.AssertEqual(maximum, GetMaxHitPoints(player), $"{label} maximum");
            ctx.AssertEqual(maximum - MissingHP, ObjectPlugin.GetCurrentHitPoints(player), $"{label} base HP retains damage");
            ctx.AssertEqual(maximum - MissingHP + 100, GetCurrentHitPoints(player), $"{label} retains temporary HP");
        }

        private static void RemoveTaggedEffect(uint creature, string tag)
        {
            var found = false;
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
                if (GetEffectTag(effect) == tag)
                {
                    found = true;
                    RemoveEffect(creature, effect);
                }
            if (!found) throw new InvalidOperationException($"Tagged effect {tag} was not found.");
        }

        private static unsafe void VerifyReloads(EngineTestContext ctx, uint player)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSCreature();
            var baseVitality = native.m_pStats.m_nConstitutionBase;
            var belt = native.m_pInventory.GetItemInSlot(1u << (int)InventorySlot.Belt);
            var beltQuantity = belt?.m_nStackSize;
            var maximum = native.GetMaxHitPoints(1);
            var current = native.GetCurrentHitPoints(1);
            CNWSCreature owned = null;
            try
            {
                for (var cycle = 0; cycle < 3; cycle++)
                {
                    using var file = new CResGFF();
                    using var root = new CResStruct();
                    using var fileType = new CExoString("BIC ");
                    using var format = new CExoString("V2.0");
                    ctx.Assert(file.CreateGFFFile(root, fileType, format) != 0, "create character export");
                    ctx.Assert(native.SaveCreature(file, root, 0, 0, 1, 0) != 0, "save character");
                    var restored = new CNWSCreature(OBJECT_INVALID, 0, 1);
                    try
                    {
                        ctx.Assert(restored.LoadCreature(file, root, 0, 0, 0, 0) != 0, "reload character");
                        restored.SetArea(null);
                        ctx.AssertEqual(maximum, restored.GetMaxHitPoints(1), $"reload {cycle} maximum does not stack");
                        ctx.AssertEqual(current, restored.GetCurrentHitPoints(1), $"reload {cycle} retains damage");
                        ctx.AssertEqual(40, (int)restored.m_pStats.GetLevel(0), "all hit dice survive");
                        ctx.AssertEqual(27, (int)restored.m_pStats.GetCONStat(), "Vitality survives");
                        if (beltQuantity.HasValue)
                        {
                            var restoredBelt = restored.m_pInventory.GetItemInSlot(1u << (int)InventorySlot.Belt);
                            ctx.Assert(restoredBelt != null, "equipped belt survives reload");
                            ctx.AssertEqual(restored.m_idSelf, restoredBelt.m_oidPossessor, "reloaded belt belongs to the restored player");
                            ctx.AssertEqual(beltQuantity.Value, restoredBelt.m_nStackSize, "belt quantity survives reload");
                        }
                        restored.m_pStats.SetCONBase((byte)(baseVitality + 1), 1);
                        ctx.AssertEqual(maximum + 20, restored.GetMaxHitPoints(1), "reloaded player can increase Vitality");
                        ctx.AssertEqual(current + 20, restored.GetCurrentHitPoints(1), "reloaded increase retains damage");
                        restored.m_pStats.SetCONBase(baseVitality, 1);
                        ctx.AssertEqual(current, restored.GetCurrentHitPoints(1), "reloaded decrease retains damage");
                    }
                    catch
                    {
                        restored.Dispose();
                        throw;
                    }
                    owned?.Dispose();
                    owned = restored;
                    native = restored;
                }
            }
            finally { owned?.Dispose(); }
        }
    }
}
