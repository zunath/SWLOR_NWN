using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Service.ImplantService
{
    public delegate void ImplantInstalledDelegate(uint creature);

    public delegate void ImplantUninstalledDelegate(uint creature);

    public delegate string ImplantValidationDelegate(uint creature);

    public class ImplantDetail
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ImplantSlotType Slot { get; set; }
        public int RequiredLevel { get; set; }
        public int HPAdjustment { get; set; }
        public int HPRegenAdjustment { get; set; }
        public int FPAdjustment { get; set; }
        public int FPRegenAdjustment { get; set; }
        public int STMAdjustment { get; set; }
        public int STMRegenAdjustment { get; set; }
        public float MovementRateAdjustment { get; set; }
        public Dictionary<AbilityType, int> StatAdjustments { get; set; }
        public ImplantInstalledDelegate InstalledAction { get; set; }
        public ImplantUninstalledDelegate UninstalledAction { get; set; }
        public ImplantValidationDelegate ValidationAction { get; set; }

        public ImplantDetail()
        {
            StatAdjustments = new Dictionary<AbilityType, int>();
        }
    }
}
