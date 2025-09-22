namespace SWLOR.Component.Properties.Contracts
{
    public interface IPositionedProperty
    {
        string AreaResref { get; set; }
        float X { get; set; }
        float Y { get; set; }
        float Z { get; set; }
        float Orientation { get; set; }
    }
}
