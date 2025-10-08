using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.Service;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Domain.Character.Events;
using SWLOR.Shared.Events.Events.Creature;

namespace SWLOR.Component.Character.EventHandlers
{
    internal class CharacterStatEventHandlers
    {
        private readonly ICharacterStatService _characterStatService;

        public CharacterStatEventHandlers(
            ICharacterStatService characterStatService)
        {
            _characterStatService = characterStatService;
        }


        [ScriptHandler<OnCreatureSpawnBefore>]
        public void RegisterNPC()
        {
            _characterStatService.RegisterNPC(OBJECT_SELF);
        }

        [ScriptHandler<OnCreatureDeathAfter>]
        public void UnregisterNPC()
        {
            _characterStatService.UnregisterNPC(OBJECT_SELF);
        }

    }
}
