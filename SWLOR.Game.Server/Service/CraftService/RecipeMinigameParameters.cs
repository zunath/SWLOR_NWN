using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// Base class for the per-recipe values one crafting minigame reads. Each minigame derives
    /// its own immutable type, named for the <see cref="RecipeBuilder"/> method that sets it
    /// (<c>ForgeParameters</c> is set by <c>.ForgeParameters(...)</c>), whose constructor
    /// arguments are named like its <see cref="Values"/>.
    /// </summary>
    public abstract class RecipeMinigameParameters
    {
        /// <summary>
        /// The name of the builder method and of this type. Generated source calls it.
        /// </summary>
        public string BuilderMethod => GetType().Name;

        /// <summary>
        /// Every value, in the order of the profile's parameter definitions.
        /// </summary>
        public abstract IReadOnlyList<RecipeParameterValue> Values { get; }
    }
}
