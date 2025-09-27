using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class KoltoBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public KoltoBombAbilityDefinition(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {
        }

        private void ApplyEffect(uint creature, int hpRegen)
        {

            RemoveEffectByTag(creature, "kolto_regen"); // Get rid of any regen effects to prevent stacking

            Effect eKolto = EffectRegenerate(hpRegen, 6f);
            eKolto = TagEffect(eKolto, "kolto_regen");

            ApplyEffectToObject(DurationType.Temporary, eKolto, creature, 6f);
        }

        public void KoltoBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 4);
        }

        public void KoltoBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 4);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public void KoltoBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 12);
        }

        public void KoltoBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 12);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public void KoltoBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 20);
        }

        public void KoltoBomb3Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 20);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            KoltoBomb1(builder);
            KoltoBomb2(builder);
            KoltoBomb3(builder);

            return builder.Build();
        }
        
        private void KoltoBomb1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoBomb1, PerkType.KoltoBomb)
                .Name("Kolto Bomb I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffectType.FogMind,
                        "grenade_kolt1_en",
                        "grenade_kolt1_hb",
                        20f);

                    EnmityService.ModifyEnmityOnAll(activator, 220);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void KoltoBomb2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoBomb2, PerkType.KoltoBomb)
                .Name("Kolto Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(7)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(20f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffectType.FogMind,
                        "grenade_kolt2_en",
                        "grenade_kolt2_hb",
                        40f);

                    EnmityService.ModifyEnmityOnAll(activator, 340);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void KoltoBomb3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoBomb3, PerkType.KoltoBomb)
                .Name("Kolto Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(9)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(25f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffectType.FogMind,
                        "grenade_kolt3_en",
                        "grenade_kolt3_hb",
                        60f);

                    EnmityService.ModifyEnmityOnAll(activator, 480);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
