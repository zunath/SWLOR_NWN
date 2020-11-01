using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ValueObject
{
    public class PCTempBaseData
    {
        public uint TargetArea { get; set; }
        public Location TargetLocation { get; set; }
        public NWObject TargetObject { get; set; }
        public bool IsConfirming { get; set; }
        public int ConfirmingPurchaseResponseID { get; set; }
        public Guid PCBaseID { get; set; }
        public int BaseStructureID { get; set; }
        public Guid StructureID { get; set; }
        public bool IsPreviewing { get; set; }
        public NWPlaceable StructurePreview { get; set; }
        public NWItem StructureItem { get; set; }
        public BuildingType BuildingType { get; set; }
        public Guid? ParentStructureID { get; set; }
        public AreaStructure ManipulatingStructure { get; set; }
        public int ApartmentBuildingID { get; set; }
        public int BuildingStyleID { get; set; }
        public int ExtensionDays { get; set; }
    }
}
