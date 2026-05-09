using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class SaberstaffActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureMultiHit(builder.Create(FeatType.DoubleStrike1, PerkType.DoubleStrike).Name("Double Strike I").Level(1), SkillType.Saberstaff, 12, 2, 4);
            ConfigureTelegraphedArea(builder.Create(FeatType.CircleSlash1, PerkType.CircleSlash).Name("Circle Slash I").Level(1), SkillType.Saberstaff, CombatImpactAreaShape.Sphere, 10, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 5, true);
            ConfigureToggle(builder.Create(FeatType.TempestStance1, PerkType.TempestStance).Name("Tempest Stance").Level(1), typeof(TempestStanceStatusEffect));
            ConfigureMultiHit(builder.Create(FeatType.DoubleStrike2, PerkType.DoubleStrike).Name("Double Strike II").Level(2), SkillType.Saberstaff, 21, 2, 6);
            ConfigureTelegraphedArea(builder.Create(FeatType.CircleSlash2, PerkType.CircleSlash).Name("Circle Slash II").Level(2), SkillType.Saberstaff, CombatImpactAreaShape.Sphere, 18, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 6, true);
            ConfigureMultiHit(builder.Create(FeatType.DoubleStrike3, PerkType.DoubleStrike).Name("Double Strike III").Level(3), SkillType.Saberstaff, 29, 2, 8);
            ConfigureTelegraphedArea(builder.Create(FeatType.CircleSlash3, PerkType.CircleSlash).Name("Circle Slash III").Level(3), SkillType.Saberstaff, CombatImpactAreaShape.Sphere, 28, 0, 0, SavingThrow.Reflex, null, 5f, 0f, 8, true);
            ConfigureMultiHit(builder.Create(FeatType.DoubleStrike4, PerkType.DoubleStrike).Name("Double Strike IV").Level(4), SkillType.Saberstaff, 38, 2, 10, bonusStatus: typeof(ForceErosionStatusEffect), bonusDamage: 15);
            ConfigureSelfStatus(builder.Create(FeatType.GuardedChannel1, PerkType.GuardedChannel).Name("Guarded Channel I").Level(1), typeof(GuardedChannelStatusEffect), 10f, 6);
            ConfigureToggle(builder.Create(FeatType.ConduitStance1, PerkType.ConduitStance).Name("Conduit Stance").Level(1), typeof(ConduitStanceStatusEffect));
            ConfigurePartyStatus(builder.Create(FeatType.ForceLens1, PerkType.ForceLens).Name("Force Lens").Level(1), typeof(ForceLensStatusEffect), 45f, 15, true);
            ConfigureSelfStatus(builder.Create(FeatType.GuardedChannel2, PerkType.GuardedChannel).Name("Guarded Channel II").Level(2), typeof(GuardedChannelStatusEffect), 12f, 8);
            ConfigureSelfStatus(builder.Create(FeatType.GuardedChannel3, PerkType.GuardedChannel).Name("Guarded Channel III").Level(3), typeof(GuardedChannelStatusEffect), 15f, 10);
            ConfigureSelfStatus(builder.Create(FeatType.ForceCapacitor1, PerkType.ForceCapacitor).Name("Force Capacitor").Level(1), typeof(ForceCapacitorStatusEffect), 20f, 5);
            ConfigureSelfStatus(builder.Create(FeatType.InfiniteConduit1, PerkType.InfiniteConduit).Name("Infinite Conduit").Level(1), typeof(InfiniteConduitStatusEffect), 20f, 5);


            return builder.Build();
        }
    }
}
