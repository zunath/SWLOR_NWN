using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class GeneralPerkDefinition: IPerkListDefinition
    {
        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            var builder = new PerkBuilder();
            Dash(builder);

            return builder.Build();
        }

        private static void Dash(PerkBuilder builder)
        {
            builder.Create(PerkCategoryType.General, PerkType.Dash)
                .Description("Increases your movement speed for a short period of time.")

                .AddPerkLevel()
                .Description("+25% speed, lasts 1 minute")
                .GrantsFeat(Feat.Dash)
                .Price(2)

                .AddPerkLevel()
                .Description("+30% speed, lasts 1 minute")
                .Price(2)

                .AddPerkLevel()
                .Description("+35% speed, lasts 1 minute")
                .Price(3)

                .AddPerkLevel()
                .Description("+40% speed, lasts 1 minute")
                .Price(3)

                .AddPerkLevel()
                .Description("+45% speed, lasts 1 minute")
                .Price(3)

                .AddPerkLevel()
                .Description("+50% speed, lasts 1 minute")
                .Price(4)

                .AddPerkLevel()
                .Description("+50% speed, lasts 2 minutes")
                .Price(5);
        }
    }
}
