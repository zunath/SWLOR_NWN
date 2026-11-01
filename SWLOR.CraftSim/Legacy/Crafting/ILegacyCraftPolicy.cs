using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Legacy.Crafting
{
    /// <summary>Chooses the old minigame's next action from the state a player can see.</summary>
    public interface ILegacyCraftPolicy
    {
        string Name { get; }

        /// <summary>The next action, or null when the player has nothing they can use.</summary>
        LegacyAction? Choose(in LegacyCraftState state, LegacyCraftContext context, SimRandom rng);
    }
}
