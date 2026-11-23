using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Native;
using ObjectType = NWN.Native.API.ObjectType;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>
/// Tells <see cref="Domain.Companions.IProgFxCoatService"/> when a coating visual effect or petrification is applied to or removed from a
/// creature. <see cref="ProgFxCoatEffectHooks"/> installs the hooks; each body calls the original function first, then
/// queues the creature's tint refresh.
/// </summary>
public sealed unsafe class ProgFxCoatEffectHooksSink : IProgFxCoatEffectHooksSink
{
    public int ApplyVisualEffect(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original)
    {
        var result = ((delegate* unmanaged<void*, void*, void*, int, int>)original)(effectListHandler, pObject, pEffect, bLoadingGame);
        QueueVisualEffect(pObject, pEffect);
        return result;
    }

    public int RemoveVisualEffect(void* effectListHandler, void* pObject, void* pEffect, nint original)
    {
        var result = ((delegate* unmanaged<void*, void*, void*, int>)original)(effectListHandler, pObject, pEffect);
        QueueVisualEffect(pObject, pEffect);
        return result;
    }

    public int ApplyPetrify(void* effectListHandler, void* pObject, void* pEffect, int bLoadingGame, nint original)
    {
        var result = ((delegate* unmanaged<void*, void*, void*, int, int>)original)(effectListHandler, pObject, pEffect, bLoadingGame);
        QueueCreature(pObject);
        return result;
    }

    public int RemovePetrify(void* effectListHandler, void* pObject, void* pEffect, nint original)
    {
        var result = ((delegate* unmanaged<void*, void*, void*, int>)original)(effectListHandler, pObject, pEffect);
        QueueCreature(pObject);
        return result;
    }

    private static void QueueVisualEffect(void* pObject, void* pEffect)
    {
        try
        {
            // Integer 0 is the visualeffects.2da row. Most effects never coat; skip them cheaply.
            if (ProgFxCoat.IsCoatingVisualEffect(CGameEffect.FromPointer(pEffect).GetInteger(0)))
                QueueCreature(pObject);
        }
        catch (Exception ex)
        {
            LogFailure(ex);
        }
    }

    private static void QueueCreature(void* pObject)
    {
        try
        {
            var target = CNWSObject.FromPointer(pObject);
            if (target == null || target.m_nObjectType != (int)ObjectType.Creature)
                return;

            var creature = target.m_idSelf;
            ServerManager.Executor.ExecuteInScriptContext(() => TintMapApply.QueueCoatRefresh(creature), creature);
        }
        catch (Exception ex)
        {
            LogFailure(ex);
        }
    }

    private static void LogFailure(Exception ex)
    {
        Log.Write(LogGroup.Error, $"ProgFX coat effect hook failed: {ex}", true);
    }
}
