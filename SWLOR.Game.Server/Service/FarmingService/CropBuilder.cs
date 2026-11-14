using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>Builds <see cref="CropDefinition"/> entries one crop at a time, in the style of the perk and fishing builders.</summary>
    public class CropBuilder
    {
        private readonly Dictionary<CropType, CropDefinition> _crops = new();
        private CropDefinition _active;

        public CropBuilder Create(CropType type)
        {
            _active = new CropDefinition { Type = type };
            _crops[type] = _active;
            return this;
        }

        public CropBuilder Name(string name)
        {
            _active.Name = name;
            return this;
        }

        /// <summary>Sets the tier and, with it, the crop level and grow time that tier's crops share.</summary>
        public CropBuilder Tier(int tier)
        {
            _active.Tier = tier;
            _active.Level = FarmingRules.GetTierLevel(tier);
            _active.GrowHours = FarmingRules.GetTierGrowHours(tier);
            return this;
        }

        /// <summary>Marks the crop as a woodlot sapling, which grows for <see cref="WoodlotRules.GrowHours"/> whatever its tier.</summary>
        public CropBuilder Sapling()
        {
            _active.IsSapling = true;
            _active.GrowHours = WoodlotRules.GrowHours;
            return this;
        }

        public CropBuilder Seed(string resref)
        {
            _active.SeedResref = resref;
            return this;
        }

        public CropBuilder Harvest(string resref)
        {
            _active.HarvestResref = resref;
            return this;
        }

        public Dictionary<CropType, CropDefinition> Build() => _crops;
    }
}
