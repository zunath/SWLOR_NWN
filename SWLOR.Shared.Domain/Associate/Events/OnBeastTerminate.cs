using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastTerminate : BaseEvent
    {
        public override string Script => AssociateScriptName.OnBeastTerminate;
    }
}
