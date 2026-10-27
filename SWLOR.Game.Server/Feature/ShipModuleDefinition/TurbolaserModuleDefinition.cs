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
    public class TurboLaserModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            Turbolaser("turbolas1", "Heavy Turbolaser Cannon", "Turbolaser1", "Deals 90 thermal DMG to your target. Designed to target capital ships, it struggles against subcapital ships and suffers a -25% hit chance against them.", 90, 1, 10);
            Turbolaser("turbolas2", "Dual Turbolaser Cannon", "Turbolaser2", "Deals 75 thermal DMG to your target, firing twice over two seconds. Designed to target capital ships, it struggles against subcapital ships and suffers a -25% hit chance against them.", 75, 2, 15);
            Turbolaser("turbolas3", "Quad Turbolaser Cannon", "Turbolaser3", "Deals 65 thermal DMG to your target, firing four times over four seconds. Designed to target capital ships, it struggles against subcapital ships and suffers a -25% hit chance against them.", 65, 4, 30);

            return _builder.Build();
        }

        private void Turbolaser(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg,
            int attacks,
            int capacitor)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.TurboLaser)
                .Texture("iit_smturbolas")
                .Description(description)
                .MaxDistance(30f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, 5)
                .Toggle(10f)
                .Capacitor(capacitor)
                .CapitalClassModule()
                .CanTargetSelf()
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    // Capital weapons fire as Capital, so the size steps replace the old flat -25% hit chance
                    // against subcapital ships.
                    const int rank = 40;
                    var shot = new SpaceWeaponShot
                    {
                        Family = SpaceWeaponFamily.Energy,
                        DamageType = CombatDamageType.Thermal,
                        Dmg = dmg + moduleBonus * 3,
                        Rank = rank,
                        Attack = rank * 2,
                        Accuracy = rank * 2,
                        Size = ShipSize.Capital
                    };

                    var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
                    var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);
                    for (var i = 0; i < attacks; i++)
                    {
                        var delay = i * 3f;
                        DelayCommand(delay, () =>
                        {
                            if (Space.CanShipFire(activator) && !GetIsDead(target))
                            {
                                var result = Space.RollWeaponShot(activator, activatorShipStatus, target, targetShipStatus, shot);
                                AssignCommand(activator, () =>
                                {
                                    ApplyEffectToObject(DurationType.Instant, sound, target);
                                    ApplyEffectToObject(DurationType.Instant, missile, target);
                                    if (result.IsHit)
                                    {
                                        ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Fire), target);
                                    }
                                });

                                Space.CompleteWeaponShot(activator, target, shot, result, 0.3f);
                            }
                        });
                    }
                });
        }
    }
}
