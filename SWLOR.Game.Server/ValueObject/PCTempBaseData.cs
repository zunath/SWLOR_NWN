using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class PCTempBaseData
    {
        public NWArea TargetArea { get; set; }
        public Location TargetLocation { get; set; }
        public NWObject TargetObject { get; set; }
        public bool IsConfirming { get; set; }
        public int ConfirmingPurchaseResponseID { get; set; }
        public int PCBaseID { get; set; }
    }
}
