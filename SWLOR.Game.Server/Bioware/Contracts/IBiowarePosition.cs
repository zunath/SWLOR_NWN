using SWLOR.Game.Server.GameObject;
using NWN;

namespace SWLOR.Game.Server.Bioware.Contracts
{
    public interface IBiowarePosition
    {
        void TurnToFaceObject(NWObject objectToFace, NWObject facer);
        void TurnToFaceLocation(Location locationToFace, NWObject facer);
        float GetChangeInX(float fDistance, float fAngle);
        float GetChangeInY(float fDistance, float fAngle);
        Vector GetChangedPosition(Vector vOriginal, float fDistance, float fAngle);
    }
}