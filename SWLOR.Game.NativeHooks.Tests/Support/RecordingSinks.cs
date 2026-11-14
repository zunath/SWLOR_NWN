using SWLOR.Game.Contracts;

namespace SWLOR.Game.NativeHooks.Tests.Support;

/// <summary>
/// Sinks that record each call (method, the engine function they received, the arguments) and return
/// <see cref="Result"/> (or <see cref="ModifierResult"/> for the one <c>sbyte</c> method), allocating nothing.
/// </summary>
internal static unsafe class RecordingSinks
{
    public const int Result = 500;
    public const sbyte ModifierResult = -100;

    public static NativeSinkSet Create() => new(
        new Chat(), new Json(), new Recorder(), new DamageRoll(), new LegacyItems(), new Neck(), new Attack(), new Creation(),
        new Checkpoint(), new PlayerList(), new Slots(), new Quickbar(), new AttackRoll(), new Inventory(), new Drive(),
        new Vitality(), new Animation());

    private sealed class Chat : IChatPortraitSpeakersSink
    {
        public int Send(void* message, uint recipient, uint speaker, void* text, nint original)
        {
            CallLog.Sink(nameof(Send), original, 4, (long)message, recipient, speaker, (long)text);
            return Result;
        }
    }

    private sealed class Json : IClientJsonUtf8GuardSink
    {
        public void* ReadJson(void* result, void* message, nint original)
        {
            CallLog.Sink(nameof(ReadJson), original, 2, (long)result, (long)message);
            return (void*)Result;
        }
    }

    private sealed class Recorder : ICombatRecorderEffectHooksSink
    {
        public int ApplyDamage(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original)
        {
            CallLog.Sink(nameof(ApplyDamage), original, 4, (long)effectListHandler, (long)pObject, (long)pEffect, bLoadingGame);
            return Result;
        }

        public int ApplyHeal(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original)
        {
            CallLog.Sink(nameof(ApplyHeal), original, 4, (long)effectListHandler, (long)pObject, (long)pEffect, bLoadingGame);
            return Result;
        }
    }

    private sealed class DamageRoll : IGetDamageRollSink
    {
        public int GetDamageRoll(void* thisPtr, void* pTarget, int bOffHand, int bCritical, int bSneakAttack, int bDeathAttack, int bForceMax)
        {
            CallLog.Sink(nameof(GetDamageRoll), 0, 7, (long)thisPtr, (long)pTarget, bOffHand, bCritical, bSneakAttack, bDeathAttack, bForceMax);
            return Result;
        }
    }

    private sealed class LegacyItems : ILegacyItemPropertiesSink
    {
        public int LoadItem(void* item, void* resource, void* structure, int firstFlag, int secondFlag, nint original)
        {
            CallLog.Sink(nameof(LoadItem), original, 5, (long)item, (long)resource, (long)structure, firstFlag, secondFlag);
            return Result;
        }
    }

    private sealed class Neck : INeckAppearanceSerializationSink
    {
        public void Write(void* message, void* player, void* itemPointer, nint original) =>
            CallLog.Sink(nameof(Write), original, 3, (long)message, (long)player, (long)itemPointer);
    }

    private sealed class Attack : IOnAIActionAttackObjectSink
    {
        public int AttackObject(void* creature, void* node)
        {
            CallLog.Sink(nameof(AttackObject), 0, 2, (long)creature, (long)node);
            return Result;
        }
    }

    private sealed class Creation : IPlayerCreationSink
    {
        public uint Validate(void* server, void* client, void* data, uint size, nint original)
        {
            CallLog.Sink(nameof(Validate), original, 4, (long)server, (long)client, (long)data, size);
            return Result;
        }
    }

    private sealed class Checkpoint : IPlayerFileCheckpointSink
    {
        public int Save(void* creature, void* resource, void* structure, int associates, int desiredArea, int exporting, int objectIds, nint original)
        {
            CallLog.Sink(nameof(Save), original, 7, (long)creature, (long)resource, (long)structure, associates, desiredArea, exporting, objectIds);
            return Result;
        }

        public int Load(void* creature, void* resource, void* structure, int saveGame, int associate, int preserveIds, int copyObject, nint original)
        {
            CallLog.Sink(nameof(Load), original, 7, (long)creature, (long)resource, (long)structure, saveGame, associate, preserveIds, copyObject);
            return Result;
        }

