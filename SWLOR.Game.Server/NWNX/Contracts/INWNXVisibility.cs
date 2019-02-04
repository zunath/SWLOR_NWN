using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXVisibility
    {
        VisibilityType GetVisibilityOverride(NWPlayer player, NWObject target);
        void SetVisibilityOverride(NWPlayer player, NWObject target, VisibilityType @override);
    }
}