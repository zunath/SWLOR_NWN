using NWN.Native.API;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>Saved quickbars may contain either targeting representation of an ability.</summary>
public sealed unsafe class QuickbarFeatLoadingSink : IQuickbarFeatLoadingSink
{
    public void LoadQuickButtons(void* creature, void* resource, void* structure, nint original)
    {
        ((delegate* unmanaged<void*, void*, void*, void>)original)(creature, resource, structure);
        try
        {
            var id = CNWSCreature.FromPointer(creature).m_idSelf;
            ServerManager.Executor.ExecuteInScriptContext(() => QuickCast.ApplyPreferences(id), id);
        }
        catch (Exception exception) { Log.WriteError(exception, "Ability targeting quickbar restoration failed"); }
    }
}
