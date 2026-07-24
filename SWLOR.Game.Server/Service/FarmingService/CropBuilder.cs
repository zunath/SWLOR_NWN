using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FarmingService
{
    public class CropBuilder
    {
        private readonly Dictionary<CropType, CropDetail> _crops = new();
        private CropDetail _activeCrop;

        /// <summary>
        /// Creates a new crop definition.
        /// </summary>
        /// <param name="type">The type of crop to create.</param>
        public CropBuilder Create(CropType type)
        {
            _activeCrop = new CropDetail();
            _crops[type] = _activeCrop;

            return this;
        }

        /// <summary>
        /// Sets the player-facing name of the crop.
        /// </summary>
        public CropBuilder Name(string name)
        {
            _activeCrop.Name = name;

            return this;
        }

        /// <summary>
        /// Sets the player-facing description of the crop.
        /// </summary>
        public CropBuilder Description(string description)
        {
            _activeCrop.Description = description;

            return this;
        }

        /// <summary>
        /// Sets the Agriculture skill rank required to plant the crop.
        /// </summary>
        public CropBuilder RequiredRank(int rank)
        {
            _activeCrop.RequiredRank = rank;

            return this;
        }

        /// <summary>
        /// Sets the resref of the seed item consumed when the crop is planted.
        /// </summary>
        public CropBuilder SeedResref(string resref)
        {
            _activeCrop.SeedResref = resref;

            return this;
        }

        /// <summary>
        /// Adds a produce item awarded when the crop is harvested. May be called multiple times.
        /// </summary>
        /// <param name="resref">The resref of the produce item.</param>
        /// <param name="quantity">The base quantity awarded before yield bonuses.</param>
        public CropBuilder Yield(string resref, int quantity)
        {
            _activeCrop.Yields[resref] = quantity;

            return this;
        }

        /// <summary>
        /// Sets the base number of real-world seconds each growth stage takes to complete.
        /// </summary>
        public CropBuilder SecondsPerStage(int seconds)
        {
            _activeCrop.SecondsPerStage = seconds;

            return this;
        }

        /// <summary>
        /// Sets the resref of the pristine variant awarded on a successful pristine harvest roll.
        /// </summary>
        public CropBuilder PristineResref(string resref)
        {
            _activeCrop.PristineResref = resref;

            return this;
        }

        /// <summary>
        /// Marks the crop as inactive, preventing it from being planted.
        /// </summary>
        public CropBuilder Inactive()
        {
            _activeCrop.IsActive = false;

            return this;
        }

        /// <summary>
        /// Returns the built dictionary of crops.
        /// </summary>
        public Dictionary<CropType, CropDetail> Build()
        {
            return _crops;
        }
    }
}
