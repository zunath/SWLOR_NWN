namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>What one unit of a cooking ingredient does to a dish.</summary>
/// <param name="Kind">Whether it pushes the flavor or thins the pot.</param>
/// <param name="Vector">The push for one unit; zero for an ingredient that dilutes.</param>
public sealed record KitchenComponentFlavor(KitchenIngredientKind Kind, FlavorVector Vector);
