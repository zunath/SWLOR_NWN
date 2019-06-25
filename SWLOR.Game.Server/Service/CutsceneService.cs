using NWN;
using SWLOR.Game.Server.GameObject;
using static NWN._;

namespace SWLOR.Game.Server.Service
{
    // This service was created based on the Gestalt Cutscene System. The NWScript code in that package has been converted
    // to C# and adjusted to fit this project.
    public static class CutsceneService
    {
        private const int AnimationNone = 999;
        private const int AnimationNormal = ANIMATION_LOOPING_TALK_NORMAL;
        private const int AnimationForceful = ANIMATION_LOOPING_TALK_FORCEFUL;
        private const int AnimationLaughing = ANIMATION_LOOPING_TALK_LAUGHING;
        private const int AnimationPleading = ANIMATION_LOOPING_TALK_PLEADING;

        private const int InventorySlotBestMelee = 999;
        private const int InventorySlotBestRanged = 998;
        private const int InventorySlotBestArmor = 997;
        private const int InventorySlotNone = 996;

        private const int Instant = DURATION_TYPE_INSTANT;
        private const int Permanent = DURATION_TYPE_PERMANENT;
        private const int Temporary = DURATION_TYPE_TEMPORARY;

        private const int TrackCurrent = 998;
        private const int TrackOriginal = 999;

        private const int EffectTypeCutsceneEffects = -1;

        private const int NWFlagAmbientAnimations = 0x00080000;
        private const int NWFlagImmobileAmbientAnimations = 0x00200000;
        private const int NWFlagAmbientAnimationsAvian = 0x00800000;

        /// <summary>
        /// Initializes the cutscene, setting its name and putting the selected player(s) into CutsceneMode
        /// </summary>
        /// <param name="oPC">the player you want the cutscene to run for</param>
        /// <param name="sName">the name of the cutscene - this is stored on all the party members as a LocalString called "cutscene" NOTE - if you don't want the player to be able to skip the cutscene, you may leave this blank</param>
        /// <param name="bCamera">sets whether or not to cancel any camera movements that may still be running from previous scripts</param>
        /// <param name="bClear">sets whether or not to clear all actions on the select PC(s)</param>
        /// <param name="bClearFX">sets whether or not to clear all visual effects from the selected PC(s) (includes cutscene invisibility, polymorph, blindness etc)</param>
        /// <param name="bResetSpeed">sets whether or not to clear all effects from the selected PC(s) that will interfere with their movement (includes sleep, paralyzation etc)</param>
        /// <param name="bStoreCam">sets whether or not to store the position of the player's camera so that it can be restored at the end of the scene. NOTE - if your cutscene is triggered from the OnEnter script of an area, the camera position which is stored will be invalid, as the player won't yet be in the area. To get around this, either set bStoreCam to FALSE and store the camera facing yourself from the script that sent the player to the new area for the cutscene OR set bStoreCam to 2 to store the camera facing a few seconds after the cutscene starts, by which time the player should be in the area.</param>
        /// <param name="iParty">sets whether the cutscene is being seen by only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void StartCutscene(NWPlayer oPC, string sName = "", bool bCamera = true, bool bClear = true, bool bClearFX = true, bool bResetSpeed = true, int bStoreCam = TRUE, int iParty = 0)
        {
            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            int iCancel = GetLocalInt(GetModule(), sName) + 1;
            SetLocalInt(GetModule(), sName, iCancel);

            string sID = sName + "_" + IntToString(iCancel);
            SetLocalString(GetModule(), "cutscene", sID);

            while (GetIsObjectValid(oParty) == TRUE)
            {
                SetCutsceneMode(oParty, TRUE);
                SetLocalString(oParty, "cutscene", sName);
                SetLocalString(oParty, "cutsceneid", sID);

                if (bCamera) { StopCameraMoves(oPC); }
                if (bClear) { AssignCommand(oParty, () => ClearAllActions(TRUE)); }
                if (bClearFX) { ClearFX(oPC); }
                if (bResetSpeed) { ResetSpeed(oPC); }
                if (bStoreCam == 1) { AssignCommand(oParty, StoreCameraFacing); }
                if (bStoreCam == 2)
                {
                    var party = oParty;
                    DelayCommand(5.0f, () => AssignCommand(party, StoreCameraFacing));
                }

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }

        /// <summary>
        /// Move one or more NPC members of oPC's party out of the way at the beginning of the cutscene
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before removing the(associates)</param>
        /// <param name="oPC">the player whose associates you want to clear</param>
        /// <param name="iAssociates">the associate(s) you want to clear - add up the numbers below for all the associates you want to clear. 1 = NPC henchman, 2 = druid's / ranger's animal companion, 4 = wizard's / sorceror's familiar, 8 = summoned creature, 16 = dominated creature, 32 = henchman's associates</param>
        /// <param name="iMethod">how you want to remove the selected associate(s). 0 (default)  beam them to the specified waypoint and keep them there. 1 = make them cutscene invisible and keep them where they are, 2 = destroy them - NOTE this is permanent, a destroyed associate cannot be returned at the end of the cutscene! 3 = keep them where they are, but keep them visible</param>
        /// <param name="oDestination">the waypoint you want your associates to be held at for the duration of the cutscene. NOTE - you only need to set this if you are using iMethod 0 (beam the associates to a holding pen)</param>
        /// <param name="sDestination">the tag of the waypoint you want oPC's associates to be beamed to. NOTE - leave this at its default value of "" if you have already set oDestination or if you want the associates to stay where they are</param>
        /// <param name="iParty">sets whether to clear the associates of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void ClearAssociates(float fDelay, NWCreature oPC, int iAssociates = 63, int iMethod = 0, NWObject oDestination = null, string sDestination = "", int iParty = 0)
        {
            if (oDestination == null) oDestination = new Object();
            
            NWCreature oParty;
            string sName = GetLocalString(GetModule(), "cutscene");

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                DelayCommand(fDelay, () => DoClearAssociates(sName, oParty, iAssociates, iMethod, oDestination, sDestination));

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }

        }

