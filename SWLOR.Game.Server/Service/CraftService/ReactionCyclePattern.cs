namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>
    /// How a recipe's reaction cycle spends its turns across Calm, Rising, Peak and Settling. Every phase lasts at least one
    /// turn; the pattern decides which phases get the turns beyond that. Recipe definitions name the members, so their numbers
    /// never change.
    /// </summary>
    public enum ReactionCyclePattern
    {
        /// <summary>Venom coatings: a patient brew with long Calm and Settling phases and a single Peak turn.</summary>
        Brew = 1,

        /// <summary>Concentrated venoms: a volatile mix with a long Rising build and a Peak that can last several turns.</summary>
        Surge = 2,

        /// <summary>Trap mechanisms: tension builds through a long Rising phase, then snaps through a single Peak turn.</summary>
        Trigger = 3
    }
}
