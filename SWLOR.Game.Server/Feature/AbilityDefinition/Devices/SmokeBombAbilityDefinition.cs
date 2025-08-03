using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class SmokeBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        private static void ApplyEffect(uint creature)
        {
            ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), creature, 6f);
        }

        [NWNEventHandler(ScriptName.OnGrenadeSmokeEnable)]
        public static void SmokeBombEnter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature);
        }

        [NWNEventHandler(ScriptName.OnGrenadeSmokeHeartbeat)]
        public static void SmokeBombHeartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SmokeBomb1();
            SmokeBomb2();
            SmokeBomb3();

            return _builder.Build();
        }
        
        private void SmokeBomb1()
        {
            _builder.Create(FeatType.SmokeBomb1, PerkType.SmokeBomb)
                .Name("Smoke Bomb I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator, 
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        20f);

                    Enmity.ModifyEnmityOnAll(activator, 90);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void SmokeBomb2()
        {
            _builder.Create(FeatType.SmokeBomb2, PerkType.SmokeBomb)
                .Name("Smoke Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(20f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        40f);

                    Enmity.ModifyEnmityOnAll(activator, 180);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void SmokeBomb3()
        {
            _builder.Create(FeatType.SmokeBomb3, PerkType.SmokeBomb)
                .Name("Smoke Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(25f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        60f);

                    Enmity.ModifyEnmityOnAll(activator, 360);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
