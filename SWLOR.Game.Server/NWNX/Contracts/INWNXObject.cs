using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.NWNX.Contracts
{
    public interface INWNXObject
    {
        int GetLocalVariableCount(NWObject obj);
        LocalVariable GetLocalVariable(NWObject obj, int index);
        NWObject StringToObject(string id);
        void SetPosition(NWObject obj, Vector pos);
        void SetCurrentHitPoints(NWCreature creature, int hp);
        void SetMaxHitPoints(NWObject creature, int hp);
        string GetPortrait(NWObject creature);
        void SetPortrait(NWObject creature, string portrait);
        string Serialize(Object obj);
        Object Deserialize(string serialized);
        string GetDialogResref(NWObject obj);
        void SetAppearance(NWObject obj, int app);
        int GetAppearance(NWObject obj);
    }
}
