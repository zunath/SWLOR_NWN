namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>An invading armada: its name, its tier 6 capital and the maker whose Ship Schematics a cleared region gives.</summary>
    public class IncursionInvaderDetail
    {
        public IncursionInvader Invader { get; set; }
        public string Name { get; set; }

        /// <summary>The faction capital that leads the armada at tier 6.</summary>
        public string CapitalTag { get; set; }

        /// <summary>The armada creatures' tag prefix: t&lt;tier&gt;&lt;prefix&gt;&lt;hull code&gt; (IncursionTuning.ArmadaShips).</summary>
        public string TagPrefix { get; set; }

        public ShipManufacturer Maker { get; set; }
    }
}
