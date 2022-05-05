using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class KoltoBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly AbilityBuilder _builder = new();

        private static void ApplyEffect(uint creature, int hpRegen)
        {
            ApplyEffectToObject(DurationType.Temporary, EffectRegenerate(hpRegen, 6f), creature, 6f);
        }

        [NWNEventHandler("grenade_kolt1_en")]
        public static void KoltoBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 4);
        }

        [NWNEventHandler("grenade_kolt1_hb")]
        public static void KoltoBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 4);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler("grenade_kolt2_en")]
        public static void KoltoBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 12);
        }

        [NWNEventHandler("grenade_kolt2_hb")]
        public static void KoltoBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 12);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        [NWNEventHandler("grenade_kolt3_en")]
        public static void KoltoBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 20);
        }

        [NWNEventHandler("grenade_kolt3_hb")]
        public static void KoltoBomb3Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 20);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            KoltoBomb1();
            KoltoBomb2();
            KoltoBomb3();

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

        private void KoltoBomb1()
        {
            _builder.Create(FeatType.KoltoBomb1, PerkType.KoltoBomb)
                .Name("Kolto Bomb I")
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
                        AreaOfEffect.FogMind,
                        "grenade_kolt1_en",
                        "grenade_kolt1_hb",
                        20f);

                    Enmity.ModifyEnmityOnAll(activator, 30);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void KoltoBomb2()
        {
            _builder.Create(FeatType.KoltoBomb2, PerkType.KoltoBomb)
                .Name("Kolto Bomb II")
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
                        AreaOfEffect.FogMind,
                        "grenade_kolt2_en",
                        "grenade_kolt2_hb",
                        40f);
                });
        }

        private void KoltoBomb3()
        {
            _builder.Create(FeatType.KoltoBomb3, PerkType.KoltoBomb)
                .Name("Kolto Bomb III")
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
                        AreaOfEffect.FogMind,
                        "grenade_kolt3_en",
                        "grenade_kolt3_hb",
                        60f);
                });
        }
    }
}
