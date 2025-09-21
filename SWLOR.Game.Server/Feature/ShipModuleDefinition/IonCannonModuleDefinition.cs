using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class IonCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly IRandomService _random;
        private readonly ICombatService _combatService;
        private readonly ISpaceService _spaceService;
        private readonly IEnmityService _enmityService;
        private readonly ICombatPointService _combatPointService;
        private readonly ShipModuleBuilder _builder = new();

        public IonCannonModuleDefinition(IRandomService random, ICombatService combatService, ISpaceService spaceService, IEnmityService enmityService, ICombatPointService combatPointService)
        {
            _random = random;
            _combatService = combatService;
            _spaceService = spaceService;
            _enmityService = enmityService;
            _combatPointService = combatPointService;
        }

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            IonCannon("ion_cann_b", "Basic Ion Cannon", "B. Ion Cann.", "Deals 15 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 1, 6, 15);
            IonCannon("ion_cann_1", "Ion Cannon I", "Ion Cann. I", "Deals 30 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 2, 8, 30);
            IonCannon("ion_cann_2", "Ion Cannon II", "Ion Cann. II", "Deals 45 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 3, 10, 45);
            IonCannon("ion_cann_3", "Ion Cannon III", "Ion Cann. III", "Deals 60 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 4, 12, 60);
            IonCannon("ion_cann_4", "Ion Cannon IV", "Ion Cann. IV", "Deals 75 EM DMG to your target. Deals reduced damage to unshielded targets, but imposes debuffs.", 5, 14, 75);

            return _builder.Build();
        }

        private void IonCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int requiredLevel,
            int capacitor,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.IonCannon)
                .Texture("iit_ess_050")
                .Description(description)
                .MaxDistance(40f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(10f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = activatorShipStatus.EMDamage;
                    var attackerStat = _spaceService.GetAttackStat(activator);
                    var attack = _spaceService.GetShipAttack(activator, attackBonus);
                    var moduleDamage = dmg + moduleBonus / 2;

                    var defenseBonus = targetShipStatus.EMDefense * 2;
                    var defense = _spaceService.GetShipDefense(target, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = _combatService.CalculateDamage(
                        attack,
                        moduleDamage,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var chanceToHit = _spaceService.CalculateChanceToHit(activator, target);
                    var roll = _random.D100(1);
                    var isHit = roll <= chanceToHit;

                    if (isHit)
                    {
                        AssignCommand(activator, () =>
                        {
                            var effect = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
                            ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ion_Shot), activator);
                        });

                        DelayCommand(0.1f, () =>
                        {
                            AssignCommand(activator, () =>
                            {
                                var shieldDamage = int.Min(damage, targetShipStatus.Shield);
                                var armorDamage = (damage-shieldDamage)/2;
                                if (armorDamage < 0)
                                {
                                    armorDamage = 0;
                                }
                                var effect = EffectVisualEffect(VisualEffect.Vfx_Imp_Dispel, false, 0.5f);
                                ApplyEffectToObject(DurationType.Instant, effect, target);
                                _spaceService.ApplyShipDamage(activator, target, shieldDamage);
                                _spaceService.ApplyShipDamage(activator, target, armorDamage);
                                if (armorDamage > 0)
                                {
                                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(50), target, 6f);
                                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Agility, 2), target, 12f);
                                }
                            });
                        });
                    }
                    else
                    {
                        AssignCommand(activator, () =>
                        {
                            var effect = EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest, true);
                            ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                        });

                        DelayCommand(0.1f, () =>
                        {
                            AssignCommand(activator, () =>
                            {
                                var effect = EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion, true);
                                ApplyEffectToObject(DurationType.Instant, effect, target);
                            });
                        });
                    }

                    var attackId = isHit ? 1 : 4;
                    var combatLogMessage = _combatService.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                    Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                    _enmityService.ModifyEnmity(activator, target, damage);
                    _combatPointService.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
    }
}