using NWN;
using SWLOR.Game.Server.Event.Legacy;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Feat
{
    public class OnHitCastSpell: IRegisteredEvent
    {
        
        private readonly IDurabilityService _durability;
        private readonly IAbilityService _ability;
        
        

        public OnHitCastSpell(
            
            IDurabilityService durability,
            IAbilityService ability
            
            )
        {
            
            _durability = durability;
            _ability = ability;
            
            
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (Object.OBJECT_SELF);

            if (player.IsValid)
            {
                _durability.OnHitCastSpell(player);
                _ability.OnHitCastSpell(player);
                SkillService.OnHitCastSpell(player);
                PerkService.OnHitCastSpell(player);
                HandleItemSpecificCastSpell();
            }
            return true;
        }
        
        private void HandleItemSpecificCastSpell()
        {
            NWObject oSpellOrigin = (_.GetSpellCastItem());
            // Item specific
            string script = oSpellOrigin.GetLocalString("JAVA_SCRIPT");

            if (!string.IsNullOrWhiteSpace(script))
            {
                App.RunEvent<LegacyJVMItemEvent>(script);
            }
        }
    }
}
