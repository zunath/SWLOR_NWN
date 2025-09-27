using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Common.Delegates;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Shared.Domain.Inventory.ValueObjects
{
    public class ItemDetail
    {
        public InitializationMessageDelegate InitializationMessageAction { get; set; }
        public ValidateItemDelegate ValidateAction { get; set; }
        public ApplyItemEffectsDelegate ApplyAction { get; set; }
        public CalculateDistanceDelegate CalculateDistanceAction { get; set; }
        public CalculateDelayDelegate DelayAction { get; set; }
        public bool UserFacesTarget { get; set; }
        public AnimationType ActivationAnimation { get; set; }
        public ReducesItemChargeDelegate ReducesItemChargeAction { get; set; }
        public bool CanTargetLocation { get; set; }
        public RecastGroup? RecastGroup { get; set; }
        public float? RecastCooldown { get; set; }

        public ItemDetail()
        {
            InitializationMessageAction = (user, item, target, location, itemPropertyIndex) => string.Empty;
            DelayAction = (user, item, target, location, itemPropertyIndex) => 0.0f;
            UserFacesTarget = false;
            ActivationAnimation = AnimationType.Invalid;
            CalculateDistanceAction = (user, item, target, location, itemPropertyIndex) => 3.5f;
            ReducesItemChargeAction = (user, item, target, location, itemPropertyIndex) => false;
            CanTargetLocation = false;
        }
    }
}