        public uint ReadStats(void* stats, void* resource, void* structure, void* appearance, int saveGame, int playerCharacter,
            int copyObject, int loadObjectId, nint original)
        {
            CallLog.Sink(nameof(ReadStats), original, 8, (long)stats, (long)resource, (long)structure, (long)appearance, saveGame,
                playerCharacter, copyObject, loadObjectId);
            return Result;
        }
    }

    private sealed class PlayerList : IPlayerListNamePrivacySink
    {
        public int SendPlayerListAdd(void* message, uint recipientId, void* newPlayer, nint original)
        {
            CallLog.Sink(nameof(SendPlayerListAdd), original, 3, (long)message, recipientId, (long)newPlayer);
            return Result;
        }
    }

    private sealed class Slots : IPlayerSlotAssignmentSink
    {
        public int SetSlidingWindow(void* netLayer, uint connectionId, uint* windowId, nint original)
        {
            CallLog.Sink(nameof(SetSlidingWindow), original, 3, (long)netLayer, connectionId, (long)windowId);
            return Result;
        }
    }

    private sealed class Quickbar : IQuickbarFeatLoadingSink
    {
        public void LoadQuickButtons(void* creature, void* resource, void* structure, nint original) =>
            CallLog.Sink(nameof(LoadQuickButtons), original, 3, (long)creature, (long)resource, (long)structure);
    }

    private sealed class AttackRoll : IResolveAttackRollSink
    {
        public void ResolveAttackRoll(void* thisPtr, void* pTarget) =>
            CallLog.Sink(nameof(ResolveAttackRoll), 0, 2, (long)thisPtr, (long)pTarget);
    }

    private sealed class Inventory : ISavedPlayerInventorySink
    {
        public int CompareItem(void* item, void* other, nint original)
        {
            CallLog.Sink(nameof(CompareItem), original, 2, (long)item, (long)other);
            return Result;
        }
    }

    private sealed class Drive : IStaleDriveAfterJumpSink
    {
        public uint JumpToPoint(void* creature, void* node, nint original)
        {
            CallLog.Sink(nameof(JumpToPoint), original, 2, (long)creature, (long)node);
            return Result;
        }

        public int AddDriveAction(void* creature, ushort groupId, void* pathStart, int bearing, int clientPathNumber, int driveFlags,
            int wayPointsToGenerate, nint original)
        {
            CallLog.Sink(nameof(AddDriveAction), original, 7, (long)creature, groupId, (long)pathStart, bearing, clientPathNumber,
                driveFlags, wayPointsToGenerate);
            return Result;
        }
    }

    private sealed class Vitality : IVitalityHitPointsSink
    {
        public short GetMaximum(void* pointer, int includeToughness, nint original)
        {
            CallLog.Sink(nameof(GetMaximum), original, 2, (long)pointer, includeToughness);
            return Result;
        }

        public void SetBase(void* pointer, byte score, int recalculateHP, nint original) =>
            CallLog.Sink(nameof(SetBase), original, 3, (long)pointer, score, recalculateHP);

        public void ApplyEffect(void* objectPointer, void* effectPointer, int loading, int initial, nint original) =>
            CallLog.Sink(nameof(ApplyEffect), original, 4, (long)objectPointer, (long)effectPointer, loading, initial);

        public void RemoveEffect(void* objectPointer, void* effectPointer, nint original) =>
            CallLog.Sink(nameof(RemoveEffect), original, 2, (long)objectPointer, (long)effectPointer);

        public int RemoveEffectById(void* objectPointer, ulong id, nint original)
        {
            CallLog.Sink(nameof(RemoveEffectById), original, 2, (long)objectPointer, (long)id);
            return Result;
        }

        public sbyte CalculateModifier(void* pointer, byte score, nint original)
        {
            CallLog.Sink(nameof(CalculateModifier), original, 2, (long)pointer, score);
            return ModifierResult;
        }

        public void DestroyStats(void* pointer, nint original) => CallLog.Sink(nameof(DestroyStats), original, 1, (long)pointer);
    }

    private sealed class Animation : IWeaponAttackAnimationSink
    {
        public uint ComputeUpdate(void* message, void* player, void* obj, void* last, int playerObject, nint original)
        {
            CallLog.Sink(nameof(ComputeUpdate), original, 5, (long)message, (long)player, (long)obj, (long)last, playerObject);
            return Result;
        }

        public void WriteUpdate(void* message, void* player, void* obj, void* last, uint updates, uint appearance, nint original) =>
            CallLog.Sink(nameof(WriteUpdate), original, 6, (long)message, (long)player, (long)obj, (long)last, updates, appearance);
    }
}
