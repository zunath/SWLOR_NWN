
namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// The crafting devices and the crafting handlers of the property and login scripts. The event handlers are on <see cref="CraftDeviceService"/>.
    /// </summary>
    public interface ICraftDeviceService
    {
        /// <summary>
        /// When a crafting device is used, display the recipe menu.
        /// </summary>
        void UseCraftingDevice();

        void UseRefinery();

        void UseResearchTerminal();

        /// <summary>
        /// When a property is removed, also remove any associated research jobs.
        /// </summary>
        void OnRemoveProperty();

        /// <summary>Applies the player's selected crafting styles again when they log in; the effects are not kept across a logout.</summary>
        void RestoreCraftingStylesOnEnter();
    }
}
