namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// The kind of dish a recipe cooks in the Kitchen minigame. It sets the shape of the recipe's flavor target. Values are
    /// written into recipe definitions and the Design Bible by name, so members are never renamed or renumbered.
    /// </summary>
    public enum KitchenDish
    {
        /// <summary>Soups, broths, stews, curries, noodles and cooking enhancements: savory with room for error.</summary>
        Soup = 1,

        /// <summary>Roasts and every other main dish: savory with a little spice, a firmer target.</summary>
        Roast = 2,

        /// <summary>Cookies, juices, drinks and desserts: sweet, with the tightest target.</summary>
        Sweet = 3,

        /// <summary>Pet food: few ingredients and a small, forgiving target.</summary>
        PetFood = 4
    }
}
