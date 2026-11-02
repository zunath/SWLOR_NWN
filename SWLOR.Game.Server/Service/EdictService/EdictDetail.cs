namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>What an Edict is: its text, weekly cost, and the rules for enacting it.</summary>
    public class EdictDetail
    {
        public EdictType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public EdictPillarType Pillar { get; set; }

        /// <summary>Credits added to the city's upkeep each week the Edict is billed.</summary>
        public int WeeklyCost { get; set; }

        /// <summary>The lowest city level that can run the Edict.</summary>
        public int MinimumCityLevel { get; set; } = 1;

        /// <summary>Whether enacting it names another city (Alliance, Embargo).</summary>
        public bool NamesTargetCity { get; set; }

        /// <summary>Whether it only means something on a planet, so it is hidden for Wild Space stations.</summary>
        public bool IsPlanetBased { get; set; }

        /// <summary>Whether its benefit goes to citizens in good standing (every Edict except Alliance, Embargo, Open Gates and Shadowport).</summary>
        public bool BenefitsCitizens { get; set; } = true;
    }
}
