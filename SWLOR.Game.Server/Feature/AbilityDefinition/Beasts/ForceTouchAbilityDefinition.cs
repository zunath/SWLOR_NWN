using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class ForceTouchAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ForceTouch1();
            ForceTouch2();
            ForceTouch3();
            ForceTouch4();
            ForceTouch5();

            return _builder.Build();
        }

        private void ForceTouch1()
        {

        }

        private void ForceTouch2()
        {

        }

        private void ForceTouch3()
        {

        }

        private void ForceTouch4()
        {

        }

        private void ForceTouch5()
        {

        }


    }
}
