using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Mod.Contracts
{
    public interface IModHandler
    {
        int ModTypeID { get; }
        string CanApply(NWPlayer player, NWItem target, params string[] args);
        void Apply(NWPlayer player, NWItem target, params string[] args);
        string Description(NWPlayer player, NWItem target, params string[] args);
    }
}
