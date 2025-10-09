using SWLOR.Component.Character.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Events.Creature;

namespace SWLOR.Component.Character.EventHandlers
{
    internal class CharacterResourceEventHandlers : IEventHandler
    {
        private readonly ICharacterResourceService _characterResourceService;

        public CharacterResourceEventHandlers(
            ICharacterResourceService characterResourceService)
        {
            _characterResourceService = characterResourceService;
        }


        [ScriptHandler<OnCreatureHeartbeatAfter>]
        public void NPCNaturalRegen()
        {
            _characterResourceService.NPCNaturalRegen();
        }
    }
}
