namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>An invading armada: its name, its tier 6 capital and the maker whose Ship Schematics a cleared region gives.</summary>
    public class IncursionInvaderDetail
    {
        public IncursionInvader Invader { get; set; }
        public string Name { get; set; }

        /// <summary>The faction capital that leads the armada at tier 6.</summary>
        public string CapitalTag { get; set; }

        /// <summary>That capital's appearance, flown by the command ship of tiers 1-5.</summary>
        public int CapitalAppearance { get; set; }

        public ShipManufacturer Maker { get; set; }
    }
}
