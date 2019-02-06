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

        /// <summary>
        /// Returns the facing of o2 from o1 based on their relative positions.
        /// o1.SetFacing(GetRelativeFacing(o1,o2)) === o1.SetFacingPoint(o2).
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        public float GetRelativeFacing(NWObject o1, NWObject o2)
        {
            float diffX = o2.Position.m_X - o1.Position.m_X;
            float diffY = o2.Position.m_Y - o1.Position.m_Y;
            
            // X/Y so that we're taking angle relative to the Y axis (X is opposite, Y adjacent)
            float angle = _.atan(diffX / diffY);

            // atan returns -90 to +90.  We need to turn it into a 360 degree facing based on 
            // whether diffX and diffY are positive.  
            // if diffX and diffY are positive, we should have a facing of 0-90.
            //   - angle will already be 0-90 and correct
            // if diffX is positive and diffY is negative, we should have a facing of 90-180.
            //   - angle will be -90 to 0, so needs +180.
            // if diffX and diffY are both negative, we should have a facing of 180-270.
            //   - angle will be 0-90 so needs +180
            // if diffX is negative and diffY is positive, we should have a facing of 270-360.
            //   - angle will be -90 to 0, so needs +360.
            // doing atan Y/X and then doing 90-atan for positive diffX and 270-atan for negative
            // diffX should be equivalent. 

            if (angle >= 0 && diffX >= 0 && diffY >= 0) angle += 0.0f;
            else if (angle <= 0 && diffX >= 0 && diffY <= 0) angle += 180.0f;
            else if (angle >= 0 && diffX <= 0 && diffY <= 0) angle+= 180.0f;
            else angle += 360.0f;

            // For some inescapable reason, NWN facings are based on 0 being due East and measured
            // counterclockwise.
            angle = 90 - angle;
            if (angle < 0) return 360.0f + angle;
            else return angle;
        }
    }
}
