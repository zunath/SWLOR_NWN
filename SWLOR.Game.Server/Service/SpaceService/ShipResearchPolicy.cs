using System;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipResearchPolicy
    {
        public static double SuccessChance(double modifier = 0) => Math.Clamp(.8 + modifier, .1, .95);
        public static int Duration(int engineering, int timeReduction) => Math.Max(1,
            (int)Math.Ceiling((300 + 30 * Math.Clamp(engineering, 0, 50)) * (1 - Math.Clamp(timeReduction, 0, 95) / 100d)));
        public static void Upgrade(BlueprintDetail blueprint, bool success, int licensedRuns, int bonusRoll, int magnitude)
        {
            // A recipe supplies its one eligible tuning slot. Research only changes the blueprint's economy.
            blueprint.EnhancementSlots = 0;
            blueprint.ItemBonuses = 0;
            blueprint.GuaranteedBonuses.Clear();
            if (!success) return;
            if (blueprint.Level >= Craft.MaxResearchLevel) throw new InvalidOperationException("Blueprint research is complete.");
            blueprint.Level++;
            if (blueprint.Level == 1) blueprint.LicensedRuns = Math.Max(1, licensedRuns);
            else if (blueprint.Level % 2 == 0) blueprint.LicensedRuns++;
            else if (bonusRoll == 0) blueprint.CreditReduction = Math.Min(95, blueprint.CreditReduction + Math.Clamp(magnitude, 1, 10));
            else blueprint.TimeReduction = Math.Min(95, blueprint.TimeReduction + Math.Clamp(magnitude, 1, 10));
        }
    }
}
