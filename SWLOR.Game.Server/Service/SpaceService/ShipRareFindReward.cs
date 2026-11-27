namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The share (0-100) of an elite or boss wreck's rare finds that is a Design Schematic, a Ship Schematic or a rare
    /// torpedo instead of the tier table's item. Rows come from the generated RevampRewardCatalog.
    /// </summary>
    public class ShipRareFindReward
    {
        public int DesignPercent { get; set; }
        public int ShipPercent { get; set; }
        public int TorpedoPercent { get; set; }
    }
}
