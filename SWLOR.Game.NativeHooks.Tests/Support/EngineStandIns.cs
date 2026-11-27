using System.Runtime.InteropServices;

namespace SWLOR.Game.NativeHooks.Tests.Support;

/// <summary>
/// Stand-ins for the 41 hooked engine functions, one per entry point (<see cref="HookedFunctions"/> order), each with the
/// engine function's signature. Each records its entry point and arguments and returns its entry point + 1.
/// </summary>
internal static unsafe class EngineStandIns
{
    /// <summary>The stand-in of each entry point, as the trampoline NWNX would hand the hook.</summary>
    public static nint[] Pointers()
    {
        return
        [
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&Talk,
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&Whisper,
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&Party,
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&Tell,
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&Shout,
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&SilentShout,
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&DMTalk,
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&DMWhisper,
            (nint)(delegate* unmanaged<void*, uint, uint, void*, int>)&DMSilentShout,
            (nint)(delegate* unmanaged<void*, void*, void*>)&ReadJson,
            (nint)(delegate* unmanaged<void*, void*, byte, int>)&HandleNuiEvent,
            (nint)(delegate* unmanaged<void*, void*, void*, int, int>)&ApplyDamage,
            (nint)(delegate* unmanaged<void*, void*, void*, int, int>)&ApplyHeal,
            (nint)(delegate* unmanaged<void*, void*, int, int, int, int, int, int>)&GetDamageRoll,
            (nint)(delegate* unmanaged<void*, void*, void*, int, int, int>)&LoadItem,
            (nint)(delegate* unmanaged<void*, void*, void*, void>)&AddItemAppearance,
            (nint)(delegate* unmanaged<void*, void*, int>)&AttackObject,
            (nint)(delegate* unmanaged<void*, void*, void*, uint, uint>)&ValidateCreate,
            (nint)(delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)&SaveCreature,
            (nint)(delegate* unmanaged<void*, void*, void*, int, int, int, int, int>)&LoadCreature,
            (nint)(delegate* unmanaged<void*, void*, void*, void*, int, int, int, int, uint>)&ReadStats,
            (nint)(delegate* unmanaged<void*, uint, void*, int>)&PlayerListAdd,
            (nint)(delegate* unmanaged<void*, uint, uint*, int>)&SetSlidingWindow,
            (nint)(delegate* unmanaged<void*, void*, void*, int, int>)&ApplyVisualEffect,
            (nint)(delegate* unmanaged<void*, void*, void*, int>)&RemoveVisualEffect,
            (nint)(delegate* unmanaged<void*, void*, void*, int, int>)&ApplyPetrify,
            (nint)(delegate* unmanaged<void*, void*, void*, int>)&RemovePetrify,
            (nint)(delegate* unmanaged<void*, void*, void*, void>)&LoadQuickButtons,
            (nint)(delegate* unmanaged<void*, void*, void>)&ResolveAttackRoll,
            (nint)(delegate* unmanaged<void*, void*, int>)&CompareItem,
            (nint)(delegate* unmanaged<void*, void*, uint>)&JumpToPoint,
            (nint)(delegate* unmanaged<void*, ushort, void*, int, int, int, int, int>)&AddDriveAction,
            (nint)(delegate* unmanaged<void*, byte, int, void>)&SetCONBase,
            (nint)(delegate* unmanaged<void*, void*, int, int, void>)&ApplyEffect,
            (nint)(delegate* unmanaged<void*, void*, void>)&RemoveEffect,
            (nint)(delegate* unmanaged<void*, ulong, int>)&RemoveEffectById,
            (nint)(delegate* unmanaged<void*, byte, sbyte>)&CalcStatModifier,
            (nint)(delegate* unmanaged<void*, void>)&DestroyStats,
            (nint)(delegate* unmanaged<void*, int, short>)&GetMaxHitPoints,
            (nint)(delegate* unmanaged<void*, void*, void*, void*, int, uint>)&ComputeUpdate,
            (nint)(delegate* unmanaged<void*, void*, void*, void*, uint, uint, void>)&WriteUpdate,
        ];
    }

    private static int Chat(int entryPoint, void* message, uint recipient, uint speaker, void* text)
    {
        CallLog.EngineFunction(entryPoint, 4, (long)message, recipient, speaker, (long)text);
        return entryPoint + 1;
    }

