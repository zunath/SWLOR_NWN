using NWN;
using SWLOR.Game.Server.Enumeration;
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
            if (oDestination == null) oDestination = new NWGameObject();
            
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
            if (oDestination == null) oDestination = new NWGameObject();
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
            DelayCommand(fDelay, () => DoSetSpeed(sName, new NWGameObject(), sActor, fTime, fDistance, run));

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
            DelayCommand(fDelay, () => DoMove(sName, new NWGameObject(), sActor, oDestination, run, fRange, fTime, sDestination, bTowards));
            RegisterActor(sName, new NWGameObject(), sActor);
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

        /// <summary>
        /// Jumps the selected actor to the position of another object
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before oActor jumps</param>
        /// <param name="oActor">the character you want to jump</param>
        /// <param name="oTarget">the object or waypoint they should jump to</param>
        /// <param name="sTarget">the tag of the object or waypoint they should move to. NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag. If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the object you want to move to. If the object you want to send the actor to exists at the start of the cutscene, leave sTarget as "".</param>
        public static void Jump(float fDelay, NWCreature oActor, NWObject oTarget, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoJump(sName, oActor, "", oTarget, sTarget));
            RegisterActor(sName, oActor);
        }

        /// <summary>
        /// Jumps the selected actor to the position of another object
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before jump is added to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to jump - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oTarget">the object or waypoint they should jump to</param>
        /// <param name="sTarget">the tag of the object or waypoint they should move to. NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag. If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the object you want to move to. If the object you want to send the actor to exists at the start of the cutscene, leave sTarget as "".</param>
        public static void TagActionJump(float fDelay, string sActor, NWObject oTarget, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoJump(sName, new NWGameObject(), sActor, oTarget, sTarget, true));
            RegisterActor(sName, new NWGameObject(), sActor);
        }
        
        /// <summary>
        /// Jumps the selected actor to the position of another object
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before the actor jumps</param>
        /// <param name="sActor">the tag of the character you want to jump - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oTarget">the object or waypoint they should jump to</param>
        /// <param name="sTarget">the tag of the object or waypoint they should move to. NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag. If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the object you want to move to. If the object you want to send the actor to exists at the start of the cutscene, leave sTarget as "".</param>
        public static void TagJump(float fDelay, string sActor, NWObject oTarget, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoJump(sName, new NWGameObject(), sActor, oTarget, sTarget));
            RegisterActor(sName, new NWGameObject(), sActor);
        }
        
        /// <summary>
        /// Tell the selected actor to play an animation
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before animation is added to oActor's action queue</param>
        /// <param name="oActor">the character you want to play the animation</param>
        /// <param name="iAnim">the animation you want them to play (ANIMATION_*)</param>
        /// <param name="fDuration">how long the animation should last (leave at 0.0 for fire-and-forget animations)</param>
        /// <param name="fSpeed">the speed of the animation (defaults to 1.0)</param>
        public static void ActionAnimate(float fDelay, NWCreature oActor, int iAnim, float fDuration = 0.0f, float fSpeed = 1.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoAnimate(sName, oActor, "", iAnim, fDuration, fSpeed, true));
            RegisterActor(sName, oActor);
        }
        
        /// <summary>
        /// Tell the selected actor to play an animation
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before playing the animation</param>
        /// <param name="oActor">the character you want to play the animation</param>
        /// <param name="iAnim">the animation you want them to play (ANIMATION_*)</param>
        /// <param name="fDuration">how long the animation should last (leave at 0.0 for fire-and-forget animations)</param>
        /// <param name="fSpeed">the speed of the animation (defaults to 1.0)</param>
        public static void Animate(float fDelay, NWCreature oActor, int iAnim, float fDuration = 0.0f, float fSpeed = 1.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoAnimate(sName, oActor, "", iAnim, fDuration, fSpeed));
            RegisterActor(sName, oActor);
        }
        
        /// <summary>
        /// Tell the selected actor to play an animation
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before animation is added to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to play the animation - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="iAnim">the animation you want them to play (ANIMATION_*)</param>
        /// <param name="fDuration">how long the animation should last (leave at 0.0 for fire-and-forget animations)</param>
        /// <param name="fSpeed">the speed of the animation (defaults to 1.0)</param>
        public static void TagActionAnimate(float fDelay, string sActor, int iAnim, float fDuration = 0.0f, float fSpeed = 1.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoAnimate(sName, new NWGameObject(), sActor, iAnim, fDuration, fSpeed, true));
            RegisterActor(sName, new NWGameObject(), sActor);
        }
        
        /// <summary>
        /// Tell the selected actor to play an animation
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before playing the animation</param>
        /// <param name="sActor">the tag of the character you want to play the animation - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="iAnim">the animation you want them to play (ANIMATION_*)</param>
        /// <param name="fDuration">how long the animation should last (leave at 0.0 for fire-and-forget animations)</param>
        /// <param name="fSpeed">the speed of the animation (defaults to 1.0)</param>
        public static void TagAnimate(float fDelay, string sActor, int iAnim, float fDuration = 0.0f, float fSpeed = 1.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoAnimate(sName, new NWGameObject(), sActor, iAnim, fDuration, fSpeed));
            RegisterActor(sName, new NWGameObject(), sActor);
        }

        /// <summary>
        /// Tell the selected actor to speak a line
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before speech is added to oActor's action queue</param>
        /// <param name="oActor">the character you want to speak the line</param>
        /// <param name="sLine">the line you want them to speak</param>
        /// <param name="iAnimation">the animation you want them to play whilst speaking the line (leave as ANIMATION_NONE for no animation). NOTE - if you are using a ANIMATION_LOOPING_TALK_* animation, all you need to use is the last word (eg FORCEFUL))</param>
        /// <param name="fDuration">how long the animation should last (leave at 0.0 for fire-and-forget animations)</param>
        /// <param name="fSpeed">the speed of the animation (defaults to 1.0)</param>
        public static void ActionSpeak(float fDelay, NWCreature oActor, string sLine, int iAnimation = AnimationNone, float fDuration = 0.0f, float fSpeed = 1.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSpeak(sName, oActor, "", sLine, iAnimation, fDuration, fSpeed, true));
            RegisterActor(sName, oActor);

        }

        // 
        // fDelay           
        // oActor           
        // sLine            
        // iAnim            
        // fDuration        
        // fSpeed           

        /// <summary>
        /// Tell the selected actor to speak a line
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before speaking line</param>
        /// <param name="oActor">the character you want to speak the line</param>
        /// <param name="sLine">the line you want them to speak</param>
        /// <param name="iAnimation">the animation you want them to play whilst speaking the line (leave as ANIMATION_NONE for no animation)</param>
        /// <param name="fDuration">how long the animation should last (leave at 0.0 for fire-and-forget animations)</param>
        /// <param name="fSpeed">the speed of the animation (defaults to 1.0)</param>
        public static void Speak(float fDelay, NWCreature oActor, string sLine, int iAnimation = AnimationNone, float fDuration = 0.0f, float fSpeed = 1.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSpeak(sName, oActor, "", sLine, iAnimation, fDuration, fSpeed));
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
            if (oDestination == null) oDestination = new NWGameObject();

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

        private static void DoAnimate(string sName, NWCreature oActor, string sActor, int iAnim, float fDuration = 0.0f, float fSpeed = 1.0f, bool bAction = false)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (bAction) { AssignCommand(oActor, () => ActionPlayAnimation(iAnim, fSpeed, fDuration)); }
            else { AssignCommand(oActor, () => PlayAnimation(iAnim, fSpeed, fDuration)); }
        }


        private static void DoSpeak(string sName, NWCreature oActor, string sActor, string sLine, int iAnimation = AnimationNone, float fDuration = 0.0f, float fSpeed = 1.0f, bool bAction = false)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (bAction)
            {
                AssignCommand(oActor, () => ActionSpeakString(sLine));
                if (iAnimation != AnimationNone)
                { AssignCommand(oActor, () => ActionPlayAnimation(iAnimation, fSpeed, fDuration)); }
            }

            else
            {
                AssignCommand(oActor, () => SpeakString(sLine));
                if (iAnimation != AnimationNone)
                { AssignCommand(oActor, () => PlayAnimation(iAnimation, fSpeed, fDuration)); }
            }
        }


        private static void PrintTimeStamp(string sMessage, float fStartTime)
        {
            float fCurrentTime = HoursToSeconds(GetTimeHour()) + IntToFloat((GetTimeMinute() * 60) + GetTimeSecond()) + (IntToFloat(GetTimeMillisecond()) / 1000);
            float fTime = fCurrentTime - fStartTime;
            SpeakString(FloatToString(fTime) + "s - " + sMessage);
        }

        private static void DoActionTimeStamp(string sName, NWCreature oActor, string sMessage, float fStartTime)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            AssignCommand(oActor, () => ActionDoCommand(() => PrintTimeStamp(sMessage, fStartTime)));
        }

        /// <summary>
        /// Makes the selected character say how many seconds it is since the cutscene began when it reaches this action in its queue
        /// This can be a useful debug tool for checking the timing of your cutscene and specific actions within it
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding this command to oActor's action queue</param>
        /// <param name="oActor">the actor whose action queue you want to place the command in (and who will speak the message)</param>
        /// <param name="sMessage">the message you want them to speak. NOTE - the number of seconds since the cutscene began will automatically be added to the start of this message</param>
        public static void ActionTimeStamp(float fDelay, NWCreature oActor, string sMessage)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            float fStartTime = HoursToSeconds(GetTimeHour()) + IntToFloat((GetTimeMinute() * 60) + GetTimeSecond()) + (IntToFloat(GetTimeMillisecond()) / 1000);
            DelayCommand(fDelay, () => DoActionTimeStamp(sName, oActor, sMessage, fStartTime));
        }

        private static void DebugOutput(NWPlayer oPC)
        {
            // Get the current position of oPC's camera
            float fDirection = GetLocalFloat(oPC, "fCameraDirection");
            float fRange = GetLocalFloat(oPC, "fCameraRange");
            float fPitch = GetLocalFloat(oPC, "fCameraPitch");

            // Fire a message to say where the camera is
            AssignCommand(oPC, () => SpeakString(FloatToString(fDirection) + ", " + FloatToString(fRange) + ", " + FloatToString(fPitch)));
        }


        /// <summary>
        /// Tell the selected actor to speak a line
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before speech is added to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to speak the line - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="sLine">the line you want them to speak</param>
        /// <param name="iAnimation">the animation you want them to play whilst speaking the line (leave as ANIMATION_NONE for no animation). NOTE - if you are using a ANIMATION_LOOPING_TALK_* animation, all you need to use is the last word (eg FORCEFUL))</param>
        /// <param name="fDuration">how long the animation should last (leave at 0.0 for fire-and-forget animations)</param>
        /// <param name="fSpeed">the speed of the animation (defaults to 1.0)</param>
        public static void TagActionSpeak(float fDelay, string sActor, string sLine, int iAnimation = AnimationNone, float fDuration = 0.0f, float fSpeed = 1.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSpeak(sName, new NWGameObject(), sActor, sLine, iAnimation, fDuration, fSpeed, true));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tell the selected actor to speak a line
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before speaking line</param>
        /// <param name="sActor">the tag of the character you want to speak the line - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="sLine">the line you want them to speak</param>
        /// <param name="iAnimation">the animation you want them to play whilst speaking the line (leave as ANIMATION_NONE for no animation)</param>
        /// <param name="fDuration">how long the animation should last (leave at 0.0 for fire-and-forget animations)</param>
        /// <param name="fSpeed">the speed of the animation (defaults to 1.0)</param>
        public static void TagSpeak(float fDelay, string sActor, string sLine, int iAnimation = AnimationNone, float fDuration = 0.0f, float fSpeed = 1.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSpeak(sName, new NWGameObject(), sActor, sLine, iAnimation, fDuration, fSpeed));
            RegisterActor(sName, new NWGameObject(), sActor);
        }
        
        private static void DoConversation(string sName, NWCreature oActor, string sActor, NWObject oTarget, string sConv = "", string sTarget = "", bool bGreet = true)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (sTarget != "")
            { oTarget = GetObjectByTag(sTarget); }

            AssignCommand(oActor, () => ActionStartConversation(oTarget, sConv, FALSE, bGreet ? TRUE : FALSE));
        }


        /// <summary>
        /// Tell the selected actor to start a conversation. NOTE players can hold a conversation while in cutscene mode.
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before speech is added to oActor's action queue</param>
        /// <param name="oActor">the character you want to start the conversation</param>
        /// <param name="oTarget">the character you want them to talk to</param>
        /// <param name="sConv">the conversation file they should use</param>
        /// <param name="sTarget">the tag of the character you want them to talk to. NOTE - this allows you to start conversations with creatures created during the cutscene, as long as they have a unique tag. If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the character you want the actor to talk to. If you have already set oTarget, leave sTarget at its default value of ""</param>
        /// <param name="bGreet">whether or not the character should play its greeting sound when the conversation starts</param>
        public static void ActionConversation(float fDelay, NWCreature oActor, NWObject oTarget, string sConv = "", string sTarget = "", bool bGreet = true)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoConversation(sName, oActor, "", oTarget, sConv, sTarget, bGreet));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Tell the selected actor to start a conversation. NOTE players can hold a conversation while in cutscene mode.
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before speech is added to oActor's action queue</param>
        /// <param name="sActor">the tag of the character you want to start the conversation - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oTarget">the character you want them to talk to</param>
        /// <param name="sConv">the conversation file they should use</param>
        /// <param name="sTarget">the tag of the character you want them to talk to. NOTE - this allows you to start conversations with creatures created during the cutscene, as long as they have a unique tag. If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the character you want the actor to talk to. If you have already set oTarget, leave sTarget at its default value of ""</param>
        /// <param name="bGreet">whether or not the character should play its greeting sound when the conversation starts</param>
        public static void TagActionConversation(float fDelay, string sActor, NWObject oTarget, string sConv = "", string sTarget = "", bool bGreet = true)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoConversation(sName, new NWGameObject(), sActor, oTarget, sConv, sTarget, bGreet));
            RegisterActor(sName, new NWGameObject(), sActor);
        }



        private static void DoFace(string sName, NWCreature oActor, string sActor, NWObject oTarget, int iFace, float fFace, bool bAction)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (iFace == 0)
            {
                if (bAction) { AssignCommand(oActor, () => ActionDoCommand(() => SetFacing(fFace))); }
                else { AssignCommand(oActor, () => SetFacing(fFace)); }
            }

            else if (iFace == 1)
            {
                if (bAction) { AssignCommand(oActor, () => ActionDoCommand(() => SetFacing(GetFacing(oTarget)))); }
                else { AssignCommand(oActor, () => SetFacing(GetFacing(oTarget))); }
            }

            else
            {
                if (bAction) { AssignCommand(oActor, () => ActionDoCommand(() => SetFacingPoint(GetPosition(oTarget)))); }
                else { AssignCommand(oActor, () => SetFacingPoint(GetPosition(oTarget))); }
            }
        }


        /// <summary>
        /// Tells the selected actor to face in a particular direction
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before facing command is added to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to turn - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="fFace">the direction you want the actor to face in (due east is 0.0, count in degrees anti-clockwise). NOTE - fFace is ignored if iFace is not set to 0</param>
        /// <param name="iFace">whether the actor should face in a specific direction (0), face in the direction the target is facing (1), or face the target (2)</param>
        /// <param name="oTarget">the object they should face (leave as OBJECT_INVALID if you don't want them to face an object)</param>
        public static void TagActionFace(float fDelay, string sActor, float fFace, int iFace, NWObject oTarget)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoFace(sName, new NWGameObject(), sActor, oTarget, iFace, fFace, true));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tells the selected actor to face in a particular direction
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before turning</param>
        /// <param name="sActor">the tag of the character you want to turn - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="fFace">the direction you want the actor to face in (due east is 0.0, count in degrees anti-clockwise). NOTE - fFace is ignored if iFace is not set to 0</param>
        /// <param name="iFace">whether the actor should face in a specific direction (0), face in the direction the target is facing (1), or face the target (2)</param>
        /// <param name="oTarget">the object they should face (leave as OBJECT_INVALID if you don't want them to face an object)</param>
        public static void TagFace(float fDelay, string sActor, float fFace, int iFace, NWObject oTarget)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoFace(sName, new NWGameObject(), sActor, oTarget, iFace, fFace, false));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tells the selected actor to face in a particular direction
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before facing command is added to oActor's action queue</param>
        /// <param name="oActor">the character you want to turn</param>
        /// <param name="fFace">the direction you want the actor to face in (due east is 0.0, count in degrees anti-clockwise). NOTE - fFace is ignored if iFace is not set to 0</param>
        /// <param name="iFace">whether the actor should face in a specific direction (0), face in the direction the target is facing (1), or face the target (2)</param>
        /// <param name="oTarget">the object they should face (leave as OBJECT_INVALID if you don't want them to face an object)</param>
        public static void ActionFace(float fDelay, NWCreature oActor, float fFace, int iFace, NWObject oTarget)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoFace(sName, oActor, "", oTarget, iFace, fFace, true));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Tells the selected actor to face in a particular direction
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before turning</param>
        /// <param name="oActor">the character you want to turn</param>
        /// <param name="fFace">the direction you want the actor to face in (due east is 0.0, count in degrees anti-clockwise). NOTE - fFace is ignored if iFace is not set to 0</param>
        /// <param name="iFace">whether the actor should face in a specific direction (0), face in the direction the target is facing (1), or face the target (2)</param>
        /// <param name="oTarget">the object they should face (leave as OBJECT_INVALID if you don't want them to face an object)</param>
        public static void Face(float fDelay, NWCreature oActor, float fFace, int iFace, NWObject oTarget)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoFace(sName, oActor, "", oTarget, iFace, fFace, false));
            RegisterActor(sName, oActor);
        }



        private static void DoEquip(string sName, NWCreature oActor, string sActor, int iSlot, NWItem oItem, string sItem)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (sItem != "")
            { oItem = GetItemPossessedBy(oActor, sItem); }

            if (iSlot == 999) { AssignCommand(oActor, () => ActionEquipMostDamagingMelee()); }
            else if (iSlot == 998) { AssignCommand(oActor, () => ActionEquipMostDamagingRanged()); }
            else if (iSlot == 997) { AssignCommand(oActor, ActionEquipMostEffectiveArmor); }
            else { AssignCommand(oActor, () => ActionEquipItem(oItem, iSlot)); }
        }


        /// <summary>
        /// Tells the selected actor to equip an item
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before equip command is added to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to equip the item - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="iSlot">the inventory slot to put the item in. INVENTORY_SLOT_BEST_MELEE will equip the actor's best melee weapon in his right hand. INVENTORY_SLOT_BEST_RANGED will equip the actor's best ranged weapon in his right hand. INVENTORY_SLOT_BEST_ARMOUR will equip the actor's best armour in his chest slot</param>
        /// <param name="oItem">the item you want to equip. NOTE - leave this as OBJECT_INVALID if you're auto-equipping an INVENTORY_SLOT_BEST_*</param>
        /// <param name="sItem">the tag of the item you want to equip. NOTE - this is included so that you can equip items that are created in the actor's inventory during a cutscene. NOTE - leave sItem at its default value of "" if you're auto-equipping an INVENTORY_SLOT_BEST_*. NOTE - leave sItem at its default value of "" if you have set oItem already</param>
        public static void TagActionEquip(float fDelay, string sActor, int iSlot, NWItem oItem, string sItem)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoEquip(sName, new NWGameObject(), sActor, iSlot, oItem, sItem));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tells the selected actor to equip an item
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before equip command is added to oActor's action queue</param>
        /// <param name="oActor">the character you want to equip the item</param>
        /// <param name="iSlot">the inventory slot to put the item in. INVENTORY_SLOT_BEST_MELEE will equip the actor's best melee weapon in his right hand. INVENTORY_SLOT_BEST_RANGED will equip the actor's best ranged weapon in his right hand. INVENTORY_SLOT_BEST_ARMOUR will equip the actor's best armour in his chest slot</param>
        /// <param name="oItem">the item you want to equip. NOTE - leave this as OBJECT_INVALID if you're auto-equipping an INVENTORY_SLOT_BEST_*</param>
        /// <param name="sItem">the tag of the item you want to equip. NOTE - this is included so that you can equip items that are created in the actor's inventory during a cutscene. NOTE - leave sItem at its default value of "" if you're auto-equipping an INVENTORY_SLOT_BEST_*. NOTE - leave sItem at its default value of "" if you have set oItem already</param>
        public static void ActionEquip(float fDelay, NWCreature oActor, int iSlot, NWItem oItem, string sItem = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoEquip(sName, oActor, "", iSlot, oItem, sItem));
            RegisterActor(sName, oActor);
        }


        private static void DoUnequip(string sName, NWCreature oActor, string sActor, int iSlot, NWItem oItem, string sItem)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (sItem != "")
            { oItem = GetItemPossessedBy(oActor, sItem); }

            if (iSlot != 996)
            { oItem = GetItemInSlot(iSlot, oActor); }

            AssignCommand(oActor, () => ActionUnequipItem(oItem));
        }


        /// <summary>
        /// Tells the selected actor to unequip an item
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before equip command is added to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to unequip the item - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="iSlot">the inventory slot you want the actor to clear. NOTE - if you set iSlot to anything other than its default INVENTORY_SLOT_NONE, the function will remove whatever item the actor has in the slot you specified</param>
        /// <param name="oItem">the item you want to equip. NOTE - leave this as OBJECT_INVALID if you're auto-unequipping a specific INVENTORY_SLOT_*</param>
        /// <param name="sItem">the tag of the item you want to equip. NOTE - this is included so that you can unequip items that are created during the cutscene. NOTE - leave sItem at its default value of "" if you're auto-unequipping a specific INVENTORY_SLOT_*. NOTE - leave sItem at its default value of "" if you have set oItem already</param>
        public static void TagActionUnequip(float fDelay, string sActor, int iSlot, NWItem oItem, string sItem = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoUnequip(sName, new NWGameObject(), sActor, iSlot, oItem, sItem));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tells the selected actor to unequip an item
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before equip command is added to the actor's action queue</param>
        /// <param name="oActor">the character you want to unequip the item</param>
        /// <param name="iSlot">the inventory slot you want the actor to clear. NOTE - if you set iSlot to anything other than its default INVENTORY_SLOT_NONE, the function will remove whatever item the actor has in the slot you specified</param>
        /// <param name="oItem">the item you want to equip. NOTE - leave this as OBJECT_INVALID if you're auto-unequipping a specific INVENTORY_SLOT_*</param>
        /// <param name="sItem">the tag of the item you want to equip. NOTE - this is included so that you can unequip items that are created during the cutscene. NOTE - leave sItem at its default value of "" if you're auto-unequipping a specific INVENTORY_SLOT_*. NOTE - leave sItem at its default value of "" if you have set oItem already</param>
        public static void ActionUnequip(float fDelay, NWCreature oActor, int iSlot, NWItem oItem, string sItem = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoUnequip(sName, oActor, "", iSlot, oItem, sItem));
            RegisterActor(sName, oActor);
        }



        private static void DoAttack(string sName, NWCreature oActor, string sActor, NWObject oTarget, bool bPassive, string sTarget)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (sTarget != "")
            { oTarget = GetObjectByTag(sTarget); }

            AssignCommand(oActor, () => _.ActionAttack(oTarget, bPassive ? TRUE : FALSE));
        }

        /// <summary>
        /// Tells the selected actor to attack something
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before attack is added to oActor's action queue</param>
        /// <param name="sActor">the tag of the character you want to carry out the attack - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oTarget">the object or character you want them to attack</param>
        /// <param name="sTarget">the tag of the object or character you want them to attack. NOTE - this is included so that you can attack objects and creatures that are created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        /// <param name="bPassive">whether or not to attack in passive mode</param>
        public static void TagActionAttack(float fDelay, string sActor, NWObject oTarget, string sTarget = "", bool bPassive = false)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoAttack(sName, new NWGameObject(), sActor, oTarget, bPassive, sTarget));
            RegisterActor(sName, new NWGameObject(), sActor);
        }

        /// <summary>
        /// Tells the selected actor to attack something
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before attack is added to oActor's action queue</param>
        /// <param name="oActor">the character you want to carry out the attack</param>
        /// <param name="oTarget">the object or character you want them to attack</param>
        /// <param name="sTarget">the tag of the object or character you want them to attack. NOTE - this is included so that you can attack objects and creatures that are created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        /// <param name="bPassive">whether or not to attack in passive mode</param>
        public static void ActionAttack(float fDelay, NWCreature oActor, NWObject oTarget, string sTarget = "", bool bPassive = false)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoAttack(sName, oActor, "", oTarget, bPassive, sTarget));
            RegisterActor(sName, oActor);
        }



        private static void DoSpellCast(string sName, NWCreature oActor, string sActor, NWObject oTarget, int iSpell, bool bFake = false, int iPath = PROJECTILE_PATH_TYPE_DEFAULT, string sTarget = "", bool bCheat = true, bool bInstant = false, int iLevel = 0, int iMeta = METAMAGIC_NONE)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (sTarget != "")
            { oTarget = GetObjectByTag(sTarget); }

            if (bFake) { AssignCommand(oActor, () => ActionCastFakeSpellAtObject(iSpell, oTarget, iPath)); }
            else { AssignCommand(oActor, () => ActionCastSpellAtObject(iSpell, oTarget, iMeta, bCheat ? TRUE : FALSE, iLevel, iPath, bInstant ? TRUE : FALSE)); }
        }


        /// <summary>
        /// Tells the actor to cast (or fake casting) a spell at an object
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the spell cast to the actor's action queue</param>
        /// <param name="oActor">the character you want to cast the spell</param>
        /// <param name="oTarget">the object you want to cast the spell at</param>
        /// <param name="iSpell">the SPELL_* you want to be cast</param>
        /// <param name="bFake">whether to only create the animations and visual effects for the spell (TRUE) or to really cast the spell (FALSE). NOTE - if iFake is TRUE, bCheat, bInstant and iMeta aren't used</param>
        /// <param name="iPath">the PROJECTILE_PATH_TYPE_* the spell should use (uses spell's default path unless told otherwise)</param>
        /// <param name="sTarget">the tag of the object you want to cast the spell at. NOTE - this is included so that you can cast spells at objects that have been created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        /// <param name="bCheat">whether or not to let the character cast the spell even if he wouldn't normally be able to</param>
        /// <param name="bInstant">if bInstant is set to TRUE, the character will cast the spell immediately without playing their casting animation</param>
        /// <param name="iLevel">if iLevel is set to anything other than 0, that is the level at which the spell will be cast, rather than the actor's real level</param>
        /// <param name="iMeta">the METAMAGIC_* type you want the caster to cast the spell using (NONE by default)</param>
        public static void ActionSpellCast(float fDelay, NWCreature oActor, NWObject oTarget, int iSpell, bool bFake = false, int iPath = PROJECTILE_PATH_TYPE_DEFAULT, string sTarget = "", bool bCheat = true, bool bInstant = false, int iLevel = 0, int iMeta = METAMAGIC_NONE)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSpellCast(sName, oActor, "", oTarget, iSpell, bFake, iPath, sTarget, bCheat, bInstant, iLevel, iMeta));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Tells the actor to cast (or fake casting) a spell at an object
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the spell cast to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to cast the spell - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oTarget">the object you want to cast the spell at</param>
        /// <param name="iSpell">the SPELL_* you want to be cast</param>
        /// <param name="bFake">whether to only create the animations and visual effects for the spell (TRUE) or to really cast the spell (FALSE). NOTE - if iFake is TRUE, bCheat, bInstant and iMeta aren't used</param>
        /// <param name="iPath">the PROJECTILE_PATH_TYPE_* the spell should use (uses spell's default path unless told otherwise)</param>
        /// <param name="sTarget">the tag of the object you want to cast the spell at. NOTE - this is included so that you can cast spells at objects that have been created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        /// <param name="bCheat">whether or not to let the character cast the spell even if he wouldn't normally be able to</param>
        /// <param name="bInstant">if bInstant is set to TRUE, the character will cast the spell immediately without playing their casting animation</param>
        /// <param name="iLevel">if iLevel is set to anything other than 0, that is the level at which the spell will be cast, rather than the actor's real level</param>
        /// <param name="iMeta">the METAMAGIC_* type you want the caster to cast the spell using (NONE by default)</param>
        public static void TagActionSpellCast(float fDelay, string sActor, NWObject oTarget, int iSpell, bool bFake = false, int iPath = PROJECTILE_PATH_TYPE_DEFAULT, string sTarget = "", bool bCheat = true, bool bInstant = false, int iLevel = 0, int iMeta = METAMAGIC_NONE)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSpellCast(sName, new NWGameObject(), sActor, oTarget, iSpell, bFake, iPath, sTarget, bCheat, bInstant, iLevel, iMeta));
            RegisterActor(sName, new NWGameObject(), sActor);
        }



        private static void DoEffect(string sName, NWCreature oActor, string sActor, NWObject oTarget, string sTarget, Effect eFect, int iDuration = Permanent, float fDuration = 0.0f, bool bAction = false)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "")
            { oActor = GetObjectByTag(sActor); }

            if (sTarget != "")
            { oTarget = GetObjectByTag(sTarget); }

            if (bAction) { AssignCommand(oActor, () => ActionDoCommand(() => ApplyEffectToObject(iDuration, eFect, oTarget, fDuration))); }
            else { ApplyEffectToObject(iDuration, eFect, oTarget, fDuration); }
        }


        /// <summary>
        /// Applies an effect to a target
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before applying the effect</param>
        /// <param name="oTarget">the object to apply the effect to</param>
        /// <param name="eFect">the effect to apply to the object (eg, EffectDeath())</param>
        /// <param name="iDuration">the DURATION_TYPE_* (NOTE you only need to use the last word - INSTANT, TEMPORARY or PERMANENT)</param>
        /// <param name="fDuration">how long the effect should last (only needed if iDuration is TEMPORARY)</param>
        /// <param name="sTarget">the tag of the object to apply the effect to. NOTE - this is included so that you can apply effects to objects and creatures that are created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void ApplyEffect(float fDelay, NWObject oTarget, Effect eFect, int iDuration = Permanent, float fDuration = 0.0f, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoEffect(sName, new NWGameObject(), "", oTarget, sTarget, eFect, iDuration, fDuration));
        }



        private static void DoLocationEffect(string sName, Location lTarget, Effect eFect, int iDuration = Permanent, float fDuration = 0.0f)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            ApplyEffectAtLocation(iDuration, eFect, lTarget, fDuration);
        }


        /// <summary>
        /// Creates an effect at a specific location
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before applying the effect</param>
        /// <param name="lTarget">the location to apply the effect at</param>
        /// <param name="eFect">the effect to apply (eg, EffectVisualEffect(VFX_FNF_FIREBALL))</param>
        /// <param name="iDuration">the DURATION_TYPE_* (NOTE you only need to use the last word - INSTANT, TEMPORARY or PERMANENT)</param>
        /// <param name="fDuration">how long the effect should last (only needed if iDuration is TEMPORARY)</param>
        public static void ApplyLocationEffect(float fDelay, Location lTarget, Effect eFect, int iDuration = Permanent, float fDuration = 0.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoLocationEffect(sName, lTarget, eFect, iDuration, fDuration));
        }


        /// <summary>
        /// Applies an effect to a target
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the effect to oActor's action queue</param>
        /// <param name="oActor">the character whose action queue you want the effect to go into. NOTE - this is NOT the character the effect is applied to!</param>
        /// <param name="oTarget">the object to apply the effect to</param>
        /// <param name="eFect">the effect to apply to the object (eg, EffectDeath())</param>
        /// <param name="iDuration">the DURATION_TYPE_* (NOTE you only need to use the last word - INSTANT, TEMPORARY or PERMANENT)</param>
        /// <param name="fDuration">how long the effect should last (only needed if iDuration is TEMPORARY)</param>
        /// <param name="sTarget">the tag of the object to apply the effect to. NOTE - this is included so that you can apply effects to objects and creatures that are created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void ActionEffect(float fDelay, NWCreature oActor, NWObject oTarget, Effect eFect, int iDuration = Permanent, float fDuration = 0.0f, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoEffect(sName, oActor, "", oTarget, sTarget, eFect, iDuration, fDuration, true));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Applies an effect to a target
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the effect to oActor's action queue</param>
        /// <param name="sActor">the tag of the character whose action queue you want the effect to go into - MAKE SURE THIS IS UNIQUE! NOTE - this is NOT the character the effect is applied to!</param>
        /// <param name="oTarget">the object to apply the effect to</param>
        /// <param name="eFect">the effect to apply to the object (eg, EffectDeath())</param>
        /// <param name="iDuration">the DURATION_TYPE_* (NOTE you only need to use the last word - INSTANT, TEMPORARY or PERMANENT)</param>
        /// <param name="fDuration">how long the effect should last (only needed if iDuration is TEMPORARY)</param>
        /// <param name="sTarget">the tag of the object to apply the effect to. NOTE - this is included so that you can apply effects to objects and creatures that are created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void TagActionEffect(float fDelay, string sActor, NWObject oTarget, Effect eFect, int iDuration = Permanent, float fDuration = 0.0f, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoEffect(sName, new NWGameObject(), sActor, oTarget, sTarget, eFect, iDuration, fDuration, true));
            RegisterActor(sName, new NWGameObject(), sActor);
        }



        private static void DoClearEffect(string sName, NWCreature oActor, int iFX = EffectTypeCutsceneEffects)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (iFX == EffectTypeCutsceneEffects)
            {
                ClearFX(oActor);
            }
            else
            {
                Effect eFect = GetFirstEffect(oActor);
                int iType = GetEffectType(eFect);
                while (GetIsEffectValid(eFect) == TRUE)
                {
                    if (iType == iFX)
                    { RemoveEffect(oActor, eFect); }
                    eFect = GetNextEffect(oActor);
                    iType = GetEffectType(eFect);
                }
            }
        }


        /// <summary>
        /// Searches for the selected effect on an actor and removes it
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before removing the effect from oActor</param>
        /// <param name="oActor">the object you want to remove the effect from</param>
        /// <param name="iFX">the effect you want to remove (using the EFFECT_TYPE_* constants). NOTE - leaving this at its default value (EFFECT_TYPE_CUTSCENE_EFFECTS) will remove all visual effects that might interfere with a cutscene - invisibility, polymorph, darkness, blindness, visual effects etc</param>
        public static void ClearEffect(float fDelay, NWCreature oActor, int iFX = EffectTypeCutsceneEffects)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoClearEffect(sName, oActor));
        }



        private static void VoidCreateObject(int iType, string sRef, Location lLoc, int iAnim, string sTag)
        {
            CreateObject(iType, sRef, lLoc, iAnim, sTag);
        }


        private static void VoidCreateItemOnObject(string sRef, NWObject oTarget, int iStack)
        {
            CreateItemOnObject(sRef, oTarget, iStack);
        }



        private static void DoCreate(string sName, NWCreature oActor, string sActor, NWObject oTarget, string sTarget, int iType, string sRef, string sTag, int iAnim, int iStack, bool bCreateOn, bool bAction = false)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "") { oActor = GetObjectByTag(sActor); }
            if (sTarget != "") { oTarget = GetObjectByTag(sTarget); }

            if (bCreateOn)
            {
                if (bAction) { AssignCommand(oActor, () => ActionDoCommand(() => VoidCreateItemOnObject(sRef, oTarget, iStack))); }
                else { CreateItemOnObject(sRef, oTarget, iStack); }
            }

            else
            {
                if (bAction) { AssignCommand(oActor, () => ActionDoCommand(() => VoidCreateObject(iType, sRef, GetLocation(oTarget), iAnim, sTag))); }
                else { CreateObject(iType, sRef, GetLocation(oTarget), iAnim, sTag); }
            }
        }


        /// <summary>
        /// Creates something on or at the selected object, creature or waypoint
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before the function is added to oActor's action queue</param>
        /// <param name="oActor">the character you want this command to go into the action queue for. NOTE - this is NOT the character the object is created on!</param>
        /// <param name="oTarget">the object, character or waypoint you want to create the item at or on</param>
        /// <param name="iType">the OBJECT_TYPE_* you want to create (eg, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE etc)</param>
        /// <param name="sRef">the resref of the object you want to create. NOTE - you can create gold by using "nw_it_gold001" as sRef and setting iStack to how many GP you want to create</param>
        /// <param name="sTag">the tag you want the object to be given when it is created. NOTE - this won't work if you're creating an item in an object's inventory. NOTE - leave sTag as "" if you want to use the default tag for the object, as defined in its blueprint</param>
        /// <param name="iAnim">whether or not the object should play its entry animation when it is created</param>
        /// <param name="iStack">sets how many of the items you want to create. NOTE - this can only be used if iType is OBJECT_TYPE_ITEM</param>
        /// <param name="bCreateOn">set this to TRUE if you want to create an item in the target's inventory. NOTE - this can only be used if iType is OBJECT_TYPE_ITEM - all other objects will always appear on the ground at oTarget's location</param>
        /// <param name="sTarget">the tag of the object, character or waypoint you want to create the item at or on. NOTE - this is included so that you can create objects on other objects that have been created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void ActionCreate(float fDelay, NWCreature oActor, NWObject oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, bool bCreateOn = false, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoCreate(sName, oActor, "", oTarget, sTarget, iType, sRef, sTag, iAnim, iStack, bCreateOn, true));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Creates something on or at the selected object, creature or waypoint
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before the function is added to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want this command to go into the action queue for - MAKE SURE THIS IS UNIQUE! NOTE - this is NOT the character the object is created on!</param>
        /// <param name="oTarget">the object, character or waypoint you want to create the item at or on</param>
        /// <param name="iType">the OBJECT_TYPE_* you want to create (eg, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE etc)</param>
        /// <param name="sRef">the resref of the object you want to create. NOTE - you can create gold by using "nw_it_gold001" as sRef and setting iStack to how many GP you want to create</param>
        /// <param name="sTag">the tag you want the object to be given when it is created. NOTE - this won't work if you're creating an item in an object's inventory. NOTE - leave sTag as "" if you want to use the default tag for the object, as defined in its blueprint</param>
        /// <param name="iAnim">whether or not the object should play its entry animation when it is created</param>
        /// <param name="iStack">sets how many of the items you want to create. NOTE - this can only be used if iType is OBJECT_TYPE_ITEM</param>
        /// <param name="bCreateOn">set this to TRUE if you want to create an item in the target's inventory. NOTE - this can only be used if iType is OBJECT_TYPE_ITEM - all other objects will always appear on the ground at oTarget's location</param>
        /// <param name="sTarget">the tag of the object, character or waypoint you want to create the item at or on. NOTE - this is included so that you can create objects on other objects that have been created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void TagActionCreate(float fDelay, string sActor, NWObject oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, bool bCreateOn = false, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoCreate(sName, new NWGameObject(), sActor, oTarget, sTarget, iType, sRef, sTag, iAnim, iStack, bCreateOn, true));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Creates something on or at the selected object, creature or waypoint
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before the object is created</param>
        /// <param name="oTarget">the object, character or waypoint you want to create the item at or on</param>
        /// <param name="iType">the OBJECT_TYPE_* you want to create (eg, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE etc)</param>
        /// <param name="sRef">the resref of the object you want to create. NOTE - you can create gold by using "nw_it_gold001" as sRef and setting iStack to how many GP you want to create</param>
        /// <param name="sTag">the tag you want the object to be given when it is created. NOTE - this won't work if you're creating an item in an object's inventory. NOTE - leave sTag as "" if you want to use the default tag for the object, as defined in its blueprint</param>
        /// <param name="iAnim">whether or not the object should play its entry animation when it is created</param>
        /// <param name="iStack">sets how many of the items you want to create. NOTE - this can only be used if iType is OBJECT_TYPE_ITEM</param>
        /// <param name="bCreateOn">set this to TRUE if you want to create an item in the target's inventory. NOTE - this can only be used if iType is OBJECT_TYPE_ITEM - all other objects will always appear on the ground at oTarget's location</param>
        /// <param name="sTarget">the tag of the object, character or waypoint you want to create the item at or on. NOTE - this is included so that you can create objects on other objects that have been created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void Create(float fDelay, NWObject oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, bool bCreateOn = false, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoCreate(sName, new NWGameObject(), "", oTarget, sTarget, iType, sRef, sTag, iAnim, iStack, bCreateOn));
        }



        private static void DoCopy(string sName, NWObject oSource, NWObject oTarget, string sTarget, string sTag, bool bCreateOn)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sTarget != "") { oTarget = GetObjectByTag(sTarget); }

            if (bCreateOn) { CopyObject(oSource, GetLocation(oTarget), oTarget, sTag); }
            else { CopyObject(oSource, GetLocation(oTarget), new NWGameObject(), sTag); }
        }


        /// <summary>
        /// Copies a creature or inventory item.
        /// Note that due to NWN limitations, this function will not work on placeable objects or doors
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before copying the object</param>
        /// <param name="oSource">the object you want to copy</param>
        /// <param name="oTarget">the object you want to create the copy at or on</param>
        /// <param name="bCreateOn">set this to TRUE if you want to put the copy in oTarget's inventory. NOTE - this can only be used for items, and will only work if oTarget has an inventory (ie, it's a creature or a container)</param>
        /// <param name="sTag">the tag you want to give the new item. NOTE - leave sTag as "" if you want to use the default tag for the object, as defined in its blueprint</param>
        /// <param name="sTarget">the tag of the object you want to create the copy at or on. NOTE - this is included so that you can create objects on other objects that have been created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void Copy(float fDelay, NWObject oSource, NWObject oTarget, bool bCreateOn = false, string sTag = "", string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoCopy(sName, oSource, oTarget, sTarget, sTag, bCreateOn));
        }



        private static void DoClone(string sName, NWObject oPC, NWObject oTarget, string sTarget, string sTag, bool bInvisible)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sTarget != "") { oTarget = GetObjectByTag(sTarget); }
            NWObject oClone = CopyObject(oPC, GetLocation(oTarget), new NWGameObject(), sTag);

            if (GetIsPC(oPC) == TRUE)
            {
                ChangeToStandardFaction(oClone, STANDARD_FACTION_COMMONER);
            }

            if (bInvisible) { ApplyEffectToObject(Permanent, EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY), oPC); }
        }


        /// <summary>
        /// Creates a clone of the selected PC which you can then move around from your cutscene script
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before copying the object</param>
        /// <param name="oPC">the PC you want to create a clone of</param>
        /// <param name="oTarget">the object you want the clone to appear at</param>
        /// <param name="sTag">the tag which the PC's clone will be given. NOTE - you need to make sure this tag is unique for every player you clone if you want to be able to do anything with them. NOTE - by default the clone will be given the tag "cloned_pc"</param>
        /// <param name="sTarget">the tag of the object you want the clone to appear at. NOTE - this is included so that you can create clones at the position of other objects created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        /// <param name="bInvisible">sets whether or not you want to make the PC invisible, allowing you to use them as a cameraman while their clone does the acting</param>
        public static void ClonePC(float fDelay, NWObject oPC, NWObject oTarget, string sTag = "cloned_pc", string sTarget = "", bool bInvisible = true)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoClone(sName, oPC, oTarget, sTarget, sTag, bInvisible));
        }



        private static void DoPickUp(string sName, NWCreature oActor, string sActor, NWItem oItem, string sItem)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "") { oActor = GetObjectByTag(sActor); }
            if (sItem != "") { oItem = GetNearestObjectByTag(sItem, oActor); }

            AssignCommand(oActor, () => ActionPickUpItem(oItem));
        }


        /// <summary>
        /// Tells the actor to pick up an object from the ground
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to pick up the item - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oItem">the object to pick up</param>
        /// <param name="sItem">the tag of the object to pick up (the game will find the nearest item with that tag to the actor). NOTE - this is included so that you can pick up an item created during the cutscene. NOTE - leave sItem at its default value of "" if you have already set oItem</param>
        public static void TagActionPickUp(float fDelay, string sActor, NWItem oItem, string sItem = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoPickUp(sName, new NWGameObject(), sActor, oItem, sItem));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tells the actor to pick up an object from the ground
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="oActor">the character you want to pick up the item</param>
        /// <param name="oItem">the object to pick up</param>
        /// <param name="sItem">the tag of the object to pick up (the game will find the nearest item with that tag to the actor). NOTE - this is included so that you can pick up an item created during the cutscene. NOTE - leave sItem at its default value of "" if you have already set oItem</param>
        public static void ActionPickUp(float fDelay, NWCreature oActor, NWItem oItem, string sItem = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoPickUp(sName, oActor, "", oItem, sItem));
            RegisterActor(sName, oActor);
        }



        private static void DoSit(string sName, NWCreature oActor, string sActor, NWObject oChair, string sChair)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "") { oActor = GetObjectByTag(sActor); }
            if (sChair != "") { oChair = GetNearestObjectByTag(sChair, oActor); }

            AssignCommand(oActor, () => _.ActionSit(oChair));
        }


        /// <summary>
        /// Tells the actor to sit down on a specified chair or other object
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to sit down - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oChair">the object you want them to sit on</param>
        /// <param name="sChair">the tag of the object you want them to sit on (the game will find the nearest object with that tag to the actor). NOTE - this is included so that you can sit on an object created during the cutscene. NOTE - leave sChair at its default value of "" if you have already set oChair</param>
        public static void TagActionSit(float fDelay, string sActor, NWObject oChair, string sChair = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSit(sName, new NWGameObject(), sActor, oChair, sChair));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tells the actor to sit down on a specified chair or other object
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="oActor">the character you want to sit down</param>
        /// <param name="oChair">the object you want them to sit on</param>
        /// <param name="sChair">the tag of the object you want them to sit on (the game will find the nearest object with that tag to the actor). NOTE - this is included so that you can sit on an object created during the cutscene. NOTE - leave sChair at its default value of "" if you have already set oChair</param>
        public static void ActionSit(float fDelay, NWCreature oActor, NWObject oChair, string sChair = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSit(sName, oActor, "", oChair, sChair));
            RegisterActor(sName, oActor);
        }


        private static float GestaltMonkSpeed(NWCreature oActor)
        {
            int iClass = GetLevelByClass(CLASS_TYPE_MONK, oActor);

            if (iClass >= 18) { return 1.50f; }
            if (iClass >= 15) { return 1.45f; }
            if (iClass >= 12) { return 1.40f; }
            if (iClass >= 9) { return 1.30f; }
            if (iClass >= 6) { return 1.20f; }
            if (iClass >= 3) { return 1.10f; }
            else { return 1.00f; }
        }


        private static void DoPlaySound(string sName, NWCreature oActor, string sActor, string sSound, bool bAction)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "") { oActor = GetObjectByTag(sActor); }
            if (bAction) { AssignCommand(oActor, () => ActionDoCommand(() => _.PlaySound(sSound))); }
            else { AssignCommand(oActor, () => _.PlaySound(sSound)); }
        }


        /// <summary>
        /// Tells the actor to play a sound file
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before playing the sound</param>
        /// <param name="oActor">the object you want to play the sound</param>
        /// <param name="sSound">the name of the sound you want to be played</param>
        /// <param name="sActor">the tag of the object you want to play the sound. NOTE - this is included so that you can play sounds on an object created during the cutscene. NOTE - leave sActor at its default value of "" if you have already set oActor</param>
        public static void PlaySound(float fDelay, NWCreature oActor, string sSound, string sActor = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoPlaySound(sName, oActor, sActor, sSound, false));

            if (sActor != "") { RegisterActor(sName, new NWGameObject(), sActor); }
            else { RegisterActor(sName, oActor); }
        }


        /// <summary>
        /// Tells the actor to play a sound file
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="oActor">the object you want to play the sound</param>
        /// <param name="sSound">the name of the sound you want to be played</param>
        /// <param name="sActor">the tag of the object you want to play the sound. NOTE - this is included so that you can play sounds on an object created during the cutscene. NOTE - leave sActor at its default value of "" if you have already set oActor</param>
        public static void ActionPlaySound(float fDelay, NWCreature oActor, string sSound, string sActor = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoPlaySound(sName, oActor, sActor, sSound, true));

            if (sActor != "") { RegisterActor(sName, new NWGameObject(), sActor); }
            else { RegisterActor(sName, oActor); }
        }



        private static void DoSoundObject(string sName, NWObject oSound, bool bOn, float fDuration, int iVolume, NWObject oPosition)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (bOn) { SoundObjectPlay(oSound); }
            else { SoundObjectStop(oSound); }

            if (fDuration > 0.0) { DelayCommand(fDuration, () => DoSoundObject(sName, oSound, !bOn, 0.0f, 128, new NWGameObject())); }
            if (iVolume < 128) { SoundObjectSetVolume(oSound, iVolume); }
            if (GetIsObjectValid(oPosition) == TRUE) { SoundObjectSetPosition(oSound, GetPosition(oPosition)); }
        }


        /// <summary>
        /// This function allows you to activate and deactivate sound objects, as well as to adjust their position and volume
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before making the change</param>
        /// <param name="oSound">the sound object you want to adjust</param>
        /// <param name="oPosition">changes the sound to play from the position of the specified object. NOTE - leave oPosition at its default value of OBJECT_INVALID to leave the position unchanged</param>
        /// <param name="bOn">set to TRUE to switch the sound object on, or FALSE to switch it off</param>
        /// <param name="fDuration">how long the sound object should stay on / off for. NOTE - leave fDuration at its default value of 0.0 to switch the sound object on / off permanently</param>
        /// <param name="iVolume">changes the volume of the sound (iVolume must be between 0 and 127). NOTE - leave iVolume at its default value of 128 to leave the volume unchanged</param>
        public static void SoundObject(float fDelay, NWObject oSound, NWObject oPosition, bool bOn = true, float fDuration = 0.0f, int iVolume = 128)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoSoundObject(sName, oSound, bOn, fDuration, iVolume, oPosition));
        }



        private static void DoAmbientSound(string sName, NWArea oArea, bool bOn, float fDuration, int iVolume)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (bOn) { AmbientSoundPlay(oArea); }
            else { AmbientSoundStop(oArea); }

            if (fDuration > 0.0) { DelayCommand(fDuration, () => DoAmbientSound(sName, oArea, !bOn, 0.0f, 128)); }

            if (iVolume < 101)
            {
                AmbientSoundSetDayVolume(oArea, iVolume);
                AmbientSoundSetNightVolume(oArea, iVolume);
            }
        }


        /// <summary>
        /// This function allows you to activate and deactivate sound objects, as well as to adjust their position and volume
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before making the change</param>
        /// <param name="oArea">the area whose ambient sound you want to adjust</param>
        /// <param name="bOn">set to TRUE to switch the ambient sound on, or FALSE to switch it off</param>
        /// <param name="fDuration">how long the ambient sound should stay on / off for. NOTE - leave fDuration at its default value of 0.0 to switch the sound on / off permanently</param>
        /// <param name="iVolume">changes the volume of the area's ambient sound (iVolume must be between 0 and 100). NOTE - leave iVolume at its default value of 128 to leave the volume unchanged</param>
        public static void AmbientSound(float fDelay, NWArea oArea, bool bOn = true, float fDuration = 0.0f, int iVolume = 128)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoAmbientSound(sName, oArea, bOn, fDuration, iVolume));
        }



        private static void DoMusic(string sName, NWArea oArea, bool bOn = true, int iTrack = TrackCurrent, float fDuration = 0.0f)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (fDuration > 0.0) { DelayCommand(fDuration, () => DoMusic(sName, oArea, !bOn, iTrack)); }

            if (GetLocalInt(oArea, "gcss_music_day") == 0)
            {
                SetLocalInt(oArea, "gcss_music_day", MusicBackgroundGetDayTrack(oArea));
                SetLocalInt(oArea, "gcss_music_night", MusicBackgroundGetNightTrack(oArea));
            }

            MusicBackgroundStop(oArea);
            MusicBattleStop(oArea);

            if (!bOn)
            { return; }

            if (iTrack == TrackCurrent)
            { MusicBackgroundPlay(oArea); }

            else if (iTrack == TrackOriginal)
            {
                MusicBackgroundChangeDay(oArea, GetLocalInt(oArea, "gcss_music_day"));
                MusicBackgroundChangeNight(oArea, GetLocalInt(oArea, "gcss_music_night"));
            }

            else
            {
                MusicBackgroundChangeDay(oArea, iTrack);
                MusicBackgroundChangeNight(oArea, iTrack);
            }
        }


        /// <summary>
        /// This function allows you to play a specific piece of soundtrack music at any point in the cutscene
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before changing the music</param>
        /// <param name="oArea">the area whose music you want to change</param>
        /// <param name="bOn">set to TRUE to switch the area music on, or FALSE to switch it off</param>
        /// <param name="iTrack">the TRACK_* you want to play. NOTE - leave iTrack at its default value of TRACK_CURRENT to leave the area music unchanged. NOTE - set iTrack to TRACK_ORIGINAL if you want to switch all the music settings for the area back to their original values</param>
        /// <param name="fDuration">how long the music should stay on / off for and how long the new piece of music (if you changed the track) should remain active. NOTE - leave fDuration at its default value of 0.0 to make the changes permanent</param>
        public static void PlayMusic(float fDelay, NWArea oArea, bool bOn = true, int iTrack = TrackCurrent, float fDuration = 0.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoMusic(sName, oArea, bOn, iTrack, fDuration));
        }



        private static void DoDoor(string sName, NWCreature oActor, string sActor, NWObject oDoor, bool bLock, bool bOpen)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "") { oActor = GetObjectByTag(sActor); }

            if (bOpen)
            {
                if (bLock) { AssignCommand(oActor, () => ActionDoCommand(() => SetLocked(oDoor, FALSE))); }
                AssignCommand(oActor, () => ActionOpenDoor(oDoor));
            }

            else
            {
                AssignCommand(oActor, () => ActionCloseDoor(oDoor));
                if (bLock) { AssignCommand(oActor, () => ActionDoCommand(() => SetLocked(oDoor, TRUE))); }
            }
        }

        /// <summary>
        /// Tells the actor to close a door
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="oActor">the character you want to close the door</param>
        /// <param name="oDoor">the door you want them to close</param>
        /// <param name="bLock">whether or not they should lock the door once it's closed</param>
        public static void ActionClose(float fDelay, NWCreature oActor, NWObject oDoor, bool bLock = false)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoDoor(sName, oActor, "", oDoor, bLock, false));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Tells the actor to open a door
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="oActor">the character you want to open the door</param>
        /// <param name="oDoor">the door you want them to open</param>
        /// <param name="bUnlock">whether or not they should unlock the door if necessary before opening it</param>
        public static void ActionOpen(float fDelay, NWCreature oActor, NWObject oDoor, bool bUnlock = true)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoDoor(sName, oActor, "", oDoor, bUnlock, true));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Tells the actor to close a door
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to close the door - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oDoor">the door you want them to close</param>
        /// <param name="bLock">whether or not they should lock the door once it's closed</param>
        public static void TagActionClose(float fDelay, string sActor, NWObject oDoor, bool bLock = false)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoDoor(sName, new NWGameObject(), sActor, oDoor, bLock, false));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tells the actor to open a door
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to the actor's action queue</param>
        /// <param name="sActor">the tag of the character you want to open the door - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="oDoor">the door you want them to open</param>
        /// <param name="bUnlock">whether or not they should unlock the door if necessary before opening it</param>
        public static void TagActionOpen(float fDelay, string sActor, NWObject oDoor, bool bUnlock = true)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoDoor(sName, new NWGameObject(), sActor, oDoor, bUnlock, true));
            RegisterActor(sName, new NWGameObject(), sActor);
        }



        private static void DoQuest(string sName, NWPlayer oPC, string sQuest, int iState, int iXP = 0, int iParty = 0, bool bRewardAll = true, bool bOverride = false)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (iParty == 1) { AddJournalQuestEntry(sQuest, iState, oPC, TRUE, FALSE, bOverride ? TRUE : FALSE); }
            else if (iParty == 2) { AddJournalQuestEntry(sQuest, iState, oPC, FALSE, TRUE, bOverride ? TRUE : FALSE); }
            else { AddJournalQuestEntry(sQuest, iState, oPC, FALSE, FALSE, bOverride ? TRUE : FALSE); }

            if (iXP == 0) { return; }
            else if (iXP == 1) { iXP = GetJournalQuestExperience(sQuest); }

            NWCreature oParty;

            if (bRewardAll) { iParty = 0; }

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                GiveXPToCreature(oParty, iXP);

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }


        /// <summary>
        /// Update the journals of the selected player(s), and (optionally) give them quest experience
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before applying the journal update</param>
        /// <param name="oPC">the PC who completed the quest</param>
        /// <param name="sQuest">the id tag of the quest you want to update</param>
        /// <param name="iState">the number of the quest entry you want to put in the journal</param>
        /// <param name="iXP">how many XP to give the player(s). NOTE - leave this at 0 if you want to give no XP. NOTE - set this to 1 if you want to give the quest XP you specified in the journal editor</param>
        /// <param name="iParty">sets whether to update the journal for only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        /// <param name="bRewardAll">sets whether or not to give the XP reward to all the players you updated the journal for, or only for oPC. NOTE - if iXP or iParty is 0 you can ignore this option</param>
        /// <param name="bOverride">sets whether or not to allow the function to give a player a quest state lower than the one they already have in that quest. NOTE - this is TRUE by default!</param>
        public static void JournalEntry(float fDelay, NWPlayer oPC, string sQuest, int iState, int iXP = 0, int iParty = 0, bool bRewardAll = true, bool bOverride = false)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoQuest(sName, oPC, sQuest, iState, iXP, iParty, bRewardAll, bOverride));
        }



        private static void DoWait(string sName, NWCreature oActor, string sActor, float fPause)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "") { oActor = GetObjectByTag(sActor); }

            AssignCommand(oActor, () => _.ActionWait(fPause));
        }


        /// <summary>
        /// Tells the actor to wait before proceeding with the actions in their queue
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the pause to their action queue</param>
        /// <param name="sActor">the tag of the character you want to pause - MAKE SURE THIS IS UNIQUE!</param>
        /// <param name="fPause">how many seconds they should pause for</param>
        public static void TagActionWait(float fDelay, string sActor, float fPause)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoWait(sName, new NWGameObject(), sActor, fPause));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Tells the actor to wait before proceeding with the actions in their queue
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the pause to their action queue</param>
        /// <param name="oActor">the character you want to pause</param>
        /// <param name="fPause">how many seconds they should pause for</param>
        public static void ActionWait(float fDelay, NWCreature oActor, float fPause)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoWait(sName, oActor, "", fPause));
            RegisterActor(sName, oActor);
        }



        private static void DoClear(string sName, NWCreature oActor, string sActor)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sActor != "") { oActor = GetObjectByTag(sActor); }

            AssignCommand(oActor, () => ClearAllActions(TRUE));
        }


        /// <summary>
        /// Tells the selected actor to stop everything he's doing and prepare for new orders.
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before applying this to oActor</param>
        /// <param name="oActor">the character whose action queue you want to clear</param>
        /// <param name="sActor">the tag of the character whose action queue you want to clear. NOTE - this is included so that you can clear the actions of a creature created during the cutscene. NOTE - leave sActor at its default value of "" if you have already set oActor</param>
        public static void ClearActions(float fDelay, NWCreature oActor, string sActor = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoClear(sName, oActor, sActor));
        }
        
        private static void DoFloatingText(string sName, NWCreature oActor, string sMessage, int bFaction = TRUE)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            FloatingTextStringOnCreature(sMessage, oActor, bFaction);
        }


        /// <summary>
        /// Creates a line of text that appears above the selected character and rises up the screen, fading out as it goes - good for creating scrolling credits for a module!
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before displaying the text</param>
        /// <param name="oActor">the object above which the text should appear</param>
        /// <param name="sMessage">the text you want to appear</param>
        /// <param name="bFaction">whether or not the text will only appear to members in the object's faction. NOTE - if you set this to TRUE and oActor is an object or an NPC which isn't in the PC's party, nobody will see it. NOTE - if you set this to TRUE and oActor is a PC, only other players in their party will see it. NOTE - if you set this to FALSE, everyone on the server will see the message appear in their chat window</param>
        public static void FloatingText(float fDelay, NWCreature oActor, string sMessage, int bFaction = TRUE)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoFloatingText(sName, oActor, sMessage, bFaction));
        }



        private static void DoExecute(string sName, NWObject oTarget, string sScript, string sTarget)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sTarget != "") { oTarget = GetObjectByTag(sTarget); }

            _.ExecuteScript(sScript, oTarget);
        }


        /// <summary>
        /// Execute another script
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before triggering the other script</param>
        /// <param name="oTarget">the object which the script will be triggered on</param>
        /// <param name="sScript">the name of the script</param>
        /// <param name="sTarget">the tag of the object which the script will be triggered on. NOTE - this is included so that you can run scripts on objects created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void ExecuteScript(float fDelay, NWObject oTarget, string sScript, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoExecute(sName, oTarget, sScript, sTarget));
        }


        /// <summary>
        /// Execute another script
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding this command to oActor's action queue</param>
        /// <param name="oActor">the actor whose action queue you want to place this command in (oActor doesn't have to be the same as oTarget)</param>
        /// <param name="oTarget">the object which the script will be triggered on</param>
        /// <param name="sScript">the name of the script</param>
        /// <param name="sTarget">the tag of the object which the script will be triggered on. NOTE - this is included so that you can run scripts on objects created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void ActionExecute(float fDelay, NWCreature oActor, NWObject oTarget, string sScript, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => AssignCommand(oActor, () => ActionDoCommand(() => DoExecute(sName, oTarget, sScript, sTarget))));
            RegisterActor(sName, oActor);
        }



        private static void DoDestroy(string sName, NWCreature oActor, string sActor, NWObject oTarget, string sTarget, bool bAction)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (sTarget != "") { oTarget = GetObjectByTag(sTarget); }

            if (bAction)
            {
                AssignCommand(oActor, () => ActionDoCommand(() => AssignCommand(oTarget, () => SetIsDestroyable(TRUE))));
                AssignCommand(oActor, () => ActionDoCommand(() => DestroyObject(oTarget)));
            }

            else
            {
                AssignCommand(oTarget, () => SetIsDestroyable(TRUE));
                DestroyObject(oTarget);
            }
        }


        /// <summary>
        /// Destroy the specified object. The function will SetIsDestroyable(TRUE) the object first to make sure it can be destroyed.
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding this command to the actor's action queue</param>
        /// <param name="oActor">the actor whose action queue you want this to be placed in. NOTE - this is not the object that will be destroyed!</param>
        /// <param name="oTarget">the object you want to destroy</param>
        /// <param name="sTarget">the tag of the object you want to destroy. NOTE - this is included so that you can destroy an object created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void ActionDestroy(float fDelay, NWCreature oActor, NWObject oTarget, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoDestroy(sName, oActor, "", oTarget, sTarget, true));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Destroy the specified object. The function will SetIsDestroyable(TRUE) the object first to make sure it can be destroyed.
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding this command to the actor's action queue</param>
        /// <param name="sActor">the tag of the actor whose action queue you want this to be placed in - MAKE SURE THIS IS UNIQUE! NOTE - this is not the object that will be destroyed!</param>
        /// <param name="oTarget">the object you want to destroy</param>
        /// <param name="sTarget">the tag of the object you want to destroy. NOTE - this is included so that you can destroy an object created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void TagActionDestroy(float fDelay, string sActor, NWObject oTarget, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoDestroy(sName, new NWGameObject(), sActor, oTarget, sTarget, true));
            RegisterActor(sName, new NWGameObject(), sActor);
        }


        /// <summary>
        /// Destroy the specified object. The function will SetIsDestroyable(TRUE) the object first to make sure it can be destroyed.
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before destroying the target</param>
        /// <param name="oTarget">the object you want to destroy</param>
        /// <param name="sTarget">the tag of the object you want to destroy. NOTE - this is included so that you can destroy an object created during the cutscene. NOTE - leave sTarget at its default value of "" if you have already set oTarget</param>
        public static void Destroy(float fDelay, NWObject oTarget, string sTarget = "")
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoDestroy(sName, new NWGameObject(), "", oTarget, sTarget, false));
        }
        
        /// <summary>
        /// Gets the vector linking object A to object B
        /// </summary>
        /// <param name="oA">The first object</param>
        /// <param name="oB">The second object</param>
        /// <returns></returns>
        public static Vector GetVectorAB(NWObject oA, NWObject oB)
        {
            Vector vA = GetPosition(oA);
            Vector vB = GetPosition(oB);
            Vector vDelta = Vector(vA.m_X - vB.m_X, vA.m_Y - vB.m_Y, vA.m_Z - vB.m_Z);
            return vDelta;
        }


        /// <summary>
        /// Finds the horizontal distance between two objects, ignoring any vertical component
        /// </summary>
        /// <param name="oA">The first object</param>
        /// <param name="oB">The second object</param>
        /// <returns></returns>
        public static float GetHorizontalDistanceBetween(NWObject oA, NWObject oB)
        {
            Vector vHorizontal = GetVectorAB(oA, oB);
            float fDistance = sqrt(pow(vHorizontal.m_X, 2.0f) + pow(vHorizontal.m_Y, 2.0f));
            return fDistance;
        }

        /// <summary>
        /// Finds the compass direction from the PC to a target object
        /// </summary>
        /// <param name="oTarget">The target object</param>
        /// <param name="oPC">The player object</param>
        /// <returns></returns>
        public static float GestaltGetDirection(NWObject oTarget, NWObject oPC)
        {
            Vector vdTarget = GetVectorAB(oTarget, oPC);
            float fDirection = VectorToAngle(vdTarget);
            return fDirection;
        }



        private static void DoCameraMode(string sName, NWPlayer oPC, int iMode)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            SetCameraMode(oPC, iMode);
        }



        private static void CameraMode(float fDelay, NWPlayer oPC, int iMode)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoCameraMode(sName, oPC, iMode));
        }



        private static void DoCameraFacing(string sName, float fDirection, float fRange, float fPitch, NWPlayer oPC, int iTransition)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            AssignCommand(oPC, () => SetCameraFacing(fDirection, fRange, fPitch, iTransition));
        }


        /// <summary>
        /// Acts just like the standard SetCameraFacing function
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before starting the movement</param>
        /// <param name="fDirection">the direction you want the camera to face in (0.0 = due east)</param>
        /// <param name="fRange">how far you want the camera to be from the PC</param>
        /// <param name="fPitch">how far from the vertical you want the camera to be tilted</param>
        /// <param name="oPC">the PC whose camera you want to move</param>
        /// <param name="iTransition">the transition speed (defaults to CAMERA_TRANSITION_TYPE_SNAP)</param>
        public static void CameraFacing(float fDelay, float fDirection, float fRange, float fPitch, NWPlayer oPC, int iTransition = CAMERA_TRANSITION_TYPE_SNAP)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoCameraFacing(sName, fDirection, fRange, fPitch, oPC, iTransition));
        }



        private static void CameraPoint(float fDirection, float fRange, float fPitch, float fdDirection, float fdRange, float fdPitch, float fd2Direction, float fd2Range, float fd2Pitch, float fCount, NWPlayer oPC, int iCamID, int iFace = 0)
        {
            // Check whether this camera movement has been stopped or ended
            string sCam = "iCamStop" + IntToString(iCamID);
            if (GetLocalInt(oPC, sCam) == 1)
            { return; }

            // Work out where to point the camera
            fDirection = fDirection + ((fd2Direction * pow(fCount, 2.0f)) / 2) + (fdDirection * fCount);
            fRange = fRange + ((fd2Range * pow(fCount, 2.0f)) / 2) + (fdRange * fCount);
            fPitch = fPitch + ((fd2Pitch * pow(fCount, 2.0f)) / 2) + (fdPitch * fCount);

            // Reset fDirectionNew if it's gone past 0 or 360 degrees
            while (fDirection < 0.0) { fDirection = (fDirection + 360.0f); }
            while (fDirection > 360.0) { fDirection = (fDirection - 360.0f); }

            // Set the camera and/or player facing, according to iFace
            if (iFace < 2) { AssignCommand(oPC, () => SetCameraFacing(fDirection, fRange, fPitch)); }
            if (iFace > 0) { AssignCommand(oPC, () => SetFacing(fDirection)); }

            // Store the current position of the camera
            SetLocalFloat(oPC, "fCameraDirection", fDirection);
            SetLocalFloat(oPC, "fCameraRange", fRange);
            SetLocalFloat(oPC, "fCameraPitch", fPitch);
        }



        private static void CameraPosition(float fDirection, float fRange, float fPitch, float fHeight, float fdDirection, float fdRange, float fdPitch, float fdHeight, float fd2Direction, float fd2Range, float fd2Pitch, float fd2Height, float fCount, NWPlayer oPC, int iCamID, int iFace = 0)
        {
            // Check whether this camera movement has been stopped or ended
            string sCam = "iCamStop" + IntToString(iCamID);
            if (GetLocalInt(oPC, sCam) == 1)
            { return; }

            // Work out where to point the camera
            fDirection = fDirection + ((fd2Direction * pow(fCount, 2.0f)) / 2) + (fdDirection * fCount);
            fRange = fRange + ((fd2Range * pow(fCount, 2.0f)) / 2) + (fdRange * fCount);
            fPitch = fPitch + ((fd2Pitch * pow(fCount, 2.0f)) / 2) + (fdPitch * fCount);
            fHeight = fHeight + ((fd2Height * pow(fCount, 2.0f)) / 2) + (fdHeight * fCount);

            // Reset fDirectionNew if it's gone past 0 or 360 degrees
            while (fDirection < 0.0) { fDirection = (fDirection + 360.0f); }
            while (fDirection > 360.0) { fDirection = (fDirection - 360.0f); }

            // Set the camera and/or player facing, according to iFace
            if (iFace < 2) { AssignCommand(oPC, () => SetCameraFacing(fDirection, fRange, fPitch)); }
            if (iFace > 0) { AssignCommand(oPC, () => SetFacing(fDirection)); }

            // Adjust camera height
            SetCameraHeight(oPC, fHeight);

            // Store the current position of the camera
            SetLocalFloat(oPC, "fCameraDirection", fDirection);
            SetLocalFloat(oPC, "fCameraRange", fRange);
            SetLocalFloat(oPC, "fCameraPitch", fPitch);
            SetLocalFloat(oPC, "fCameraHeight", fHeight);
        }



        private static void CameraFaceTarget(NWObject oTarget, float fRange, float fPitch, NWPlayer oPC, int iFace, int iParty = 0, int iCamID = 0)
        {
            // Check whether this camera movement has been stopped
            string sCam = "iCamStop" + IntToString(iCamID);
            if (iCamID > 0 && GetLocalInt(oPC, sCam) == 1)
            { return; }

            float fDirection;
            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                fDirection = GestaltGetDirection(oTarget, oParty);

                if (iFace < 2) { AssignCommand(oParty, () => SetCameraFacing(fDirection, fRange, fPitch)); }
                if (iFace > 0) { AssignCommand(oParty, () => SetFacing(fDirection)); }

                // Store the current position of the camera
                SetLocalFloat(oParty, "fCameraDirection", fDirection);
                SetLocalFloat(oParty, "fCameraRange", fRange);
                SetLocalFloat(oParty, "fCameraPitch", fPitch);

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static float GetPanRate(float fDirection, float fDirection2, float fTicks, int iClockwise)
        {
            // Calculates how far the camera needs to move each to tick to go from fDirection to fDirection2
            // in fTicks steps, correcting as necessary to account for clockwise or anti-clockwise movement

            float fdDirection = 0.0f;

            if (iClockwise == 0)
            {
                if (fDirection > fDirection2) { fdDirection = ((fDirection2 + 360.0f - fDirection) / fTicks); }
                else { fdDirection = ((fDirection2 - fDirection) / fTicks); }
            }

            if (iClockwise == 1)
            {
                if (fDirection2 > fDirection) { fdDirection = ((fDirection2 - fDirection - 360.0f) / fTicks); }
                else { fdDirection = ((fDirection2 - fDirection) / fTicks); }
            }

            if (iClockwise == 2)
            {
                float fCheck = fDirection2 - fDirection;
                if (fCheck > 180.0f) { fdDirection = ((fDirection2 - fDirection - 360.0f) / fTicks); }
                else if (fCheck < -180.0f) { fdDirection = ((fDirection2 + 360.0f - fDirection) / fTicks); }
                else { fdDirection = ((fDirection2 - fDirection) / fTicks); }
            }

            return fdDirection;
        }

        /// <summary>
        /// Moves the camera smoothly from one position to another over the specified time
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before starting the movement</param>
        /// <param name="fDirection">initial direction (0.0 = due east)</param>
        /// <param name="fRange">initial distance between player and camera</param>
        /// <param name="fPitch">initial pitch (vertical tilt)</param>
        /// <param name="fDirection2">finishing direction</param>
        /// <param name="fRange2">finishing distance</param>
        /// <param name="fPitch2">finishing tilt</param>
        /// <param name="fTime">number of seconds it takes camera to complete movement</param>
        /// <param name="fFrameRate">number of movements per second (governs how smooth the motion is)</param>
        /// <param name="oPC">the PC you want to apply the camera movement to</param>
        /// <param name="iClockwise">set to 1 if you want the camera to rotate clockwise, 0 for anti-clockwise, or 2 for auto-select</param>
        /// <param name="iFace">sets whether the camera (0), the character (2) or both (1) turn to face the specified direction</param>
        /// <param name="iParty">sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void CameraMove(float fDelay, float fDirection, float fRange, float fPitch, float fDirection2, float fRange2, float fPitch2, float fTime, float fFrameRate, NWPlayer oPC, int iClockwise = 0, int iFace = 0, int iParty = 0)
        {
            // Get timing information
            float fTicks = (fTime * fFrameRate);
            float fdTime = (fTime / fTicks);
            float fStart = fDelay;
            float fCount;

            float fdDirection = GetPanRate(fDirection, fDirection2, fTicks, iClockwise);
            float fdRange = ((fRange2 - fRange) / fTicks);
            float fdPitch = ((fPitch2 - fPitch) / fTicks);

            int iCamID;
            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                // Set the camera to top down mode
                CameraMode(fDelay, oParty.Object, CAMERA_MODE_TOP_DOWN);

                // Give the camera movement a unique id code so that it can be stopped
                iCamID = GetLocalInt(oParty, "iCamCount") + 1;
                SetLocalInt(oParty, "iCamCount", iCamID);

                // reset variables
                fCount = 0.0f;
                fDelay = fStart;

                // Uncomment the line below to get a message in the game telling you the id of this camera movement
                // AssignCommand(oParty,SpeakString("Camera id - " + IntToString(iCamID)));

                // After delay, stop any older camera movements and start this one
                DelayCommand(fStart, () => StopCameraMoves(oParty, 0, false, iCamID - 1));

                while (fCount <= fTicks)
                {
                    DelayCommand(fDelay, () => CameraPoint(fDirection, fRange, fPitch, fdDirection, fdRange, fdPitch, 0.0f, 0.0f, 0.0f, fCount, oParty.Object, iCamID, iFace));
                    fCount = (fCount + 1.0f);
                    fDelay = fStart + (fCount * fdTime);
                }

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }


        /// <summary>
        /// Just like GestaltCameraMove, but with the added advantage of being able to move the point the camera is centered on up and down
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before starting the movement</param>
        /// <param name="fDirection">initial direction (0.0 = due east)</param>
        /// <param name="fRange">initial distance between player and camera</param>
        /// <param name="fPitch">initial pitch (vertical tilt)</param>
        /// <param name="fHeight">initial height above the PC where the camera should point</param>
        /// <param name="fDirection2">finishing direction</param>
        /// <param name="fRange2">finishing distance</param>
        /// <param name="fPitch2">finishing tilt</param>
        /// <param name="fHeight2">finishing height</param>
        /// <param name="fTime">number of seconds it takes camera to complete movement</param>
        /// <param name="fFrameRate">number of movements per second (governs how smooth the motion is)</param>
        /// <param name="oPC">the PC you want to apply the camera movement to</param>
        /// <param name="iClockwise">set to 1 if you want the camera to rotate clockwise, 0 for anti-clockwise, or 2 for auto-select</param>
        /// <param name="iFace">sets whether the camera (0), the character (2) or both (1) turn to face the specified direction</param>
        /// <param name="iParty">sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void CameraCrane(float fDelay, float fDirection, float fRange, float fPitch, float fHeight, float fDirection2, float fRange2, float fPitch2, float fHeight2, float fTime, float fFrameRate, NWPlayer oPC, int iClockwise = 0, int iFace = 0, int iParty = 0)
        {
            // Get timing information
            float fTicks = (fTime * fFrameRate);
            float fdTime = (fTime / fTicks);
            float fStart = fDelay;
            float fCount;

            float fdDirection = GetPanRate(fDirection, fDirection2, fTicks, iClockwise);
            float fdRange = ((fRange2 - fRange) / fTicks);
            float fdPitch = ((fPitch2 - fPitch) / fTicks);
            float fdHeight = ((fHeight2 - fHeight) / fTicks);

            int iCamID;
            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                // Set the camera to top down mode
                CameraMode(fDelay, oParty.Object, CAMERA_MODE_TOP_DOWN);

                // Give the camera movement a unique id code so that it can be stopped
                iCamID = GetLocalInt(oParty, "iCamCount") + 1;
                SetLocalInt(oParty, "iCamCount", iCamID);

                // reset variables
                fCount = 0.0f;
                fDelay = fStart;

                // Uncomment the line below to get a message in the game telling you the id of this camera movement
                // AssignCommand(oParty,SpeakString("Camera id - " + IntToString(iCamID)));

                // After delay, stop any older camera movements and start this one
                var party = oParty;
                DelayCommand(fStart, () => StopCameraMoves(party, 0, false, iCamID - 1));

                while (fCount <= fTicks)
                {
                    DelayCommand(fDelay, () => CameraPosition(fDirection, fRange, fPitch, fHeight, fdDirection, fdRange, fdPitch, fdHeight, 0.0f, 0.0f, 0.0f, 0.0f, fCount, oParty.Object, iCamID, iFace));
                    fCount = (fCount + 1.0f);
                    fDelay = fStart + (fCount * fdTime);
                }

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static void CameraSmoothStart(float fdDirection1, float fdRange1, float fdPitch1, float fdDirection2, float fdRange2, float fdPitch2, float fTime, float fFrameRate, NWCreature oParty, NWObject oSync, int iCamID)
        {
            // Get starting position for camera
            float fDirection = GetLocalFloat(oSync, "fCameraDirection");
            float fRange = GetLocalFloat(oSync, "fCameraRange");
            float fPitch = GetLocalFloat(oSync, "fCameraPitch");

            // Get timing information
            float fTicks = (fTime * fFrameRate);
            float fdTime = (fTime / fTicks);
            float fDelay = 0.0f;
            float fCount = 0.0f;

            // Get camera speed and acceleration
            float fdDirection = fdDirection1 / fFrameRate;
            float fdRange = fdRange1 / fFrameRate;
            float fdPitch = fdPitch1 / fFrameRate;

            float fd2Direction = (fdDirection2 - fdDirection1) / ((fTicks - 1) * fFrameRate);
            float fd2Range = (fdRange2 - fdRange1) / ((fTicks - 1) * fFrameRate);
            float fd2Pitch = (fdPitch2 - fdPitch1) / ((fTicks - 1) * fFrameRate);

            // Start camera movement
            while (fCount < fTicks)
            {
                DelayCommand(fDelay, () => CameraPoint(fDirection, fRange, fPitch, fdDirection, fdRange, fdPitch, fd2Direction, fd2Range, fd2Pitch, fCount, oParty.Object, iCamID));
                fCount = (fCount + 1.0f);
                fDelay = (fCount * fdTime);
            }

            // Uncomment the line below to display the starting position of the camera movement
            // GestaltDebugOutput(oSync);

            // Uncomment the line below to display the finishing position of the camera movement
            // DelayCommand(fDelay,GestaltDebugOutput(oSync));
        }


        /// <summary>
        /// Produces smooth transitions between different camera movements by setting initial and final speeds
        /// The function then interpolates between the two so that the movement rate changes smoothly over the duration of the movement.
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before starting the movement</param>
        /// <param name="fdDirection1">how fast the camera's compass direction should change by in degrees per second. positive numbers produce an anti-clockwise movement, negative anti-clockwise</param>
        /// <param name="fdRange1">how fast the camera's range should change in meters per second. positive numbers move the camera away from the player, negative towards them</param>
        /// <param name="fdPitch1">how fast the camera's pitch should change in degrees per second. positive numbers tilt the camera down towards the ground, negative up towards vertical</param>
        /// <param name="fdDirection2">how fast the camera's compass direction should change by in degrees per second. positive numbers produce an anti-clockwise movement, negative anti-clockwise</param>
        /// <param name="fdRange2">how fast the camera's range should change in meters per second. positive numbers move the camera away from the player, negative towards them</param>
        /// <param name="fdPitch2">how fast the camera's pitch should change in degrees per second. positive numbers tilt the camera down towards the ground, negative up towards vertical</param>
        /// <param name="fTime">number of seconds it should take the camera to complete movement</param>
        /// <param name="fFrameRate">number of movements per second (governs how smooth the motion is)</param>
        /// <param name="oPC">the player whose camera you want to move</param>
        /// <param name="iParty">sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        /// <param name="iSync">sets whether to use separate camera starting positions for every player (0) or sync them all to oPC's camera position (1)</param>
        public static void CameraSmooth(float fDelay, float fdDirection1, float fdRange1, float fdPitch1, float fdDirection2, float fdRange2, float fdPitch2, float fTime, float fFrameRate, NWPlayer oPC, int iParty = 0, int iSync = 1)
        {
            NWCreature oParty;
            NWObject oSync;
            int iCamID;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                // Work out whose camera position to use as the starting position
                if (iSync == 1) { oSync = oPC; }
                else { oSync = oParty; }

                // Set the camera to top down mode
                CameraMode(fDelay, oParty.Object, CAMERA_MODE_TOP_DOWN);

                // Give the camera movement a unique id code so that it can be stopped
                iCamID = GetLocalInt(oParty, "iCamCount") + 1;
                SetLocalInt(oParty, "iCamCount", iCamID);

                // Uncomment the line below to get a message in the game telling you the id of this camera movement
                // AssignCommand(oParty,SpeakString("Camera id - " + IntToString(iCamID)));

                // After delay, stop any older camera movements and start this one
                var party = oParty;
                var camId = iCamID;
                DelayCommand(fDelay, () => StopCameraMoves(party, 0, false, camId - 1));
                var party1 = oParty;
                var sync = oSync;
                var id = iCamID;
                DelayCommand(fDelay, () => CameraSmoothStart(fdDirection1, fdRange1, fdPitch1, fdDirection2, fdRange2, fdPitch2, fTime, fFrameRate, party1, sync, id));

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static void CameraCraneSmoothStart(float fdDirection1, float fdRange1, float fdPitch1, float fdHeight1, float fdDirection2, float fdRange2, float fdPitch2, float fdHeight2, float fTime, float fFrameRate, NWCreature oParty, NWObject oSync, int iCamID)
        {
            // Get starting position for camera
            float fDirection = GetLocalFloat(oSync, "fCameraDirection");
            float fRange = GetLocalFloat(oSync, "fCameraRange");
            float fPitch = GetLocalFloat(oSync, "fCameraPitch");
            float fHeight = GetLocalFloat(oSync, "fCameraHeight");

            // Get timing information
            float fTicks = (fTime * fFrameRate);
            float fdTime = (fTime / fTicks);
            float fDelay = 0.0f;
            float fCount = 0.0f;

            // Get camera speed and acceleration
            float fdDirection = fdDirection1 / fFrameRate;
            float fdRange = fdRange1 / fFrameRate;
            float fdPitch = fdPitch1 / fFrameRate;
            float fdHeight = fdHeight1 / fFrameRate;

            float fd2Direction = (fdDirection2 - fdDirection1) / ((fTicks - 1) * fFrameRate);
            float fd2Range = (fdRange2 - fdRange1) / ((fTicks - 1) * fFrameRate);
            float fd2Pitch = (fdPitch2 - fdPitch1) / ((fTicks - 1) * fFrameRate);
            float fd2Height = (fdHeight2 - fdHeight1) / ((fTicks - 1) * fFrameRate);

            // Start camera movement
            while (fCount < fTicks)
            {
                DelayCommand(fDelay, () => CameraPosition(fDirection, fRange, fPitch, fHeight, fdDirection, fdRange, fdPitch, fdHeight, fd2Direction, fd2Range, fd2Pitch, fd2Height, fCount, oParty.Object, iCamID));
                fCount = (fCount + 1.0f);
                fDelay = (fCount * fdTime);
            }

            // Uncomment the line below to display the starting position of the camera movement
            // GestaltDebugOutput(oSync);

            // Uncomment the line below to display the finishing position of the camera movement
            // DelayCommand(fDelay,GestaltDebugOutput(oSync));
        }


        /// <summary>
        /// Just like GestaltCameraSmooth, but with the added advantage of being able to move the point the camera is centered on up and down
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before starting the movement</param>
        /// <param name="fdDirection1">how fast the camera's compass direction should change by in degrees per second. positive numbers produce an anti-clockwise movement, negative anti-clockwise</param>
        /// <param name="fdRange1">how fast the camera's range should change in meters per second. positive numbers move the camera away from the player, negative towards them</param>
        /// <param name="fdPitch1">how fast the camera's pitch should change in degrees per second. positive numbers tilt the camera down towards the ground, negative up towards vertical</param>
        /// <param name="fdHeight1">how fast the camera's vertical height should change in meters per second. positive numbers move the camera up, negative numbers move it down</param>
        /// <param name="fdDirection2">how fast the camera's compass direction should change by in degrees per second. positive numbers produce an anti-clockwise movement, negative anti-clockwise</param>
        /// <param name="fdRange2">how fast the camera's range should change in meters per second. positive numbers move the camera away from the player, negative towards them</param>
        /// <param name="fdPitch2">how fast the camera's pitch should change in degrees per second. positive numbers tilt the camera down towards the ground, negative up towards vertical</param>
        /// <param name="fdHeight2">how fast the camera's vertical height should change in meters per second. positive numbers move the camera up, negative numbers move it down</param>
        /// <param name="fTime">number of seconds it should take the camera to complete movement</param>
        /// <param name="fFrameRate">number of movements per second (governs how smooth the motion is)</param>
        /// <param name="oPC">the player whose camera you want to move</param>
        /// <param name="iParty">sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        /// <param name="iSync">sets whether to use separate camera starting positions for every player (0) or sync them all to oPC's camera position (1)</param>
        public static void CameraCraneSmooth(float fDelay, float fdDirection1, float fdRange1, float fdPitch1, float fdHeight1, float fdDirection2, float fdRange2, float fdPitch2, float fdHeight2, float fTime, float fFrameRate, NWPlayer oPC, int iParty = 0, int iSync = 1)
        {
            NWCreature oParty;
            NWObject oSync;
            int iCamID;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                // Work out whose camera position to use as the starting position
                if (iSync == 1) { oSync = oPC; }
                else { oSync = oParty; }

                // Set the camera to top down mode
                CameraMode(fDelay, oParty.Object, CAMERA_MODE_TOP_DOWN);

                // Give the camera movement a unique id code so that it can be stopped
                iCamID = GetLocalInt(oParty, "iCamCount") + 1;
                SetLocalInt(oParty, "iCamCount", iCamID);

                // Uncomment the line below to get a message in the game telling you the id of this camera movement
                // AssignCommand(oParty,SpeakString("Camera id - " + IntToString(iCamID)));

                // After delay, stop any older camera movements and start this one
                var id = iCamID;
                var party = oParty;
                DelayCommand(fDelay, () => StopCameraMoves(party, 0, false, id - 1));
                var party1 = oParty;
                DelayCommand(fDelay, () => CameraCraneSmoothStart(fdDirection1, fdRange1, fdPitch1, fdHeight1, fdDirection2, fdRange2, fdPitch2, fdHeight2, fTime, fFrameRate, party1, oSync, iCamID));

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static void DoCameraSetup(string sName, NWPlayer oPC, float fDirection, float fRange, float fPitch, float fHeight)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            SetLocalFloat(oPC, "fCameraDirection", fDirection);
            SetLocalFloat(oPC, "fCameraRange", fRange);
            SetLocalFloat(oPC, "fCameraPitch", fPitch);
            SetLocalFloat(oPC, "fCameraHeight", fHeight);
        }


        /// <summary>
        /// Sets where the camera will start when you next use GestaltCameraSmooth and GestaltCameraCrane - it has no effect on other functions
        /// NOTE GestaltCameraSmooth, GestaltCameraCrane, GestaltCameraCraneSmooth and GestaltCameraMove automatically store the current position of the camera after each step -
        /// GestaltCameraSetup should only be used at the start of a cutscene to set the initial position for your first GestaltCameraSmooth,
        /// or during a gap between camera movements if you want to set a new starting position midway through a cutscene
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before setting the starting position</param>
        /// <param name="oPC">the player whose camera you're going to be moving</param>
        /// <param name="fDirection">the compass direction the camera should start from</param>
        /// <param name="fRange">the distance between the camera and the player it belongs to</param>
        /// <param name="fPitch">the vertical tilt</param>
        /// <param name="fHeight">how far above the character the camera should be centered (only needed for Crane shots)</param>
        public static void CameraSetup(float fDelay, NWPlayer oPC, float fDirection, float fRange, float fPitch, float fHeight = 0.0f)
        {
            string sName = GetLocalString(GetModule(), "cutscene");

            if (fDelay == 0.0f) { DoCameraSetup(sName, oPC, fDirection, fRange, fPitch, fHeight); }
            else { DelayCommand(fDelay, () => DoCameraSetup(sName, oPC, fDirection, fRange, fPitch, fHeight)); }
        }


        /// <summary>
        /// Turns the camera and/or player between two objects
        /// NOTE that this will only work properly if the player and target objects are stationary while the function is active
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before starting the movement</param>
        /// <param name="oStart">object to face at start of movement</param>
        /// <param name="fRange">initial distance between player and camera</param>
        /// <param name="fPitch">initial pitch (vertical tilt)</param>
        /// <param name="oEnd">object to finish movement facing</param>
        /// <param name="fRange2">finishing distance</param>
        /// <param name="fPitch2">finishing tilt</param>
        /// <param name="fTime">number of seconds it takes camera to complete movement</param>
        /// <param name="fFrameRate">number of movements per second (governs how smooth the motion is)</param>
        /// <param name="oPC">the player whose camera you want to move</param>
        /// <param name="iClockwise">set to 1 if you want the camera to rotate clockwise, 0 for anti-clockwise, or 2 for auto-select</param>
        /// <param name="iFace">controls whether the camera (0), the character (2) or both (1) turn</param>
        /// <param name="iParty">sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void CameraFace(float fDelay, NWObject oStart, float fRange, float fPitch, NWObject oEnd, float fRange2, float fPitch2, float fTime, float fFrameRate, NWPlayer oPC, int iClockwise = 0, int iFace = 0, int iParty = 0)
        {
            // Get timing information
            float fCount = 0.0f;
            float fStart = fDelay;
            float fTicks = (fTime * fFrameRate);
            float fdTime = (fTime / fTicks);

            float fDirection;
            float fDirection2;

            float fdDirection;
            float fdRange = ((fRange2 - fRange) / fTicks);
            float fdPitch = ((fPitch2 - fPitch) / fTicks);

            NWCreature oParty;
            int iCamID;

            // Get first player
            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                // Set the camera to top down mode
                CameraMode(fDelay, oParty.Object, CAMERA_MODE_TOP_DOWN);

                // Give the camera movement a unique id code so that it can be stopped
                iCamID = GetLocalInt(oParty, "iCamCount") + 1;
                SetLocalInt(oParty, "iCamCount", iCamID);

                // reset variables
                fCount = 0.0f;
                fDelay = fStart;

                // Work out rotation rate for this player
                fDirection = GestaltGetDirection(oStart, oParty);
                fDirection2 = GestaltGetDirection(oEnd, oParty);
                fdDirection = GetPanRate(fDirection, fDirection2, fTicks, iClockwise);

                // After delay, stop any older camera movements and start this one
                var party = oParty;
                var id = iCamID;
                DelayCommand(fStart, () => StopCameraMoves(party, 0, false, id - 1));

                while (fCount <= fTicks)
                {
                    var direction = fDirection;
                    var direction1 = fdDirection;
                    var camId = iCamID;
                    var party1 = oParty;
                    var count = fCount;
                    DelayCommand(fDelay, () => CameraPoint(direction, fRange, fPitch, direction1, fdRange, fdPitch, 0.0f, 0.0f, 0.0f, count, party1.Object, camId, iFace));
                    fCount = (fCount + 1.0f);
                    fDelay = fStart + (fCount * fdTime);
                }

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }


        /// <summary>
        /// Tracks a moving object, turning the player's camera so that it always faces towards it
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before starting the movement</param>
        /// <param name="oTrack">object to track the movement of</param>
        /// <param name="fRange">initial distance between player and camera</param>
        /// <param name="fPitch">initial pitch (vertical tilt)</param>
        /// <param name="fRange2">finishing distance</param>
        /// <param name="fPitch2">finishing tilt</param>
        /// <param name="fTime">how long the camera will track the object for</param>
        /// <param name="fFrameRate">number of movements per second (governs how smooth the motion is)</param>
        /// <param name="oPC">the PC you want to apply the camera movement to</param>
        /// <param name="iFace">controls whether the camera (0), the character (2) or both (1) turn</param>
        /// <param name="iParty">sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void CameraTrack(float fDelay, NWObject oTrack, float fRange, float fPitch, float fRange2, float fPitch2, float fTime, float fFrameRate, NWPlayer oPC, int iFace = 0, int iParty = 0)
        {
            // Get timing information
            float fCount;
            float fStart = fDelay;
            float fTicks = (fTime * fFrameRate);
            float fdTime = (fTime / fTicks);

            float fSRange = fRange;
            float fSPitch = fPitch;

            float fdRange = ((fRange2 - fRange) / fTicks);
            float fdPitch = ((fPitch2 - fPitch) / fTicks);

            NWCreature oParty;
            int iCamID;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                // Set the camera to top down mode
                CameraMode(fDelay, oParty.Object, CAMERA_MODE_TOP_DOWN);

                // Give the camera movement a unique id code so that it can be stopped
                iCamID = GetLocalInt(oParty, "iCamCount") + 1;
                SetLocalInt(oParty, "iCamCount", iCamID);

                // reset variables
                fCount = 0.0f;
                fDelay = fStart;
                fRange = fSRange;
                fPitch = fSPitch;

                // After delay, stop any older camera movements and start this one
                var id = iCamID;
                var party = oParty;
                var id1 = id;
                DelayCommand(fStart, () => StopCameraMoves(party, 0, false, id1 - 1));

                while (fCount <= fTicks)
                {
                    var range = fRange;
                    var pitch = fPitch;
                    id = iCamID;
                    var party1 = oParty;
                    DelayCommand(fDelay, () => CameraFaceTarget(oTrack, range, pitch, party1.Object, iFace, 0, id));
                    fPitch = (fPitch + fdPitch);
                    fRange = (fRange + fdRange);
                    fCount = (fCount + 1.0f);
                    fDelay = fStart + (fCount * fdTime);
                }

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static void DoFadeOut(string sName, NWPlayer oPC, float fSpeed, int iParty)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                FadeToBlack(oParty, fSpeed);

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static void DoFadeIn(string sName, NWPlayer oPC, float fSpeed, int iParty)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                FadeFromBlack(oParty, fSpeed);

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static void DoBlack(string sName, NWPlayer oPC, int iParty)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                BlackScreen(oParty);

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static void DoStopFade(string sName, NWPlayer oPC, int iParty)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            NWCreature oParty;

            if (iParty == 1) { oParty = GetFirstFactionMember(oPC); }
            else if (iParty == 2) { oParty = GetFirstPC(); }
            else { oParty = oPC; }

            while (GetIsObjectValid(oParty) == TRUE)
            {
                StopFade(oParty);

                if (iParty == 1) { oParty = GetNextFactionMember(oParty, TRUE); }
                else if (iParty == 2) { oParty = GetNextPC(); }
                else { return; }
            }
        }



        private static void DoFade(string sName, NWPlayer oPC, CutsceneFadeType iFade, float fSpeed, float fDuration, int iParty)
        {
            if (GetLocalInt(GetModule(), sName) == TRUE)
            { return; }

            if (iFade == CutsceneFadeType.FadeIn)
            {
                if (fDuration > 0.0) { DoBlack(sName, oPC, iParty); }
                DelayCommand(fDuration, () => DoFadeIn(sName, oPC, fSpeed, iParty));
            }

            else if (iFade == CutsceneFadeType.FadeOut)
            {
                DoFadeOut(sName, oPC, fSpeed, iParty);
                if (fDuration > 0.0) { DelayCommand(fDuration, () => DoStopFade(sName, oPC, iParty)); }
            }

            else
            {
                DoFadeOut(sName, oPC, fSpeed, iParty);
                DelayCommand(fDuration, () => DoFadeIn(sName, oPC, fSpeed, iParty));
            }
        }


        /// <summary>
        /// Fades the screen of the specified player(s) to and/or from black
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before adding the command to oActor's action queue</param>
        /// <param name="oActor">the actor whose action queue you want to place this command in (oActor doesn't have to be the same as oPC)</param>
        /// <param name="oPC">the player you want to fade the screen of</param>
        /// <param name="iFade">sets what kind of fade you want - if iFade is FADE_IN, the screen will start black and then become visible. if iFade is FADE_OUT, the screen will start visible and then become black. if iFade is FADE_CROSS, the screen will start visible, fade to black and then become visible again</param>
        /// <param name="fSpeed">the speed at which the fade(s) should take place. NOTE - always use the FADE_SPEED_* constants for this unless you really know what you're doing!</param>
        /// <param name="fDuration">how many seconds the fade should last. if iFade is FADE_IN, this is how long the screen will remain black before the fade begins. if iFade is FADE_OUT, this is the time between the fade out beginning and the screen being cleared again - leave at 0.0 to keep the screen black. if iFade is FADE_CROSS, this is the time between the fade out beginning and the fade in beginning</param>
        /// <param name="iParty">sets whether to fade the screen of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void ActionCameraFade(float fDelay, NWCreature oActor, NWPlayer oPC, CutsceneFadeType iFade, float fSpeed = FADE_SPEED_MEDIUM, float fDuration = 0.0f, int iParty = 0)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => AssignCommand(oActor, () => ActionDoCommand(() => DoFade(sName, oPC, iFade, fSpeed, fDuration, iParty))));
            RegisterActor(sName, oActor);
        }


        /// <summary>
        /// Fades the screen of the specified player(s) to and/or from black
        /// </summary>
        /// <param name="fDelay">how many seconds to wait before fading the screen</param>
        /// <param name="oPC">the player you want to fade the screen of</param>
        /// <param name="iFade">sets what kind of fade you want - if iFade is FADE_IN, the screen will start black and then become visible. if iFade is FADE_OUT, the screen will start visible and then become black. if iFade is FADE_CROSS, the screen will start visible, fade to black and then become visible again</param>
        /// <param name="fSpeed">the speed at which the fade(s) should take place. NOTE - always use the FADE_SPEED_* constants for this unless you really know what you're doing!</param>
        /// <param name="fDuration">how many seconds the fade should last. if iFade is FADE_IN, this is how long the screen will remain black before the fade begins. if iFade is FADE_OUT, this is the time between the fade out beginning and the screen being cleared again - leave at 0.0 to keep the screen black. if iFade is FADE_CROSS, this is the time between the fade out beginning and the fade in beginning</param>
        /// <param name="iParty">sets whether to fade the screen of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)</param>
        public static void CameraFade(float fDelay, NWPlayer oPC, CutsceneFadeType iFade, float fSpeed = FADE_SPEED_MEDIUM, float fDuration = 0.0f, int iParty = 0)
        {
            string sName = GetLocalString(GetModule(), "cutscene");
            DelayCommand(fDelay, () => DoFade(sName, oPC, iFade, fSpeed, fDuration, iParty));
        }


        /// <summary>
        /// Gives the illusion of the camera being fixed in one place and rotating to face the player as they move
        /// To setup a fixed camera position, place a waypoint with a unique tag in your area
        /// Set the camera's tag as a LocalString "sGestaltFixedCamera" on the PC to let them know to use that camera
        /// Set a LocalFloat "fGestaltFixedCamera" on the PC to set the camera's vertical position
        /// Set "sGestaltFixedCamera" to "" to pause the tracking, or to "STOP" to end the tracking
        /// </summary>
        /// <param name="oPC">the PC you want to apply the camera movement to</param>
        /// <param name="fFrameRate">number of movements per second (governs how smooth the motion is)</param>
        public static void FixedCamera(NWPlayer oPC, float fFrameRate = 50.0f)
        {
            // Thanks to Tenchi Masaki for the idea for this function
            string sCamera = GetLocalString(oPC, "sGestaltFixedCamera");     // Gets the camera position to use
            if (sCamera == "STOP")                                          // Camera tracking is turned off, stop script and don't recheck
            { return; }
            if (sCamera == "")                                              // Camera tracking is inactive, stop script but recheck in a second
            {
                DelayCommand(1.0f, () => FixedCamera(oPC, fFrameRate));
                return;
            }

            float fHeight = GetLocalFloat(oPC, "fGestaltFixedCamera");       // Gets the camera height to use
            if (fHeight == 0.0f) { fHeight = 10.0f; }                 // Defaults camera height to 10.0 if none has been set yet

            NWObject oCamera = GetObjectByTag(sCamera);
            float fDelay = 1.0f / fFrameRate;
            float fRange = GetHorizontalDistanceBetween(oPC, oCamera);

            float fAngle = GestaltGetDirection(oPC, oCamera);                // Works out angle between camera and player
            float fPitch = atan(fRange / fHeight);                            // Works out vertical tilt
            float fDistance = sqrt(pow(fHeight, 2.0f) + pow(fRange, 2.0f));     // Works out camera distance from player
            if (fDistance > 30.0) { fDistance = 30.0f; }               // Sets distance to 30.0 if player is too far away
            if (fDistance < 5.0) { fDistance = 5.0f; }                // Sets distance to 5.0 if player is too close

            AssignCommand(oPC, () => SetCameraFacing(fAngle, fDistance, fPitch));
            DelayCommand(fDelay, () => FixedCamera(oPC, fFrameRate));
        }

    }
}
