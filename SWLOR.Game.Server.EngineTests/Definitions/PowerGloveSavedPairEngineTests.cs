using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PowerGloveSavedPairEngineTests
{
    private static bool _watchingAcquire;
    private static int _preAreaPlayerCompanionAcquires;

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void ObserveSavedCompanionAcquire()
    {
        if (!_watchingAcquire) return;
        var companion = GetModuleItemAcquired();
        if (!PowerGloveEquipment.IsGeneratedCompanion(companion)) return;
        var owner = GetItemPossessor(companion);
        if (GetIsObjectValid(owner) && GetIsPC(owner) && !GetIsObjectValid(GetArea(owner)))
            _preAreaPlayerCompanionAcquires++;
    }

    [EngineTest("Saved Power Glove player loads preserve equipped ownership across two native reloads", Category = "PowerGlovePair", TimeoutSeconds = 45f)]
    public static async Task SavedEquippedPairSurvivesAcquireBeforeAreaEntry(EngineTestContext context)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(context);
        player.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
        var originalMain = await PowerGlovePairEngineTests.EquipMain(context, player.Creature);
        var originalCompanion = await PowerGlovePairEngineTests.WaitForPair(context, player.Creature, originalMain);
        var identity = GetObjectUUID(originalMain);
        var properties = PowerGlovePairEngineTests.Properties(originalMain);
        var load = typeof(PlayerAbilityFixture).GetMethod("LoadPlayerCreature", BindingFlags.Static | BindingFlags.NonPublic)!;
        var enter = typeof(PlayerAbilityFixture).GetMethod("EnterArena", BindingFlags.Static | BindingFlags.NonPublic)!;
        var source = player.Creature;
        _preAreaPlayerCompanionAcquires = 0;
        _watchingAcquire = true;
        try
        {
            for (var cycle = 0; cycle < 2; cycle++)
            {
                var loaded = OBJECT_INVALID;
                var before = _preAreaPlayerCompanionAcquires;
                await context.ExecuteInCreatureContextAsync(source, () =>
                    loaded = (uint)load.Invoke(null, new object[] { context, source }));
                context.Track(loaded);
                context.Assert(_preAreaPlayerCompanionAcquires > before, "The saved PC's companion traverses the real pre-area module acquire event.");
                var main = GetItemInSlot(InventorySlot.RightHand, loaded);
                var companion = GetItemInSlot(InventorySlot.LeftHand, loaded);
                context.Assert(PowerGloveEquipment.IsLinkedPair(loaded, main, companion), "Acquire normalization preserves the loaded equipped pair without an explicit repair call.");
                context.AssertEqual(identity, GetObjectUUID(main), "The native reload preserves the same real item UUID.");
                context.AssertEqual(properties, PowerGlovePairEngineTests.Properties(main), "Saved real-item properties are unchanged.");
                PowerGlovePairEngineTests.AssertOwnedItems(context, loaded, main, 1);
                PowerGlovePairEngineTests.AssertCosmeticCompanion(context, companion);
                context.Assert(PowerGloveEquipment.IsLinkedPair(player.Creature, originalMain, originalCompanion), "Stale saved references cannot modify the original live pair.");
                // The existing fixture loader clears its temporary PC flag before arena entry and cleanup.
                context.Assert(!GetIsPC(loaded), "The detached copied creature is safe for native arena entry.");
                await context.ExecuteInCreatureContextAsync(source, () =>
                    enter.Invoke(null, new object[] { context, loaded, player.Creature }));
                context.AssertEqual(context.Arena, GetArea(loaded), "The copied native creature enters the tracked arena.");
                context.Assert(!PowerGloveEquipment.TryRestoreSavedCompanion(companion), "An in-world NPC cannot use the saved-player recovery path.");
                source = loaded;
            }

            await context.ExecuteInCreatureContextAsync(player.Creature, () => ActionUnequipItem(originalMain));
            await PowerGlovePairEngineTests.WaitForRemovedPair(context, player.Creature, originalMain, originalCompanion);
            await context.ExecuteInCreatureContextAsync(player.Creature, () => PowerGloveEquipment.RestorePair(player.Creature));
            await context.DelaySecondsAsync(.5f);
            context.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.RightHand, player.Creature), "Recovery never auto-equips a legitimately carried real glove.");
            PowerGlovePairEngineTests.AssertOwnedItems(context, player.Creature, originalMain, 0);
        }
        finally
        {
            _watchingAcquire = false;
            _preAreaPlayerCompanionAcquires = 0;
        }
    }
}
