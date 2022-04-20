namespace SWLOR.Game.Server.Service.PropertyService
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
