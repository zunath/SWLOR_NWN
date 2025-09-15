using System.Numerics;
using SWLOR.NWN.API.Engine;
using static System.Math;

namespace SWLOR.Game.Server.Core.Bioware
{
    public static class BiowareVector
    {
        // givemedeath - had to add this one to the Service as it wasn't in the ported script and I needed it for HoloComs.
        public static Location MoveLocation(Location lCurrent, float fDirection, float fDistance, float fOffFacing = 0.0f, float fOffZ = 0.0f)
        {

            var vThrow = VectorNormalize(AngleToVector(fDirection));
            vThrow.X *= fDistance;
            vThrow.Y *= fDistance;

            vThrow.Z += fOffZ;
            var position = GetPositionFromLocation(lCurrent);
            position.X += vThrow.X;
            position.Y += vThrow.Y;
            return Location(GetAreaFromLocation(lCurrent), position, GetFacingFromLocation(lCurrent) + fOffFacing);
        }

        // Returns the Vector3 from vA to vB.
        public static Vector3 AtoB(Vector3 vA, Vector3 vB)
        {
            //return vB - vA;
            return new Vector3(vB.X - vA.X, vB.Y - vA.Y, vB.Z - vA.Z);
        }


        // Returns a Vector3 fDist away from vRef at fAngle.
        public static Vector3 VAtAngleToV(Vector3 vRef, float fDist, float fAngle)
        {
            return new Vector3(vRef.X + fDist * (float)Cos(fAngle), vRef.Y + fDist * (float)Sin(fAngle), vRef.Z);
        }

        /*
        // Returns the projection of v2 onto v1. The Vector3 component of v2
        // in the direction of, or along v1.
        public static Vector3 VectorProjection(Vector3 v1, Vector3 v2)
        {
            return (DotProduct(v1, v2) / VectorMagnitude(v1)) * VectorNormalize(v1);
        }

        // Finds the scalar projection of v2 onto v1. The length of the Vector3
        // projection of v2 on to v1.
        public static float ScalarProjection(Vector3 v1, Vector3 v2)
        {
            return DotProduct(v1, v2) / VectorMagnitude(v1);
        }

        // Returns the enclosed angle between two Vectors.
        public static float EnclosedAngle(Vector3 v1, Vector3 v2)
        {
            return aCos(DotProduct(v1, v2) / (VectorMagnitude(v1) * VectorMagnitude(v2)));
        }
        */

        // Returns the scalar triple product of v1, v2 and v3.
        // - The scalar triple product is equivalent to the volume of a
        // parallelepiped of sides v1, v2 v3.
        // - the volume of a tetrahedron is one sixth the volume of the associated
        // parallelepiped.
        // - If the scalar triple product is 0.0, v1, v2 and v3 are co-planar
        public static float ScalarTripleProduct(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            return fabs(DotProduct(v1, CrossProduct(v2, v3)));
        }

        // Returns the cross product of Vectors v1 and v2.
        // - the magnitude of the cross product is equivalent to the area of a
        // parallelogram with sides v1 and v2.
        // - the are of triangle made by v1 and v2 is half the area of the associated
        // parallelogram.
        public static Vector3 CrossProduct(Vector3 v1, Vector3 v2)
        {
            return Vector3(v1.Y * v2.Z - v2.Y * v1.Z, v2.X * v1.Z - v1.X * v2.Z, v1.X * v2.Y - v2.X * v1.Y);
        }


        // Returns the dot product of two Vectors.
        public static float DotProduct(Vector3 v1, Vector3 v2)
        {
            return (v1.X * v2.X) + (v1.Y * v2.Y) + (v1.Z * v2.Z);
        }


        // ** Angles to the axes functions


        // Determines the quadrant that v1 is relative to vOrigin.
        // Returns 0 if is at 0, 90, 180, or 270 degrees to the
        // positive x-axis.
        public static int DetermineQuadrant(Vector3 vOrigin, Vector3 v1)
        {
            var vNew = AtoB(vOrigin, v1);
            if (vNew.X > 0.0 && vNew.Y > 0.0)
            {
                return 1;
            }
            else if (vNew.X < 0.0 && vNew.Y > 0.0)
            {
                return 2;
            }
            else if (vNew.X < 0.0 && vNew.Y < 0.0)
            {
                return 3;
            }
            else if (vNew.X > 0.0 && vNew.Y < 0.0)
            {
                return 4;
            }
            else
            {
                return 0;
            }
        }

        // Returns the adjacent angle, given the length of the hypotenuse and
        // opposite side. * returns 0.0 for divide by 0.
        public static float soh(float fOppositeLength, float fHypotenuseLength)
        {
            if (fHypotenuseLength == 0.0)
            {
                return 0.0f;
            }
            else
            {
                return (float)Asin(fOppositeLength / fHypotenuseLength);
            }
        }

