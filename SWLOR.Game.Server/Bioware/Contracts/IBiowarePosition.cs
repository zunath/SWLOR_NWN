using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.Bioware.Contracts
{
    public interface IBiowarePosition
    {
        void TurnToFaceObject(NWObject oObjectToFace, NWObject oTarget);
        float GetChangeInX(float fDistance, float fAngle);
        float GetChangeInY(float fDistance, float fAngle);
        Vector GetChangedPosition(Vector vOriginal, float fDistance, float fAngle);
    }
}