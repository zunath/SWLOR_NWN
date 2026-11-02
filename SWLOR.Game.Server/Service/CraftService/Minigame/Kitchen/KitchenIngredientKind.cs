namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>How a committed ingredient acts on the dish when it is added.</summary>
public enum KitchenIngredientKind
{
    /// <summary>It pushes the flavor profile by its vector.</summary>
    Flavor = 0,

    /// <summary>It thins the pot: every flavor axis shrinks by a share.</summary>
    Dilute = 1
}
