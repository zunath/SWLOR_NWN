using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class IncendiaryBombAbilityDefinition : ExplosiveBaseAbilityDefinition
    {
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;

        public IncendiaryBombAbilityDefinition(IRandomService random, IItemService itemService, IPerkService perkService, IStatService statService, ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService) 
            : base(random, itemService, perkService, statService, combatService, combatPointService, enmityService, statusEffectService)
        {
            _combatService = combatService;
            _statService = statService;
        }

        private void ApplyEffect(uint creature, int dmg)
        {
            var attackerStat = GetLocalInt(OBJECT_SELF, "DEVICE_ACC");
            var attack = GetLocalInt(OBJECT_SELF, "DEVICE_ATK");
            dmg += GetLocalInt(OBJECT_SELF, "DEVICE_DMG");

            var defense = _statService.GetDefense(creature, CombatDamageType.Physical, AbilityType.Vitality);
            var defenderStat = GetAbilityScore(creature, AbilityType.Vitality);
            var damage = _combatService.CalculateDamage(
                attack,
                dmg, 
                attackerStat, 
                defense, 
                defenderStat, 
                0);

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), creature);
        }

        public void IncendiaryBomb1Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 4);
        }

        public void IncendiaryBomb1Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 4);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public void IncendiaryBomb2Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 10);
        }

        public void IncendiaryBomb2Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 10);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public void IncendiaryBomb3Enter()
        {
            var creature = GetEnteringObject();
            ApplyEffect(creature, 16);
        }

        public void IncendiaryBomb3Heartbeat()
        {
            var creature = GetFirstInPersistentObject(OBJECT_SELF);
            while (GetIsObjectValid(creature))
            {
                ApplyEffect(creature, 16);
                creature = GetNextInPersistentObject(OBJECT_SELF);
            }
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            IncendiaryBomb1(builder);
            IncendiaryBomb2(builder);
            IncendiaryBomb3(builder);

            return builder.Build();
        }
        
        private void IncendiaryBomb1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IncendiaryBomb1, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc1_en",
                        "grenade_inc1_hb",
                        20f);

                    _enmityService.ModifyEnmityOnAll(activator, 250);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void IncendiaryBomb2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IncendiaryBomb2, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(20f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc2_en",
                        "grenade_inc2_hb",
                        40f);

                    _enmityService.ModifyEnmityOnAll(activator, 350);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }

        private void IncendiaryBomb3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.IncendiaryBomb3, PerkType.IncendiaryBomb)
                .Name("Incendiary Bomb III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Bombs, 60f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .BreaksStealth()
                .HasMaxRange(25f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    ExplosiveAOEImpact(
                        activator,
                        targetLocation,
                        AreaOfEffect.FogFire,
                        "grenade_inc3_en",
                        "grenade_inc3_hb",
                        60f);

                    _enmityService.ModifyEnmityOnAll(activator, 450);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
                });
        }
    }
}