    [UnmanagedCallersOnly] private static int Talk(void* m, uint r, uint s, void* t) => Chat(0, m, r, s, t);
    [UnmanagedCallersOnly] private static int Whisper(void* m, uint r, uint s, void* t) => Chat(1, m, r, s, t);
    [UnmanagedCallersOnly] private static int Party(void* m, uint r, uint s, void* t) => Chat(2, m, r, s, t);
    [UnmanagedCallersOnly] private static int Tell(void* m, uint r, uint s, void* t) => Chat(3, m, r, s, t);
    [UnmanagedCallersOnly] private static int Shout(void* m, uint r, uint s, void* t) => Chat(4, m, r, s, t);
    [UnmanagedCallersOnly] private static int SilentShout(void* m, uint r, uint s, void* t) => Chat(5, m, r, s, t);
    [UnmanagedCallersOnly] private static int DMTalk(void* m, uint r, uint s, void* t) => Chat(6, m, r, s, t);
    [UnmanagedCallersOnly] private static int DMWhisper(void* m, uint r, uint s, void* t) => Chat(7, m, r, s, t);
    [UnmanagedCallersOnly] private static int DMSilentShout(void* m, uint r, uint s, void* t) => Chat(8, m, r, s, t);

    [UnmanagedCallersOnly]
    private static void* ReadJson(void* result, void* message)
    {
        CallLog.EngineFunction(9, 2, (long)result, (long)message);
        return (void*)10;
    }

    [UnmanagedCallersOnly]
    private static int HandleNuiEvent(void* message, void* player, byte minor)
    {
        CallLog.EngineFunction(10, 3, (long)message, (long)player, minor);
        return 11;
    }

    [UnmanagedCallersOnly]
    private static int ApplyDamage(void* handler, void* target, void* effect, int loading)
    {
        CallLog.EngineFunction(11, 4, (long)handler, (long)target, (long)effect, loading);
        return 12;
    }

    [UnmanagedCallersOnly]
    private static int ApplyHeal(void* handler, void* target, void* effect, int loading)
    {
        CallLog.EngineFunction(12, 4, (long)handler, (long)target, (long)effect, loading);
        return 13;
    }

    [UnmanagedCallersOnly]
    private static int GetDamageRoll(void* stats, void* target, int offHand, int critical, int sneak, int death, int forceMax)
    {
        CallLog.EngineFunction(13, 7, (long)stats, (long)target, offHand, critical, sneak, death, forceMax);
        return 14;
    }

    [UnmanagedCallersOnly]
    private static int LoadItem(void* item, void* resource, void* structure, int first, int second)
    {
        CallLog.EngineFunction(14, 5, (long)item, (long)resource, (long)structure, first, second);
        return 15;
    }

    [UnmanagedCallersOnly]
    private static void AddItemAppearance(void* message, void* player, void* item) =>
        CallLog.EngineFunction(15, 3, (long)message, (long)player, (long)item);

    [UnmanagedCallersOnly]
    private static int AttackObject(void* creature, void* node)
    {
        CallLog.EngineFunction(16, 2, (long)creature, (long)node);
        return 17;
    }

    [UnmanagedCallersOnly]
    private static uint ValidateCreate(void* server, void* client, void* data, uint size)
    {
        CallLog.EngineFunction(17, 4, (long)server, (long)client, (long)data, size);
        return 18;
    }

    [UnmanagedCallersOnly]
    private static int SaveCreature(void* creature, void* resource, void* structure, int a, int b, int c, int d)
    {
        CallLog.EngineFunction(18, 7, (long)creature, (long)resource, (long)structure, a, b, c, d);
        return 19;
    }

    [UnmanagedCallersOnly]
    private static int LoadCreature(void* creature, void* resource, void* structure, int a, int b, int c, int d)
    {
        CallLog.EngineFunction(19, 7, (long)creature, (long)resource, (long)structure, a, b, c, d);
        return 20;
    }

    [UnmanagedCallersOnly]
    private static uint ReadStats(void* stats, void* resource, void* structure, void* appearance, int a, int b, int c, int d)
    {
        CallLog.EngineFunction(20, 8, (long)stats, (long)resource, (long)structure, (long)appearance, a, b, c, d);
        return 21;
    }

