namespace SWLOR.Game.Server.Service;

public interface ITileMagicService
{
    /// When the module loads, load the tile magic configured on every area.
    void ApplyAreaConfiguration();
}