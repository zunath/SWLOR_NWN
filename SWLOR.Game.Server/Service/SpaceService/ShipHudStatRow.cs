namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One line of the Ship HUD's Stats tab. A line with no value is a section heading.</summary>
    public class ShipHudStatRow
    {
        public string Label { get; set; }
        public string Value { get; set; }

        public ShipHudStatRow(string label, string value = "")
        {
            Label = label;
            Value = value;
        }
    }
}
