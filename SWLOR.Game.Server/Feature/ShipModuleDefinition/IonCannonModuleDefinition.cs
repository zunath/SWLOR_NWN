using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class IonCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            IonCannon("ion_cann_b", "Basic Ion Cannon", "B. Ion Cann.", "Deals light EM damage to your target.", 1, 3f, 6, 8);
            IonCannon("ion_cann_1", "Ion Cannon I", "Ion Cann. I", "Deals light EM damage to your target.", 2, 4f, 9, 12);
            IonCannon("ion_cann_2", "Ion Cannon II", "Ion Cann. II", "Deals light EM damage to your target.", 3, 5f, 12, 17);
            IonCannon("ion_cann_3", "Ion Cannon III", "Ion Cann. III", "Deals light EM damage to your target.", 4, 6f, 15, 21);
            IonCannon("ion_cann_4", "Ion Cannon IV", "Ion Cann. IV", "Deals light EM damage to your target.", 5, 7f, 18, 26);

            return _builder.Build();
        }

        private void IonCannon(
            string itemTag, 
            string name, 
            string shortName, 
            string description, 
            int requiredLevel, 
            float recast, 
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
                .RequiresTarget()
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(recast)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var attackBonus = moduleBonus * 2 + activatorShipStatus.EMDamage;
                    var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
                    var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Piloting, attackBonus);

                    var defenseBonus = targetShipStatus.EMDefense * 2;
                    var defense = Stat.GetDefense(target, CombatDamageType.EM, AbilityType.Vitality, defenseBonus);
                    var defenderStat = GetAbilityScore(target, AbilityType.Vitality);
                    var damage = Combat.CalculateDamage(
                        attack,
                        dmg,
                        attackerStat,
                        defense,
                        defenderStat,
                        0);

                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
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
                                var effect = EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion,false, 0.5f);
                                ApplyEffectToObject(DurationType.Instant, effect, target);
                                Space.ApplyShipDamage(activator, target, damage);
                            });
                        });
                    }
                    else
                    {
                        AssignCommand(activator, () =>
                        {
                            var effect = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest, true);
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
                    var combatLogMessage = Combat.BuildCombatLogMessage(GetName(activator), GetName(target), attackId, chanceToHit);
                    Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                    Enmity.ModifyEnmity(activator, target, damage);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
    }
}