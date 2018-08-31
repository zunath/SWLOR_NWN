using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXItem
    {
        void SetWeight(NWItem oItem, int w);
        void SetBaseGoldPieceValue(NWItem oItem, int g);
        void SetAddGoldPieceValue(NWItem oItem, int g);
        int GetBaseGoldPieceValue(NWItem oItem);
        int GetAddGoldPieceValue(NWItem oItem);
        void SetBaseItemType(NWItem oItem, int nBaseItem);
        void SetItemAppearance(NWItem oItem, int nType, int nIndex, int nValue);
        string GetEntireItemAppearance(NWItem oItem);
        void RestoreItemAppearance(NWItem oItem, string sApp);
    }
}