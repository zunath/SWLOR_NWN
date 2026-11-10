using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

namespace SWLOR.Game.Server.Tests.Service.Kitchen;

/// <summary>
/// A plain, patient cook for feasibility checks. It adds whatever lowers the expected distance to the target until the dish is
/// close, then simmers at Medium heat, correcting drift with the pantry as it goes. It is not an optimal player and is not a
/// balance measure; the crafting simulator measures balance.
/// </summary>
internal static class KitchenSimpleCook
{
    private const int MaxActions = 80;

    public static void Cook(KitchenEngine engine)
    {
        var actions = 0;
        while (engine.Status == MinigameStatus.InProgress && actions++ < MaxActions)
        {
            var distance = engine.DistanceFromCenter;
            var inner = engine.Recipe.Target.InnerRadius;
            var previews = engine.PreviewAll().Where(preview => preview.Enabled).ToList();

            MinigameActionPreview best = null;
            var bestScore = distance - 0.5;
            if (distance > inner * 0.6)
            {
                foreach (var preview in previews.Where(preview => preview.Action.Id.StartsWith("add.", StringComparison.Ordinal)))
                {
                    var expected = preview.Outcomes.Sum(outcome =>
                        outcome.ChancePercent / 100.0 * (outcome.Change(KitchenEngine.DistanceMeterId)?.After ?? 0));
                    var bonus = preview.Action.Id.StartsWith("add.ingredient.", StringComparison.Ordinal) ? 0.25 : 0;
                    if (expected - bonus < bestScore)
                    {
                        bestScore = expected - bonus;
                        best = preview;
                    }
                }
            }

            if (best != null)
            {
                engine.Perform(best.Action.Id);
                continue;
            }

            if (engine.Heat == KitchenHeat.High)
                engine.Perform(KitchenActionIds.HeatMedium);
            else if (previews.Any(preview => preview.Action.Id == KitchenActionIds.Simmer))
                engine.Perform(KitchenActionIds.Simmer);
            else
                engine.Perform(KitchenActionIds.HeatMedium);
        }
    }
}
