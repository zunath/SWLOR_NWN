using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.Bioware
{
    /// <summary>
    /// Position utility functions converted to C# from Bioware's x2_io_position file.
    /// </summary>
    public class BiowarePosition : IBiowarePosition
    {
        private readonly INWScript _;

        public BiowarePosition(INWScript script)
        {
            _ = script;
        }

        /// <summary>
        /// Causes object to face another object.
        /// </summary>
        /// <param name="objectToFace">The object to face towards</param>
        /// <param name="facer">The object which will change facing</param>
        public void TurnToFaceObject(NWObject objectToFace, NWObject facer)
        {
            facer.AssignCommand(() =>
            {
                _.SetFacingPoint(objectToFace.Position);
            });
        }

        /// <summary>
        /// Causes object to face a location
        /// </summary>
        /// <param name="locationToFace">The location to face towards</param>
        /// <param name="facer">The object which will change facing</param>
        public void TurnToFaceLocation(Location locationToFace, NWObject facer)
        {
            facer.AssignCommand(() =>
            {
                _.SetFacingPoint(_.GetPositionFromLocation(locationToFace));
            });
        }
        /// <summary>
        /// Convenience function to calculate the change in the X axis. 
        /// </summary>
        /// <param name="fDistance"></param>
        /// <param name="fAngle"></param>
        /// <returns></returns>
        public float GetChangeInX(float fDistance, float fAngle)
        {
            return fDistance * _.cos(fAngle);
        }

        /// <summary>
        /// Convenience function to calculate the change in the Y axis. 
        /// </summary>
        /// <param name="fDistance"></param>
        /// <param name="fAngle"></param>
        /// <returns></returns>
        public float GetChangeInY(float fDistance, float fAngle)
        {
            return fDistance * _.sin(fAngle);
        }

        /// <summary>
        /// Convenience function that returns a vector that is fDistance away in fAngle direction.
        /// </summary>
        /// <param name="vOriginal"></param>
        /// <param name="fDistance"></param>
        /// <param name="fAngle"></param>
        /// <returns></returns>
        public Vector GetChangedPosition(Vector vOriginal, float fDistance, float fAngle)
        {
            float changedZ = vOriginal.m_Z;

            var changedX = vOriginal.m_X + GetChangeInX(fDistance, fAngle);
            if (changedX < 0.0)
                changedX = -changedX;
            var changedY = vOriginal.m_Y + GetChangeInY(fDistance, fAngle);
            if (changedY < 0.0)
                changedY = -changedY;

            return _.Vector(changedX, changedY, changedZ);
        }

    }
}
