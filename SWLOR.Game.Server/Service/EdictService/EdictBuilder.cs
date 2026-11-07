using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.EdictService
{
    public class EdictBuilder
    {
        private readonly Dictionary<EdictType, EdictDetail> _edicts = new();
        private EdictDetail _active;

        /// <summary>Starts a new Edict.</summary>
        public EdictBuilder Create(EdictType type, EdictPillarType pillar)
        {
            _active = new EdictDetail { Type = type, Pillar = pillar };
            _edicts[type] = _active;
            return this;
        }

        public EdictBuilder Name(string name)
        {
            _active.Name = name;
            return this;
        }

        public EdictBuilder Description(string description)
        {
            _active.Description = description;
            return this;
        }

        /// <summary>The weekly cost in credits.</summary>
        public EdictBuilder WeeklyCost(int cost)
        {
            _active.WeeklyCost = cost;
            return this;
        }

        /// <summary>The lowest city level that can run the Edict.</summary>
        public EdictBuilder MinimumCityLevel(int level)
        {
            _active.MinimumCityLevel = level;
            return this;
        }

        /// <summary>Enacting the Edict names another city.</summary>
        public EdictBuilder NamesTargetCity()
        {
            _active.NamesTargetCity = true;
            return this;
        }

        /// <summary>The Edict only applies on a planet, so Wild Space stations cannot enact it.</summary>
        public EdictBuilder PlanetBased()
        {
            _active.IsPlanetBased = true;
            return this;
        }

        /// <summary>The Edict only means something at a lawful port, so a city whose Starport exchange is an open port cannot enact it.</summary>
        public EdictBuilder LawfulPortOnly()
        {
            _active.RequiresLawfulPort = true;
            return this;
        }

        /// <summary>The Edict is an access rule for outsiders or allies, not a benefit for the city's own citizens.</summary>
        public EdictBuilder NotACitizenBenefit()
        {
            _active.BenefitsCitizens = false;
            return this;
        }

        public Dictionary<EdictType, EdictDetail> Build()
        {
            return _edicts;
        }
    }
}
