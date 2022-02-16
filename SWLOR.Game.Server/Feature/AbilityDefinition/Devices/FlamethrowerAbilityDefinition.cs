using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class FlamethrowerAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Flamethrower1();
            Flamethrower2();
            Flamethrower3();

            return _builder.Build();
        }

        private void Impact(uint activator, Location location, float dmg, int burningChance)
        {
            var origin = GetPositionFromLocation(location);
            const float ConeSize = 10f;

            AssignCommand(activator, () =>
            {
                // todo: fix the burning hands flame and play it here
            });

            var perception = GetAbilityModifier(AbilityType.Perception, activator);
            var target = GetFirstObjectInShape(Shape.Cone, ConeSize, location, true, ObjectType.Creature, origin);
            while (GetIsObjectValid(target))
            {
                if (target != activator)
                {
                    var defense = Stat.GetDefense(target, CombatDamageType.Physical) +
                                  Stat.GetDefense(target, CombatDamageType.Fire);
                    var damage = Combat.CalculateDamage(dmg, perception, defense, defense, 0);

                    ApplyEffectToObject(DurationType.Instant, EffectDamage(damage, DamageType.Fire), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S), target);

                    if (Random.D100(1) <= burningChance)
                    {
                        StatusEffect.Apply(activator, target, StatusEffectType.Burn, 30f);
                    }

                    Enmity.ModifyEnmity(activator, target, 20);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Devices, 3);

                }

                target = GetNextObjectInShape(Shape.Cone, ConeSize, location, true, ObjectType.Creature, origin);
            }
        }

        private void Flamethrower1()
        {
            _builder.Create(FeatType.Flamethrower1, PerkType.Flamethrower)
                .Name("Flamethrower I")
                .HasRecastDelay(RecastGroup.Flamethrower, 180f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var location = GetLocation(activator);
                    Impact(activator, location, 3.0f, 0);
                });
        }

        private void Flamethrower2()
        {
            _builder.Create(FeatType.Flamethrower2, PerkType.Flamethrower)
                .Name("Flamethrower II")
                .HasRecastDelay(RecastGroup.Flamethrower, 180f)
                .HasActivationDelay(1f)
                .RequirementStamina(4)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var location = GetLocation(activator);
                    Impact(activator, location,5.0f, 30);
                });
        }

        private void Flamethrower3()
        {
            _builder.Create(FeatType.Flamethrower3, PerkType.Flamethrower)
                .Name("Flamethrower III")
                .HasRecastDelay(RecastGroup.Flamethrower, 180f)
                .HasActivationDelay(1f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var location = GetLocation(activator);
                    Impact(activator, location, 8.0f, 50);
                });
        }
    }
}
