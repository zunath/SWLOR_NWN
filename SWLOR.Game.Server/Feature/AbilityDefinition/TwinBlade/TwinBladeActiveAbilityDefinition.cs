using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class TwinBladeActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureMultiHit(builder.Create(FeatType.CrossCut1, PerkType.CrossCut).Name("Cross Cut I").Level(1), SkillType.TwinBlade, 8, 2, 4, 8, 12, SavingThrow.Reflex, typeof(DisorientedStatusEffect));
            ConfigureTelegraphedArea(builder.Create(FeatType.SpinningWhirl1, PerkType.SpinningWhirl).Name("Spinning Whirl I").Level(1), SkillType.TwinBlade, CombatImpactAreaShape.Sphere, 10, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 5, true);
            ConfigureToggle(builder.Create(FeatType.CycloneStance1, PerkType.CycloneStance).Name("Cyclone Stance").Level(1), typeof(CycloneStanceStatusEffect));
            ConfigureMultiHit(builder.Create(FeatType.CrossCut2, PerkType.CrossCut).Name("Cross Cut II").Level(2), SkillType.TwinBlade, 17, 2, 6, 10, 15, SavingThrow.Reflex, typeof(DisorientedStatusEffect));
            ConfigureTelegraphedArea(builder.Create(FeatType.SpinningWhirl2, PerkType.SpinningWhirl).Name("Spinning Whirl II").Level(2), SkillType.TwinBlade, CombatImpactAreaShape.Sphere, 18, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 6, true);
            ConfigureMultiHit(builder.Create(FeatType.CrossCut3, PerkType.CrossCut).Name("Cross Cut III").Level(3), SkillType.TwinBlade, 25, 2, 8, 12, 18, SavingThrow.Reflex, typeof(DisorientedStatusEffect));
            ConfigureTelegraphedArea(builder.Create(FeatType.SpinningWhirl3, PerkType.SpinningWhirl).Name("Spinning Whirl III").Level(3), SkillType.TwinBlade, CombatImpactAreaShape.Sphere, 28, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 8, true);
            ConfigureMultiHit(builder.Create(FeatType.CrossCut4, PerkType.CrossCut).Name("Cross Cut IV").Level(4), SkillType.TwinBlade, 34, 2, 10, 12, 18, SavingThrow.Reflex, typeof(DisorientedStatusEffect), typeof(HamstringStatusEffect));
            ConfigureToggle(builder.Create(FeatType.DuelistStance1, PerkType.DuelistStance).Name("Duelist Stance").Level(1), typeof(DuelistStanceStatusEffect));
            ConfigureTargetStatus(builder.Create(FeatType.DuelistsChallenge1, PerkType.DuelistsChallenge).Name("Duelist's Challenge").Level(1), typeof(DuelistsChallengeStatusEffect), 20f, 5);
            ConfigureSelfStatus(builder.Create(FeatType.FinalForm1, PerkType.FinalForm).Name("Final Form").Level(1), typeof(FinalFormStatusEffect), 20f, 8);


            return builder.Build();
        }
    }
}
