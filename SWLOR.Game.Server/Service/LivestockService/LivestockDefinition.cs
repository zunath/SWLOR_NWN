namespace SWLOR.Game.Server.Service.LivestockService
{
    /// <summary>One kind of animal: its tier, the creature it is raised from, the young animal item and what it yields.</summary>
    public class LivestockDefinition
    {
        public LivestockType Type { get; set; }

        public string Name { get; set; }

        /// <summary>1 to 5. Crop Management must reach this tier to raise the animal, and the animal eats crops of this tier or lower.</summary>
        public int Tier { get; set; }

        /// <summary>The level that sets the Agriculture XP its products pay: 5, 15, 25, 35 or 45, the crop level of its tier.</summary>
        public int Level { get; set; }

        /// <summary>The creature blueprint a placed animal is spawned from. Its level in the Design Bible's World NPCs tab sets the tier.</summary>
        public string SourceCreatureResref { get; set; }

        /// <summary>The young animal item the beast trader sells and a player places into a corral.</summary>
        public string YoungResref { get; set; }

        /// <summary>The meat butchering yields (an existing item that cooking already uses).</summary>
        public string MeatResref { get; set; } = string.Empty;

        /// <summary>The one second product butchering yields, or empty where the plan lists none.</summary>
        public string SecondProductResref { get; set; } = string.Empty;

        /// <summary>A second item every collection yields besides manure (milk from a Thune), or empty.</summary>
        public string CollectExtraResref { get; set; } = string.Empty;

        /// <summary>The manure every collection yields, of the animal's tier.</summary>
        public string ManureResref => LivestockRules.GetManureResref(Tier);
    }
}
