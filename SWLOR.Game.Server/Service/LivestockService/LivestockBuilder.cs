using System.Collections.Generic;
using SWLOR.Game.Server.Service.FarmingService;

namespace SWLOR.Game.Server.Service.LivestockService
{
    /// <summary>Builds <see cref="LivestockDefinition"/> entries one animal at a time, in the style of <c>CropBuilder</c>.</summary>
    public class LivestockBuilder
    {
        private readonly Dictionary<LivestockType, LivestockDefinition> _livestock = new();
        private LivestockDefinition _active;

        public LivestockBuilder Create(LivestockType type)
        {
            _active = new LivestockDefinition { Type = type };
            _livestock[type] = _active;
            return this;
        }

        public LivestockBuilder Name(string name)
        {
            _active.Name = name;
            return this;
        }

        /// <summary>Sets the tier and, with it, the level that sets the XP the animal's products pay.</summary>
        public LivestockBuilder Tier(int tier)
        {
            _active.Tier = tier;
            _active.Level = FarmingRules.GetTierLevel(tier);
            return this;
        }

        public LivestockBuilder SourceCreature(string resref)
        {
            _active.SourceCreatureResref = resref;
            return this;
        }

        public LivestockBuilder Young(string resref)
        {
            _active.YoungResref = resref;
            return this;
        }

        public LivestockBuilder Meat(string resref)
        {
            _active.MeatResref = resref;
            return this;
        }

        /// <summary>The one second product a butchered animal yields.</summary>
        public LivestockBuilder SecondProduct(string resref)
        {
            _active.SecondProductResref = resref;
            return this;
        }

        /// <summary>A second item every collection yields besides manure.</summary>
        public LivestockBuilder CollectExtra(string resref)
        {
            _active.CollectExtraResref = resref;
            return this;
        }

        public Dictionary<LivestockType, LivestockDefinition> Build() => _livestock;
    }
}
