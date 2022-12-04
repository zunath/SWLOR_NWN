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
            IonCannon("ion_cann_b", "Basic Ion Cannon", "B. Ion Cann.", "Deals 8 EM DMG to your target.", 1, 3f, 6, 8);
            IonCannon("ion_cann_1", "Ion Cannon I", "Ion Cann. I", "Deals 12 EM DMG to your target.", 2, 4f, 9, 12);
            IonCannon("ion_cann_2", "Ion Cannon II", "Ion Cann. II", "Deals 17 EM DMG to your target.", 3, 5f, 12, 17);
            IonCannon("ion_cann_3", "Ion Cannon III", "Ion Cann. III", "Deals 21 EM DMG to your target.", 4, 6f, 15, 21);
            IonCannon("ion_cann_4", "Ion Cannon IV", "Ion Cann. IV", "Deals 26 EM DMG to your target.", 5, 7f, 18, 26);
            IonCannon("cap_turbo_1", "Capital Ship Turbolaser I", "Cap Turbo I", "Deals 45 DMG to target every 2.5 rounds.", 5, 15f, 20, 45);
            IonCannon("cap_turbo_2", "Capital Ship Turbolaser II", "Cap Turbo II", "Deals 45 DMG to target every 1.5 rounds.", 5, 9f, 20, 45);
            IonCannon("cap_turbo_3", "Capital Ship Turbolaser III", "Cap Turbo III", "Deals 60 DMG to target every 2.5 rounds.", 5, 15f, 20, 60);
            IonCannon("cap_turbo_4", "Capital Ship Turbolaser IV", "Cap Turbo IV", "Deals 60 DMG to target every 1.5 rounds.", 5, 9f, 20, 60);
            IonCannon("cap_turbo_5", "Capital Ship Turbolaser V", "Cap Turbo V", "Deals 75 DMG to target every 2.5 rounds.", 5, 15f, 20, 75);
            IonCannon("cap_turbo_6", "Capital Ship Turbolaser VI", "Cap Turbo VI", "Deals 90 DMG to target every 2.5 rounds.", 5, 15f, 20, 90);
            IonCannon("cap_turbo_7", "Capital Ship Turbolaser VII", "Cap Turbo VII", "Deals 90 DMG to target every 1.5 rounds.", 5, 9f, 20, 90);

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
                    var combatLogMessage = Combat.BuildCombatLogMessage(activator, target, attackId, chanceToHit);
                    Messaging.SendMessageNearbyToPlayers(target, combatLogMessage, 60f);

                    Enmity.ModifyEnmity(activator, target, damage);
                    CombatPoint.AddCombatPoint(activator, target, SkillType.Piloting);
                });
        }
    }
}