        /// <summary>
        /// Returns one or more NPC members of oPC's party which were cleared out of the way at the beginning of the cutscene 
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before returning the associate(s)</param>
        /// <param name="oPC">the player whose associates you want to return</param>
        /// <param name="iAssociates">iAssociates  the associate(s) you want to return - add up the numbers below for all the associates you want to return. 1 = NPC henchman, 2 = druid's / ranger's animal companion, 4 = wizard's / sorceror's familiar, 8 = summoned creature, 16 = dominated creature, 32 = henchman's associates</param>
        /// <param name="iMethod">how you removed the associate(s). 0 (default)  you beamed them to a holding position (so just beam them back), 1 = you made them cutscene invisible and kept them where they were (so cancel that effect and beam them back), 3 = you froze them where they were but kept them visible. NOTE - make sure this matches the iMethod you used in GestaltClearAssociates if you want to make sure they return correctly!</param>
        /// <param name="oDestination">the waypoint you want oPC's associates to be beamed to. NOTE - if you leave this as OBJECT_INVALID the associates will be beamed to oPC's current position</param>
        /// <param name="sDestination">the tag of the waypoint you want oPC's associates to be beamed to. NOTE - leave this at its default value of "" if you have already set oDestination or if you want the associates to be beamed to oPC's current position</param>
        /// <param name="iParty">sets whether to return the associates of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void ReturnAssociates(float fDelay, NWCreature oPC, int iAssociates = 63, int iMethod = 0, NWObject oDestination = null, string sDestination = "", int iParty = 0)
        {
            if (oDestination == null) oDestination = new Object();
            NWCreature oParty;
            string sName = GetLocalString(GetModule(), "cutscene");

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                DelayCommand(fDelay, () => DoReturnAssociates(sName, oParty, iAssociates, iMethod, oDestination, sDestination));

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }


        /// <summary>
        /// Stops all camera movements immediately
        /// </summary>
        /// <param name="oPC">the player whose camera movements you want to stop</param>
        /// <param name="iParty">sets whether to stop the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void StopCameraMoves(NWPlayer oPC, int iParty)
        {
            StopCameraMoves(oPC, iParty, true, 0);
        }
        
