using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The pilot attributes and shield regen of a preset space NPC ship. They stand in for the creature's own
    /// ability scores in the shot math, and their presence marks the ship as a preset.
    /// </summary>
    public class ShipNpcPilot
    {
        public int Agility { get; set; }
        public int Perception { get; set; }
        public int Vitality { get; set; }
        public int Willpower { get; set; }
        public int Might { get; set; }

        /// <summary>The shield regained per second, in place of the size and WIL formula.</summary>
        public double ShieldRegenPerSecond { get; set; }

        /// <summary>The score for an attribute; attributes the ship math does not use read as 10.</summary>
        public int GetScore(AbilityType ability)
        {
            return ability switch
            {
                AbilityType.Agility => Agility,
                AbilityType.Perception => Perception,
                AbilityType.Vitality => Vitality,
                AbilityType.Willpower => Willpower,
                AbilityType.Might => Might,
                _ => 10,
            };
        }
    }
}
