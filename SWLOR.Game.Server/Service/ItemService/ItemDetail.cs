using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.ItemService
{
    public delegate void ApplyItemEffectsDelegate(uint user, uint item, uint target, Location targetLocation);

    public delegate string ValidateItemDelegate(uint user, uint item, uint target, Location targetLocation);
    public class ItemDetail
    {
        public string InitializationMessage { get; set; }
        public ValidateItemDelegate ValidateAction { get; set; }
        public ApplyItemEffectsDelegate ApplyAction { get; set; }
        public float Delay { get; set; }
        public bool UserFacesTarget { get; set; }
        public Animation ActivationAnimation { get; set; }
        public float MaximumDistance { get; set; }
        public bool ReducesItemCharge { get; set; }
        public bool CanTargetLocation { get; set; }

        public ItemDetail()
        {
            InitializationMessage = string.Empty;
            Delay = 0.0f;
            UserFacesTarget = false;
            ActivationAnimation = Animation.Invalid;
            MaximumDistance = 3.5f;
            ReducesItemCharge = false;
            CanTargetLocation = false;
        }
        
    }
}