        /// <summary>
        /// Stops all camera movements immediately
        /// </summary>
        /// <param name="oPC">the player whose camera movements you want to stop</param>
        /// <param name="iParty">sets whether to stop the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        /// <param name="bAuto">sets whether the function should stop all camera movement (TRUE) or only ones with an id lower than iCamID (FALSE)</param>
        /// <param name="iCamID">the ID of the last camera move you want to stop (this is only needed if bAuto is set to FALSE)</param>
        private static void StopCameraMoves(NWCreature oPC, int iParty = 0, bool bAuto = true, int iCamID = 0)
        {
            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                if (bAuto)
                { iCamID = GetLocalInt(oParty, "iCamCount"); }

                var iCount = iCamID;

                while (iCount > 0)
                {
                    // Find the camera movement
                    var sCam = "iCamStop" + IntToString(iCount);
                    SetLocalInt(oParty, sCam, 1);
                    iCount--;

                    // Uncomment the line below to get a message in the game confirming each id which is cancelled
                    // AssignCommand(oParty,SpeakString("Camera movement id " + IntToString(iCount) + "has been stopped"));
                }

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }

        
        /// <summary>
        /// Stops the current cutscene, cancelling all camera movements and Gestalt* actions in cutscene.
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before ending the cutscene</param>
        /// <param name="oPC">the player running the cutscene you want to stop</param>
        /// <param name="sDestination">the tag of the waypoint where you want the PC to be at the end of the cutscene. NOTE - if you set this to anything other than its default value of "" the PC will automatically be beamed to the specified waypoint when GestaltStopCutscene is called. NOTE - you can use this in the OnCutsceneAbort script to send the PC to wherever they would have been at the end of the cutscene if they hadn't skipped it</param>
        /// <param name="bMode">sets whether or not to cancel CutsceneMode</param>
        /// <param name="bCamera">sets whether or not to cancel all camera movements</param>
        /// <param name="bClear">sets whether or not to clear all actions on the select PC(s)</param>
        /// <param name="bClearFX">sets whether or not to clear all visual effects from the selected PC(s) (includes cutscene invisibility, polymorph, blindness etc)</param>
        /// <param name="bResetSpeed">sets whether or not to clear all effects from the selected PC(s) that will interfere with their movement (includes sleep, paralyzation etc)</param>
        /// <param name="bClearActors">sets whether or not to clear all actions on all the actors that took part in the cutscene and beam them to their finishing positions (if they have one). NOTE - the finishing position of an actor is a waypoint with a tag equal to the name of the cutscene plus the tag of the actor. FOR EXAMPLE - the finishing position for an actor with the tag "freda" in a cutscene called "bigscene" is a waypoint with the tag "bigscenefreda". NOTE - objects can't have a tag longer than 32 characters, so make sure your actor tags and cutscene names aren't too long!</param>
        /// <param name="iParty">make this the same as you used in GestaltStartCutscene to make sure the cutscene is cancelled for everyone together</param>
        public static void StopCutscene(float fDelay, NWCreature oPC, string sDestination = "", bool bMode = true, bool bCamera = true, bool bClear = true, bool bClearFX = true, bool bResetSpeed = true, bool bClearActors = true, int iParty = 0)
        {
            string sName = GetLocalString(oPC, "cutscene");
            string sID = GetLocalString(oPC, "cutsceneid");
            DelayCommand(fDelay, () => DoStopCutscene(sName, sID, oPC, sDestination, bMode, bCamera, bClear, bClearFX, bResetSpeed, bClearActors, iParty));
        }
        