        // Returns the adjacent angle, given the length of the hypotenuse and
        // adjacent side. * returns 0.0 for divide by 0.
        public static float cah(float fAdjacentLength, float fHypotenuseLength)
        {
            if (fHypotenuseLength == 0.0)
            {
                return 0.0f;
            }
            else
            {
                return (float)Acos(fAdjacentLength / fHypotenuseLength);
            }
        }

        // Returns the adjacent angle, given the length of the opposite side
        // and the adjacent side. * returns 0.0 for divide by 0.
        public static float toa(float fOppositeLength, float fAdjacentLength)
        {
            if (fAdjacentLength == 0.0)
            {
                return 0.0f;
            }
            else
            {
                return atan(fOppositeLength / fAdjacentLength);
            }
        }

        // Returns the angle between a Vector3 (position) and the positive x-axis
        public static float GetXAngle(Vector3 v1)
        {
            return cah(v1.X, VectorMagnitude(v1));
        }

        // Returns the angle between a Vector3 (position) and the positive y-axis
        public static float GetYAngle(Vector3 v1)
        {
            return cah(v1.Y, VectorMagnitude(v1));
        }

        // Returns the angle between a Vector3 (position) and the positive z-axis
        public static float GetZAngle(Vector3 v1)
        {
            return cah(v1.Z, VectorMagnitude(v1));
        }


        // ** Locations relative to a Point functions

        // Returns a Location fDist in front of oObj, facing oObj.

        public static Location LocInFrontOfObj(uint oObj, float fDist)
        {
            return LocAtAngleToLoc(GetLocation(oObj), fDist, 0.0f);
        }

        // Returns a Location fDist behind oObj, facing oObj.
        public static Location LocBehindObj(uint oObj, float fDist)
        {
            return LocAtAngleToLoc(GetLocation(oObj), fDist, 180.0f);
        }

        // Returns a Location fDist to the right of oObj, facing oObj.
        public static Location LocRSideOfObj(uint oObj, float fDist)
        {
            return LocAtAngleToLoc(GetLocation(oObj), fDist, -90.0f);
        }

        // Returns a Location fDist to the left of oObj, facing oObj.
        public static Location LocLSideOfObj(uint oObj, float fDist)
        {
            return LocAtAngleToLoc(GetLocation(oObj), fDist, 90.0f);
        }

        // Returns a Location fDist at angle fAngle around oObj, facing oObj.
        // 0 degrees is the facing of oObj, so 90.0 degrees is left of the oObj.
        public static Location LocAtAngleToObj(uint oObj, float fDist, float fAngle)
        {
            return LocAtAngleToLoc(GetLocation(oObj), fDist, fAngle);
        }

        // Returns a Location fDist in front of lRef, facing lRef.
        public static Location LocInFrontOfLoc(Location lRef, float fDist)
        {
            return LocAtAngleToLoc(lRef, fDist, 0.0f);
        }

        // Returns a Location fDist in front of lRef, facing lRef.
        public static Location LocBehindLoc(Location lRef, float fDist)
        {
            return LocAtAngleToLoc(lRef, fDist, 180.0f);
        }

        // Returns a Location fDist in front of lRef, facing lRef.
        public static Location LocRSideOfLoc(Location lRef, float fDist)
        {
            return LocAtAngleToLoc(lRef, fDist, -90.0f);
        }

        // Returns a Location fDist in front of lRef, facing lRef.
        public static Location LocLSideOfLoc(Location lRef, float fDist)
        {
            return LocAtAngleToLoc(lRef, fDist, 90.0f);
        }

        // Returns a Location fDist at angle fAngle around lRef, facing lRef.
        // 0 degrees is the facing of the Location, so 90.0 degrees is left of the Location.
        public static Location LocAtAngleToLoc(Location lRef, float fDist, float fAngle)
        {
            var fFacing = GetFacingFromLocation(lRef) + fAngle;
            return LocAtAngleToLocFacing(lRef, fDist, fAngle, fFacing - 180.0f);
        }

        // Returns a Location fDist at angle fAngle around lRef, facing fFacing.
        // 0 degrees is the facing of the Location, so 90.0 degrees is left of the Location.
        public static Location LocAtAngleToLocFacing(Location lRef, float fDist, float fAngle, float fNew)
        {
            var oArea = GetAreaFromLocation(lRef);
            var vRef = GetPositionFromLocation(lRef);
            var fFacing = GetFacingFromLocation(lRef);

            var vNewPos = VAtAngleToV(vRef, fDist, fFacing + fAngle);
            return Location(oArea, vNewPos, fNew);
        }
    }
}
