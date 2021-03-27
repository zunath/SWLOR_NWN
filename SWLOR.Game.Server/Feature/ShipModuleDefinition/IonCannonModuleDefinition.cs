using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class IonCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            IonCannon("ion_cann_b", "Basic Ion Cannon", "B. Ion Cann.", "Deals light EM damage to your target.", 1, 3f, 6, 5);
            IonCannon("ion_cann_1", "Ion Cannon I", "Ion Cann. I", "Deals light EM damage to your target.", 2, 4f, 9, 8);
            IonCannon("ion_cann_2", "Ion Cannon II", "Ion Cann. II", "Deals light EM damage to your target.", 3, 5f, 12, 11);
            IonCannon("ion_cann_3", "Ion Cannon III", "Ion Cann. III", "Deals light EM damage to your target.", 4, 6f, 15, 14);
            IonCannon("ion_cann_4", "Ion Cannon IV", "Ion Cann. IV", "Deals light EM damage to your target.", 5, 7f, 18, 16);

            return _builder.Build();
        }

        private void IonCannon(string itemTag, string name, string shortName, string description, int requiredLevel, float recast, int capacitor, int baseDamage)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.IonCannon)
                .Texture("iit_ess_050")
                .Description(description)
                .RequiresTarget()
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(recast)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus) =>
                {
                    var targetDefense = targetShipStatus.EMDefense;
                    var attackerDamage = baseDamage + activatorShipStatus.EMDamage;

                    var damage = attackerDamage - targetDefense;
                    if (damage < 0) damage = 0;

                    var chanceToHit = Space.CalculateChanceToHit(activator, target);
                    var roll = Random.D100(1);
                    var isHit = roll <= chanceToHit;

                    if (isHit)
                    {
                        AssignCommand(activator, () =>
                        {
                            var effect = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
                            ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                        });

                        DelayCommand(0.1f, () =>
                        {
                            AssignCommand(activator, () =>
                            {
                                var effect = EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion);
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
                        SendMessageToPC(activator, "You miss your target.");
                    }

                    Enmity.ModifyEnmity(activator, target, damage);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
    }
}