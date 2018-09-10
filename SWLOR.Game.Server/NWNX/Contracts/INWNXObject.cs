using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXObject
    {
        Object Deserialize(string serialized);
        int GetAppearance(NWObject obj);
        string GetDialogResref(NWObject obj);
        LocalVariable GetLocalVariable(NWObject obj, int index);
        int GetLocalVariableCount(NWObject obj);
        string GetPortrait(NWObject creature);
        string Serialize(Object obj);
        void SetAppearance(NWObject obj, int app);
        void SetCurrentHitPoints(NWCreature creature, int hp);
        void SetDialogResref(NWObject obj, string dialog);
        void SetMaxHitPoints(NWObject creature, int hp);
        void SetPortrait(NWObject creature, string portrait);
        void SetPosition(NWObject obj, Vector pos);
        NWObject StringToObject(string id);
    }
}
