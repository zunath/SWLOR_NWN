using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class SmokeBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        private static void ApplyEffect(uint creature)
        {
            ApplyEffectToObject(DurationType.Temporary, EffectInvisibility(InvisibilityType.Normal), creature, 6f);
        }

        [NWNEventHandler("grenade_smoke_en")]
        public static void SmokeBombEnter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature);
        }

        [NWNEventHandler("grenade_smoke_hb")]
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

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!HasExplosives(activator))
            {
                return "You have no explosives.";
            }

            return string.Empty;
        }

        private void SmokeBomb1()
        {
            _builder.Create(FeatType.SmokeBomb1, PerkType.SmokeBomb)
                .Name("Smoke Bomb I")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator, 
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        20f);

                    Enmity.ModifyEnmityOnAll(activator, 30);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void SmokeBomb2()
        {
            _builder.Create(FeatType.SmokeBomb2, PerkType.SmokeBomb)
                .Name("Smoke Bomb II")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(20f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        40f);
                });
        }

        private void SmokeBomb3()
        {
            _builder.Create(FeatType.SmokeBomb3, PerkType.SmokeBomb)
                .Name("Smoke Bomb III")
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(25f)
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogOfBewilderment,
                        "grenade_smoke_en",
                        "grenade_smoke_hb",
                        60f);
                });
        }
    }
}
