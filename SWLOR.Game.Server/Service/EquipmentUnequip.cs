using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// The static API of the equipment removal guards. The logic and state live in <see cref="IEquipmentUnequipService"/>;
/// every member here forwards to the container's instance, and the event handler stays here so its name in the handler
/// order manifest does not change.
/// </summary>
public static class EquipmentUnequip
{
    private static IEquipmentUnequipService Instance => FacadeAccessor.Resolve<IEquipmentUnequipService>();

    public static void RegisterGuard(string key, Func<uint, uint, bool> guard)
    {
        Instance.RegisterGuard(key, guard);
    }

    public static bool TryHandle(uint creature, uint item)
    {
        return Instance.TryHandle(creature, item);
    }

    [NWNEventHandler(ScriptName.OnEquipmentUnequipBefore)]
    public static void BeforeUnequip()
    {
        Instance.BeforeUnequip();
    }
}
