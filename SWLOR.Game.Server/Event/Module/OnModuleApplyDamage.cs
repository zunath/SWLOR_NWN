using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleApplyDamage: IRegisteredEvent
    {
        private readonly IRuneService _rune;
        private readonly IAbilityService _ability;

        public OnModuleApplyDamage(IRuneService rune,
            IAbilityService ability)
        {
            _rune = rune;
            _ability = ability;
        }

        public bool Run(params object[] args)
        {
            _rune.OnModuleApplyDamage();
            _ability.OnModuleApplyDamage();
            return true;
        }
    }
}
