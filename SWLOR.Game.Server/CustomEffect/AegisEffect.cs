using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.CustomEffect
{
    public class AegisEffect: ICustomEffect
    {
        private readonly ISkillService _skill;

        public AegisEffect(ISkillService skill)
        {
            _skill = skill;
        }

        public void Apply(NWCreature oCaster, NWObject oTarget)
        {
            _skill.ApplyStatChanges((NWPlayer)oTarget, null);
        }

        public void Tick(NWCreature oCaster, NWObject oTarget)
        {
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget)
        {
            _skill.ApplyStatChanges((NWPlayer)oTarget, null);
        }
    }
}
