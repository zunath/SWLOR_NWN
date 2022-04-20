using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.ItemService
{
    public delegate void ApplyItemEffectsDelegate(uint user, uint item, uint target, Location targetLocation);

    public delegate string ValidateItemDelegate(uint user, uint item, uint target, Location targetLocation);

    public delegate float CalculateDistanceDelegate(uint user, uint item, uint target, Location targetLocation);

    public delegate bool ReducesItemChargeDelegate(uint user, uint item, uint target, Location targetLocation);

    public delegate string InitializationMessageDelegate(uint user, uint item, uint target, Location targetLocation);

    public delegate float CalculateDelayDelegate(uint user, uint item, uint target, Location targetLocation);
    public class ItemDetail
    {
        public InitializationMessageDelegate InitializationMessageAction { get; set; }
        public ValidateItemDelegate ValidateAction { get; set; }
        public ApplyItemEffectsDelegate ApplyAction { get; set; }
        public CalculateDistanceDelegate CalculateDistanceAction { get; set; }
        public CalculateDelayDelegate DelayAction { get; set; }
        public bool UserFacesTarget { get; set; }
        public Animation ActivationAnimation { get; set; }
        public ReducesItemChargeDelegate ReducesItemChargeAction { get; set; }
        public bool CanTargetLocation { get; set; }

        public ItemDetail()
        {
            InitializationMessageAction = (user, item, target, location) => string.Empty;
            DelayAction = (user, item, target, location) => 0.0f;
            UserFacesTarget = false;
            ActivationAnimation = Animation.Invalid;
            CalculateDistanceAction = (user, item, target, location) => 3.5f;
            ReducesItemChargeAction = (user, item, target, location) => false;
            CanTargetLocation = false;
        }
        
    }
}
