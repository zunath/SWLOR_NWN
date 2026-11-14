using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Native;

namespace SWLOR.Game.Server.NativeSinks;

/// <summary>Keeps saved item stacks intact while a player file is being loaded.</summary>
public sealed unsafe class SavedPlayerInventorySink : ISavedPlayerInventorySink
{
    public int CompareItem(void* item, void* other, nint original)
    {
        // Native loading can combine differently priced blueprints before migrations
        // see them. Keep each saved stack; ordinary gameplay merging stays unchanged.
        if (SavedPlayerInventory.LoadDepth > 0)
            return 0;
        return ((delegate* unmanaged<void*, void*, int>)original)(item, other);
    }
}
