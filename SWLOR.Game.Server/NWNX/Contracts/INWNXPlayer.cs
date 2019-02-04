using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXPlayer
    {
        void ForcePlaceableExamineWindow(NWPlayer player, NWPlaceable placeable);
        string GetBicFileName(NWPlayer player);
        QuickBarSlot GetQuickBarSlot(NWPlayer player, int slot);
        void SetAlwaysWalk(NWPlayer player, int bWalk);
        void SetQuickBarSlot(NWPlayer player, int slot, QuickBarSlot qbs);
        void StartGuiTimingBar(NWPlayer player, float seconds, string script);
        void StopGuiTimingBar(NWPlayer player, string script);
        void StopGuiTimingBar(NWPlayer player, string script, int id);
        void ShowVisualEffect(NWPlayer player, int effectId, Vector position);
        void MusicBackgroundChangeDay(NWPlayer player, int track);
        void MusicBackgroundChangeNight(NWPlayer player, int track);
        void MusicBackgroundStart(NWPlayer player);
        void MusicBackgroundStop(NWPlayer player);
        void MusicBattleChange(NWPlayer player, int track);
        void MusicBattleStart(NWPlayer player);
        void MusicBattleStop(NWPlayer player);
        void PlaySound(NWPlayer player, string sound, NWObject target);
        void SetPlaceableUseable(NWPlayer player, NWPlaceable placeable, bool isUseable);
        void SetRestDuration(NWPlayer player, int duration);
    }
}