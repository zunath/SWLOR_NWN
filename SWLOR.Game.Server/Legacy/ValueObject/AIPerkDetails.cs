using SWLOR.Game.Server.Legacy.Enumeration;

namespace SWLOR.Game.Server.Legacy.ValueObject
{
    public class AIPerkDetails
    {
        public int FeatID { get; set; }
        public PerkExecutionType ExecutionType { get; set; }

        public AIPerkDetails(int featID, PerkExecutionType executionType)
        {
            FeatID = featID;
            ExecutionType = executionType;
        }
    }
}
