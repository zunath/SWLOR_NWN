using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Performance
{
    public class PlayInstrumentAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PlayInstrument(builder, FeatType.PlayInstrument1, "Play Instrument I", 1);
            PlayInstrument(builder, FeatType.PlayInstrument2, "Play Instrument II", 2);
            PlayInstrument(builder, FeatType.PlayInstrument3, "Play Instrument III", 3);
            PlayInstrument(builder, FeatType.PlayInstrument4, "Play Instrument IV", 4);

            return builder.Build();
        }

        private static void PlayInstrument(AbilityBuilder builder, FeatType featType, string name, int level)
        {
            builder
                .Create(featType, PerkType.PlayInstrument)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.PlayGuitar)
                .PreservesNativeAnimationChoreography()
                .UsesNativeAnimationPreview(Animation.PlayGuitar, 3f)
                .SkillType(SkillType.Performance)
                .HasImpactAction(PlayInstrumentImpactAction)
                .IsCastedAbility();
        }

        private static void PlayInstrumentImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Service.Performance.TogglePlayInstrument(activator, level);
        }
    }
}
