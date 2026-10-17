namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>An ongoing effect whose benefits require its declared equipment arrangement.</summary>
    public interface IEquipmentBoundStatusEffect
    {
        bool HasValidEquipment(uint creature);
    }
}
