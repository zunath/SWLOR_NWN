namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>Why a farmhand action cannot be done. <see cref="None"/> means it can.</summary>
    public enum FarmhandRefusal
    {
        None = 0,

        /// <summary>Only characters can assign farmhands.</summary>
        NotAPlayer = 1,

        /// <summary>The house or corral is not there any more.</summary>
        NoTarget = 2,

        /// <summary>A farmhand works one of the character's own houses, or a corral in the city they are a citizen of.</summary>
        NoAccess = 3,

        /// <summary>A droid already works the house or corral.</summary>
        AlreadyAssigned = 4,

        /// <summary>The character does not carry that droid, or it is busy.</summary>
        NoDroid = 5,

        /// <summary>The droid's controller is not one that can take the post.</summary>
        NoFarmhand = 6,

        /// <summary>Only the droid's owner, or someone who manages the property, can recall it.</summary>
        NotTheOwner = 7,

        /// <summary>The feed hopper is full.</summary>
        FeedHopperFull = 8,

        /// <summary>The character carries none of that crop.</summary>
        NoCrops = 9,

        /// <summary>The output hopper is empty.</summary>
        NothingToTake = 10,

        /// <summary>The house or corral has no farmhand.</summary>
        NoFarmhandHere = 11,

        /// <summary>The character file could not be saved, so nothing was changed.</summary>
        NotSaved = 12,
    }
}
