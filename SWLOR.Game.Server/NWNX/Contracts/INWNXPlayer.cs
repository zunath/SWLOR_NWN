using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXPlayer
    {
        void ForcePlaceableExamineWindow(NWPlayer player, NWPlaceable placeable);
        string GetBicFileName(NWPlayer player);
        QuickBarSlot GetQuickBarSlot(NWPlayer player, int slot);
        int GetVisibilityOverride(NWPlayer player, NWObject target);
        void SetAlwaysWalk(NWPlayer player, int bWalk);
        void SetQuickBarSlot(NWPlayer player, int slot, QuickBarSlot qbs);
        void SetVisibilityOverride(NWPlayer player, NWObject target, int @override);
        void StartGuiTimingBar(NWPlayer player, float seconds, string script);
        void StopGuiTimingBar(NWPlayer player, string script);
        void StopGuiTimingBar(NWPlayer player, string script, int id);
    }
}