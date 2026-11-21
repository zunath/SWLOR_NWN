using System.Collections.Generic;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Emergency Repair I-IV: repairs the hull of a party ship within command range, the pilot's own included, for a
    /// flat amount plus a percent of its maximum hull plus WIL scaling. Ported from the First Aid Med Kit (the
    /// percents, STM and cooldown are the ground's). The flat amounts are the Med Kit's doubled, because a ship's
    /// hull plus shield is about twice a ground Normal HP in the balance model and a repair restores hull only. It
    /// goes through the module repair path, so it generates repair threat and Repair Efficiency applies.
    /// </summary>
    public class EmergencyRepairAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.CommandEmergencyRepair1, PerkType.CommandEmergencyRepair).Name("Emergency Repair I").Level(1), new HealAmount(22, 2f), 4);
            Rank(_builder.Create(FeatType.CommandEmergencyRepair2, PerkType.CommandEmergencyRepair).Name("Emergency Repair II").Level(2), new HealAmount(84, 3.5f), 6);
            Rank(_builder.Create(FeatType.CommandEmergencyRepair3, PerkType.CommandEmergencyRepair).Name("Emergency Repair III").Level(3), new HealAmount(160, 5f), 7);
            Rank(_builder.Create(FeatType.CommandEmergencyRepair4, PerkType.CommandEmergencyRepair).Name("Emergency Repair IV").Level(4), new HealAmount(216, 6f), 8);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, HealAmount heal, int stamina)
        {
            PilotingAbilityConfiguration.ConfigureAllyRepair(
                ability.HasRecastDelay(RecastGroup.CommandEmergencyRepair, 10.5f),
                stamina,
                (activator, target, _, _) => Repair(activator, target, heal));
        }

        /// <summary>The hull a repair restores before Repair Efficiency: the heal's size scaled by the repairer's WIL.</summary>
        public static int GetRepairAmount(HealAmount heal, int maxHull, int willpower)
        {
            return AbilityEffectScaling.ScaleRestorativeEffect(heal.ForMaxHitPoints(maxHull), willpower);
        }

        private static void Repair(uint activator, uint target, HealAmount heal)
        {
            var activatorStatus = Space.GetShipStatus(activator);
            var targetStatus = Space.GetShipStatus(target);
            if (activatorStatus == null || targetStatus == null)
                return;

            var amount = GetRepairAmount(heal, targetStatus.MaxHull, GetAbilityScore(SpaceCrew.GetAbilityUser(activator), AbilityType.Willpower));
            ShipModuleActions.RepairHull(activator, activatorStatus, target, targetStatus, amount);

            // An ability runs outside a module's cycle, whose end saves a player's ship status, so the repaired ship is
            // saved here: the target, or the activator's own when the repair fell back to it.
            var repaired = ShipModuleActions.GetSupportTarget(activator, target);
            Space.SaveShipStatus(repaired, repaired == target ? targetStatus : activatorStatus);
            ExecuteScript("pc_target_upd", repaired);
        }
    }
}
