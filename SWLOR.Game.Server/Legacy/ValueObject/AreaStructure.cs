using System;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.ValueObject
{
    public class AreaStructure
    {
        public Guid PCBaseID { get; set; }
        public Guid PCBaseStructureID { get; set; }
        public NWPlaceable Structure { get; set; }
        public bool IsEditable { get; set; }
        public NWPlaceable ChildStructure { get; set; }

        public AreaStructure(
            Guid pcBaseID, 
            Guid pcBaseStructureID, 
            NWPlaceable structure, 
            bool isEditable,
            NWPlaceable childStructure)
        {
            PCBaseID = pcBaseID;
            PCBaseStructureID = pcBaseStructureID;
            Structure = structure;
            IsEditable = isEditable;
            ChildStructure = childStructure;
        }
    }
}
