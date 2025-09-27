using SWLOR.NWN.API.Engine;

namespace SWLOR.Shared.Domain.Inventory.Delegates
{
    public delegate void ApplyItemEffectsDelegate(uint user, uint item, uint target, Location targetLocation, int itemPropertyIndex);

    public delegate string ValidateItemDelegate(uint user, uint item, uint target, Location targetLocation, int itemPropertyIndex);

    public delegate float CalculateDistanceDelegate(uint user, uint item, uint target, Location targetLocation, int itemPropertyIndex);

    public delegate bool ReducesItemChargeDelegate(uint user, uint item, uint target, Location targetLocation, int itemPropertyIndex);

    public delegate string InitializationMessageDelegate(uint user, uint item, uint target, Location targetLocation, int itemPropertyIndex);

    public delegate float CalculateDelayDelegate(uint user, uint item, uint target, Location targetLocation, int itemPropertyIndex);

}
