using NWN;
using SWLOR.Game.Server.Event.Legacy;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Event.Feat
{
    public class OnHitCastSpell: IRegisteredEvent
    {
        
        private readonly IDurabilityService _durability;
        private readonly IAbilityService _ability;
        private readonly ISkillService _skill;
        private readonly IPerkService _perk;

        public OnHitCastSpell(
            
            IDurabilityService durability,
            IAbilityService ability,
            ISkillService skill,
            IPerkService perk)
        {
            
            _durability = durability;
            _ability = ability;
            _skill = skill;
            _perk = perk;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (Object.OBJECT_SELF);

            if (player.IsValid)
            {
                _durability.OnHitCastSpell(player);
                _ability.OnHitCastSpell(player);
                _skill.OnHitCastSpell(player);
                _perk.OnHitCastSpell(player);
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
