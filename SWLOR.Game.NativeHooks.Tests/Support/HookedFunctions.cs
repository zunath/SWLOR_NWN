using NWNX.NET.Native;
using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks.Tests.Support;

/// <summary>
/// The 36 engine functions the 17 hook files hook, in the order the hooks are requested when every file is enabled in
/// id order. Symbols and orders are copied from the hook files as they were before the trampolines moved out of the game
/// (P4-11), so a changed symbol or order fails the tests.
/// </summary>
internal static unsafe class HookedFunctions
{
    private const int Replaced = -1000000;

    public static readonly HookedFunction[] All = Build();

    private static void* P(int n) => (void*)(0x1000 + n * 0x100);

    private static long L(int n) => 0x1000 + n * 0x100;

    private static HookedFunction Chat(int entryPoint, string symbol) =>
        new(entryPoint, NativeHookId.ChatPortraitSpeakers, symbol, HookOrder.Latest, "Send", true, true,
            [L(1), 7, 8, L(2)],
            hook => ((delegate* unmanaged<void*, uint, uint, void*, int>)hook)(P(1), 7, 8, P(2)));

    private static HookedFunction[] Build()
    {
        HookedFunction[] all =
        [
            Chat(0, "_ZN11CNWSMessage27SendServerToPlayerChat_TalkEjj10CExoString"),
            Chat(1, "_ZN11CNWSMessage30SendServerToPlayerChat_WhisperEjj10CExoString"),
            Chat(2, "_ZN11CNWSMessage28SendServerToPlayerChat_PartyEjj10CExoString"),
            Chat(3, "_ZN11CNWSMessage27SendServerToPlayerChat_TellEjj10CExoString"),
            Chat(4, "_ZN11CNWSMessage28SendServerToPlayerChat_ShoutEjj10CExoString"),
            Chat(5, "_ZN11CNWSMessage35SendServerToPlayerChat_Silent_ShoutEjj10CExoString"),
            Chat(6, "_ZN11CNWSMessage30SendServerToPlayerChat_DM_TalkEjj10CExoString"),
            Chat(7, "_ZN11CNWSMessage33SendServerToPlayerChat_DM_WhisperEjj10CExoString"),
            Chat(8, "_ZN11CNWSMessage38SendServerToPlayerChat_DM_Silent_ShoutEjj10CExoString"),
            new(9, NativeHookId.ClientJsonUtf8Guard, "_ZN10CNWMessage8ReadJSONB5cxx11Ev", HookOrder.Earliest, "ReadJson", true, true,
                [L(1), L(2)],
                hook => (long)((delegate* unmanaged<void*, void*, void*>)hook)(P(1), P(2))),
            new(10, NativeHookId.CombatRecorderEffectHooks, "_ZN21CNWSEffectListHandler13OnApplyDamageEP10CNWSObjectP11CGameEffecti",
                HookOrder.Earliest, "ApplyDamage", true, true,
                [L(1), L(2), L(3), 1],
                hook => ((delegate* unmanaged<void*, void*, void*, int, int>)hook)(P(1), P(2), P(3), 1)),
            new(11, NativeHookId.CombatRecorderEffectHooks, "_ZN21CNWSEffectListHandler11OnApplyHealEP10CNWSObjectP11CGameEffecti",
                HookOrder.Earliest, "ApplyHeal", true, true,
                [L(1), L(2), L(3), 0],
                hook => ((delegate* unmanaged<void*, void*, void*, int, int>)hook)(P(1), P(2), P(3), 0)),
            new(12, NativeHookId.GetDamageRoll, "_ZN17CNWSCreatureStats13GetDamageRollEP10CNWSObjectiiiii", Replaced, "GetDamageRoll", false, true,
                [L(1), L(2), 1, 2, 3, 4, 5],
                hook => ((delegate* unmanaged<void*, void*, int, int, int, int, int, int>)hook)(P(1), P(2), 1, 2, 3, 4, 5)),
            new(13, NativeHookId.LegacyItemProperties, "_ZN8CNWSItem8LoadItemEP7CResGFFP10CResStructii", HookOrder.Early, "LoadItem", true, true,
                [L(1), L(2), L(3), 4, 5],
                hook => ((delegate* unmanaged<void*, void*, void*, int, int, int>)hook)(P(1), P(2), P(3), 4, 5)),
            new(14, NativeHookId.NeckAppearanceSerialization, "_ZN11CNWSMessage26AddItemAppearanceToMessageEP10CNWSPlayerP8CNWSItem",
                HookOrder.Late, "Write", true, false,
                [L(1), L(2), L(3)],
                hook =>
                {
                    ((delegate* unmanaged<void*, void*, void*, void>)hook)(P(1), P(2), P(3));
                    return 0;
                }),
            new(15, NativeHookId.OnAIActionAttackObject, "_ZN12CNWSCreature20AIActionAttackObjectEP20CNWSObjectActionNode", Replaced,
                "AttackObject", false, true,
                [L(1), L(2)],
                hook => ((delegate* unmanaged<void*, void*, int>)hook)(P(1), P(2))),
            new(16, NativeHookId.PlayerCreation, "_ZN21CServerExoAppInternal29ValidateCreateServerCharacterEP10CNWSPlayerPvj", HookOrder.Early,
                "Validate", true, true,
                [L(1), L(2), L(3), 64],
                hook => ((delegate* unmanaged<void*, void*, void*, uint, uint>)hook)(P(1), P(2), P(3), 64)),
            new(17, NativeHookId.PlayerFileCheckpoint, "_ZN12CNWSCreature12SaveCreatureEP7CResGFFP10CResStructiiii", HookOrder.Early, "Save", true, true,
                [L(1), L(2), L(3), 1, 2, 3, 4],
                hook => ((delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)hook)(P(1), P(2), P(3), 1, 2, 3, 4)),
            new(18, NativeHookId.PlayerFileCheckpoint, "_ZN12CNWSCreature12LoadCreatureEP7CResGFFP10CResStructiiii", HookOrder.Early, "Load", true, true,
                [L(1), L(2), L(3), 5, 6, 7, 8],
                hook => ((delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)hook)(P(1), P(2), P(3), 5, 6, 7, 8)),
            new(19, NativeHookId.PlayerFileCheckpoint,
                "_ZN17CNWSCreatureStats16ReadStatsFromGffEP7CResGFFP10CResStructP26CNWSCreatureAppearanceInfoiiii", HookOrder.Early,
                "ReadStats", true, true,
                [L(1), L(2), L(3), L(4), 1, 2, 3, 4],
                hook => ((delegate* unmanaged<void*, void*, void*, void*, int, int, int, int, uint>)hook)(P(1), P(2), P(3), P(4), 1, 2, 3, 4)),
            new(20, NativeHookId.PlayerListNamePrivacy, "_ZN11CNWSMessage32SendServerToPlayerPlayerList_AddEjP10CNWSPlayer", HookOrder.Earliest,
                "SendPlayerListAdd", true, true,
                [L(1), 9, L(2)],
                hook => ((delegate* unmanaged<void*, uint, void*, int>)hook)(P(1), 9, P(2))),
            new(21, NativeHookId.PlayerSlotAssignment, "_ZN17CNetLayerInternal16SetSlidingWindowE16CNetConnectionIdPj", HookOrder.Earliest,
                "SetSlidingWindow", true, true,
                [L(1), 3, L(2)],
                hook => ((delegate* unmanaged<void*, uint, uint*, int>)hook)(P(1), 3, (uint*)P(2))),
            new(22, NativeHookId.QuickbarFeatLoading, "_ZN12CNWSCreature16LoadQuickButtonsEP7CResGFFP10CResStruct", HookOrder.Early,
                "LoadQuickButtons", true, false,
                [L(1), L(2), L(3)],
                hook =>
                {
                    ((delegate* unmanaged<void*, void*, void*, void>)hook)(P(1), P(2), P(3));
                    return 0;
                }),
            new(23, NativeHookId.ResolveAttackRoll, "_ZN12CNWSCreature17ResolveAttackRollEP10CNWSObject", Replaced, "ResolveAttackRoll", false, false,
                [L(1), L(2)],
                hook =>
                {
                    ((delegate* unmanaged<void*, void*, void>)hook)(P(1), P(2));
                    return 0;
                }),
            new(24, NativeHookId.SavedPlayerInventory, "_ZN8CNWSItem11CompareItemEPS_", HookOrder.Early, "CompareItem", true, true,
                [L(1), L(2)],
                hook => ((delegate* unmanaged<void*, void*, int>)hook)(P(1), P(2))),
            new(25, NativeHookId.StaleDriveAfterJump, "_ZN12CNWSCreature19AIActionJumpToPointEP20CNWSObjectActionNode", HookOrder.Late,
                "JumpToPoint", true, true,
                [L(1), L(2)],
                hook => ((delegate* unmanaged<void*, void*, uint>)hook)(P(1), P(2))),
            new(26, NativeHookId.StaleDriveAfterJump, "_ZN12CNWSCreature14AddDriveActionEtRK6Vectoriiii", HookOrder.Late, "AddDriveAction", true, true,
                [L(1), 12, L(2), 3, 4, 5, 6],
                hook => ((delegate* unmanaged<void*, ushort, void*, int, int, int, int, int>)hook)(P(1), 12, P(2), 3, 4, 5, 6)),
            new(27, NativeHookId.VitalityHitPoints, "_ZN17CNWSCreatureStats10SetCONBaseEhi", HookOrder.Early, "SetBase", true, false,
                [L(1), 27, 1],
                hook =>
                {
                    ((delegate* unmanaged<void*, byte, int, void>)hook)(P(1), 27, 1);
                    return 0;
                }),
            new(28, NativeHookId.VitalityHitPoints, "_ZN10CNWSObject11ApplyEffectEP11CGameEffectii", HookOrder.Earliest - 1, "ApplyEffect", true, false,
                [L(1), L(2), 1, 0],
                hook =>
                {
                    ((delegate* unmanaged<void*, void*, int, int, void>)hook)(P(1), P(2), 1, 0);
                    return 0;
                }),
            new(29, NativeHookId.VitalityHitPoints, "_ZN10CNWSObject12RemoveEffectEP11CGameEffect", HookOrder.Earliest - 1, "RemoveEffect", true, false,
                [L(1), L(2)],
                hook =>
                {
                    ((delegate* unmanaged<void*, void*, void>)hook)(P(1), P(2));
                    return 0;
                }),
            new(30, NativeHookId.VitalityHitPoints, "_ZN10CNWSObject16RemoveEffectByIdEm", HookOrder.Early, "RemoveEffectById", true, true,
                [L(1), 77],
                hook => ((delegate* unmanaged<void*, ulong, int>)hook)(P(1), 77)),
            new(31, NativeHookId.VitalityHitPoints, "_ZN17CNWSCreatureStats16CalcStatModifierEh", HookOrder.Late, "CalculateModifier", true, true,
                [L(1), 27],
                hook => ((delegate* unmanaged<void*, byte, sbyte>)hook)(P(1), 27)),
            new(32, NativeHookId.VitalityHitPoints, "_ZN17CNWSCreatureStatsD1Ev", HookOrder.Early, "DestroyStats", true, false,
                [L(1)],
                hook =>
                {
                    ((delegate* unmanaged<void*, void>)hook)(P(1));
                    return 0;
                }),
            new(33, NativeHookId.VitalityHitPoints, "_ZN12CNWSCreature15GetMaxHitPointsEi", HookOrder.Late, "GetMaximum", true, true,
                [L(1), 1],
                hook => ((delegate* unmanaged<void*, int, short>)hook)(P(1), 1)),
            new(34, NativeHookId.WeaponAttackAnimation,
                "_ZN11CNWSMessage21ComputeUpdateRequiredEP10CNWSPlayerP10CNWSObjectP17CLastUpdateObjecti", HookOrder.Late, "ComputeUpdate",
                true, true,
                [L(1), L(2), L(3), L(4), 1],
                hook => ((delegate* unmanaged<void*, void*, void*, void*, int, uint>)hook)(P(1), P(2), P(3), P(4), 1)),
            new(35, NativeHookId.WeaponAttackAnimation,
                "_ZN11CNWSMessage31WriteGameObjUpdate_UpdateObjectEP10CNWSPlayerP10CNWSObjectP17CLastUpdateObjectjj", HookOrder.Late,
                "WriteUpdate", true, false,
                [L(1), L(2), L(3), L(4), 6, 7],
                hook =>
                {
                    ((delegate* unmanaged<void*, void*, void*, void*, uint, uint, void>)hook)(P(1), P(2), P(3), P(4), 6, 7);
                    return 0;
                }),
        ];

        for (var index = 0; index < all.Length; index++)
        {
            if (all[index].EntryPoint != index)
                throw new InvalidOperationException($"Entry point {all[index]} is at index {index}.");
        }

        return all;
    }
}
