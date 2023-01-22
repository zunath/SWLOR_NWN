using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Bite();

            return _builder.Build();
        }

        private void Bite()
        {
            _builder.Create(PerkCategoryType.BeastDamage, PerkType.Bite)
                .Name("Bite")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("")
                .Price(1)
                .RequirementBeastLevel(5)

                .AddPerkLevel()
                .Description("")
                .Price(1)
                .RequirementBeastLevel(15)

                .AddPerkLevel()
                .Description("")
                .Price(1)
                .RequirementBeastLevel(25)

                .AddPerkLevel()
                .Description("")
                .Price(2)
                .RequirementBeastLevel(35)

                .AddPerkLevel()
                .Description("")
                .Price(2)
                .RequirementBeastLevel(45);
        }
    }
}
