using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class AreaStructure
    {
        public int PCBaseID { get; set; }
        public int PCBaseStructureID { get; set; }
        public NWPlaceable Structure { get; set; }
        public bool IsEditable { get; set; }
        public NWPlaceable ChildStructure { get; set; }

        public AreaStructure(
            int pcBaseID, 
            int pcBaseStructureID, 
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
