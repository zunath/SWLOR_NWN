using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.ValueObject
{
    public class AIPerkDetails
    {
        public Feat FeatID { get; set; }
        public PerkExecutionType ExecutionType { get; set; }

        public AIPerkDetails(Feat featID, PerkExecutionType executionType)
        {
            FeatID = featID;
            ExecutionType = executionType;
        }
    }
}
