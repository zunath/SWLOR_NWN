using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Domain.Character.Events;

namespace SWLOR.Component.Character.EventHandlers
{
    internal class StatApplicationEventHandlers
    {
        private readonly IStatApplicationService _statApplication;
        public StatApplicationEventHandlers(
            IStatApplicationService statApplication)
        {
            _statApplication = statApplication;
        }


        [ScriptHandler<OnCharacterMaxHPChanged>]
        public void ApplyMaxHPChange()
        {
            _statApplication.ApplyCharacterMaxHP(OBJECT_SELF);
        }
        
    }
}
