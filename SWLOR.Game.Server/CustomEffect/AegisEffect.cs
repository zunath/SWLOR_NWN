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

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            _skill.ApplyStatChanges((NWPlayer)oTarget, null);

            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            _skill.ApplyStatChanges((NWPlayer)oTarget, null);
        }
    }
}
