namespace SWLOR.Game.Server.Service.HouseSpecializationService
{
    /// <summary>
    /// The job the owner of a house in a city gives it (Player City Revamp, "House specializations"). Values are saved on the
    /// house and never change.
    /// </summary>
    public enum HouseSpecializationType
    {
        /// <summary>No job. Houses start here.</summary>
        None = 0,

        /// <summary>A Craftsmanship and Control bonus for one chosen crafting skill, for anyone crafting in the house.</summary>
        Workshop = 1,

        /// <summary>50% more of the layout's own storage. Furniture storage comes on top, unchanged.</summary>
        Storehouse = 2,

        /// <summary>Twice the garden plots, and crops harvested there yield 1 more item.</summary>
        Greenhouse = 3
    }
}
