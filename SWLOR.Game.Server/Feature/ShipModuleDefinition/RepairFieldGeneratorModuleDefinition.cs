using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    public class RepairFieldGeneratorModuleDefinition : IShipModuleListDefinition
    {
        private readonly ShipModuleBuilder _builder = new ShipModuleBuilder();

        public Dictionary<string, ShipModuleDetail> BuildShipModules()
        {
            RepairFieldGenerator("repairfield", "Repair Field Generator", "Rep Field Gen", "A suite of welding lasers and other advanced devices serves to repair 60 hull HP over the course of 6 seconds. Affects all nearby targets", 10);

            return _builder.Build();
        }

        private void RepairFieldGenerator(
            string itemTag,
            string name,
            string shortName,
            string description,
            int repairAmount)
        {
            _builder.Create(itemTag)
                .Name(name)
                .ShortName(shortName)
                .Type(ShipModuleType.RepairFieldGenerator)
                .Texture("iit_ess_074")
                .Description(description)
                .PowerType(ShipModulePowerType.High)
                .Capacitor(25)
                .Recast(12f)
                .CapitalClassModule()
                .CanTargetSelf()
                .ActivatedAction((activator, activatorShipStatus, target, targetShipStatus, moduleBonus) =>
                {
                    var industrialBonus = Space.GetShipStatus(activator).Industrial;
                    repairAmount += (industrialBonus + moduleBonus) / 2;

                    for (int i = 0; i < 6; i++)
                    {
                        float delay = i * 1f;
                        DelayCommand(delay, () =>
                        {
                            target = GetFirstObjectInShape(Shape.Sphere, 8.0f, GetLocation(activator), true, ObjectType.Creature);
                            if (!GetIsEnemy(target, activator) && !GetIsDead(activator) && Space.GetShipStatus(target) != null)
                            {
                                targetShipStatus = Space.GetShipStatus(target);
                                ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, activator, BodyNode.Chest), target, 1.0f);
                                Space.RestoreHull(target, targetShipStatus, repairAmount);
                            }
                        });
                    }

                    Messaging.SendMessageNearbyToPlayers(activator, $"{GetName(activator)} begins restoring {repairAmount * 6} armor HP to nearby ships.");
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Piloting);
                });
        }
    }
}
