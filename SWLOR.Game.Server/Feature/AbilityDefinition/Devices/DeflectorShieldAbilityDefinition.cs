﻿using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class DeflectorShieldAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {

            return _builder.Build();
        }
    }
}
