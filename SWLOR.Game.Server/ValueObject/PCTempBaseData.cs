using NWN;
using SWLOR.Game.Server.Enumeration;
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
        public int StructureID { get; set; }
        public bool IsPreviewing { get; set; }
        public NWPlaceable StructurePreview { get; set; }
        public NWItem StructureItem { get; set; }
        public BuildingType BuildingType { get; set; }
        public int? ParentStructureID { get; set; }
        public AreaStructure ManipulatingStructure { get; set; }
        public int ApartmentBuildingID { get; set; }
        public int BuildingStyleID { get; set; }
        public int ExtensionDays { get; set; }
    }
}
