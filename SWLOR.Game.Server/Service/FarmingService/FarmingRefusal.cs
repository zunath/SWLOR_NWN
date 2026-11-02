namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>Why a farming action cannot be done. <see cref="None"/> means it can.</summary>
    public enum FarmingRefusal
    {
        None = 0,

        /// <summary>Only characters can farm.</summary>
        NotAPlayer = 1,

        /// <summary>The garden plot is not there any more.</summary>
        NoPlot = 2,

        /// <summary>The plot already has a crop.</summary>
        PlotOccupied = 3,

        /// <summary>The plot has no crop.</summary>
        PlotEmpty = 4,

        /// <summary>The character lacks garden permission for the property the plot stands in.</summary>
        NoPermission = 5,

        /// <summary>Crop Management has not reached the crop's tier.</summary>
        TierLocked = 6,

        /// <summary>The CD key already has the most crops it can grow at once.</summary>
        CropCap = 7,

        /// <summary>The character carries no seed for the crop.</summary>
        NoSeed = 8,

        /// <summary>The crop is not old enough to tend yet.</summary>
        TooYoungToTend = 9,

        /// <summary>The crop has been tended already.</summary>
        AlreadyTended = 10,

        /// <summary>The crop is not ripe.</summary>
        NotRipe = 11,

        /// <summary>Only the planter can do this to the crop.</summary>
        NotThePlanter = 12,

        /// <summary>The crop has withered.</summary>
        Withered = 13,

        /// <summary>The crop is not one the farm can grow.</summary>
        UnknownCrop = 14,

        /// <summary>A sapling needs no tending: its harvest is flat.</summary>
        NeedsNoTending = 15,

        /// <summary>A sapling grows only in an outdoor plot of a city, not in a house.</summary>
        OutdoorPlotOnly = 16,

        /// <summary>A station deck has no open ground for a sapling.</summary>
        NoOpenGroundOnStation = 17,

        /// <summary>Fertilizer does nothing for a sapling, so a sapling planted with it is refused and the fertilizer is not used.</summary>
        NoFertilizerOnSaplings = 18,

        /// <summary>The house lost its Greenhouse and this plot is past the first 5, so it cannot be planted again.</summary>
        PlotAboveLimit = 19,
    }
}
