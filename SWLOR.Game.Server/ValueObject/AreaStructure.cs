using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class AreaStructure
    {
        public int PCBaseID { get; set; }
        public int PCBaseStructureID { get; set; }
        public NWPlaceable Structure { get; set; }

        public AreaStructure(int pcBaseID, int pcBaseStructureID, NWPlaceable structure)
        {
            PCBaseID = pcBaseID;
            PCBaseStructureID = pcBaseStructureID;
            Structure = structure;
        }
    }
}
