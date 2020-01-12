
using SWLOR.Game.Server.GameObject;
using System;
using static SWLOR.Game.Server.NWScript._;
using NWN;

namespace SWLOR.Game.Server.AI
{


    public class WaypointBehaviour : StandardBehaviour
    {
        const string sWalkwayVarname = "NW_WALK_CONDITION";

        // If set, the creature's waypoints have been initialized.
        const int NW_WALK_FLAG_INITIALIZED = 0x00000001;

        // If set, the creature will walk its waypoints constantly,
        // moving on in each OnHeartbeat event. Otherwise,
        // it will walk to the next only when triggered by an
        // OnPerception event.
        const int NW_WALK_FLAG_CONSTANT = 0x00000002;

        // Set when the creature is walking day waypoints.
        const int NW_WALK_FLAG_IS_DAY = 0x00000004;

        // Set when the creature is walking back
        const int NW_WALK_FLAG_BACKWARDS = 0x00000008;

        public override void OnHeartbeat(NWCreature self)
        {
            base.OnHeartbeat(self);

            //WalkWayPoints(self);
            ExecuteScript("nw_d2_walkways",self);

        }


        /**********************************************************************
         * FUNCTION DEFINITIONS
         **********************************************************************/

        // Get whether the specified WalkWayPoints condition is set
       /* int GetWalkCondition(int nCondition, NWCreature oCreature)
        {
            return (GetLocalInt(oCreature, sWalkwayVarname) & nCondition);
        }

        // Set a given WalkWayPoints condition
        void SetWalkCondition(int nCondition, NWCreature oCreature, int bValid = true)
        {
            int nCurrentCond = GetLocalInt(oCreature, sWalkwayVarname);
            if (bValid == true)
            {
                SetLocalInt(oCreature, sWalkwayVarname, nCurrentCond | nCondition);
            }
            else
            {
                SetLocalInt(oCreature, sWalkwayVarname, nCurrentCond & ~nCondition);
            }
        }


        // Get a waypoint number suffix, padded if necessary
        string GetWaypointSuffix(int i)
        {
            if (i < 10)
            {
                return "0" + IntToString(i);
            }
            return IntToString(i);
        }

        // Look up the caller's waypoints and store them on the creature.
        // Waypoint variables:
        //        WP_NUM     : number of day waypoints
        //        WN_NUM     : number of night waypoints
        //        WP_#, WN_# : the waypoint objects
        //        WP_CUR     : the current waypoint number

        void LookUpWalkWayPoints(NWCreature oCreature)
        {
            // check if the module enables area transitions for walkwaypoints
            int bCrossAreas = 0;

            SetLocalInt(oCreature, "WP_CUR", -1);

            string sTag = "WP_" + GetTag(oCreature) + "_";

            int nNth = 1;
            NWGameObject oWay;

            if (bCrossAreas != 1)
            {
                oWay = GetNearestObjectByTag(sTag + GetWaypointSuffix(nNth));
            }
            else
            {
                oWay = GetObjectByTag(sTag + GetWaypointSuffix(nNth));
            }
            if (GetIsObjectValid(oWay) != true)
            {
                if (bCrossAreas != 1)
                {
                    oWay = GetNearestObjectByTag("POST_" + GetTag(oCreature));
                }
                else
                {
                    oWay = GetObjectByTag("POST_" + GetTag(oCreature));
                }
                if (GetIsObjectValid(oWay) == true)
                {
                    // no waypoints but a post
                    SetLocalInt(oCreature, "WP_NUM", 1);
                    SetLocalObject(oCreature, "WP_1", oWay);
                }
                else
                {
                    // no waypoints or post
                    SetLocalInt(oCreature, "WP_NUM", -1);
                }
            }
            else
            {
                // look up and store all the waypoints
                while (GetIsObjectValid(oWay) == true)
                {
                    SetLocalObject(oCreature, "WP_" + IntToString(nNth), oWay);
                    nNth++;
                    if (bCrossAreas == 0)
                    {
                        oWay = GetNearestObjectByTag(sTag + GetWaypointSuffix(nNth));
                    }
                    else
                    {
                        oWay = GetObjectByTag(sTag + GetWaypointSuffix(nNth));
                    }
                }
                nNth--;
                SetLocalInt(oCreature, "WP_NUM", nNth);
            }

        }

        // Get the creature's next waypoint.
        // If it has just become day/night, or if this is
        // the first time we're getting a waypoint, we go
        // to the nearest waypoint in our new set.
        NWGameObject GetNextWalkWayPoint(NWCreature oCreature)
        {
            string sPrefix = "WP_";

            // if we only have one post, just go there
            int nPoints = GetLocalInt(oCreature, sPrefix + "NUM");
            if (nPoints == 1)
            {
                return GetLocalObject(oCreature, sPrefix + "1");
            }

            // Move up to the next waypoint

            // Get the current waypoint
            int nCurWay = GetLocalInt(oCreature, "WP_CUR");

            // Check to see if this is the first time
            if (nCurWay == -1)
            {
                nCurWay = GetNearestWalkWayPoint(oCreature);
            }
            else
            {
                // we're either walking forwards or backwards -- check
                int bGoingBackwards = GetWalkCondition(NW_WALK_FLAG_BACKWARDS, oCreature);
                if (bGoingBackwards == true)
                {
                    nCurWay--;
                    if (nCurWay == 0)
                    {
                        nCurWay = 2;
                        SetWalkCondition(NW_WALK_FLAG_BACKWARDS, oCreature, false);
                    }
                }
                else
                {
                    nCurWay++;
                    if (nCurWay > nPoints)
                    {
                        nCurWay = nCurWay - 2;
                        SetWalkCondition(NW_WALK_FLAG_BACKWARDS, oCreature, true);
                    }
                }
            }

            // Set our current point and return
            SetLocalInt(oCreature, "WP_CUR", nCurWay);
            if (nCurWay == -1)
                return NWGameObject.OBJECT_INVALID;

            NWGameObject oRet = GetLocalObject(oCreature, sPrefix + IntToString(nCurWay));
            return oRet;
        }

        // Get the number of the nearest of the creature's current
        // set of waypoints (respecting day/night).
        int GetNearestWalkWayPoint(NWCreature oCreature)
        {
            int nNumPoints;
            string sPrefix;

            nNumPoints = GetLocalInt(oCreature, "WP_NUM");
            sPrefix = "WP_";


            if (nNumPoints < 1) return -1;
            int i;
            int nNearest = -1;
            float fDist = 1000000.0F;

            NWGameObject oTmp;
            float fTmpDist;
            for (i = 1; i <= nNumPoints; i++)
            {
                oTmp = GetLocalObject(oCreature, sPrefix + IntToString(i));
                fTmpDist = GetDistanceBetween(oTmp, oCreature);
                if (fTmpDist >= 0.0 && fTmpDist < fDist)
                {
                    nNearest = i;
                    fDist = fTmpDist;
                }
            }
            return nNearest;
        }


        // Make the caller walk through their waypoints or go to their post.
        void WalkWayPoints(NWCreature oCreature, int nRun = false, float fPause = 1.0F)
        {
            // * don't interrupt current circuit
            NWGameObject oNearestEnemy = GetNearestCreature(CREATURE_TYPE_REPUTATION, REPUTATION_TYPE_ENEMY);
            int bIsEnemyValid = GetIsObjectValid(oNearestEnemy);

            // * if I can see an enemy I should not be trying to walk waypoints
            if (bIsEnemyValid == true)
            {
                if (GetObjectSeen(oNearestEnemy) == true)
                {
                    return;
                }
            }

            //int bIsFighting = GetIsFighting(NWGameObject.OBJECT_SELF);
            int bIsInConversation = IsInConversation(oCreature);
            int bMoving = GetCurrentAction(oCreature);
            int bWaiting = GetCurrentAction(oCreature);

            if (bIsInConversation == true || bMoving == true || bWaiting == true)
                return;

            // Initialize if necessary
            if (GetWalkCondition(NW_WALK_FLAG_INITIALIZED, oCreature) != true)
            {
                LookUpWalkWayPoints(oCreature);
                SetWalkCondition(NW_WALK_FLAG_INITIALIZED, oCreature);
            }
            // Move to the next waypoint
            NWGameObject oWay = GetNextWalkWayPoint(oCreature);
            if (GetIsObjectValid(oWay) == true)
            {
                SetWalkCondition(NW_WALK_FLAG_CONSTANT, oCreature);
                // * Feb 7 2003: Moving this from 299 to 321, because I don't see the point
                // * in clearing actions unless I actually have waypoints to walk
                //ClearAllActions();
                Console.WriteLine("waypoints!");
                //SpeakString("Moving to waypoint: " + GetTag(oWay));
                ActionMoveToObject(oWay, nRun);
                if (GetLocalInt(oWay, "X2_L_WAYPOINT_SETFACING") == 1)
                {
                    ActionDoCommand(() => SetFacing(GetFacing(oWay)));

                }
                ActionWait(fPause);
                ActionDoCommand(() => WalkWayPoints(oCreature, nRun, fPause));     // February 14 2003 added else route only happens once
                Console.WriteLine("we did it!!");
            }

        }
    */}
}