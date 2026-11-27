namespace SWLOR.Game.Server.Domain.World
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
