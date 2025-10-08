using SWLOR.Component.Character.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
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

        [ScriptHandler<OnCharacterMightChanged>]
        public void ApplyMightChange()
        {
            _statApplication.ApplyCharacterMight(OBJECT_SELF);
        }


        [ScriptHandler<OnCharacterPerceptionChanged>]
        public void ApplyPerceptionChange()
        {
            _statApplication.ApplyCharacterPerception(OBJECT_SELF);
        }


        [ScriptHandler<OnCharacterVitalityChanged>]
        public void ApplyVitalityChange()
        {
            _statApplication.ApplyCharacterVitality(OBJECT_SELF);
        }


        [ScriptHandler<OnCharacterWillpowerChanged>]
        public void ApplyWillpowerChange()
        {
            _statApplication.ApplyCharacterWillpower(OBJECT_SELF);
        }


        [ScriptHandler<OnCharacterAgilityChanged>]
        public void ApplyAgilityChange()
        {
            _statApplication.ApplyCharacterAgility(OBJECT_SELF);
        }


        [ScriptHandler<OnCharacterSocialChanged>]
        public void ApplySocialChange()
        {
            _statApplication.ApplyCharacterSocial(OBJECT_SELF);
        }

    }
}