        /// <summary>
        /// Sets the speed of the selected character so that they will go fDistance metres in fTime seconds
        /// NOTE - to reset the character's speed to its normal rate, set the fTime parameter to 0.0
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before setting the character's speed</param>
        /// <param name="oActor">the character whose speed you want to adjust</param>
        /// <param name="fTime">how long you want them to take to move to fDistance</param>
        /// <param name="fDistance">how far you want them to move</param>
        /// <param name="run">sets whether they will be walking (false) or running (true)</param>
        public static void SetSpeed(float fDelay, NWCreature oActor, float fTime, float fDistance, bool run)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSetSpeed(sName, oActor, "", fTime, fDistance, run));
        }

        

        /// <summary>
        /// Sets the speed of the selected character so that they will go fDistance metres in fTime seconds
        /// NOTE - to reset the character's speed to its normal rate, set the fTime parameter to 0.0
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before setting the character's speed</param>
        /// <param name="sActor">the tag of the character whose speed you want to adjust - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="fTime">how long you want them to take to move fDistance</param>
        /// <param name="fDistance">how far you want them to move</param>
        /// <param name="run">sets whether they will be walking (false) or running (true)</param>
        public static void TagSetSpeed(float fDelay, string sActor, float fTime, float fDistance, bool run)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSetSpeed(sName, new Object(), sActor, fTime, fDistance, run));

        }

        /// <summary>
        /// Makes the selected actor completely invisible, using CUTSCENE_INVISIBILITY
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before applying the effect</param>
        /// <param name="oActor">the object you want to make invisible</param>
        /// <param name="fTime">how long the object will remain invisible. NOTE - leave this at its default value of 0.0 to make the effect permanent (it can be cancelled later using GestaltClearFX)</param>
        /// <param name="sActor">the tag of the object you want to make invisible. NOTE - this allows you to make objects created during the cutscene invisible. NOTE - leave this at its default value of "" if you have already set oActor</param>
        public static void Invisibility(float fDelay, NWCreature oActor, float fTime = 0.0f, string sActor = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoInvisibility(sName, oActor, sActor, fTime));
        }
        
        /// <summary>
        /// Moves the selected actor to a target object in a specified time
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before movement is added to oActor's action queue</param>
        /// <param name="oActor">the character you want to move</param>
        /// <param name="oDestination">the object or waypoint they should move to</param>
        /// <param name="run">sets whether the actor will walk (FALSE) or run (TRUE)</param>
        /// <param name="fRange">how many metres from the target the actor should be at the end of movement (keep this number low if you're timing the movement!). NOTE - due to a bug in BioWare's ActionMoveToObject function, if you set fRange > 0.0 for a PC, the PC will run regardless of what you set iRun to be</param>
        /// <param name="fTime">how many seconds the movement should take - leave at 0.0 if you don't want to adjust the actor's speed</param>
        /// <param name="sDestination">the tag of the object or waypoint they should move to. NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag. If you want to do this, set oDestination to OBJECT_INVALID and sDestination to the tag of the object you want to move to. If the object you want to send the actor to exists at the start of the cutscene, leave sDestination as "".</param>
        /// <param name="bTowards">sets whether the actor should move towards (TRUE) or away from (FALSE) the destination</param>
        public static void ActionMove(float fDelay, NWCreature oActor, NWObject oDestination, bool run = false, float fRange = 0.0f, float fTime = 0.0f, string sDestination = "", bool bTowards = true)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoMove(sName, oActor, "", oDestination, run, fRange, fTime, sDestination, bTowards));
            RegisterActor(sName, oActor);

        }
        
        /// <summary>
        /// Moves the selected actor to a target object in a specified time
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before movement is added to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to move - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oDestination">the object or waypoint they should move to</param>
        /// <param name="run">sets whether the actor will walk (false) or run (true)</param>
        /// <param name="fRange">how many metres from the target the actor should be at the end of movement (keep this number low if you're timing the movement!). NOTE - due to a bug in BioWare's ActionMoveToObject function, if you set fRange > 0.0 for a PC, the PC will run regardless of what you set iRun to be</param>
        /// <param name="fTime">how many seconds the movement should take - leave at 0.0 if you don't want to adjust the actor's speed</param>
        /// <param name="sDestination">the tag of the object or waypoint they should move to. NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag. If you want to do this, set oDestination to OBJECT_INVALID and sDestination to the tag of the object you want to move to. If the object you want to send the actor to exists at the start of the cutscene, leave sDestination as "". </param>
        /// <param name="bTowards">sets whether the actor should move towards (TRUE) or away from (FALSE) the destination</param>
        public static void TagActionMove(float fDelay, string sActor, NWObject oDestination, bool run = false, float fRange = 0.0f, float fTime = 0.0f, string sDestination = "", bool bTowards = true)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoMove(sName, new Object(), sActor, oDestination, run, fRange, fTime, sDestination, bTowards));
            RegisterActor(sName, new Object(), sActor);
        }

        /// <summary>
        /// Jumps the selected actor to the position of another object
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before jump is added to oActor's action queue</param>
        /// <param name="oActor">the character you want to jump</param>
        /// <param name="oTarget">the object or waypoint they should jump to</param>
        /// <param name="sTarget">the tag of the object or waypoint they should move to. NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag. If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the object you want to move to. If the object you want to send the actor to exists at the start of the cutscene, leave sTarget as "".</param>
        public static void ActionJump(float fDelay, NWCreature oActor, NWObject oTarget, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoJump(sName, oActor, "", oTarget, sTarget, true));
            RegisterActor(sName, oActor);
        }


        private static void ClearFX(NWCreature oActor)
        {
            Effect eFect = GetFirstEffect(oActor);
            int iType = GetEffectType(eFect);
            while (GetIsEffectValid(eFect) == TRUE)
            {
                if (iType == EFFECT_TYPE_IMPROVEDINVISIBILITY
                    || iType == EFFECT_TYPE_CUTSCENEGHOST
                    || iType == EFFECT_TYPE_VISUALEFFECT
                    || iType == EFFECT_TYPE_INVISIBILITY
                    || iType == EFFECT_TYPE_SANCTUARY
                    || iType == EFFECT_TYPE_POLYMORPH
                    || iType == EFFECT_TYPE_BLINDNESS
                    || iType == EFFECT_TYPE_ETHEREAL
                    || iType == EFFECT_TYPE_DARKNESS)
                { RemoveEffect(oActor, eFect); }
                eFect = GetNextEffect(oActor);
                iType = GetEffectType(eFect);
            }
        }


        private static void ResetSpeed(NWCreature oActor)
        {
            Effect eEffect = GetFirstEffect(oActor);
            int iType = GetEffectType(eEffect);
            while (iType != EFFECT_TYPE_INVALIDEFFECT)
            {
                if (iType == EFFECT_TYPE_MOVEMENT_SPEED_DECREASE
                    || iType == EFFECT_TYPE_MOVEMENT_SPEED_INCREASE
                    || iType == EFFECT_TYPE_CUTSCENE_PARALYZE
                    || iType == EFFECT_TYPE_FRIGHTENED
                    || iType == EFFECT_TYPE_DOMINATED
                    || iType == EFFECT_TYPE_ENTANGLE
                    || iType == EFFECT_TYPE_CONFUSED
                    || iType == EFFECT_TYPE_PARALYZE
                    || iType == EFFECT_TYPE_TIMESTOP
                    || iType == EFFECT_TYPE_STUNNED
                    || iType == EFFECT_TYPE_PETRIFY
                    || iType == EFFECT_TYPE_DAZED
                    || iType == EFFECT_TYPE_SLEEP)
                { RemoveEffect(oActor, eEffect); }
                eEffect = GetNextEffect(oActor);
                iType = GetEffectType(eEffect);
            }
        }

        private static void DoClearAssociates(string sName, NWCreature oPC, int iAssociates = 63, int iMethod = 0, NWObject oDestination = null, string sDestination = "")
        {
            if (oDestination == null) oDestination = new Object();

            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sDestination != "")
            { oDestination = GetWaypointByTag(sDestination); }

            NWObject oAssociate;

            // Check how many henchman may be present
            int iHenchmen = GetMaxHenchmen();
            int i;

            if (iAssociates >= 32)
            {
                i = 1;
                while (i <= iHenchmen)
                {
                    oAssociate = GetAssociate(1, oPC, i);

                    if (GetIsObjectValid(oAssociate) == TRUE)
                    {
                        int iCount = 1;

                        while (iCount <= 5)
                        {
                            NWObject oSecondary = GetAssociate(iCount, oAssociate);
                            DoClearAssociate(oSecondary, iMethod, oDestination);
                            iCount++;
                        }
                    }

                    i++;
                }

                iAssociates -= 32;
            }

            if (iAssociates >= 16)
            {
                oAssociate = GetAssociate(5, oPC);
                DoClearAssociate(oAssociate, iMethod, oDestination);
                iAssociates -= 16;
            }

            if (iAssociates >= 8)
            {
                oAssociate = GetAssociate(4, oPC);
                DoClearAssociate(oAssociate, iMethod, oDestination);
                iAssociates -= 8;
            }

            if (iAssociates >= 4)
            {
                oAssociate = GetAssociate(3, oPC);
                DoClearAssociate(oAssociate, iMethod, oDestination);
                iAssociates -= 4;
            }

            if (iAssociates >= 2)
            {
                oAssociate = GetAssociate(2, oPC);
                DoClearAssociate(oAssociate, iMethod, oDestination);
                iAssociates -= 2;
            }

            if (iAssociates >= 1)
            {
                i = 1;
                while (i <= iHenchmen)
                {
                    oAssociate = GetAssociate(1, oPC, i);
                    DoClearAssociate(oAssociate, iMethod, oDestination);
                    i++;
                }
                iAssociates -= 1;
            }
        }

        private static void DoClearAssociate(NWObject oAssociate, int iMethod, NWObject oDestination)
        {
            if (GetIsObjectValid(oAssociate) == FALSE)
            { return; }
            
            AssignCommand(oAssociate, () => ClearAllActions(TRUE));

            if (iMethod == 0)
            {
                AssignCommand(oAssociate, () => JumpToObject(oDestination));
            }

            else if (iMethod == 1)
            {
                ApplyEffectToObject(Permanent, EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY), oAssociate);
                ApplyEffectToObject(Permanent, EffectCutsceneGhost(), oAssociate);
            }

            else if (iMethod == 2)
            {
                AssignCommand(oAssociate, () => SetIsDestroyable(TRUE));
                DestroyObject(oAssociate);
            }

            DelayCommand(0.1f, () => ApplyEffectToObject(Permanent, EffectCutsceneParalyze(), oAssociate));
            DelayCommand(0.1f, () => SetCommandable(FALSE, oAssociate));
        }

        private static void DoReturnAssociate(NWCreature oAssociate, NWObject oDestination, int iMethod)
        {
            if (GetIsObjectValid(oAssociate) == FALSE)
            { return; }

            SetCommandable(TRUE, oAssociate);
            ResetSpeed(oAssociate);
            if (iMethod == 1) { ClearFX(oAssociate); }
            DelayCommand(0.1f, () => AssignCommand(oAssociate, () => JumpToObject(oDestination)));
        }

        private static void DoReturnAssociates(string sName, NWCreature oPC, int iAssociates, int iMethod, NWObject oDestination, string sDestination)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sDestination != "")
            { oDestination = GetWaypointByTag(sDestination); }

            if (GetIsObjectValid(oDestination) == FALSE)
            { oDestination = oPC; }

            NWCreature oAssociate;

            // Check how many henchman may be present
            int iHenchmen = GetMaxHenchmen();
            int i;

            if (iAssociates >= 32)
            {
                i = 1;
                while (i <= iHenchmen)
                {
                    oAssociate = GetAssociate(1, oPC, i);

                    if (GetIsObjectValid(oAssociate) == TRUE)
                    {
                        int iCount = 1;
                        while (iCount <= 5)
                        {
                            NWCreature oSecondary = GetAssociate(iCount, oAssociate);
                            DoReturnAssociate(oSecondary, oAssociate, iMethod);
                            iCount++;
                        }
                    }

                    i++;
                }
                iAssociates -= 32;
            }

            if (iAssociates >= 16)
            {
                oAssociate = GetAssociate(5, oPC);
                DoReturnAssociate(oAssociate, oDestination, iMethod);
                iAssociates -= 16;
            }

            if (iAssociates >= 8)
            {
                oAssociate = GetAssociate(4, oPC);
                DoReturnAssociate(oAssociate, oDestination, iMethod);
                iAssociates -= 8;
            }

            if (iAssociates >= 4)
            {
                oAssociate = GetAssociate(3, oPC);
                DoReturnAssociate(oAssociate, oDestination, iMethod);
                iAssociates -= 4;
            }

            if (iAssociates >= 2)
            {
                oAssociate = GetAssociate(2, oPC);
                DoReturnAssociate(oAssociate, oDestination, iMethod);
                iAssociates -= 2;
            }

            if (iAssociates >= 1)
            {
                i = 1;
                while (i <= iHenchmen)
                {
                    oAssociate = GetAssociate(1, oPC, i);
                    DoReturnAssociate(oAssociate, oDestination, iMethod);
                    i++;
                }
                iAssociates -= 1;
            }
        }


        private static void DoStopCutscene(string sName, string sID, NWCreature oPC, string sDestination = "", bool bMode = true, bool bCamera = true, bool bClear = true, bool bClearFX = true, bool bResetSpeed = true, bool bClearActors = true, int iParty = 0)
        {
            // Check cutscene hasn't been stopped already
            if (GetLocalInt(GetModule(), sID) == TRUE)
            { return; }

            // Otherwise stop cutscene
            SetLocalInt(GetModule(), sID, TRUE);
            DeleteLocalString(GetModule(), "cutscene");

            NWCreature oParty;
            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            if (bClearActors) { ClearActors(sName, sID); }

            ClearActorSpeeds(sID);

            while (GetIsObjectValid(oParty) == TRUE)
            {
                // End cutscene mode and clear selected player
                if (bMode) { SetCutsceneMode(oParty, FALSE); }
                if (bCamera) { StopCameraMoves(oParty); }
                if (bClear) { AssignCommand(oParty, () => ClearAllActions(TRUE)); }
                if (bClearFX) { ClearFX(oParty); }
                if (bResetSpeed) { ResetSpeed(oParty); }
                if (sDestination != "") { AssignCommand(oParty, () => JumpToObject(GetWaypointByTag(sDestination))); }

                SetCameraHeight(oParty, 0.0f);
                DeleteLocalString(oParty, "cutscene");
                DeleteLocalString(oParty, "cutsceneid");

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }


        private static void ClearActorSpeeds(string sName)
        {
            int iCount = 1;
            int iActors = GetLocalInt(GetModule(), sName + "actorsmodified");

            DeleteLocalInt(GetModule(), sName + "actorsmodified");

            while (iCount <= iActors)
            {
                NWCreature oActor = GetLocalObject(GetModule(), sName + "actorspeedmodified" + IntToString(iCount));
                DeleteLocalObject(GetModule(), sName + "actorspeedmodified" + IntToString(iCount));
                ResetSpeed(oActor);
                iCount++;
            }
        }



        private static void ClearActors(string sName, string sID)
        {
            int iCount = 1;
            int iActors = GetLocalInt(GetModule(), sID + "actorsregistered");
            NWCreature oActor = GetLocalObject(GetModule(), sID + "actor" + IntToString(iCount));

            while (iCount <= iActors)
            {
                DeleteLocalObject(GetModule(), sID + "actor" + IntToString(iCount));

                // If the actor is valid, reset them
                if (GetIsObjectValid(oActor) == TRUE)
                {
                    var sActor = GetTag(oActor);
                    DeleteLocalInt(GetModule(), sName + sActor + "registered");
                    AssignCommand(oActor, () => ClearAllActions(TRUE));

                    if (GetLocalInt(oActor, "gcss_ambient") == 1)
                    {
                        DeleteLocalInt(oActor, "gcss_ambient");
                        SetSpawnCondition(oActor, NWFlagAmbientAnimations, TRUE);
                    }

                    if (GetLocalInt(oActor, "gcss_immobile") == 1)
                    {
                        DeleteLocalInt(oActor, "gcss_immobile");
                        SetSpawnCondition(oActor, NWFlagImmobileAmbientAnimations, TRUE);
                    }

                    if (GetLocalInt(oActor, "gcss_avian") == 1)
                    {
                        DeleteLocalInt(oActor, "gcss_avian");
                        SetSpawnCondition(oActor, NWFlagAmbientAnimationsAvian, TRUE);
                    }

                    NWObject oWP = GetWaypointByTag(sName + sActor);

                    if (GetIsObjectValid(oWP) == TRUE)
                    {
                        var actor = oActor;
                        DelayCommand(0.1f, () => AssignCommand(actor, () => ActionJumpToObject(oWP)));
                        DelayCommand(0.1f, () => AssignCommand(actor, () => ActionDoCommand(() => SetFacing(GetFacing(oWP)))));
                    }
                }

                // Find the next actor
                iCount++;
                oActor = GetLocalObject(GetModule(), sID + "actor" + IntToString(iCount));
            }

            DeleteLocalInt(GetModule(), sID + "actorsregistered");
        }

        private static void SetSpawnCondition(NWCreature oActor, int nCondition, int bValid)
        {
            int nPlot = GetLocalInt(oActor, "NW_GENERIC_MASTER");

            if (bValid == TRUE)
            {
                nPlot = nPlot | nCondition;
                SetLocalInt(oActor, "NW_GENERIC_MASTER", nPlot);
            }

            else if (bValid == FALSE)
            {
                nPlot = nPlot & ~nCondition;
                SetLocalInt(oActor, "NW_GENERIC_MASTER", nPlot);
            }
        }


        private static void DoSetSpeed(string sName, NWCreature oActor, string sActor, float fTime, float fDistance, bool run, float fStops = 0.0f)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (fTime == 0.0f)
            {
                if (GetIsPC(oActor) == TRUE) { SetCutsceneCameraMoveRate(oActor, 1.0f); }
                else { ResetSpeed(oActor); }
                return;
            }

            float fActualSpeed = GetSpeed(oActor, run);
            float fTargetSpeed = fDistance / fTime;

            if (fActualSpeed == fTargetSpeed) { return; }

            float fPercent = fTargetSpeed / fActualSpeed;

            if (fPercent < 0.1) { fPercent = 0.1f; }
            if (fPercent > 2.0) { fPercent = 2.0f; }

            if (GetIsPC(oActor) == TRUE)
            {
                SetCutsceneCameraMoveRate(oActor, fPercent);
            }
            else
            {
                ResetSpeed(oActor);

                int iPercent;

                int iCount = GetLocalInt(GetModule(), sName + "actorsmodified") + 1;
                SetLocalInt(GetModule(), sName + "actorsmodified", iCount);
                SetLocalObject(GetModule(), sName + "actorspeedmodified" + IntToString(iCount), oActor);

                if (fActualSpeed < fTargetSpeed)
                {
                    fPercent = 100 * ((fTargetSpeed - fActualSpeed) / fTargetSpeed);
                    iPercent = FloatToInt(fPercent);
                    //      AssignCommand(oActor,SpeakString("Speed increase " + IntToString(iPercent),TALKVOLUME_SHOUT));     // DEBUG LINE
                    ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectMovementSpeedIncrease(iPercent), oActor);
                }

                else
                {
                    fPercent = 100 * ((fActualSpeed - fTargetSpeed) / fActualSpeed);
                    iPercent = FloatToInt(fPercent);
                    //      AssignCommand(oActor,SpeakString("Speed decrease " + IntToString(iPercent),TALKVOLUME_SHOUT));     // DEBUG LINE
                    ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectMovementSpeedDecrease(iPercent), oActor);
                }
            }
        }

        private static float GetSpeed(NWCreature oActor, bool run)
        {
            float fSpeed = 0.0f;
            int iRate = GetMovementRate(oActor);

            switch (iRate)
            {
                case 0: fSpeed = 2.00f; break;    // PCs
                case 1: fSpeed = 0.00f; break;    // Immobile
                case 2: fSpeed = 0.75f; break;    // Very Slow
                case 3: fSpeed = 1.25f; break;    // Slow
                case 4: fSpeed = 1.75f; break;    // Normal
                case 5: fSpeed = 2.25f; break;    // Fast
                case 6: fSpeed = 2.75f; break;    // Very Fast
                case 7: fSpeed = 5.50f; break;    // DM Fast
            }

            if (run) { fSpeed = fSpeed * 2; }
            //    if (GetHasFeat(FEAT_BARBARIAN_ENDURANCE,oActor))    { fSpeed = fSpeed * 1.1; }
            //    if (GetHasFeat(FEAT_MONK_ENDURANCE,oActor))         { fSpeed = fSpeed * GestaltMonkSpeed(oActor); }

            //    AssignCommand(oActor,SpeakString(FloatToString(fSpeed),TALKVOLUME_SHOUT));  // DEBUG LINE

            return fSpeed;
        }


        private static void DoInvisibility(string sName, NWCreature oActor, string sActor, float fTime)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (fTime > 0.0)
            {
                ApplyEffectToObject(Temporary, EffectCutsceneGhost(), oActor, fTime);
                ApplyEffectToObject(Temporary, EffectEthereal(), oActor, fTime);
                ApplyEffectToObject(Temporary, EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY), oActor, fTime);
            }

            else
            {
                ApplyEffectToObject(Permanent, EffectCutsceneGhost(), oActor);
                ApplyEffectToObject(Permanent, EffectEthereal(), oActor);
                ApplyEffectToObject(Permanent, EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY), oActor);
            }
        }

        private static void DoMove(string sName, NWCreature oActor, string sActor, NWObject oDestination, bool run, float fRange, float fTime, string sDestination, bool bTowards)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (sDestination != "")
            { oDestination = GetObjectByTag(sDestination); }

            if (fTime > 0.0)
            { AssignCommand(oActor, () => ActionDoCommand(() => DoSetSpeed(sName, oActor, "", fTime, GetDistanceBetween(oActor, oDestination), run))); }

            if (!bTowards) { AssignCommand(oActor, () => ActionMoveAwayFromObject(oDestination, run ? TRUE: FALSE, fRange)); }
            else if (fRange > 0.0) { AssignCommand(oActor, () => ActionMoveToObject(oDestination, run ? TRUE: FALSE, fRange)); }
            else { AssignCommand(oActor, () => ActionMoveToLocation(GetLocation(oDestination), run ? TRUE : FALSE)); }
        }


        private static void RegisterActor(string sName, NWCreature oActor, string sActor = "")
        {
            // Make sure the actor is a valid NPC
            if (GetObjectType(oActor) != OBJECT_TYPE_CREATURE) { return; }
            if (GetIsPC(oActor) == TRUE) { return; }
            if (sActor != "") { oActor = GetObjectByTag(sActor); }
            if (sActor == "") { sActor = GetTag(oActor); }

            ResetSpeed(oActor);
            SetCutsceneCameraMoveRate(oActor, 1.0f);

            if (GetLocalInt(GetModule(), sName + sActor + "registered") == TRUE)
            { return; }

            if (GetSpawnCondition(oActor, NWFlagAmbientAnimations))
            {
                SetSpawnCondition(oActor,  NWFlagAmbientAnimations, FALSE);
                SetLocalInt(oActor, "gcss_ambient", 1);
            }

            if (GetSpawnCondition(oActor, NWFlagImmobileAmbientAnimations))
            {
                SetSpawnCondition(oActor,  NWFlagImmobileAmbientAnimations, FALSE);
                SetLocalInt(oActor, "gcss_immobile", 1);
            }

            if (GetSpawnCondition(oActor, NWFlagAmbientAnimationsAvian))
            {
                SetSpawnCondition(oActor, NWFlagAmbientAnimationsAvian, FALSE);
                SetLocalInt(oActor, "gcss_avian", 1);
            }

            int iActors = GetLocalInt(GetModule(), sName + "actorsregistered") + 1;
            SetLocalObject(GetModule(), sName + "actor" + IntToString(iActors), oActor);
            SetLocalInt(GetModule(), sName + "actorsregistered", iActors);
            SetLocalInt(GetModule(), sName + sActor + "registered", TRUE);
        }


        private static bool GetSpawnCondition(NWCreature oActor, int nCondition)
        {
            int nPlot = GetLocalInt(oActor, "NW_GENERIC_MASTER");
            if (nPlot == TRUE && nCondition == TRUE)
            { return true; }
            return false;
        }


        private static void DoJump(string sName, NWCreature oActor, string sActor, NWObject oTarget, string sTarget, bool bAction = false)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (sTarget != "")
            { oTarget = GetObjectByTag(sTarget); }

            if (bAction) { AssignCommand(oActor, () => ActionJumpToObject(oTarget, FALSE)); }
            else { AssignCommand(oActor, () => JumpToObject(oTarget, FALSE)); }
        }

    }
}
