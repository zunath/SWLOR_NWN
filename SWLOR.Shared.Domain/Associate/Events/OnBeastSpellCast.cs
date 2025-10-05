using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Associate.Events
{
    public class OnBeastSpellCast : BaseEvent
    {
        public override string Script => ScriptName.OnBeastSpellCast;
    }
}
