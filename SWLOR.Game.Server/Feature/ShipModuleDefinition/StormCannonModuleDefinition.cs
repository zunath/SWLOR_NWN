using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class StormCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            StormCannon("storm_cann", "Storm Cannon", "Storm Cann.", "A stream of high energy particles deals 140 Damage over 1 second. Deals reduced damage to unshielded targets, but imposes debuffs.", 35);

            return _builder.Build();
        }

        private void StormCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.IonCannon)
                .Texture("iit_smstormcann")
                .Description(description)
                .MaxDistance(40f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 5)
                .Recast(8f)
                .Capacitor(30)
                .CapitalClassModule()
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    const int rank = 40;
                    var shot = new SpaceWeaponShot
                    {
                        Family = SpaceWeaponFamily.Energy,
                        DamageType = CombatDamageType.EM,
                        Dmg = dmg + moduleBonus,
                        Rank = rank,
                        Attack = rank * 2,
                        Accuracy = rank * 2,
                        Size = ShipSize.Capital,
                        ShieldDamageMultiplier = 1.5
                    };

                    var effectBeam = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
                    var effectLightning = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Chest);

                    for (var i = 0; i < 4; i++)
                    {
                        var delay = i * 0.33f;
                        DelayCommand(delay, () =>
                        {
                            if (Space.CanShipFire(activator) && !GetIsDead(target))
                            {
                                var result = Space.RollWeaponShot(activator, activatorShipStatus, target, targetShipStatus, shot);

                                AssignCommand(activator, () =>
                                {
                                    ApplyEffectToObject(DurationType.Temporary, effectBeam, target, 1.0f);
                                    ApplyEffectToObject(DurationType.Temporary, effectLightning, target, 1.0f);
                                    if (result.IsHit)
                                    {
                                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ion_Shot), activator);
                                    }
                                });

                                if (result.IsHit)
                                {
                                    DelayCommand(0.1f, () =>
                                    {
                                        AssignCommand(activator, () =>
                                        {
                                            var effect = EffectVisualEffect(VisualEffect.Vfx_Imp_Dispel, false);
                                            ApplyEffectToObject(DurationType.Instant, effect, target);
                                        });
                                    });
                                }

                                // Damage that gets past the shield slows the target.
                                Space.CompleteWeaponShot(activator, target, shot, result, 0.1f, landed =>
                                {
                                    if (landed.HullDamage > 0)
                                    {
                                        ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(75), target, 6f);
                                        ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Agility, 4), target, 12f);
                                    }
                                });
                            }
                        });
                    }
                });
        }
    }
}
