using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    public class OnModuleApplyDamage: IRegisteredEvent
    {
        private readonly IModService _mod;
        private readonly IAbilityService _ability;

        public OnModuleApplyDamage(IModService mod,
            IAbilityService ability)
        {
            _mod = mod;
            _ability = ability;
        }

        public bool Run(params object[] args)
        {
            _mod.OnModuleApplyDamage();
            _ability.OnModuleApplyDamage();
            return true;
        }
    }
}