    [UnmanagedCallersOnly]
    private static int PlayerListAdd(void* message, uint recipient, void* player)
    {
        CallLog.EngineFunction(21, 3, (long)message, recipient, (long)player);
        return 22;
    }

    [UnmanagedCallersOnly]
    private static int SetSlidingWindow(void* netLayer, uint connection, uint* window)
    {
        CallLog.EngineFunction(22, 3, (long)netLayer, connection, (long)window);
        return 23;
    }

    [UnmanagedCallersOnly]
    private static int ApplyVisualEffect(void* handler, void* target, void* effect, int loading)
    {
        CallLog.EngineFunction(23, 4, (long)handler, (long)target, (long)effect, loading);
        return 24;
    }

    [UnmanagedCallersOnly]
    private static int RemoveVisualEffect(void* handler, void* target, void* effect)
    {
        CallLog.EngineFunction(24, 3, (long)handler, (long)target, (long)effect);
        return 25;
    }

    [UnmanagedCallersOnly]
    private static int ApplyPetrify(void* handler, void* target, void* effect, int loading)
    {
        CallLog.EngineFunction(25, 4, (long)handler, (long)target, (long)effect, loading);
        return 26;
    }

    [UnmanagedCallersOnly]
    private static int RemovePetrify(void* handler, void* target, void* effect)
    {
        CallLog.EngineFunction(26, 3, (long)handler, (long)target, (long)effect);
        return 27;
    }

    [UnmanagedCallersOnly]
    private static void LoadQuickButtons(void* creature, void* resource, void* structure) =>
        CallLog.EngineFunction(27, 3, (long)creature, (long)resource, (long)structure);

    [UnmanagedCallersOnly]
    private static void ResolveAttackRoll(void* creature, void* target) =>
        CallLog.EngineFunction(28, 2, (long)creature, (long)target);

    [UnmanagedCallersOnly]
    private static int CompareItem(void* item, void* other)
    {
        CallLog.EngineFunction(29, 2, (long)item, (long)other);
        return 30;
    }

    [UnmanagedCallersOnly]
    private static uint JumpToPoint(void* creature, void* node)
    {
        CallLog.EngineFunction(30, 2, (long)creature, (long)node);
        return 31;
    }

    [UnmanagedCallersOnly]
    private static int AddDriveAction(void* creature, ushort group, void* start, int bearing, int path, int flags, int generate)
    {
        CallLog.EngineFunction(31, 7, (long)creature, group, (long)start, bearing, path, flags, generate);
        return 32;
    }

    [UnmanagedCallersOnly]
    private static void SetCONBase(void* stats, byte score, int recalculate) =>
        CallLog.EngineFunction(32, 3, (long)stats, score, recalculate);

    [UnmanagedCallersOnly]
    private static void ApplyEffect(void* target, void* effect, int loading, int initial) =>
        CallLog.EngineFunction(33, 4, (long)target, (long)effect, loading, initial);

    [UnmanagedCallersOnly]
    private static void RemoveEffect(void* target, void* effect) =>
        CallLog.EngineFunction(34, 2, (long)target, (long)effect);

    [UnmanagedCallersOnly]
    private static int RemoveEffectById(void* target, ulong id)
    {
        CallLog.EngineFunction(35, 2, (long)target, (long)id);
        return 36;
    }

    [UnmanagedCallersOnly]
    private static sbyte CalcStatModifier(void* stats, byte score)
    {
        CallLog.EngineFunction(36, 2, (long)stats, score);
        return 37;
    }

    [UnmanagedCallersOnly]
    private static void DestroyStats(void* stats) => CallLog.EngineFunction(37, 1, (long)stats);

    [UnmanagedCallersOnly]
    private static short GetMaxHitPoints(void* creature, int includeToughness)
    {
        CallLog.EngineFunction(38, 2, (long)creature, includeToughness);
        return 39;
    }

    [UnmanagedCallersOnly]
    private static uint ComputeUpdate(void* message, void* player, void* obj, void* last, int playerObject)
    {
        CallLog.EngineFunction(39, 5, (long)message, (long)player, (long)obj, (long)last, playerObject);
        return 40;
    }

    [UnmanagedCallersOnly]
    private static void WriteUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance) =>
        CallLog.EngineFunction(40, 6, (long)message, (long)player, (long)obj, (long)last, updates, appearance);
}
