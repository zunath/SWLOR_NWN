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
    public class BeamCannonModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            BeamCannon("beamcannon1", "Basic Beam Cannon", "Basic Beam C.", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 3 thermal DMG on a hit.", 3, 8, 1);
            BeamCannon("beamcannon2", "Beam Cannon I", "Beam Cann. 1", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 6 thermal DMG on a hit.", 6, 10, 2);
            BeamCannon("beamcannon3", "Beam Cannon II", "Beam Cann. 2", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 9 thermal DMG on a hit.", 9, 12, 3);
            BeamCannon("beamcannon4", "Beam Cannon III", "Beam Cann. 3", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 12 thermal DMG on a hit.", 12, 14, 4);
            BeamCannon("beamcannon5", "Beam Cannon IV", "Beam Cann. 4", "A stream of high energy particles deals damage over time, three attacks are made over the course of one second, each tick doing 15 thermal DMG on a hit.", 15, 16, 5);

            return _builder.Build();
        }

        private void BeamCannon(
            string itemTag,
            string name,
            string shortName,
            string description,
            int dmg,
            int capacitor,
            int requiredLevel)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.BeamLaser)
                .Texture("iit_smbeamcann")
                .Description(description)
                .MaxDistance(20f)
                .ValidTargetType(ObjectType.Creature)
                .PowerType(ShipModulePowerType.High)
                .RequirePerk(PerkType.OffensiveModules, requiredLevel)
                .Recast(10f)
                .Capacitor(capacitor)
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var rank = (requiredLevel - 1) * 10;
                    var shot = new SpaceWeaponShot
                    {
                        Family = SpaceWeaponFamily.Energy,
                        DamageType = CombatDamageType.Thermal,
                        Dmg = dmg + moduleBonus / 3,
                        Rank = rank,
                        Attack = rank * 2,
                        Accuracy = rank * 2
                    };

                    var beam = EffectBeam(VisualEffect.Vfx_Beam_Holy, activator, BodyNode.Chest);

                    for (var i = 0; i < 3; i++)
                    {
                        var delay = i * 0.33f;
                        DelayCommand(delay, () =>
                        {
                            if (!Space.CanShipFire(activator))
                                return;

                            AssignCommand(activator, () =>
                            {
                                ApplyEffectToObject(DurationType.Temporary, beam, target, 0.2f);
                            });

                            Space.ResolveWeaponShot(activator, activatorShipStatus, target, targetShipStatus, shot);
                        });
                    }
                });
        }
    }
}
