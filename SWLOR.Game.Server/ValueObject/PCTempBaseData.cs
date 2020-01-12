﻿using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript;

namespace SWLOR.Game.Server.ValueObject
{
    public class PCTempBaseData
    {
        public NWArea TargetArea { get; set; }
        public Location TargetLocation { get; set; }
        public NWObject TargetObject { get; set; }
        public bool IsConfirming { get; set; }
        public int ConfirmingPurchaseResponseID { get; set; }
        public Guid PCBaseID { get; set; }
        public BaseStructure BaseStructureID { get; set; }
        public Guid StructureID { get; set; }
        public bool IsPreviewing { get; set; }
        public NWPlaceable StructurePreview { get; set; }
        public NWItem StructureItem { get; set; }
        public BuildingType BuildingType { get; set; }
        public Guid? ParentStructureID { get; set; }
        public AreaStructure ManipulatingStructure { get; set; }
        public ApartmentType ApartmentType { get; set; }
        public BuildingStyle BuildingStyleID { get; set; }
        public int ExtensionDays { get; set; }
    }
}
