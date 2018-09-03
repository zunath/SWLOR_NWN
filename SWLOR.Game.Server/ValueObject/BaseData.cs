using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class BaseData
    {
        public NWArea TargetArea { get; set; }
        public Location TargetLocation { get; set; }
        public NWObject TargetObject { get; set; }
        public bool IsConfirmingPurchase { get; set; }
        public int ConfirmingPurchaseResponseID { get; set; }
    }
}
