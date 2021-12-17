using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class GeneralPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            WeaponFinesse();

            return _builder.Build();
        }

        private void WeaponFinesse()
        {

            _builder.Create(PerkCategoryType.General, PerkType.WeaponFinesse)
                .Name("Weapon Finesse")

                .AddPerkLevel()
                .Description("You make melee attack rolls with your PER score if it is higher than your MGT score.")
                .Price(3)
                .GrantsFeat(FeatType.WeaponFinesse);
        }
    }
}
