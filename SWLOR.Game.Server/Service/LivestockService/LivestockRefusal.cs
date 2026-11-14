namespace SWLOR.Game.Server.Service.LivestockService
{
    /// <summary>Why a livestock action cannot be done. <see cref="None"/> means it can.</summary>
    public enum LivestockRefusal
    {
        None = 0,

        /// <summary>Only characters can raise animals.</summary>
        NotAPlayer = 1,

        /// <summary>The animal is not there any more: it wandered off or was butchered.</summary>
        NoAnimal = 2,

        /// <summary>Only the animal's owner can do this.</summary>
        NotTheOwner = 3,

        /// <summary>The animal was fed less than 20 hours ago.</summary>
        FeedTooSoon = 4,

        /// <summary>The crop is of a higher tier than the animal eats.</summary>
        CropTierTooHigh = 5,

        /// <summary>The character does not carry 2 of the crop.</summary>
        NotEnoughCrops = 6,

        /// <summary>The animal is still young.</summary>
        StillYoung = 7,

        /// <summary>The animal was not fed in the last 24 hours.</summary>
        NotFedRecently = 8,

        /// <summary>The animal was collected from less than 20 hours ago.</summary>
        CollectTooSoon = 9,

        /// <summary>Predators raided the corrals and the animal gives no products for now.</summary>
        PredatorPenalty = 10,

        /// <summary>Crop Management has not reached the animal's tier.</summary>
        TierLocked = 11,

        /// <summary>The CD key already has the most animals it can raise at once.</summary>
        CdKeyCap = 12,

        /// <summary>The corral already holds 3 animals.</summary>
        CorralFull = 13,

        /// <summary>The corral is no longer here.</summary>
        NoCorral = 14,

        /// <summary>The city's level no longer allows animals in this corral.</summary>
        CorralOverLimit = 15,

        /// <summary>The character may not add animals to this corral.</summary>
        NoAccess = 16,

        /// <summary>The item is not a young or crated animal.</summary>
        NotAnAnimal = 17,

        /// <summary>The character file could not be saved, so nothing was changed.</summary>
        NotSaved = 18,
    }
}
