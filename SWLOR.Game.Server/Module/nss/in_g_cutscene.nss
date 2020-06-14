const int FADE_IN = 1;
const int FADE_OUT = 2;
const int FADE_CROSS = 3;

const int ANIMATION_NONE = 999;
const int NORMAL = ANIMATION_LOOPING_TALK_NORMAL;
const int FORCEFUL = ANIMATION_LOOPING_TALK_FORCEFUL;
const int LAUGHING = ANIMATION_LOOPING_TALK_LAUGHING;
const int PLEADING = ANIMATION_LOOPING_TALK_PLEADING;

const int INVENTORY_SLOT_BEST_MELEE = 999;
const int INVENTORY_SLOT_BEST_RANGED = 998;
const int INVENTORY_SLOT_BEST_ARMOUR = 997;
const int INVENTORY_SLOT_NONE = 996;

const int INSTANT = DURATION_TYPE_INSTANT;
const int PERMANENT = DURATION_TYPE_PERMANENT;
const int TEMPORARY = DURATION_TYPE_TEMPORARY;

const int TRACK_CURRENT = 998;
const int TRACK_ORIGINAL = 999;

const int EFFECT_TYPE_CUTSCENE_EFFECTS = -1;

const int NW_FLAG_AMBIENT_ANIMATIONS          = 0x00080000;
const int NW_FLAG_IMMOBILE_AMBIENT_ANIMATIONS = 0x00200000;
const int NW_FLAG_AMBIENT_ANIMATIONS_AVIAN    = 0x00800000;



// Initializes the cutscene, setting its name and putting the selected player(s) into CutsceneMode
    // oPC              the player you want the cutscene to run for
    // sName            the name of the cutscene - this is stored on all the party members as a LocalString called "cutscene"
        // NOTE - if you don't want the player to be able to skip the cutscene, you may leave this blank
    // bCamera          sets whether or not to cancel any camera movements that may still be running from previous scripts
    // bClear           sets whether or not to clear all actions on the select PC(s)
    // bClearFX         sets whether or not to clear all visual effects from the selected PC(s) (includes cutscene invisibility, polymorph, blindness etc)
    // bResetSpeed      sets whether or not to clear all effects from the selected PC(s) that will interfere with their movement (includes sleep, paralyzation etc)
    // bStoreCam        sets whether or not to store the position of the player's camera so that it can be restored at the end of the scene
        // NOTE - if your cutscene is triggered from the OnEnter script of an area, the camera position which is stored will be invalid, as the player won't yet be in the area
            // To get around this, either set bStoreCam to FALSE and store the camera facing yourself from the script that sent the player to the new area for the cutscene
            // OR set bStoreCam to 2 to store the camera facing a few seconds after the cutscene starts, by which time the player should be in the area
    // iParty           sets whether the cutscene is being seen by only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltStartCutscene(object oPC, string sName = "", int bCamera = TRUE, int bClear = TRUE, int bClearFX = TRUE, int bResetSpeed = TRUE, int bStoreCam = TRUE, int iParty = 0);

// Move one or more NPC members of oPC's party out of the way at the beginning of the cutscene
    // fDelay           how many seconds to wait before removing the(associates)
    // oPC              the player whose associates you want to clear
    // iAssociates      the associate(s) you want to clear - add up the numbers below for all the associates you want to clear
        // 1            NPC henchman
        // 2            druid's / ranger's animal companion
        // 4            wizard's / sorceror's familiar
        // 8            summoned creature
        // 16           dominated creature
        // 32           henchman's associates
        // EXAMPLES -   to remove the henchman and all the henchman's associates, use 33 (1 + 32)
        //              to remove everyone apart from the henchman, use 30 (2 + 4 + 8 + 16)
        //              to remove all associates, leave iAssociates at its default value of 63
    // iMethod          how you want to remove the selected associate(s)
        // 0 (default)  beam them to the specified waypoint and keep them there
        // 1            make them cutscene invisible and keep them where they are
        // 2            destroy them - NOTE this is permanent, a destroyed associate cannot be returned at the end of the cutscene!
        // 3            keep them where they are, but keep them visible
    // oDestination     the waypoint you want your associates to be held at for the duration of the cutscene
        // NOTE - you only need to set this if you are using iMethod 0 (beam the associates to a holding pen)
    // sDestination     the tag of the waypoint you want oPC's associates to be beamed to
        // NOTE - leave this at its default value of "" if you have already set oDestination or if you want the associates to stay where they are
    // iParty           sets whether to clear the associates of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltClearAssociates(float fDelay, object oPC, int iAssociates = 63, int iMethod = 0, object oDestination = OBJECT_INVALID, string sDestination = "", int iParty = 0);

// Returns one or more NPC members of oPC's party which were cleared out of the way at the beginning of the cutscene
    // fDelay           how many seconds to wait before returning the associate(s)
    // oPC              the player whose associates you want to return
    // iAssociates      the associate(s) you want to return - add up the numbers below for all the associates you want to return
        // 1            NPC henchman
        // 2            druid's / ranger's animal companion
        // 4            wizard's / sorceror's familiar
        // 8            summoned creature
        // 16           dominated creature
        // 32           henchman's associates
        // EXAMPLES -   to return the henchman and all the henchman's associates, use 33 (1 + 32)
        //              to return everyone apart from the henchman, use 30 (2 + 4 + 8 + 16)
        //              to return all associates, leave iAssociates at its default value of 63
        // NOTE - make sure this matches the number you used in GestaltClearAssociates if you want to return all the associates!
    // iMethod          how you removed the associate(s)
        // 0 (default)  you beamed them to a holding position (so just beam them back)
        // 1            you made them cutscene invisible and kept them where they were (so cancel that effect and beam them back)
        // 3            you froze them where they were but kept them visible
        // NOTE - make sure this matches the iMethod you used in GestaltClearAssociates if you want to make sure they return correctly!
    // oDestination     the waypoint you want oPC's associates to be beamed to
        // NOTE - if you leave this as OBJECT_INVALID the associates will be beamed to oPC's current position
    // sDestination     the tag of the waypoint you want oPC's associates to be beamed to
        // NOTE - leave this at its default value of "" if you have already set oDestination or if you want the associates to be beamed to oPC's current position
    // iParty           sets whether to return the associates of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltReturnAssociates(float fDelay, object oPC, int iAssociates = 63, int iMethod = 0, object oDestination = OBJECT_INVALID, string sDestination = "", int iParty = 0);

// Stops the current cutscene, cancelling all camera movements and Gestalt* actions in cutscene
    // fDelay           how many seconds to wait before ending the cutscene
    // oPC              the player running the cutscene you want to stop
    // sDestination     the tag of the waypoint where you want the PC to be at the end of the cutscene
        // NOTE - if you set this to anything other than its default value of "" the PC will automatically be beamed to the specified waypoint when GestaltStopCutscene is called
        // NOTE - you can use this in the OnCutsceneAbort script to send the PC to wherever they would have been at the end of the cutscene if they hadn't skipped it
    // bMode            sets whether or not to cancel CutsceneMode
    // bCamera          sets whether or not to cancel all camera movements
    // bClear           sets whether or not to clear all actions on the select PC(s)
    // bClearFX         sets whether or not to clear all visual effects from the selected PC(s) (includes cutscene invisibility, polymorph, blindness etc)
    // bResetSpeed      sets whether or not to clear all effects from the selected PC(s) that will interfere with their movement (includes sleep, paralyzation etc)
    // bClearActors     sets whether or not to clear all actions on all the actors that took part in the cutscene and beam them to their finishing positions (if they have one)
        // NOTE - the finishing position of an actor is a waypoint with a tag equal to the name of the cutscene plus the tag of the actor
        // FOR EXAMPLE - the finishing position for an actor with the tag "freda" in a cutscene called "bigscene" is a waypoint with the tag "bigscenefreda"
        // NOTE - objects can't have a tag longer than 32 characters, so make sure your actor tags and cutscene names aren't too long!
    // iParty           make this the same as you used in GestaltStartCutscene to make sure the cutscene is cancelled for everyone together
void GestaltStopCutscene(float fDelay, object oPC, string sDestination = "", int bMode = TRUE, int bCamera = TRUE, int bClear = TRUE, int bClearFX = TRUE, int bResetSpeed = TRUE, int bClearActors = TRUE, int iParty = 0);

// Sets the speed of the selected character so that they will go fDistance metres in fTime seconds
// NOTE - to reset the character's speed to its normal rate, set the fTime parameter to 0.0
    // fDelay           how many seconds to wait before setting the character's speed
    // oActor           the character whose speed you want to adjust
    // fTime            how long you want them to take ..
    // fDistance        .. to move this far
    // iRun             sets whether they will be walking (0) or running (1)
void GestaltSetSpeed(float fDelay, object oActor, float fTime, float fDistance, int iRun);

// Sets the speed of the selected character so that they will go fDistance metres in fTime seconds
// NOTE - to reset the character's speed to its normal rate, set the fTime parameter to 0.0
    // fDelay           how many seconds to wait before setting the character's speed
    // sActor           the tag of the character whose speed you want to adjust - MAKE SURE THIS IS UNIQUE!
    // fTime            how long you want them to take ..
    // fDistance        .. to move this far
    // iRun             sets whether they will be walking (0) or running (1)
void GestaltTagSetSpeed(float fDelay, string sActor, float fTime, float fDistance, int iRun);

// Makes the selected actor completely invisible, using CUTSCENE_INVISIBILITY
    // fDelay           how many seconds to wait before applying the effect
    // oActor           the object you want to make invisible
    // fTime            how long the object will remain invisible
        // NOTE - leave this at its default value of 0.0 to make the effect permanent (it can be cancelled later using GestaltClearFX)
    // sActor           the tag of the object you want to make invisible
        // NOTE - this allows you to make objects created during the cutscene invisible
        // NOTE - leave this at its default value of "" if you have already set oActor
void GestaltInvisibility(float fDelay, object oActor, float fTime = 0.0, string sActor = "");

// Moves the selected actor to a target object in a specified time
    // fDelay           how many seconds to wait before movement is added to oActor's action queue
    // oActor           the character you want to move
    // oDestination     the object or waypoint they should move to
    // iRun             sets whether the actor will walk (FALSE) or run (TRUE)
    // fRange           how many metres from the target the actor should be at the end of movement (keep this number low if you're timing the movement!)
        // NOTE - due to a bug in BioWare's ActionMoveToObject function, if you set fRange > 0.0 for a PC, the PC will run regardless of what you set iRun to be
    // fTime            how many seconds the movement should take - leave at 0.0 if you don't want to adjust the actor's speed
    // sDestination     the tag of the object or waypoint they should move to
        // NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag
        // If you want to do this, set oDestination to OBJECT_INVALID and sDestination to the tag of the object you want to move to
        // If the object you want to send the actor to exists at the start of the cutscene, leave sDestination as ""
    // bTowards         sets whether the actor should move towards (TRUE) or away from (FALSE) the destination
void GestaltActionMove(float fDelay, object oActor, object oDestination, int iRun = FALSE, float fRange = 0.0, float fTime = 0.0, string sDestination = "", int bTowards = TRUE);

// Moves the selected actor to a target object in a specified time
    // fDelay           how many seconds to wait before movement is added to the actor's action queue
    // sActor           the tag of the character you want to move - MAKE SURE THIS IS UNIQUE!
    // oDestination     the object or waypoint they should move to
    // iRun             sets whether the actor will walk (FALSE) or run (TRUE)
    // fRange           how many metres from the target the actor should be at the end of movement (keep this number low if you're timing the movement!)
        // NOTE - due to a bug in BioWare's ActionMoveToObject function, if you set fRange > 0.0 for a PC, the PC will run regardless of what you set iRun to be
    // fTime            how many seconds the movement should take - leave at 0.0 if you don't want to adjust the actor's speed
    // sDestination     the tag of the object or waypoint they should move to
        // NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag
        // If you want to do this, set oDestination to OBJECT_INVALID and sDestination to the tag of the object you want to move to
        // If the object you want to send the actor to exists at the start of the cutscene, leave sDestination as ""
    // bTowards         sets whether the actor should move towards (TRUE) or away from (FALSE) the destination
void GestaltTagActionMove(float fDelay, string sActor, object oDestination, int iRun = FALSE, float fRange = 0.0, float fTime = 0.0, string sDestination = "", int bTowards = TRUE);

// Jumps the selected actor to the position of another object
    // fDelay           how many seconds to wait before jump is added to oActor's action queue
    // oActor           the character you want to jump
    // oTarget          the object or waypoint they should jump to
    // sTarget          the tag of the object or waypoint they should move to
        // NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag
        // If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the object you want to move to
        // If the object you want to send the actor to exists at the start of the cutscene, leave sTarget as ""
void GestaltActionJump(float fDelay, object oActor, object oTarget, string sTarget = "");

// Jumps the selected actor to the position of another object
    // fDelay           how many seconds to wait before oActor jumps
    // oActor           the character you want to jump
    // oTarget          the object or waypoint they should jump to
    // sTarget          the tag of the object or waypoint they should move to
        // NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag
        // If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the object you want to move to
        // If the object you want to send the actor to exists at the start of the cutscene, leave sTarget as ""
void GestaltJump(float fDelay, object oActor, object oTarget, string sTarget = "");

// Jumps the selected actor to the position of another object
    // fDelay           how many seconds to wait before jump is added to the actor's action queue
    // sActor           the tag of the character you want to jump - MAKE SURE THIS IS UNIQUE!
    // oTarget          the object or waypoint they should jump to
    // sTarget          the tag of the object or waypoint they should move to
        // NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag
        // If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the object you want to move to
        // If the object you want to send the actor to exists at the start of the cutscene, leave sTarget as ""
void GestaltTagActionJump(float fDelay, string sActor, object oTarget, string sTarget = "");

// Jumps the selected actor to the position of another object
    // fDelay           how many seconds to wait before the actor jumps
    // sActor           the tag of the character you want to jump - MAKE SURE THIS IS UNIQUE!
    // oTarget          the object or waypoint they should jump to
    // sTarget          the tag of the object or waypoint they should move to
        // NOTE - this allows you to send actors to objects or waypoints created during the cutscene, as long as they have a unique tag
        // If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the object you want to move to
        // If the object you want to send the actor to exists at the start of the cutscene, leave sTarget as ""
void GestaltTagJump(float fDelay, string sActor, object oTarget, string sTarget = "");

// Tell the selected actor to play an animation
    // fDelay           how many seconds to wait before animation is added to oActor's action queue
    // oActor           the character you want to play the animation
    // iAnim            the animation you want them to play (ANIMATION_*)
    // fDuration        how long the animation should last (leave at 0.0 for fire-and-forget animations)
    // fSpeed           the speed of the animation (defaults to 1.0)
void GestaltActionAnimate(float fDelay, object oActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0);

// Tell the selected actor to play an animation
    // fDelay           how many seconds to wait before playing the animation
    // oActor           the character you want to play the animation
    // iAnim            the animation you want them to play (ANIMATION_*)
    // fDuration        how long the animation should last (leave at 0.0 for fire-and-forget animations)
    // fSpeed           the speed of the animation (defaults to 1.0)
void GestaltAnimate(float fDelay, object oActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0);

// Tell the selected actor to play an animation
    // fDelay           how many seconds to wait before animation is added to the actor's action queue
    // sActor           the tag of the character you want to play the animation - MAKE SURE THIS IS UNIQUE!
    // iAnim            the animation you want them to play (ANIMATION_*)
    // fDuration        how long the animation should last (leave at 0.0 for fire-and-forget animations)
    // fSpeed           the speed of the animation (defaults to 1.0)
void GestaltTagActionAnimate(float fDelay, string sActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0);

// Tell the selected actor to play an animation
    // fDelay           how many seconds to wait before playing the animation
    // sActor           the tag of the character you want to play the animation - MAKE SURE THIS IS UNIQUE!
    // iAnim            the animation you want them to play (ANIMATION_*)
    // fDuration        how long the animation should last (leave at 0.0 for fire-and-forget animations)
    // fSpeed           the speed of the animation (defaults to 1.0)
void GestaltTagAnimate(float fDelay, string sActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0);

// Tell the selected actor to speak a line
    // fDelay           how many seconds to wait before speech is added to oActor's action queue
    // oActor           the character you want to speak the line
    // sLine            the line you want them to speak
    // iAnim            the animation you want them to play whilst speaking the line (leave as ANIMATION_NONE for no animation)
        // NOTE - if you are using a ANIMATION_LOOPING_TALK_* animation, all you need to use is the last word (eg FORCEFUL))
    // fDuration        how long the animation should last (leave at 0.0 for fire-and-forget animations)
    // fSpeed           the speed of the animation (defaults to 1.0)
void GestaltActionSpeak(float fDelay, object oActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0);

// Tell the selected actor to speak a line
    // fDelay           how many seconds to wait before speaking line
    // oActor           the character you want to speak the line
    // sLine            the line you want them to speak
    // iAnim            the animation you want them to play whilst speaking the line (leave as ANIMATION_NONE for no animation)
    // fDuration        how long the animation should last (leave at 0.0 for fire-and-forget animations)
    // fSpeed           the speed of the animation (defaults to 1.0)
void GestaltSpeak(float fDelay, object oActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0);

// Tell the selected actor to speak a line
    // fDelay           how many seconds to wait before speech is added to the actor's action queue
    // sActor           the tag of the character you want to speak the line - MAKE SURE THIS IS UNIQUE!
    // sLine            the line you want them to speak
    // iAnim            the animation you want them to play whilst speaking the line (leave as ANIMATION_NONE for no animation)
        // NOTE - if you are using a ANIMATION_LOOPING_TALK_* animation, all you need to use is the last word (eg FORCEFUL))
    // fDuration        how long the animation should last (leave at 0.0 for fire-and-forget animations)
    // fSpeed           the speed of the animation (defaults to 1.0)
void GestaltTagActionSpeak(float fDelay, string sActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0);

// Tell the selected actor to speak a line
    // fDelay           how many seconds to wait before speaking line
    // sActor           the tag of the character you want to speak the line - MAKE SURE THIS IS UNIQUE!
    // sLine            the line you want them to speak
    // iAnim            the animation you want them to play whilst speaking the line (leave as ANIMATION_NONE for no animation)
    // fDuration        how long the animation should last (leave at 0.0 for fire-and-forget animations)
    // fSpeed           the speed of the animation (defaults to 1.0)
void GestaltTagSpeak(float fDelay, string sActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0);

// Tell the selected actor to start a conversation. NOTE players can hold a conversation while in cutscene mode.
    // fDelay           how many seconds to wait before speech is added to oActor's action queue
    // oActor           the character you want to start the conversation
    // oTarget          the character you want them to talk to
    // sConv            the conversation file they should use
    // sTarget          the tag of the character you want them to talk to
        // NOTE - this allows you to start conversations with creatures created during the cutscene, as long as they have a unique tag
        // If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the character you want the actor to talk to
        // If you have already set oTarget, leave sTarget at its default value of ""
    // bGreet           whether or not the character should play its greeting sound when the conversation starts
void GestaltActionConversation(float fDelay, object oActor, object oTarget, string sConv = "", string sTarget = "", int bGreet = TRUE);

// Tell the selected actor to start a conversation. NOTE players can hold a conversation while in cutscene mode.
    // fDelay           how many seconds to wait before speech is added to oActor's action queue
    // sActor           the tag of the character you want to start the conversation - MAKE SURE THIS IS UNIQUE!
    // oTarget          the character you want them to talk to
    // sConv            the conversation file they should use
    // sTarget          the tag of the character you want them to talk to
        // NOTE - this allows you to start conversations with creatures created during the cutscene, as long as they have a unique tag
        // If you want to do this, set oTarget to OBJECT_INVALID and sTarget to the tag of the character you want the actor to talk to
        // If you have already set oTarget, leave sTarget at its default value of ""
    // bGreet           whether or not the character should play its greeting sound when the conversation starts
void GestaltTagActionConversation(float fDelay, string sActor, object oTarget, string sConv = "", string sTarget = "", int bGreet = TRUE);

// Tells the selected actor to face in a particular direction
    // fDelay           how many seconds to wait before facing command is added to oActor's action queue
    // oActor           the character you want to turn
    // fFace            the direction you want the actor to face in (due east is 0.0, count in degrees anti-clockwise)
        // NOTE - fFace is ignored if iFace is not set to 0
    // iFace            whether the actor should face in a specific direction (0), face in the direction the target is facing (1), or face the target (2)
    // oTarget          the object they should face (leave as OBJECT_INVALID if you don't want them to face an object)
void GestaltActionFace(float fDelay, object oActor, float fFace, int iFace = 0, object oTarget = OBJECT_INVALID);

// Tells the selected actor to face in a particular direction
    // fDelay           how many seconds to wait before turning
    // oActor           the character you want to turn
    // fFace            the direction you want the actor to face in (due east is 0.0, count in degrees anti-clockwise)
        // NOTE - fFace is ignored if iFace is not set to 0
    // iFace            whether the actor should face in a specific direction (0), face in the direction the target is facing (1), or face the target (2)
    // oTarget          the object they should face (leave as OBJECT_INVALID if you don't want them to face an object)
void GestaltFace(float fDelay, object oActor, float fFace, int iFace = 0, object oTarget = OBJECT_INVALID);

// Tells the selected actor to face in a particular direction
    // fDelay           how many seconds to wait before facing command is added to the actor's action queue
    // sActor           the tag of the character you want to turn - MAKE SURE THIS IS UNIQUE!
    // fFace            the direction you want the actor to face in (due east is 0.0, count in degrees anti-clockwise)
        // NOTE - fFace is ignored if iFace is not set to 0
    // iFace            whether the actor should face in a specific direction (0), face in the direction the target is facing (1), or face the target (2)
    // oTarget          the object they should face (leave as OBJECT_INVALID if you don't want them to face an object)
void GestaltTagActionFace(float fDelay, string sActor, float fFace, int iFace = 0, object oTarget = OBJECT_INVALID);

// Tells the selected actor to face in a particular direction
    // fDelay           how many seconds to wait before turning
    // sActor           the tag of the character you want to turn - MAKE SURE THIS IS UNIQUE!
    // fFace            the direction you want the actor to face in (due east is 0.0, count in degrees anti-clockwise)
        // NOTE - fFace is ignored if iFace is not set to 0
    // iFace            whether the actor should face in a specific direction (0), face in the direction the target is facing (1), or face the target (2)
    // oTarget          the object they should face (leave as OBJECT_INVALID if you don't want them to face an object)
void GestaltTagFace(float fDelay, string sActor, float fFace, int iFace = 0, object oTarget = OBJECT_INVALID);

// Tells the selected actor to equip an item
    // fDelay           how many seconds to wait before equip command is added to oActor's action queue
    // oActor           the character you want to equip the item
    // iSlot            the inventory slot to put the item in
        // INVENTORY_SLOT_BEST_MELEE will equip the actor's best melee weapon in his right hand
        // INVENTORY_SLOT_BEST_RANGED will equip the actor's best ranged weapon in his right hand
        // INVENTORY_SLOT_BEST_ARMOUR will equip the actor's best armour in his chest slot
    // oItem            the item you want to equip
        // NOTE - leave this as OBJECT_INVALID if you're auto-equipping an INVENTORY_SLOT_BEST_*
    // sItem            the tag of the item you want to equip
        // NOTE - this is included so that you can equip items that are created in the actor's inventory during a cutscene
        // NOTE - leave sItem at its default value of "" if you're auto-equipping an INVENTORY_SLOT_BEST_*
        // NOTE - leave sItem at its default value of "" if you have set oItem already
void GestaltActionEquip(float fDelay, object oActor, int iSlot = INVENTORY_SLOT_BEST_MELEE, object oItem = OBJECT_INVALID, string sItem = "");

// Tells the selected actor to equip an item
    // fDelay           how many seconds to wait before equip command is added to the actor's action queue
    // sActor           the tag of the character you want to equip the item - MAKE SURE THIS IS UNIQUE!
    // iSlot            the inventory slot to put the item in
        // INVENTORY_SLOT_BEST_MELEE will equip the actor's best melee weapon in his right hand
        // INVENTORY_SLOT_BEST_RANGED will equip the actor's best ranged weapon in his right hand
        // INVENTORY_SLOT_BEST_ARMOUR will equip the actor's best armour in his chest slot
    // oItem            the item you want to equip
        // NOTE - leave this as OBJECT_INVALID if you're auto-equipping an INVENTORY_SLOT_BEST_*
    // sItem            the tag of the item you want to equip
        // NOTE - this is included so that you can equip items that are created in the actor's inventory during a cutscene
        // NOTE - leave sItem at its default value of "" if you're auto-equipping an INVENTORY_SLOT_BEST_*
        // NOTE - leave sItem at its default value of "" if you have set oItem already
void GestaltTagActionEquip(float fDelay, string sActor, int iSlot = INVENTORY_SLOT_BEST_MELEE, object oItem = OBJECT_INVALID, string sItem = "");

// Tells the selected actor to unequip an item
    // fDelay           how many seconds to wait before equip command is added to the actor's action queue
    // sActor           the tag of the character you want to unequip the item - MAKE SURE THIS IS UNIQUE!
    // iSlot            the inventory slot you want the actor to clear
        // NOTE - if you set iSlot to anything other than its default INVENTORY_SLOT_NONE, the function will remove whatever item the actor has in the slot you specified
    // oItem            the item you want to equip
        // NOTE - leave this as OBJECT_INVALID if you're auto-unequipping a specific INVENTORY_SLOT_*
    // sItem            the tag of the item you want to equip
        // NOTE - this is included so that you can unequip items that are created during the cutscene
        // NOTE - leave sItem at its default value of "" if you're auto-unequipping a specific INVENTORY_SLOT_*
        // NOTE - leave sItem at its default value of "" if you have set oItem already
void GestaltTagActionUnequip(float fDelay, string sActor, int iSlot = INVENTORY_SLOT_NONE, object oItem = OBJECT_INVALID, string sItem = "");

// Tells the selected actor to unequip an item
    // fDelay           how many seconds to wait before equip command is added to the actor's action queue
    // oActor           the character you want to unequip the item
    // iSlot            the inventory slot you want the actor to clear
        // NOTE - if you set iSlot to anything other than its default INVENTORY_SLOT_NONE, the function will remove whatever item the actor has in the slot you specified
    // oItem            the item you want to equip
        // NOTE - leave this as OBJECT_INVALID if you're auto-unequipping a specific INVENTORY_SLOT_*
    // sItem            the tag of the item you want to equip
        // NOTE - this is included so that you can unequip items that are created during the cutscene
        // NOTE - leave sItem at its default value of "" if you're auto-unequipping a specific INVENTORY_SLOT_*
        // NOTE - leave sItem at its default value of "" if you have set oItem already
void GestaltActionUnequip(float fDelay, object oActor, int iSlot = INVENTORY_SLOT_NONE, object oItem = OBJECT_INVALID, string sItem = "");

// Tells the selected actor to attack something
    // fDelay           how many seconds to wait before attack is added to oActor's action queue
    // oActor           the character you want to carry out the attack
    // oTarget          the object or character you want them to attack
    // sTarget          the tag of the object or character you want them to attack
        // NOTE - this is included so that you can attack objects and creatures that are created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
    // bPassive         whether or not to attack in passive mode
void GestaltActionAttack(float fDelay, object oActor, object oTarget, string sTarget = "", int bPassive = FALSE);

// Tells the selected actor to attack something
    // fDelay           how many seconds to wait before attack is added to oActor's action queue
    // sActor           the tag of the character you want to carry out the attack - MAKE SURE THIS IS UNIQUE!
    // oTarget          the object or character you want them to attack
    // sTarget          the tag of the object or character you want them to attack
        // NOTE - this is included so that you can attack objects and creatures that are created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
    // bPassive         whether or not to attack in passive mode
void GestaltTagActionAttack(float fDelay, string sActor, object oTarget, string sTarget = "", int bPassive = FALSE);

// Applies an effect to a target
    // fDelay           how many seconds to wait before applying the effect
    // oTarget          the object to apply the effect to
    // eFect            the effect to apply to the object (eg, EffectDeath())
    // iDuration        the DURATION_TYPE_* (NOTE you only need to use the last word - INSTANT, TEMPORARY or PERMANENT)
    // fDuration        how long the effect should last (only needed if iDuration is TEMPORARY)
    // sTarget          the tag of the object to apply the effect to
        // NOTE - this is included so that you can apply effects to objects and creatures that are created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltApplyEffect(float fDelay, object oTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0, string sTarget = "");

// Creates an effect at a specific location
    // fDelay           how many seconds to wait before applying the effect
    // lTarget          the location to apply the effect at
    // eFect            the effect to apply (eg, EffectVisualEffect(VFX_FNF_FIREBALL))
    // iDuration        the DURATION_TYPE_* (NOTE you only need to use the last word - INSTANT, TEMPORARY or PERMANENT)
    // fDuration        how long the effect should last (only needed if iDuration is TEMPORARY)
void GestaltApplyLocationEffect(float fDelay, location lTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0);

// Applies an effect to a target
    // fDelay           how many seconds to wait before adding the effect to oActor's action queue
    // oActor           the character whose action queue you want the effect to go into
        // NOTE - this is NOT the character the effect is applied to!
    // oTarget          the object to apply the effect to
    // eFect            the effect to apply to the object (eg, EffectDeath())
    // iDuration        the DURATION_TYPE_* (NOTE you only need to use the last word - INSTANT, TEMPORARY or PERMANENT)
    // fDuration        how long the effect should last (only needed if iDuration is TEMPORARY)
    // sTarget          the tag of the object to apply the effect to
        // NOTE - this is included so that you can apply effects to objects and creatures that are created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltActionEffect(float fDelay, object oActor, object oTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0, string sTarget = "");

// Applies an effect to a target
    // fDelay           how many seconds to wait before adding the effect to oActor's action queue
    // sActor           the tag of the character whose action queue you want the effect to go into - MAKE SURE THIS IS UNIQUE!
        // NOTE - this is NOT the character the effect is applied to!
    // oTarget          the object to apply the effect to
    // eFect            the effect to apply to the object (eg, EffectDeath())
    // iDuration        the DURATION_TYPE_* (NOTE you only need to use the last word - INSTANT, TEMPORARY or PERMANENT)
    // fDuration        how long the effect should last (only needed if iDuration is TEMPORARY)
    // sTarget          the tag of the object to apply the effect to
        // NOTE - this is included so that you can apply effects to objects and creatures that are created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltTagActionEffect(float fDelay, string sActor, object oTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0, string sTarget = "");

// Searches for the selected effect on an actor and removes it
    // fDelay           how many seconds to wait before removing the effect from oActor
    // oActor           the object you want to remove the effect from
    // iFX              the effect you want to remove (using the EFFECT_TYPE_* constants)
        // NOTE - leaving this at its default value (EFFECT_TYPE_CUTSCENE_EFFECTS) will remove all visual effects that
        // might interfere with a cutscene - invisibility, polymorph, darkness, blindness, visual effects etc
void GestaltClearEffect(float fDelay, object oActor, int iFX = EFFECT_TYPE_CUTSCENE_EFFECTS);

// Creates something on or at the selected object, creature or waypoint
    // fDelay           how many seconds to wait before the function is added to oActor's action queue
    // oActor           the character you want this command to go into the action queue for
        // NOTE - this is NOT the character the object is created on!
    // oTarget          the object, character or waypoint you want to create the item at or on
    // iType            the OBJECT_TYPE_* you want to create (eg, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE etc)
    // sRef             the resref of the object you want to create
        // NOTE - you can create gold by using "nw_it_gold001" as sRef and setting iStack to how many GP you want to create
    // sTag             the tag you want the object to be given when it is created
        // NOTE - this won't work if you're creating an item in an object's inventory
        // NOTE - leave sTag as "" if you want to use the default tag for the object, as defined in its blueprint
    // iAnim            whether or not the object should play its entry animation when it is created
    // iStack           sets how many of the items you want to create
        // NOTE - this can only be used if iType is OBJECT_TYPE_ITEM
    // bCreateOn        set this to TRUE if you want to create an item in the target's inventory
        // NOTE - this can only be used if iType is OBJECT_TYPE_ITEM - all other objects will always appear on the ground at oTarget's location
    // sTarget          the tag of the object, character or waypoint you want to create the item at or on
        // NOTE - this is included so that you can create objects on other objects that have been created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltActionCreate(float fDelay, object oActor, object oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, int bCreateOn = FALSE, string sTarget = "");

// Creates something on or at the selected object, creature or waypoint
    // fDelay           how many seconds to wait before the function is added to the actor's action queue
    // sActor           the tag of the character you want this command to go into the action queue for - MAKE SURE THIS IS UNIQUE!
        // NOTE - this is NOT the character the object is created on!
    // oTarget          the object, character or waypoint you want to create the item at or on
    // iType            the OBJECT_TYPE_* you want to create (eg, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE etc)
    // sRef             the resref of the object you want to create
        // NOTE - you can create gold by using "nw_it_gold001" as sRef and setting iStack to how many GP you want to create
    // sTag             the tag you want the object to be given when it is created
        // NOTE - this won't work if you're creating an item in an object's inventory
        // NOTE - leave sTag as "" if you want to use the default tag for the object, as defined in its blueprint
    // iAnim            whether or not the object should play its entry animation when it is created
    // iStack           sets how many of the items you want to create
        // NOTE - this can only be used if iType is OBJECT_TYPE_ITEM
    // bCreateOn        set this to TRUE if you want to create an item in the target's inventory
        // NOTE - this can only be used if iType is OBJECT_TYPE_ITEM - all other objects will always appear on the ground at oTarget's location
    // sTarget          the tag of the object, character or waypoint you want to create the item at or on
        // NOTE - this is included so that you can create objects on other objects that have been created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltTagActionCreate(float fDelay, string sActor, object oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, int bCreateOn = FALSE, string sTarget = "");

// Creates something on or at the selected object, creature or waypoint
    // fDelay           how many seconds to wait before the object is created
    // oTarget          the object, character or waypoint you want to create the item at or on
    // iType            the OBJECT_TYPE_* you want to create (eg, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE etc)
    // sRef             the resref of the object you want to create
        // NOTE - you can create gold by using "nw_it_gold001" as sRef and setting iStack to how many GP you want to create
    // sTag             the tag you want the object to be given when it is created
        // NOTE - this won't work if you're creating an item in an object's inventory
        // NOTE - leave sTag as "" if you want to use the default tag for the object, as defined in its blueprint
    // iAnim            whether or not the object should play its entry animation when it is created
    // iStack           sets how many of the items you want to create
        // NOTE - this can only be used if iType is OBJECT_TYPE_ITEM
    // bCreateOn        set this to TRUE if you want to create an item in the target's inventory
        // NOTE - this can only be used if iType is OBJECT_TYPE_ITEM - all other objects will always appear on the ground at oTarget's location
    // sTarget          the tag of the object, character or waypoint you want to create the item at or on
        // NOTE - this is included so that you can create objects on other objects that have been created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltCreate(float fDelay, object oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, int bCreateOn = FALSE, string sTarget = "");

// Copies a creature or inventory item
// Note that due to NWN limitations, this function will not work on placeable objects or doors
    // fDelay           how many seconds to wait before copying the object
    // oSource          the object you want to copy
    // oTarget          the object you want to create the copy at or on
    // bCreateOn        set this to TRUE if you want to put the copy in oTarget's inventory
        // NOTE - this can only be used for items, and will only work if oTarget has an inventory (ie, it's a creature or a container)
    // sTag             the tag you want to give the new item
        // NOTE - leave sTag as "" if you want to use the default tag for the object, as defined in its blueprint
    // sTarget          the tag of the object you want to create the copy at or on
        // NOTE - this is included so that you can create objects on other objects that have been created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltCopy(float fDelay, object oSource, object oTarget, int bCreateOn = FALSE, string sTag = "", string sTarget = "");

// Creates a clone of the selected PC which you can then move around from your cutscene script
    // fDelay           how many seconds to wait before copying the object
    // oPC              the PC you want to create a clone of
    // oTarget          the object you want the clone to appear at
    // sTag             the tag which the PC's clone will be given
        // NOTE - you need to make sure this tag is unique for every player you clone if you want to be able to do anything with them
        // NOTE - by default the clone will be given the tag "cloned_pc"
    // sTarget          the tag of the object you want the clone to appear at
        // NOTE - this is included so that you can create clones at the position of other objects created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
    // bInvisible       sets whether or not you want to make the PC invisible, allowing you to use them as a cameraman while their clone does the acting
void GestaltClonePC(float fDelay, object oPC, object oTarget, string sTag = "cloned_pc", string sTarget = "", int bInvisible = TRUE);

// Tells the actor to cast (or fake casting) a spell at an object
    // fDelay           how many seconds to wait before adding the spell cast to the actor's action queue
    // oActor           the character you want to cast the spell
    // oTarget          the object you want to cast the spell at
    // iSpell           the SPELL_* you want to be cast
    // bFake            whether to only create the animations and visual effects for the spell (TRUE) or to really cast the spell (FALSE)
        // NOTE - if iFake is TRUE, bCheat, bInstant and iMeta aren't used
    // iPath            the PROJECTILE_PATH_TYPE_* the spell should use (uses spell's default path unless told otherwise)
    // sTarget          the tag of the object you want to cast the spell at
        // NOTE - this is included so that you can cast spells at objects that have been created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
    // bCheat           whether or not to let the character cast the spell even if he wouldn't normally be able to
    // bInstant         if bInstant is set to TRUE, the character will cast the spell immediately without playing their casting animation
    // iLevel           if iLevel is set to anything other than 0, that is the level at which the spell will be cast, rather than the actor's real level
    // iMeta            the METAMAGIC_* type you want the caster to cast the spell using (NONE by default)
void GestaltActionSpellCast(float fDelay, object oActor, object oTarget, int iSpell, int bFake = FALSE, int iPath = PROJECTILE_PATH_TYPE_DEFAULT, string sTarget = "", int bCheat = TRUE, int bInstant = FALSE, int iLevel = 0, int iMeta = METAMAGIC_NONE);

// Tells the actor to cast (or fake casting) a spell at an object
    // fDelay           how many seconds to wait before adding the spell cast to the actor's action queue
    // sActor           the tag of the character you want to cast the spell - MAKE SURE THIS IS UNIQUE!
    // oTarget          the object you want to cast the spell at
    // iSpell           the SPELL_* you want to be cast
    // bFake            whether to only create the animations and visual effects for the spell (TRUE) or to really cast the spell (FALSE)
        // NOTE - if iFake is TRUE, bCheat, bInstant and iMeta aren't used
    // iPath            the PROJECTILE_PATH_TYPE_* the spell should use (uses spell's default path unless told otherwise)
    // sTarget          the tag of the object you want to cast the spell at
        // NOTE - this is included so that you can cast spells at objects that have been created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
    // bCheat           whether or not to let the character cast the spell even if he wouldn't normally be able to
    // bInstant         if bInstant is set to TRUE, the character will cast the spell immediately without playing their casting animation
    // iLevel           if iLevel is set to anything other than 0, that is the level at which the spell will be cast, rather than the actor's real level
    // iMeta            the METAMAGIC_* type you want the caster to cast the spell using (NONE by default)
void GestaltTagActionSpellCast(float fDelay, string sActor, object oTarget, int iSpell, int bFake = FALSE, int iPath = PROJECTILE_PATH_TYPE_DEFAULT, string sTarget = "", int bCheat = TRUE, int bInstant = FALSE, int iLevel = 0, int iMeta = METAMAGIC_NONE);

// Tells the actor to close a door
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // oActor           the character you want to close the door
    // oDoor            the door you want them to close
    // bLock            whether or not they should lock the door once it's closed
void GestaltActionClose(float fDelay, object oActor, object oDoor, int bLock = FALSE);

// Tells the actor to open a door
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // oActor           the character you want to open the door
    // oDoor            the door you want them to open
    // bUnlock          whether or not they should unlock the door if necessary before opening it
void GestaltActionOpen(float fDelay, object oActor, object oDoor, int bUnlock = TRUE);

// Tells the actor to close a door
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // sActor           the tag of the character you want to close the door - MAKE SURE THIS IS UNIQUE!
    // oDoor            the door you want them to close
    // bLock            whether or not they should lock the door once it's closed
void GestaltTagActionClose(float fDelay, string sActor, object oDoor, int bLock = FALSE);

// Tells the actor to open a door
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // sActor           the tag of the character you want to open the door - MAKE SURE THIS IS UNIQUE!
    // oDoor            the door you want them to open
    // bUnlock          whether or not they should unlock the door if necessary before opening it
void GestaltTagActionOpen(float fDelay, string sActor, object oDoor, int bUnlock = TRUE);

// Tells the actor to pick up an object from the ground
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // oActor           the character you want to pick up the item
    // oItem            the object to pick up
    // sItem            the tag of the object to pick up (the game will find the nearest item with that tag to the actor)
        // NOTE - this is included so that you can pick up an item created during the cutscene
        // NOTE - leave sItem at its default value of "" if you have already set oItem
void GestaltActionPickUp(float fDelay, object oActor, object oItem, string sItem = "");

// Tells the actor to pick up an object from the ground
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // sActor           the tag of the character you want to pick up the item - MAKE SURE THIS IS UNIQUE!
    // oItem            the object to pick up
    // sItem            the tag of the object to pick up (the game will find the nearest item with that tag to the actor)
        // NOTE - this is included so that you can pick up an item created during the cutscene
        // NOTE - leave sItem at its default value of "" if you have already set oItem
void GestaltTagActionPickUp(float fDelay, string sActor, object oItem, string sItem = "");

// Tells the actor to sit down on a specified chair or other object
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // oActor           the character you want to sit down
    // oChair           the object you want them to sit on
    // sChair           the tag of the object you want them to sit on (the game will find the nearest object with that tag to the actor)
        // NOTE - this is included so that you can sit on an object created during the cutscene
        // NOTE - leave sChair at its default value of "" if you have already set oChair
void GestaltActionSit(float fDelay, object oActor, object oChair, string sChair = "");

// Tells the actor to sit down on a specified chair or other object
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // sActor           the tag of the character you want to sit down - MAKE SURE THIS IS UNIQUE!
    // oChair           the object you want them to sit on
    // sChair           the tag of the object you want them to sit on (the game will find the nearest object with that tag to the actor)
        // NOTE - this is included so that you can sit on an object created during the cutscene
        // NOTE - leave sChair at its default value of "" if you have already set oChair
void GestaltTagActionSit(float fDelay, string sActor, object oChair, string sChair = "");

// This function allows you to activate and deactivate sound objects, as well as to adjust their position and volume
    // fDelay           how many seconds to wait before making the change
    // oSound           the sound object you want to adjust
    // bOn              set to TRUE to switch the sound object on, or FALSE to switch it off
    // fDuration        how long the sound object should stay on / off for
        // NOTE - leave fDuration at its default value of 0.0 to switch the sound object on / off permanently
    // iVolume          changes the volume of the sound (iVolume must be between 0 and 127)
        // NOTE - leave iVolume at its default value of 128 to leave the volume unchanged
    // oPosition        changes the sound to play from the position of the specified object
        // NOTE - leave oPosition at its default value of OBJECT_INVALID to leave the position unchanged
void GestaltSoundObject(float fDelay, object oSound, int bOn = TRUE, float fDuration = 0.0, int iVolume = 128, object oPosition = OBJECT_INVALID);

// This function allows you to activate and deactivate sound objects, as well as to adjust their position and volume
    // fDelay           how many seconds to wait before making the change
    // oArea            the area whose ambient sound you want to adjust
    // bOn              set to TRUE to switch the ambient sound on, or FALSE to switch it off
    // fDuration        how long the ambient sound should stay on / off for
        // NOTE - leave fDuration at its default value of 0.0 to switch the sound on / off permanently
    // iVolume          changes the volume of the area's ambient sound (iVolume must be between 0 and 100)
        // NOTE - leave iVolume at its default value of 128 to leave the volume unchanged
void GestaltAmbientSound(float fDelay, object oArea, int bOn = TRUE, float fDuration = 0.0, int iVolume = 128);

// This function allows you to play a specific piece of soundtrack music at any point in the cutscene
    // fDelay           how many seconds to wait before changing the music
    // oArea            the area whose music you want to change
    // bOn              set to TRUE to switch the area music on, or FALSE to switch it off
    // iTrack           the TRACK_* you want to play
        // NOTE - leave iTrack at its default value of TRACK_CURRENT to leave the area music unchanged
        // NOTE - set iTrack to TRACK_ORIGINAL if you want to switch all the music settings for the area back to their original values
    // fDuration        how long the music should stay on / off for and how long the new piece of music (if you changed the track) should remain active
        // NOTE - leave fDuration at its default value of 0.0 to make the changes permanent
void GestaltPlayMusic(float fDelay, object oArea, int bOn = TRUE, int iTrack = TRACK_CURRENT, float fDuration = 0.0);

// Tells the actor to play a sound file
    // fDelay           how many seconds to wait before playing the sound
    // oActor           the object you want to play the sound
    // sSound           the name of the sound you want to be played
    // sActor           the tag of the object you want to play the sound
        // NOTE - this is included so that you can play sounds on an object created during the cutscene
        // NOTE - leave sActor at its default value of "" if you have already set oActor
void GestaltPlaySound(float fDelay, object oActor, string sSound, string sActor = "");

// Tells the actor to play a sound file
    // fDelay           how many seconds to wait before adding the command to the actor's action queue
    // oActor           the object you want to play the sound
    // sSound           the name of the sound you want to be played
    // sActor           the tag of the object you want to play the sound
        // NOTE - this is included so that you can play sounds on an object created during the cutscene
        // NOTE - leave sActor at its default value of "" if you have already set oActor
void GestaltActionPlaySound(float fDelay, object oActor, string sSound, string sActor = "");

// Tells the actor to wait before proceeding with the actions in their queue
    // fDelay           how many seconds to wait before adding the pause to their action queue
    // oActor           the character you want to pause
    // fPause           how many seconds they should pause for
void GestaltActionWait(float fDelay, object oActor, float fPause);

// Tells the actor to wait before proceeding with the actions in their queue
    // fDelay           how many seconds to wait before adding the pause to their action queue
    // sActor           the tag of the character you want to pause - MAKE SURE THIS IS UNIQUE!
    // fPause           how many seconds they should pause for
void GestaltTagActionWait(float fDelay, string sActor, float fPause);

// Tells the selected actor to stop everything he's doing and prepare for new orders
    // fDelay           how many seconds to wait before applying this to oActor
    // oActor           the character whose action queue you want to clear
    // sActor           the tag of the character whose action queue you want to clear
        // NOTE - this is included so that you can clear the actions of a creature created during the cutscene
        // NOTE - leave sActor at its default value of "" if you have already set oActor
void GestaltClearActions(float fDelay, object oActor, string sActor="");

// Creates a line of text that appears above the selected character and rises up the screen, fading out as it goes - good for creating scrolling credits for a module!
    // fDelay           how many seconds to wait before displaying the text
    // oActor           the object above which the text should appear
    // sMessage         the text you want to appear
    // bFaction         whether or not the text will only appear to members in the object's faction
        // NOTE - if you set this to TRUE and oActor is an object or an NPC which isn't in the PC's party, nobody will see it
        // NOTE - if you set this to TRUE and oActor is a PC, only other players in their party will see it
        // NOTE - if you set this to FALSE, everyone on the server will see the message appear in their chat window
void GestaltFloatingText(float fDelay, object oActor, string sMessage, int bFaction = TRUE);

// Destroy the specified object. The function will SetIsDestroyable(TRUE) the object first to make sure it can be destroyed.
    // fDelay           how many seconds to wait before destroying the target
    // oTarget          the object you want to destroy
    // sTarget          the tag of the object you want to destroy
        // NOTE - this is included so that you can destroy an object created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltDestroy(float fDelay, object oTarget, string sTarget = "");

// Destroy the specified object. The function will SetIsDestroyable(TRUE) the object first to make sure it can be destroyed.
    // fDelay           how many seconds to wait before adding this command to the actor's action queue
    // oActor           the actor whose action queue you want this to be placed in
        // NOTE - this is not the object that will be destroyed!
    // oTarget          the object you want to destroy
    // sTarget          the tag of the object you want to destroy
        // NOTE - this is included so that you can destroy an object created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltActionDestroy(float fDelay, object oActor, object oTarget, string sTarget = "");

// Destroy the specified object. The function will SetIsDestroyable(TRUE) the object first to make sure it can be destroyed.
    // fDelay           how many seconds to wait before adding this command to the actor's action queue
    // sActor           the tag of the actor whose action queue you want this to be placed in - MAKE SURE THIS IS UNIQUE!
        // NOTE - this is not the object that will be destroyed!
    // oTarget          the object you want to destroy
    // sTarget          the tag of the object you want to destroy
        // NOTE - this is included so that you can destroy an object created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltTagActionDestroy(float fDelay, string sActor, object oTarget, string sTarget = "");

// Update the journals of the selected player(s), and (optionally) give them quest experience
    // fDelay           how many seconds to wait before applying the journal update
    // oPC              the PC who completed the quest
    // sQuest           the id tag of the quest you want to update
    // iState           the number of the quest entry you want to put in the journal
    // iXP              how many XP to give the player(s)
        // NOTE - leave this at 0 if you want to give no XP
        // NOTE - set this to 1 if you want to give the quest XP you specified in the journal editor
    // iParty           sets whether to update the journal for only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
    // bRewardAll       sets whether or not to give the XP reward to all the players you updated the journal for, or only for oPC
        // NOTE - if iXP or iParty is 0 you can ignore this option
    // bOverride        sets whether or not to allow the function to give a player a quest state lower than the one they already have in that quest
        // NOTE - this is TRUE by default!
void GestaltJournalEntry(float fDelay, object oPC, string sQuest, int iState, int iXP = 0, int iParty = 0, int bRewardAll = TRUE, int bOverride = FALSE);

// Execute another script
    // fDelay           how many seconds to wait before triggering the other script
    // oTarget          the object which the script will be triggered on
    // sScript          the name of the script
    // sTarget          the tag of the object which the script will be triggered on
        // NOTE - this is included so that you can run scripts on objects created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltExecuteScript(float fDelay, object oTarget, string sScript, string sTarget = "");

// Execute another script
    // fDelay           how many seconds to wait before adding this command to oActor's action queue
    // oActor           the actor whose action queue you want to place this command in (oActor doesn't have to be the same as oTarget)
    // oTarget          the object which the script will be triggered on
    // sScript          the name of the script
    // sTarget          the tag of the object which the script will be triggered on
        // NOTE - this is included so that you can run scripts on objects created during the cutscene
        // NOTE - leave sTarget at its default value of "" if you have already set oTarget
void GestaltActionExecute(float fDelay, object oActor, object oTarget, string sScript, string sTarget = "");

// Makes the selected character say how many seconds it is since the cutscene began when it reaches this action in its queue
// This can be a useful debug tool for checking the timing of your cutscene and specific actions within it
    // fDelay           how many seconds to wait before adding this command to oActor's action queue
    // oActor           the actor whose action queue you want to place the command in (and who will speak the message)
    // sMessage         the message you want them to speak
        // NOTE - the number of seconds since the cutscene began will automatically be added to the start of this message
void GestaltActionTimeStamp(float fDelay, object oActor, string sMessage);

// Stops all camera movements immediately
    // oPC              the player whose camera movements you want to stop
    // iParty           sets whether to stop the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
// DO NOT CHANGE THE FOLLOWING SETTINGS!
    // bAuto            sets whether the function should stop all camera movement (TRUE) or only ones with an id lower than iCamID (FALSE)
    // iCamID           the ID of the last camera move you want to stop (this is only needed if bAuto is set to FALSE)
void GestaltStopCameraMoves(object oPC, int iParty = 0, int bAuto = TRUE, int iCamID = 0);

// Gets the vector linking object A to object B
vector GetVectorAB(object oA, object oB);

// Finds the horizontal distance between two objects, ignoring any vertical component
float GetHorizontalDistanceBetween(object oA, object oB);

// Finds the compass direction from the PC to a target object
float GestaltGetDirection(object oTarget, object oPC);

// Acts just like the standard SetCameraFacing function
    // STARTING TIME -
        // fDelay           how many seconds to wait before starting the movement
    // STARTING CONDITIONS -
        // fDirection       the direction you want the camera to face in (0.0 = due east)
        // fRange           how far you want the camera to be from the PC
        // fPitch           how far from the vertical you want the camera to be tilted
    // MISC SETTINGS -
        // oPC              the PC whose camera you want to move
        // iTransition      the transition speed (defaults to CAMERA_TRANSITION_TYPE_SNAP)
void GestaltCameraFacing(float fDelay, float fDirection, float fRange, float fPitch, object oPC, int iTransition = CAMERA_TRANSITION_TYPE_SNAP);

// Moves the camera smoothly from one position to another over the specified time
    // STARTING TIME -
        // fDelay           how many seconds to wait before starting the movement
    // STARTING CONDITIONS -
        // fDirection       initial direction (0.0 = due east)
        // fRange           initial distance between player and camera
        // fPitch           initial pitch (vertical tilt)
    // FINAL CONDITIONS -
        // fDirection2      finishing direction
        // fRange2          finishing distance
        // fPitch2          finishing tilt
    // TIME SETTINGS -
        // fTime            number of seconds it takes camera to complete movement
        // fFrameRate       number of movements per second (governs how smooth the motion is)
    // MISC SETTINGS -
        // oPC              the PC you want to apply the camera movement to
        // iClockwise       set to 1 if you want the camera to rotate clockwise, 0 for anti-clockwise, or 2 for auto-select
        // iFace            sets whether the camera (0), the character (2) or both (1) turn to face the specified direction
        // iParty           sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltCameraMove(float fDelay, float fDirection, float fRange, float fPitch, float fDirection2, float fRange2, float fPitch2, float fTime, float fFrameRate, object oPC, int iClockwise = 0, int iFace = 0, int iParty = 0);

// Just like GestaltCameraMove, but with the added advantage of being able to move the point the camera is centered on up and down
    // STARTING TIME -
        // fDelay           how many seconds to wait before starting the movement
    // STARTING CONDITIONS -
        // fDirection       initial direction (0.0 = due east)
        // fRange           initial distance between player and camera
        // fPitch           initial pitch (vertical tilt)
        // fHeight          initial height above the PC where the camera should point
    // FINAL CONDITIONS -
        // fDirection2      finishing direction
        // fRange2          finishing distance
        // fPitch2          finishing tilt
        // fHeight2         finishing height
    // TIME SETTINGS -
        // fTime            number of seconds it takes camera to complete movement
        // fFrameRate       number of movements per second (governs how smooth the motion is)
    // MISC SETTINGS -
        // oPC              the PC you want to apply the camera movement to
        // iClockwise       set to 1 if you want the camera to rotate clockwise, 0 for anti-clockwise, or 2 for auto-select
        // iFace            sets whether the camera (0), the character (2) or both (1) turn to face the specified direction
        // iParty           sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltCameraCrane(float fDelay, float fDirection, float fRange, float fPitch, float fHeight, float fDirection2, float fRange2, float fPitch2, float fHeight2, float fTime, float fFrameRate, object oPC, int iClockwise = 0, int iFace = 0, int iParty = 0);

// Produces smooth transitions between different camera movements by setting initial and final speeds
// The function then interpolates between the two so that the movement rate changes smoothly over the
//  duration of the movement.
    // STARTING TIME -
        // fDelay           how many seconds to wait before starting the movement
    // MOVEMENT RATES AT START OF MOTION -
        // fdDirection1     how fast the camera's compass direction should change by in degrees per second
                            // positive numbers produce an anti-clockwise movement, negative anti-clockwise
        // fdRange1         how fast the camera's range should change in meters per second
                            // positive numbers move the camera away from the player, negative towards them
        // fdPitch1         how fast the camera's pitch should change in degrees per second
                            // positive numbers tilt the camera down towards the ground, negative up towards vertical
    // MOVEMENT RATES AT END OF MOTION -
        // fdDirection2     how fast the camera's compass direction should change by in degrees per second
                            // positive numbers produce an anti-clockwise movement, negative anti-clockwise
        // fdRange2         how fast the camera's range should change in meters per second
                            // positive numbers move the camera away from the player, negative towards them
        // fdPitch2         how fast the camera's pitch should change in degrees per second
                            // positive numbers tilt the camera down towards the ground, negative up towards vertical
    // TIME SETTINGS -
        // fTime            number of seconds it should take the camera to complete movement
        // fFrameRate       number of movements per second (governs how smooth the motion is)
    // MISC SETTINGS -
        // oPC              the player whose camera you want to move
        // iParty           sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
        // iSync            sets whether to use separate camera starting positions for every player (0) or sync them all to oPC's camera position (1)
void GestaltCameraSmooth(float fDelay, float fdDirection1, float fdRange1, float fdPitch1, float fdDirection2, float fdRange2, float fdPitch2, float fTime, float fFrameRate, object oPC, int iParty = 0, int iSync = 1);

// Just like GestaltCameraSmooth, but with the added advantage of being able to move the point the camera is centered on up and down
    // STARTING TIME -
        // fDelay           how many seconds to wait before starting the movement
    // MOVEMENT RATES AT START OF MOTION -
        // fdDirection1     how fast the camera's compass direction should change by in degrees per second
                            // positive numbers produce an anti-clockwise movement, negative anti-clockwise
        // fdRange1         how fast the camera's range should change in meters per second
                            // positive numbers move the camera away from the player, negative towards them
        // fdPitch1         how fast the camera's pitch should change in degrees per second
                            // positive numbers tilt the camera down towards the ground, negative up towards vertical
        // fdHeight1        how fast the camera's vertical height should change in meters per second
                            // positive numbers move the camera up, negative numbers move it down
    // MOVEMENT RATES AT END OF MOTION -
        // fdDirection2     how fast the camera's compass direction should change by in degrees per second
                            // positive numbers produce an anti-clockwise movement, negative anti-clockwise
        // fdRange2         how fast the camera's range should change in meters per second
                            // positive numbers move the camera away from the player, negative towards them
        // fdPitch2         how fast the camera's pitch should change in degrees per second
                            // positive numbers tilt the camera down towards the ground, negative up towards vertical
        // fdHeight2        how fast the camera's vertical height should change in meters per second
                            // positive numbers move the camera up, negative numbers move it down
    // TIME SETTINGS -
        // fTime            number of seconds it should take the camera to complete movement
        // fFrameRate       number of movements per second (governs how smooth the motion is)
    // MISC SETTINGS -
        // oPC              the player whose camera you want to move
        // iParty           sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
        // iSync            sets whether to use separate camera starting positions for every player (0) or sync them all to oPC's camera position (1)
void GestaltCameraCraneSmooth(float fDelay, float fdDirection1, float fdRange1, float fdPitch1, float fdHeight1, float fdDirection2, float fdRange2, float fdPitch2, float fdHeight2, float fTime, float fFrameRate, object oPC, int iParty = 0, int iSync = 1);

// Sets where the camera will start when you next use GestaltCameraSmooth and GestaltCameraCrane - it has no effect on other functions
// NOTE GestaltCameraSmooth, GestaltCameraCrane, GestaltCameraCraneSmooth and GestaltCameraMove automatically store the current position of the camera after each step -
//  GestaltCameraSetup should only be used at the start of a cutscene to set the initial position for your first GestaltCameraSmooth,
//  or during a gap between camera movements if you want to set a new starting position midway through a cutscene
    // STARTING TIME -
        // fDelay       how many seconds to wait before setting the starting position
    // PLAYER -
        // oPC          the player whose camera you're going to be moving
    // STARTING POSITION -
        // fDirection   the compass direction the camera should start from
        // fRange       the distance between the camera and the player it belongs to
        // fPitch       the vertical tilt
        // fHeight      how far above the character the camera should be centered (only needed for Crane shots)
void GestaltCameraSetup(float fDelay, object oPC, float fDirection, float fRange, float fPitch, float fHeight = 0.0);

// Turns the camera and/or player between two objects
// NOTE that this will only work properly if the player and target objects are stationary while the function is active
    // STARTING TIME -
        // fDelay           how many seconds to wait before starting the movement
    // STARTING CONDITIONS -
        // oStart           object to face at start of movement
        // fRange           initial distance between player and camera
        // fPitch           initial pitch (vertical tilt)
    // FINAL CONDITIONS -
        // oEnd             object to finish movement facing
        // fRange2          finishing distance
        // fPitch2          finishing tilt
    // TIME SETTINGS -
        // fTime            number of seconds it takes camera to complete movement
        // fFrameRate       number of movements per second (governs how smooth the motion is)
    // MISC SETTINGS -
        // oPC              the player whose camera you want to move
        // iClockwise       set to 1 if you want the camera to rotate clockwise, 0 for anti-clockwise, or 2 for auto-select
        // iFace            controls whether the camera (0), the character (2) or both (1) turn
        // iParty           sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltCameraFace(float fDelay, object oStart, float fRange, float fPitch, object oEnd, float fRange2, float fPitch2, float fTime, float fFrameRate, object oPC, int iClockwise = 0, int iFace = 0, int iParty = 0);

// Tracks a moving object, turning the player's camera so that it always faces towards it
    // STARTING TIME -
        // fDelay           how many seconds to wait before starting the movement
    // TARGET -
        // oTrack           object to track the movement of
    // STARTING CONDITIONS -
        // fRange           initial distance between player and camera
        // fPitch           initial pitch (vertical tilt)
    // FINAL CONDITIONS -
        // fRange2          finishing distance
        // fPitch2          finishing tilt
    // TIME SETTINGS -
        // fTime            how long the camera will track the object for
        // fFrameRate       number of movements per second (governs how smooth the motion is)
    // MISC SETTINGS -
        // oPC              the PC you want to apply the camera movement to
        // iFace            controls whether the camera (0), the character (2) or both (1) turn
        // iParty           sets whether to move the camera of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltCameraTrack(float fDelay, object oTrack, float fRange, float fPitch, float fRange2, float fPitch2, float fTime, float fFrameRate, object oPC, int iFace = 0, int iParty = 0);

// Fades the screen of the specified player(s) to and/or from black
    // fDelay           how many seconds to wait before fading the screen
    // oPC              the player you want to fade the screen of
    // iFade            sets what kind of fade you want -
        // if iFade is FADE_IN, the screen will start black and then become visible
        // if iFade is FADE_OUT, the screen will start visible and then become black
        // if iFade is FADE_CROSS, the screen will start visible, fade to black and then become visible again
    // fSpeed           the speed at which the fade(s) should take place
        // NOTE - always use the FADE_SPEED_* constants for this unless you really know what you're doing!
    // fDuration        how many seconds the fade should last
        // if iFade is FADE_IN, this is how long the screen will remain black before the fade begins
        // if iFade is FADE_OUT, this is the time between the fade out beginning and the screen being cleared again - leave at 0.0 to keep the screen black
        // if iFade is FADE_CROSS, this is the time between the fade out beginning and the fade in beginning
    // iParty           sets whether to fade the screen of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltCameraFade(float fDelay, object oPC, int iFade, float fSpeed = FADE_SPEED_MEDIUM, float fDuration = 0.0, int iParty = 0);

// Fades the screen of the specified player(s) to and/or from black
    // fDelay           how many seconds to wait before adding the command to oActor's action queue
    // oActor           the actor whose action queue you want to place this command in (oActor doesn't have to be the same as oPC)
    // oPC              the player you want to fade the screen of
    // iFade            sets what kind of fade you want -
        // if iFade is FADE_IN, the screen will start black and then become visible
        // if iFade is FADE_OUT, the screen will start visible and then become black
        // if iFade is FADE_CROSS, the screen will start visible, fade to black and then become visible again
    // fSpeed           the speed at which the fade(s) should take place
        // NOTE - always use the FADE_SPEED_* constants for this unless you really know what you're doing!
    // fDuration        how many seconds the fade should last
        // if iFade is FADE_IN, this is how long the screen will remain black before the fade begins
        // if iFade is FADE_OUT, this is the time between the fade out beginning and the screen being cleared again - leave at 0.0 to keep the screen black
        // if iFade is FADE_CROSS, this is the time between the fade out beginning and the fade in beginning
    // iParty           sets whether to fade the screen of only oPC (0), all the players in oPC's party (1) or all the players on the server (2)
void GestaltActionCameraFade(float fDelay, object oActor, object oPC, int iFade, float fSpeed = FADE_SPEED_MEDIUM, float fDuration = 0.0, int iParty = 0);

// Gives the illusion of the camera being fixed in one place and rotating to face the player as they move
    // oPC              the PC you want to apply the camera movement to
    // fFrameRate       number of movements per second (governs how smooth the motion is)
//
// To setup a fixed camera position, place a waypoint with a unique tag in your area
    // Set the camera's tag as a LocalString "sGestaltFixedCamera" on the PC to let them know to use that camera
    // Set a LocalFloat "fGestaltFixedCamera" on the PC to set the camera's vertical position
    // Set "sGestaltFixedCamera" to "" to pause the tracking, or to "STOP" to end the tracking
void GestaltFixedCamera(object oPC, float fFrameRate = 50.0);








// Subfunctions - these are called by other functions within the include file

void GestaltClearFX(object oActor)
{
    effect eFect = GetFirstEffect(oActor);
    int iType = GetEffectType(eFect);
    while (GetIsEffectValid(eFect))
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
            { RemoveEffect(oActor,eFect); }
        eFect = GetNextEffect(oActor);
        iType = GetEffectType(eFect);
        }
}



float GestaltMonkSpeed(object oActor)
{
    int iClass = GetLevelByClass(CLASS_TYPE_MONK,oActor);

    if (iClass >= 18)           { return 1.50; }
    if (iClass >= 15)           { return 1.45; }
    if (iClass >= 12)           { return 1.40; }
    if (iClass >= 9)            { return 1.30; }
    if (iClass >= 6)            { return 1.20; }
    if (iClass >= 3)            { return 1.10; }
    else                        { return 1.00; }
}



float GestaltGetSpeed(object oActor,int iRun)
{
    float fSpeed = 0.0;
    int iRate = GetMovementRate(oActor);

    switch(iRate)
        {
        case 0:               fSpeed = 2.00;      break;    // PCs
        case 1:               fSpeed = 0.00;      break;    // Immobile
        case 2:               fSpeed = 0.75;      break;    // Very Slow
        case 3:               fSpeed = 1.25;      break;    // Slow
        case 4:               fSpeed = 1.75;      break;    // Normal
        case 5:               fSpeed = 2.25;      break;    // Fast
        case 6:               fSpeed = 2.75;      break;    // Very Fast
        case 7:               fSpeed = 5.50;      break;    // DM Fast
        }

    if (iRun == TRUE)                                   { fSpeed = fSpeed * 2; }
//    if (GetHasFeat(FEAT_BARBARIAN_ENDURANCE,oActor))    { fSpeed = fSpeed * 1.1; }
//    if (GetHasFeat(FEAT_MONK_ENDURANCE,oActor))         { fSpeed = fSpeed * GestaltMonkSpeed(oActor); }

//    AssignCommand(oActor,SpeakString(FloatToString(fSpeed),TALKVOLUME_SHOUT));  // DEBUG LINE

    return fSpeed;
}



void GestaltResetSpeed(object oActor)
{
    effect eEffect = GetFirstEffect(oActor);
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
            { RemoveEffect(oActor,eEffect); }
        eEffect = GetNextEffect(oActor);
        iType = GetEffectType(eEffect);
        }
}



void SetSpawnCondition(object oActor, int nCondition, int bValid)
{
    int nPlot = GetLocalInt(oActor,"NW_GENERIC_MASTER");

    if(bValid == TRUE)
        {
        nPlot = nPlot | nCondition;
        SetLocalInt(oActor,"NW_GENERIC_MASTER",nPlot);
        }

    else if (bValid == FALSE)
        {
        nPlot = nPlot & ~nCondition;
        SetLocalInt(oActor,"NW_GENERIC_MASTER",nPlot);
        }
}



int GetSpawnCondition(object oActor, int nCondition)
{
    int nPlot = GetLocalInt(oActor,"NW_GENERIC_MASTER");
    if(nPlot & nCondition)
        { return TRUE; }
    return FALSE;
}



void GestaltRegisterActor(string sName, object oActor, string sActor = "")
{
    // Make sure the actor is a valid NPC
    if (GetObjectType(oActor) != OBJECT_TYPE_CREATURE)  { return; }
    if (GetIsPC(oActor))                                { return; }
    if (sActor != "")                                   { oActor = GetObjectByTag(sActor); }
    if (sActor == "")                                   { sActor = GetTag(oActor); }

    GestaltResetSpeed(oActor);
    SetCutsceneCameraMoveRate(oActor,1.0);

    if (GetLocalInt(GetModule(),sName + sActor + "registered"))
        { return; }

    if (GetSpawnCondition(oActor,NW_FLAG_AMBIENT_ANIMATIONS))
        {
        SetSpawnCondition(oActor,NW_FLAG_AMBIENT_ANIMATIONS,FALSE);
        SetLocalInt(oActor,"gcss_ambient",1);
        }

    if (GetSpawnCondition(oActor,NW_FLAG_IMMOBILE_AMBIENT_ANIMATIONS))
        {
        SetSpawnCondition(oActor,NW_FLAG_IMMOBILE_AMBIENT_ANIMATIONS,FALSE);
        SetLocalInt(oActor,"gcss_immobile",1);
        }

    if (GetSpawnCondition(oActor,NW_FLAG_AMBIENT_ANIMATIONS_AVIAN))
        {
        SetSpawnCondition(oActor,NW_FLAG_AMBIENT_ANIMATIONS_AVIAN,FALSE);
        SetLocalInt(oActor,"gcss_avian",1);
        }

    int iActors = GetLocalInt(GetModule(),sName + "actorsregistered") + 1;
    SetLocalObject(GetModule(),sName + "actor" + IntToString(iActors),oActor);
    SetLocalInt(GetModule(),sName + "actorsregistered",iActors);
    SetLocalInt(GetModule(),sName + sActor + "registered",TRUE);
}



void GestaltClearActorSpeeds(string sName)
{
    int iCount = 1;
    int iActors = GetLocalInt(GetModule(),sName + "actorsmodified");
    object oActor;

    DeleteLocalInt(GetModule(),sName + "actorsmodified");

    while (iCount <= iActors)
        {
        oActor = GetLocalObject(GetModule(),sName + "actorspeedmodified" + IntToString(iCount));
        DeleteLocalObject(GetModule(),sName + "actorspeedmodified" + IntToString(iCount));
        GestaltResetSpeed(oActor);
        iCount++;
        }
}



void GestaltClearActors(string sName, string sID)
{
    int iCount = 1;
    int iActors = GetLocalInt(GetModule(),sID + "actorsregistered");
    object oActor = GetLocalObject(GetModule(),sID + "actor" + IntToString(iCount));
    string sActor;
    object oWP;

    while (iCount <= iActors)
        {
        DeleteLocalObject(GetModule(),sID + "actor" + IntToString(iCount));

        // If the actor is valid, reset them
        if (GetIsObjectValid(oActor))
            {
            sActor = GetTag(oActor);
            DeleteLocalInt(GetModule(),sName + sActor + "registered");
            AssignCommand(oActor,ClearAllActions(TRUE));

            if (GetLocalInt(oActor,"gcss_ambient") == 1)
                {
                DeleteLocalInt(oActor,"gcss_ambient");
                SetSpawnCondition(oActor,NW_FLAG_AMBIENT_ANIMATIONS,TRUE);
                }

            if (GetLocalInt(oActor,"gcss_immobile") == 1)
                {
                DeleteLocalInt(oActor,"gcss_immobile");
                SetSpawnCondition(oActor,NW_FLAG_IMMOBILE_AMBIENT_ANIMATIONS,TRUE);
                }

            if (GetLocalInt(oActor,"gcss_avian") == 1)
                {
                DeleteLocalInt(oActor,"gcss_avian");
                SetSpawnCondition(oActor,NW_FLAG_AMBIENT_ANIMATIONS_AVIAN,TRUE);
                }

            oWP = GetWaypointByTag(sName + sActor);

            if (GetIsObjectValid(oWP))
                {
                DelayCommand(0.1,AssignCommand(oActor,ActionJumpToObject(oWP)));
                DelayCommand(0.1,AssignCommand(oActor,ActionDoCommand(SetFacing(GetFacing(oWP)))));
                }
            }

        // Find the next actor
        iCount++;
        oActor = GetLocalObject(GetModule(),sID + "actor" + IntToString(iCount));
        }

    DeleteLocalInt(GetModule(),sID + "actorsregistered");
}



// Cutscene setup / abort control functions

void GestaltStartCutscene(object oPC, string sName, int bCamera = TRUE, int bClear = TRUE, int bClearFX = TRUE, int bResetSpeed = TRUE, int bStoreCam = TRUE, int iParty = 0)
{
    object oParty;

    if (iParty == 1)            { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2)       { oParty = GetFirstPC(); }
    else                        { oParty = oPC; }

    int iCancel = GetLocalInt(GetModule(),sName) + 1;
    SetLocalInt(GetModule(),sName,iCancel);

    string sID = sName + "_" + IntToString(iCancel);
    SetLocalString(GetModule(),"cutscene",sID);

    while (GetIsObjectValid(oParty))
        {
        SetCutsceneMode(oParty,TRUE);
        SetLocalString(oParty,"cutscene",sName);
        SetLocalString(oParty,"cutsceneid",sID);

        if (bCamera)            { GestaltStopCameraMoves(oPC); }
        if (bClear)             { AssignCommand(oParty,ClearAllActions(TRUE)); }
        if (bClearFX)           { GestaltClearFX(oPC); }
        if (bResetSpeed)        { GestaltResetSpeed(oPC); }
        if (bStoreCam)          { AssignCommand(oParty,StoreCameraFacing()); }
        if (bStoreCam == 2)     { DelayCommand(5.0,AssignCommand(oParty,StoreCameraFacing())); }

        if (iParty == 1)        { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)   { oParty = GetNextPC(); }
        else                    { return; }
        }
}



void GestaltDoStopCutscene(string sName, string sID, object oPC, string sDestination = "", int bMode = TRUE, int bCamera = TRUE, int bClear = TRUE, int bClearFX = TRUE, int bResetSpeed = TRUE, int bClearActors = TRUE, int iParty = 0)
{
    // Check cutscene hasn't been stopped already
    if (GetLocalInt(GetModule(),sID))
        { return; }

    // Otherwise stop cutscene
    SetLocalInt(GetModule(),sID,TRUE);
    DeleteLocalString(GetModule(),"cutscene");

    object oParty;
    if (iParty == 1)            { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2)       { oParty = GetFirstPC(); }
    else                        { oParty = oPC; }

    if (bClearActors)           { GestaltClearActors(sName,sID); }

    GestaltClearActorSpeeds(sID);

    while (GetIsObjectValid(oParty))
        {
        // End cutscene mode and clear selected player
        if (bMode)              { SetCutsceneMode(oParty,FALSE); }
        if (bCamera)            { GestaltStopCameraMoves(oParty); }
        if (bClear)             { AssignCommand(oParty,ClearAllActions(TRUE)); }
        if (bClearFX)           { GestaltClearFX(oParty); }
        if (bResetSpeed)        { GestaltResetSpeed(oParty); }
        if (sDestination != "") { AssignCommand(oParty,JumpToObject(GetWaypointByTag(sDestination))); }

        SetCameraHeight(oParty,0.0);
        DeleteLocalString(oParty,"cutscene");
        DeleteLocalString(oParty,"cutsceneid");

        if (iParty == 1)        { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)   { oParty = GetNextPC(); }
        else                    { return; }
        }
}



void GestaltStopCutscene(float fDelay, object oPC, string sDestination = "", int bMode = TRUE, int bCamera = TRUE, int bClear = TRUE, int bClearFX = TRUE, int bResetSpeed = TRUE, int bClearActors = TRUE, int iParty = 0)
{
    string sName = GetLocalString(oPC,"cutscene");
    string sID = GetLocalString(oPC,"cutsceneid");
    DelayCommand(fDelay,GestaltDoStopCutscene(sName,sID,oPC,sDestination,bMode,bCamera,bClear,bClearFX,bResetSpeed,bClearActors,iParty));
}



void GestaltDoClearAssociate(object oAssociate, int iMethod, object oDestination)
{
    if (!GetIsObjectValid(oAssociate))
        { return; }

    AssignCommand(oAssociate,ClearAllActions(TRUE));

    if (iMethod == 0)
        {
        AssignCommand(oAssociate,JumpToObject(oDestination));
        }

    else if (iMethod == 1)
        {
        ApplyEffectToObject(PERMANENT,EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY),oAssociate);
        ApplyEffectToObject(PERMANENT,EffectCutsceneGhost(),oAssociate);
        }

    else if (iMethod == 2)
        {
        AssignCommand(oAssociate,SetIsDestroyable(TRUE));
        DestroyObject(oAssociate);
        }

    DelayCommand(0.1,ApplyEffectToObject(PERMANENT,EffectCutsceneParalyze(),oAssociate));
    DelayCommand(0.1,SetCommandable(FALSE,oAssociate));
}



void GestaltDoClearAssociates(string sName, object oPC, int iAssociates = 63, int iMethod = 0, object oDestination = OBJECT_INVALID, string sDestination = "")
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sDestination != "")
        { oDestination = GetWaypointByTag(sDestination); }

    object oAssociate;

    // Check how many henchman may be present
    int iHenchmen = GetMaxHenchmen();
    int i;

    if (iAssociates >= 32)
        {
        i = 1;
        while (i <= iHenchmen)
            {
            oAssociate = GetAssociate(1,oPC,i);

            if (GetIsObjectValid(oAssociate))
                {
                int iCount = 1;
                object oSecondary;

                while (iCount <= 5)
                    {
                    oSecondary = GetAssociate(iCount,oAssociate);
                    GestaltDoClearAssociate(oSecondary,iMethod,oDestination);
                    iCount++;
                    }
                }

            i++;
            }

        iAssociates -= 32;
        }

    if (iAssociates >= 16)
        {
        oAssociate = GetAssociate(5,oPC);
        GestaltDoClearAssociate(oAssociate,iMethod,oDestination);
        iAssociates -= 16;
        }

    if (iAssociates >= 8)
        {
        oAssociate = GetAssociate(4,oPC);
        GestaltDoClearAssociate(oAssociate,iMethod,oDestination);
        iAssociates -= 8;
        }

    if (iAssociates >= 4)
        {
        oAssociate = GetAssociate(3,oPC);
        GestaltDoClearAssociate(oAssociate,iMethod,oDestination);
        iAssociates -= 4;
        }

    if (iAssociates >= 2)
        {
        oAssociate = GetAssociate(2,oPC);
        GestaltDoClearAssociate(oAssociate,iMethod,oDestination);
        iAssociates -= 2;
        }

    if (iAssociates >= 1)
        {
        i = 1;
        while (i <= iHenchmen)
            {
            oAssociate = GetAssociate(1,oPC,i);
            GestaltDoClearAssociate(oAssociate,iMethod,oDestination);
            i++;
            }
        iAssociates -= 1;
        }
}



void GestaltClearAssociates(float fDelay, object oPC, int iAssociates = 63, int iMethod = 0, object oDestination = OBJECT_INVALID, string sDestination = "", int iParty = 0)
{
    object oParty;
    string sName = GetLocalString(GetModule(),"cutscene");

    if (iParty == 1)            { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2)       { oParty = GetFirstPC(); }
    else                        { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        DelayCommand(fDelay,GestaltDoClearAssociates(sName,oParty,iAssociates,iMethod,oDestination,sDestination));

        if (iParty == 1)        { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)   { oParty = GetNextPC(); }
        else                    { return; }
        }
}



void GestaltDoReturnAssociate(object oAssociate, object oDestination, int iMethod)
{
    if (!GetIsObjectValid(oAssociate))
        { return; }

    SetCommandable(TRUE,oAssociate);
    GestaltResetSpeed(oAssociate);
    if (iMethod == 1)           { GestaltClearFX(oAssociate); }
    DelayCommand(0.1,AssignCommand(oAssociate,JumpToObject(oDestination)));
}



void GestaltDoReturnAssociates(string sName, object oPC, int iAssociates, int iMethod, object oDestination, string sDestination)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sDestination != "")
        { oDestination = GetWaypointByTag(sDestination); }

    if (!GetIsObjectValid(oDestination))
        { oDestination = oPC; }

    object oAssociate;

    // Check how many henchman may be present
    int iHenchmen = GetMaxHenchmen();
    int i;

    if (iAssociates >= 32)
        {
        i = 1;
        while (i <= iHenchmen)
            {
            oAssociate = GetAssociate(1,oPC,i);

            if (GetIsObjectValid(oAssociate))
                {
                int iCount = 1;
                object oSecondary;
                while (iCount <= 5)
                    {
                    oSecondary = GetAssociate(iCount,oAssociate);
                    GestaltDoReturnAssociate(oSecondary,oAssociate,iMethod);
                    iCount++;
                    }
                }

            i++;
            }
        iAssociates -= 32;
        }

    if (iAssociates >= 16)
        {
        oAssociate = GetAssociate(5,oPC);
        GestaltDoReturnAssociate(oAssociate,oDestination,iMethod);
        iAssociates -= 16;
        }

    if (iAssociates >= 8)
        {
        oAssociate = GetAssociate(4,oPC);
        GestaltDoReturnAssociate(oAssociate,oDestination,iMethod);
        iAssociates -= 8;
        }

    if (iAssociates >= 4)
        {
        oAssociate = GetAssociate(3,oPC);
        GestaltDoReturnAssociate(oAssociate,oDestination,iMethod);
        iAssociates -= 4;
        }

    if (iAssociates >= 2)
        {
        oAssociate = GetAssociate(2,oPC);
        GestaltDoReturnAssociate(oAssociate,oDestination,iMethod);
        iAssociates -= 2;
        }

    if (iAssociates >= 1)
        {
        i = 1;
        while (i <= iHenchmen)
            {
            oAssociate = GetAssociate(1,oPC,i);
            GestaltDoReturnAssociate(oAssociate,oDestination,iMethod);
            i++;
            }
        iAssociates -= 1;
        }
}



void GestaltReturnAssociates(float fDelay, object oPC, int iAssociates = 63, int iMethod = 0, object oDestination = OBJECT_INVALID, string sDestination = "", int iParty = 0)
{
    object oParty;
    string sName = GetLocalString(GetModule(),"cutscene");

    if (iParty == 1)            { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2)       { oParty = GetFirstPC(); }
    else                        { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        DelayCommand(fDelay,GestaltDoReturnAssociates(sName,oParty,iAssociates,iMethod,oDestination,sDestination));

        if (iParty == 1)        { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)   { oParty = GetNextPC(); }
        else                    { return; }
        }
}






// Debug tools

void GestaltPrintTimeStamp(string sMessage, float fStartTime)
{
    float fCurrentTime = HoursToSeconds(GetTimeHour()) + IntToFloat((GetTimeMinute() * 60) + GetTimeSecond()) + (IntToFloat(GetTimeMillisecond()) / 1000);
    float fTime = fCurrentTime - fStartTime;
    SpeakString(FloatToString(fTime) + "s - " + sMessage);
}

void GestaltDoActionTimeStamp(string sName, object oActor, string sMessage, float fStartTime)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    AssignCommand(oActor,ActionDoCommand(GestaltPrintTimeStamp(sMessage,fStartTime)));
}

void GestaltActionTimeStamp(float fDelay, object oActor, string sMessage)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    float fStartTime = HoursToSeconds(GetTimeHour()) + IntToFloat((GetTimeMinute() * 60) + GetTimeSecond()) + (IntToFloat(GetTimeMillisecond()) / 1000);
    DelayCommand(fDelay,GestaltDoActionTimeStamp(sName,oActor,sMessage,fStartTime));
}

void GestaltDebugOutput(object oPC)
{
    // Get the current position of oPC's camera
    float fDirection = GetLocalFloat(oPC,"fCameraDirection");
    float fRange = GetLocalFloat(oPC,"fCameraRange");
    float fPitch = GetLocalFloat(oPC,"fCameraPitch");

    // Fire a message to say where the camera is
    AssignCommand(oPC,SpeakString(FloatToString(fDirection) + ", " + FloatToString(fRange) + ", " + FloatToString(fPitch)));
}






// Action functions

void GestaltDoSetSpeed(string sName, object oActor, string sActor, float fTime, float fDistance, int iRun, float fStops = 0.0)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (fTime == 0.0)
        {
        if (GetIsPC(oActor))                        { SetCutsceneCameraMoveRate(oActor,1.0); }
        else                                        { GestaltResetSpeed(oActor); }
        return;
        }

    float fActualSpeed = GestaltGetSpeed(oActor,iRun);
    float fTargetSpeed = fDistance / fTime;

    if (fActualSpeed == fTargetSpeed)               { return; }

    float fPercent = fTargetSpeed / fActualSpeed;

    if (fPercent < 0.1)                             { fPercent = 0.1; }
    if (fPercent > 2.0)                             { fPercent = 2.0; }

    if (GetIsPC(oActor))
        {
        SetCutsceneCameraMoveRate(oActor,fPercent);
        }
    else
        {
        GestaltResetSpeed(oActor);

        int iPercent;

        int iCount = GetLocalInt(GetModule(),sName + "actorsmodified") + 1;
        SetLocalInt(GetModule(),sName + "actorsmodified",iCount);
        SetLocalObject(GetModule(),sName + "actorspeedmodified" + IntToString(iCount),oActor);

        if (fActualSpeed < fTargetSpeed)
            {
            fPercent = 100 * ((fTargetSpeed - fActualSpeed) / fTargetSpeed);
            iPercent = FloatToInt(fPercent);
    //      AssignCommand(oActor,SpeakString("Speed increase " + IntToString(iPercent),TALKVOLUME_SHOUT));     // DEBUG LINE
            ApplyEffectToObject(DURATION_TYPE_PERMANENT,EffectMovementSpeedIncrease(iPercent),oActor);
            }

        else
            {
            fPercent = 100 * ((fActualSpeed - fTargetSpeed) / fActualSpeed);
            iPercent = FloatToInt(fPercent);
    //      AssignCommand(oActor,SpeakString("Speed decrease " + IntToString(iPercent),TALKVOLUME_SHOUT));     // DEBUG LINE
            ApplyEffectToObject(DURATION_TYPE_PERMANENT,EffectMovementSpeedDecrease(iPercent),oActor);
            }
        }
}



void GestaltTagSetSpeed(float fDelay, string sActor, float fTime, float fDistance, int iRun)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSetSpeed(sName,OBJECT_INVALID,sActor,fTime,fDistance,iRun));
}



void GestaltSetSpeed(float fDelay, object oActor, float fTime, float fDistance, int iRun)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSetSpeed(sName,oActor,"",fTime,fDistance,iRun));
}



void GestaltDoInvisibility(string sName, object oActor, string sActor, float fTime)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (fTime > 0.0)
        {
        ApplyEffectToObject(TEMPORARY,EffectCutsceneGhost(),oActor,fTime);
        ApplyEffectToObject(TEMPORARY,EffectEthereal(),oActor,fTime);
        ApplyEffectToObject(TEMPORARY,EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY),oActor,fTime);
        }

    else
        {
        ApplyEffectToObject(PERMANENT,EffectCutsceneGhost(),oActor);
        ApplyEffectToObject(PERMANENT,EffectEthereal(),oActor);
        ApplyEffectToObject(PERMANENT,EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY),oActor);
        }
}



void GestaltInvisibility(float fDelay, object oActor, float fTime = 0.0, string sActor = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoInvisibility(sName,oActor,sActor,fTime));
}



void GestaltDoMove(string sName, object oActor, string sActor, object oDestination, int iRun, float fRange, float fTime, string sDestination, int bTowards)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (sDestination != "")
        { oDestination = GetObjectByTag(sDestination); }

    if (fTime > 0.0)
        { AssignCommand(oActor,ActionDoCommand(GestaltDoSetSpeed(sName,oActor,"",fTime,GetDistanceBetween(oActor,oDestination),iRun))); }

    if (!bTowards)          { AssignCommand(oActor,ActionMoveAwayFromObject(oDestination,iRun,fRange)); }
    else if (fRange > 0.0)  { AssignCommand(oActor,ActionMoveToObject(oDestination,iRun,fRange)); }
    else                    { AssignCommand(oActor,ActionMoveToLocation(GetLocation(oDestination),iRun)); }
}



void GestaltTagActionMove(float fDelay, string sActor, object oDestination, int iRun = FALSE, float fRange = 0.0, float fTime = 0.0, string sDestination = "", int bTowards = TRUE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoMove(sName,OBJECT_INVALID,sActor,oDestination,iRun,fRange,fTime,sDestination,bTowards));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionMove(float fDelay, object oActor, object oDestination, int iRun = FALSE, float fRange = 0.0, float fTime = 0.0, string sDestination = "", int bTowards = TRUE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoMove(sName,oActor,"",oDestination,iRun,fRange,fTime,sDestination,bTowards));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoJump(string sName, object oActor, string sActor, object oTarget, string sTarget, int bAction = FALSE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (sTarget != "")
        { oTarget = GetObjectByTag(sTarget); }

    if (bAction)        { AssignCommand(oActor,ActionJumpToObject(oTarget,FALSE)); }
    else                { AssignCommand(oActor,JumpToObject(oTarget,FALSE)); }
}



void GestaltTagActionJump(float fDelay, string sActor, object oTarget, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoJump(sName,OBJECT_INVALID,sActor,oTarget,sTarget,TRUE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltTagJump(float fDelay, string sActor, object oTarget, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoJump(sName,OBJECT_INVALID,sActor,oTarget,sTarget));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionJump(float fDelay, object oActor, object oTarget, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoJump(sName,oActor,"",oTarget,sTarget,TRUE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltJump(float fDelay, object oActor, object oTarget, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoJump(sName,oActor,"",oTarget,sTarget));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoAnimate(string sName, object oActor, string sActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0, int bAction = FALSE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (bAction)        { AssignCommand(oActor,ActionPlayAnimation(iAnim,fSpeed,fDuration)); }
    else                { AssignCommand(oActor,PlayAnimation(iAnim,fSpeed,fDuration)); }
}



void GestaltTagActionAnimate(float fDelay, string sActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoAnimate(sName,OBJECT_INVALID,sActor,iAnim,fDuration,fSpeed,TRUE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltTagAnimate(float fDelay, string sActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoAnimate(sName,OBJECT_INVALID,sActor,iAnim,fDuration,fSpeed));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionAnimate(float fDelay, object oActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoAnimate(sName,oActor,"",iAnim,fDuration,fSpeed,TRUE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltAnimate(float fDelay, object oActor, int iAnim, float fDuration = 0.0, float fSpeed = 1.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoAnimate(sName,oActor,"",iAnim,fDuration,fSpeed));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoSpeak(string sName, object oActor, string sActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0, int bAction = FALSE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (bAction)
        {
        AssignCommand(oActor,ActionSpeakString(sLine));
        if (iAnimation != ANIMATION_NONE)
            { AssignCommand(oActor,ActionPlayAnimation(iAnimation,fSpeed,fDuration)); }
        }

    else
        {
        AssignCommand(oActor,SpeakString(sLine));
        if (iAnimation != ANIMATION_NONE)
            { AssignCommand(oActor,PlayAnimation(iAnimation,fSpeed,fDuration)); }
        }
}



void GestaltTagActionSpeak(float fDelay, string sActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSpeak(sName,OBJECT_INVALID,sActor,sLine,iAnimation,fDuration,fSpeed,TRUE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltTagSpeak(float fDelay, string sActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSpeak(sName,OBJECT_INVALID,sActor,sLine,iAnimation,fDuration,fSpeed));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionSpeak(float fDelay, object oActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSpeak(sName,oActor,"",sLine,iAnimation,fDuration,fSpeed,TRUE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltSpeak(float fDelay, object oActor, string sLine, int iAnimation = ANIMATION_NONE, float fDuration = 0.0, float fSpeed = 1.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSpeak(sName,oActor,"",sLine,iAnimation,fDuration,fSpeed));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoConversation(string sName, object oActor, string sActor, object oTarget, string sConv = "", string sTarget = "", int bGreet = TRUE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (sTarget != "")
        { oTarget = GetObjectByTag(sTarget); }

    AssignCommand(oActor,ActionStartConversation(oTarget,sConv,FALSE,bGreet));
}



void GestaltActionConversation(float fDelay, object oActor, object oTarget, string sConv = "", string sTarget = "", int bGreet = TRUE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoConversation(sName,oActor,"",oTarget,sConv,sTarget,bGreet));
    GestaltRegisterActor(sName,oActor);
}



void GestaltTagActionConversation(float fDelay, string sActor, object oTarget, string sConv = "", string sTarget = "", int bGreet = TRUE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoConversation(sName,OBJECT_INVALID,sActor,oTarget,sConv,sTarget,bGreet));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltDoFace(string sName, object oActor, string sActor, object oTarget, int iFace, float fFace, int bAction)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (iFace == 0)
        {
        if (bAction)        { AssignCommand(oActor,ActionDoCommand(SetFacing(fFace))); }
        else                { AssignCommand(oActor,SetFacing(fFace)); }
        }

    else if (iFace == 1)
        {
        if (bAction)        { AssignCommand(oActor,ActionDoCommand(SetFacing(GetFacing(oTarget)))); }
        else                { AssignCommand(oActor,SetFacing(GetFacing(oTarget))); }
        }

    else
        {
        if (bAction)        { AssignCommand(oActor,ActionDoCommand(SetFacingPoint(GetPosition(oTarget)))); }
        else                { AssignCommand(oActor,SetFacingPoint(GetPosition(oTarget))); }
        }
}



void GestaltTagActionFace(float fDelay, string sActor, float fFace, int iFace = 0, object oTarget = OBJECT_INVALID)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoFace(sName,OBJECT_INVALID,sActor,oTarget,iFace,fFace,TRUE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltTagFace(float fDelay, string sActor, float fFace, int iFace = 0, object oTarget = OBJECT_INVALID)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoFace(sName,OBJECT_INVALID,sActor,oTarget,iFace,fFace,FALSE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionFace(float fDelay, object oActor, float fFace, int iFace = 0, object oTarget = OBJECT_INVALID)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoFace(sName,oActor,"",oTarget,iFace,fFace,TRUE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltFace(float fDelay, object oActor, float fFace, int iFace = 0, object oTarget = OBJECT_INVALID)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoFace(sName,oActor,"",oTarget,iFace,fFace,FALSE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoEquip(string sName, object oActor, string sActor, int iSlot, object oItem, string sItem)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (sItem != "")
        { oItem = GetItemPossessedBy(oActor,sItem); }

    if (iSlot == 999)       { AssignCommand(oActor,ActionEquipMostDamagingMelee()); }
    else if (iSlot == 998)  { AssignCommand(oActor,ActionEquipMostDamagingRanged()); }
    else if (iSlot == 997)  { AssignCommand(oActor,ActionEquipMostEffectiveArmor()); }
    else                    { AssignCommand(oActor,ActionEquipItem(oItem,iSlot)); }
}



void GestaltTagActionEquip(float fDelay, string sActor, int iSlot = INVENTORY_SLOT_BEST_MELEE, object oItem = OBJECT_INVALID, string sItem = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoEquip(sName,OBJECT_INVALID,sActor,iSlot,oItem,sItem));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionEquip(float fDelay, object oActor, int iSlot = INVENTORY_SLOT_BEST_MELEE, object oItem = OBJECT_INVALID, string sItem = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoEquip(sName,oActor,"",iSlot,oItem,sItem));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoUnequip(string sName, object oActor, string sActor, int iSlot, object oItem, string sItem)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (sItem != "")
        { oItem = GetItemPossessedBy(oActor,sItem); }

    if (iSlot != 996)
        { oItem = GetItemInSlot(iSlot,oActor); }

    AssignCommand(oActor,ActionUnequipItem(oItem));
}



void GestaltTagActionUnequip(float fDelay, string sActor, int iSlot = INVENTORY_SLOT_NONE, object oItem = OBJECT_INVALID, string sItem = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoUnequip(sName,OBJECT_INVALID,sActor,iSlot,oItem,sItem));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionUnequip(float fDelay, object oActor, int iSlot = INVENTORY_SLOT_NONE, object oItem = OBJECT_INVALID, string sItem = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoUnequip(sName,oActor,"",iSlot,oItem,sItem));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoAttack(string sName, object oActor, string sActor, object oTarget, int bPassive, string sTarget)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (sTarget != "")
        { oTarget = GetObjectByTag(sTarget); }

    AssignCommand(oActor,ActionAttack(oTarget,bPassive));
}



void GestaltTagActionAttack(float fDelay, string sActor, object oTarget, string sTarget = "", int bPassive = FALSE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoAttack(sName,OBJECT_INVALID,sActor,oTarget,bPassive,sTarget));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionAttack(float fDelay, object oActor, object oTarget, string sTarget = "", int bPassive = FALSE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoAttack(sName,oActor,"",oTarget,bPassive,sTarget));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoSpellCast(string sName, object oActor, string sActor, object oTarget, int iSpell, int bFake = FALSE, int iPath = PROJECTILE_PATH_TYPE_DEFAULT, string sTarget = "", int bCheat = TRUE, int bInstant = FALSE, int iLevel = 0, int iMeta = METAMAGIC_NONE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (sTarget != "")
        { oTarget = GetObjectByTag(sTarget); }

    if (bFake)                  { AssignCommand(oActor,ActionCastFakeSpellAtObject(iSpell,oTarget,iPath)); }
    else                        { AssignCommand(oActor,ActionCastSpellAtObject(iSpell,oTarget,iMeta,bCheat,iLevel,iPath,bInstant)); }
}



void GestaltActionSpellCast(float fDelay, object oActor, object oTarget, int iSpell, int bFake = FALSE, int iPath = PROJECTILE_PATH_TYPE_DEFAULT, string sTarget = "", int bCheat = TRUE, int bInstant = FALSE, int iLevel = 0, int iMeta = METAMAGIC_NONE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSpellCast(sName,oActor,"",oTarget,iSpell,bFake,iPath,sTarget,bCheat,bInstant,iLevel,iMeta));
    GestaltRegisterActor(sName,oActor);
}



void GestaltTagActionSpellCast(float fDelay, string sActor, object oTarget, int iSpell, int bFake = FALSE, int iPath = PROJECTILE_PATH_TYPE_DEFAULT, string sTarget = "", int bCheat = TRUE, int bInstant = FALSE, int iLevel = 0, int iMeta = METAMAGIC_NONE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSpellCast(sName,OBJECT_INVALID,sActor,oTarget,iSpell,bFake,iPath,sTarget,bCheat,bInstant,iLevel,iMeta));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltDoEffect(string sName, object oActor, string sActor, object oTarget, string sTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0, int bAction = FALSE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")
        { oActor = GetObjectByTag(sActor); }

    if (sTarget != "")
        { oTarget = GetObjectByTag(sTarget); }

    if (bAction)                { AssignCommand(oActor,ActionDoCommand(ApplyEffectToObject(iDuration,eFect,oTarget,fDuration))); }
    else                        { ApplyEffectToObject(iDuration,eFect,oTarget,fDuration); }
}



void GestaltApplyEffect(float fDelay, object oTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoEffect(sName,OBJECT_INVALID,"",oTarget,sTarget,eFect,iDuration,fDuration));
}



void GestaltDoLocationEffect(string sName, location lTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    ApplyEffectAtLocation(iDuration,eFect,lTarget,fDuration);
}



void GestaltApplyLocationEffect(float fDelay, location lTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoLocationEffect(sName,lTarget,eFect,iDuration,fDuration));
}



void GestaltActionEffect(float fDelay, object oActor, object oTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoEffect(sName,oActor,"",oTarget,sTarget,eFect,iDuration,fDuration,TRUE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltTagActionEffect(float fDelay, string sActor, object oTarget, effect eFect, int iDuration = PERMANENT, float fDuration = 0.0, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoEffect(sName,OBJECT_INVALID,sActor,oTarget,sTarget,eFect,iDuration,fDuration,TRUE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltDoClearEffect(string sName, object oActor, int iFX = EFFECT_TYPE_CUTSCENE_EFFECTS)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (iFX == EFFECT_TYPE_CUTSCENE_EFFECTS)
        {
        GestaltClearFX(oActor);
        }
    else
        {
        effect eFect = GetFirstEffect(oActor);
        int iType = GetEffectType(eFect);
        while (GetIsEffectValid(eFect))
            {
            if (iType == iFX)
                { RemoveEffect(oActor,eFect); }
            eFect = GetNextEffect(oActor);
            iType = GetEffectType(eFect);
            }
        }
}



void GestaltClearEffect(float fDelay, object oActor, int iFX = EFFECT_TYPE_CUTSCENE_EFFECTS)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoClearEffect(sName,oActor));
}



void VoidCreateObject(int iType, string sRef, location lLoc, int iAnim, string sTag)
{
    CreateObject(iType,sRef,lLoc,iAnim,sTag);
}


void VoidCreateItemOnObject(string sRef, object oTarget, int iStack)
{
    CreateItemOnObject(sRef,oTarget,iStack);
}



void GestaltDoCreate(string sName, object oActor, string sActor, object oTarget, string sTarget, int iType, string sRef, string sTag, int iAnim, int iStack, int bCreateOn, int bAction = FALSE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")       { oActor = GetObjectByTag(sActor); }
    if (sTarget != "")      { oTarget = GetObjectByTag(sTarget); }

    if (bCreateOn)
        {
        if (bAction)        { AssignCommand(oActor,ActionDoCommand(VoidCreateItemOnObject(sRef,oTarget,iStack))); }
        else                { CreateItemOnObject(sRef,oTarget,iStack); }
        }

    else
        {
        if (bAction)        { AssignCommand(oActor,ActionDoCommand(VoidCreateObject(iType,sRef,GetLocation(oTarget),iAnim,sTag))); }
        else                { CreateObject(iType,sRef,GetLocation(oTarget),iAnim,sTag); }
        }
}



void GestaltActionCreate(float fDelay, object oActor, object oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, int bCreateOn = FALSE, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoCreate(sName,oActor,"",oTarget,sTarget,iType,sRef,sTag,iAnim,iStack,bCreateOn,TRUE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltTagActionCreate(float fDelay, string sActor, object oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, int bCreateOn = FALSE, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoCreate(sName,OBJECT_INVALID,sActor,oTarget,sTarget,iType,sRef,sTag,iAnim,iStack,bCreateOn,TRUE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltCreate(float fDelay, object oTarget, int iType, string sRef, string sTag = "", int iAnim = FALSE, int iStack = 0, int bCreateOn = FALSE, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoCreate(sName,OBJECT_INVALID,"",oTarget,sTarget,iType,sRef,sTag,iAnim,iStack,bCreateOn));
}



void GestaltDoCopy(string sName, object oSource, object oTarget, string sTarget, string sTag, int bCreateOn)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sTarget != "")  { oTarget = GetObjectByTag(sTarget); }

    if (bCreateOn)      { CopyObject(oSource,GetLocation(oTarget),oTarget,sTag); }
    else                { CopyObject(oSource,GetLocation(oTarget),OBJECT_INVALID,sTag); }
}



void GestaltCopy(float fDelay, object oSource, object oTarget, int bCreateOn = FALSE, string sTag = "", string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoCopy(sName,oSource,oTarget,sTarget,sTag,bCreateOn));
}



void GestaltDoClone(string sName, object oPC, object oTarget, string sTarget, string sTag, int bInvisible)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sTarget != "")  { oTarget = GetObjectByTag(sTarget); }
    object oClone = CopyObject(oPC,GetLocation(oTarget),OBJECT_INVALID,sTag);

    if (GetIsPC(oPC))
        {
        ChangeToStandardFaction(oClone, STANDARD_FACTION_COMMONER);
        }

    if (bInvisible)     { ApplyEffectToObject(PERMANENT,EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY),oPC); }
}



void GestaltClonePC(float fDelay, object oPC, object oTarget, string sTag = "cloned_pc", string sTarget = "", int bInvisible = TRUE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoClone(sName,oPC,oTarget,sTarget,sTag,bInvisible));
}



void GestaltDoPickUp(string sName, object oActor, string sActor, object oItem, string sItem)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")   { oActor = GetObjectByTag(sActor); }
    if (sItem != "")    { oItem = GetNearestObjectByTag(sItem,oActor); }

    AssignCommand(oActor,ActionPickUpItem(oItem));
}



void GestaltTagActionPickUp(float fDelay, string sActor, object oItem, string sItem = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoPickUp(sName,OBJECT_INVALID,sActor,oItem,sItem));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionPickUp(float fDelay, object oActor, object oItem, string sItem = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoPickUp(sName,oActor,"",oItem,sItem));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoSit(string sName, object oActor, string sActor, object oChair, string sChair)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")   { oActor = GetObjectByTag(sActor); }
    if (sChair != "")   { oChair = GetNearestObjectByTag(sChair,oActor); }

    AssignCommand(oActor,ActionSit(oChair));
}



void GestaltTagActionSit(float fDelay, string sActor, object oChair, string sChair = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSit(sName,OBJECT_INVALID,sActor,oChair,sChair));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionSit(float fDelay, object oActor, object oChair, string sChair = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSit(sName,oActor,"",oChair,sChair));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoPlaySound(string sName, object oActor, string sActor, string sSound, int bAction)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")   { oActor = GetObjectByTag(sActor); }
    if (bAction)        { AssignCommand(oActor,ActionDoCommand(PlaySound(sSound))); }
    else                { AssignCommand(oActor,PlaySound(sSound)); }
}



void GestaltPlaySound(float fDelay, object oActor, string sSound, string sActor = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoPlaySound(sName,oActor,sActor,sSound,FALSE));

    if (sActor != "")           { GestaltRegisterActor(sName,OBJECT_INVALID,sActor); }
    else                        { GestaltRegisterActor(sName,oActor); }
}



void GestaltActionPlaySound(float fDelay, object oActor, string sSound, string sActor = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoPlaySound(sName,oActor,sActor,sSound,TRUE));

    if (sActor != "")           { GestaltRegisterActor(sName,OBJECT_INVALID,sActor); }
    else                        { GestaltRegisterActor(sName,oActor); }
}



void GestaltDoSoundObject(string sName, object oSound, int bOn, float fDuration, int iVolume, object oPosition)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (bOn)                            { SoundObjectPlay(oSound); }
    else                                { SoundObjectStop(oSound); }

    if (fDuration > 0.0)                { DelayCommand(fDuration,GestaltDoSoundObject(sName,oSound,!bOn,0.0,128,OBJECT_INVALID)); }
    if (iVolume < 128)                  { SoundObjectSetVolume(oSound,iVolume); }
    if (GetIsObjectValid(oPosition))    { SoundObjectSetPosition(oSound,GetPosition(oPosition)); }
}



void GestaltSoundObject(float fDelay, object oSound, int bOn = TRUE, float fDuration = 0.0, int iVolume = 128, object oPosition = OBJECT_INVALID)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoSoundObject(sName,oSound,bOn,fDuration,iVolume,oPosition));
}



void GestaltDoAmbientSound(string sName, object oArea, int bOn, float fDuration, int iVolume)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (bOn)                            { AmbientSoundPlay(oArea); }
    else                                { AmbientSoundStop(oArea); }

    if (fDuration > 0.0)                { DelayCommand(fDuration,GestaltDoAmbientSound(sName,oArea,!bOn,0.0,128)); }

    if (iVolume < 101)
        {
        AmbientSoundSetDayVolume(oArea,iVolume);
        AmbientSoundSetNightVolume(oArea,iVolume);
        }
}



void GestaltAmbientSound(float fDelay, object oArea, int bOn = TRUE, float fDuration = 0.0, int iVolume = 128)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoAmbientSound(sName,oArea,bOn,fDuration,iVolume));
}



void GestaltDoMusic(string sName, object oArea, int bOn = TRUE, int iTrack = TRACK_CURRENT, float fDuration = 0.0)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (fDuration > 0.0)                { DelayCommand(fDuration,GestaltDoMusic(sName,oArea,!bOn,iTrack)); }

    if (GetLocalInt(oArea,"gcss_music_day") == 0)
        {
        SetLocalInt(oArea,"gcss_music_day",MusicBackgroundGetDayTrack(oArea));
        SetLocalInt(oArea,"gcss_music_night",MusicBackgroundGetNightTrack(oArea));
        }

    MusicBackgroundStop(oArea);
    MusicBattleStop(oArea);

    if (!bOn)
        { return; }

    if (iTrack == TRACK_CURRENT)
        { MusicBackgroundPlay(oArea); }

    else if (iTrack == TRACK_ORIGINAL)
        {
        MusicBackgroundChangeDay(oArea,GetLocalInt(oArea,"gcss_music_day"));
        MusicBackgroundChangeNight(oArea,GetLocalInt(oArea,"gcss_music_night"));
        }

    else
        {
        MusicBackgroundChangeDay(oArea,iTrack);
        MusicBackgroundChangeNight(oArea,iTrack);
        }
}



void GestaltPlayMusic(float fDelay, object oArea, int bOn = TRUE, int iTrack = TRACK_CURRENT, float fDuration = 0.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoMusic(sName,oArea,bOn,iTrack,fDuration));
}



void GestaltDoDoor(string sName, object oActor, string sActor, object oDoor, int bLock, int bOpen)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")   { oActor = GetObjectByTag(sActor); }

    if (bOpen)
        {
        if (bLock)      { AssignCommand(oActor,ActionDoCommand(SetLocked(oDoor,FALSE))); }
        AssignCommand(oActor,ActionOpenDoor(oDoor));
        }

    else
        {
        AssignCommand(oActor,ActionCloseDoor(oDoor));
        if (bLock)      { AssignCommand(oActor,ActionDoCommand(SetLocked(oDoor,TRUE))); }
        }
}



void GestaltActionClose(float fDelay, object oActor, object oDoor, int bLock = FALSE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoDoor(sName,oActor,"",oDoor,bLock,FALSE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltActionOpen(float fDelay, object oActor, object oDoor, int bUnlock = TRUE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoDoor(sName,oActor,"",oDoor,bUnlock,TRUE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltTagActionClose(float fDelay, string sActor, object oDoor, int bLock = FALSE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoDoor(sName,OBJECT_INVALID,sActor,oDoor,bLock,FALSE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltTagActionOpen(float fDelay, string sActor, object oDoor, int bUnlock = TRUE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoDoor(sName,OBJECT_INVALID,sActor,oDoor,bUnlock,TRUE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltDoQuest(string sName, object oPC, string sQuest, int iState, int iXP = 0, int iParty = 0, int bRewardAll = TRUE, int bOverride = FALSE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (iParty == 1)        { AddJournalQuestEntry(sQuest,iState,oPC,TRUE,FALSE,bOverride); }
    else if (iParty == 2)   { AddJournalQuestEntry(sQuest,iState,oPC,FALSE,TRUE,bOverride); }
    else                    { AddJournalQuestEntry(sQuest,iState,oPC,FALSE,FALSE,bOverride); }

    if (iXP == 0)           { return; }
    else if (iXP == 1)      { iXP = GetJournalQuestExperience(sQuest); }

    object oParty;

    if (bRewardAll)         { iParty = 0; }

    if (iParty == 1)        { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2)   { oParty = GetFirstPC(); }
    else                    { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        GiveXPToCreature(oParty,iXP);

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltJournalEntry(float fDelay, object oPC, string sQuest, int iState, int iXP = 0, int iParty = 0, int bRewardAll = TRUE, int bOverride = FALSE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoQuest(sName,oPC,sQuest,iState,iXP,iParty,bRewardAll,bOverride));
}



void GestaltDoWait(string sName, object oActor, string sActor, float fPause)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")   { oActor = GetObjectByTag(sActor); }

    AssignCommand(oActor,ActionWait(fPause));
}



void GestaltTagActionWait(float fDelay, string sActor, float fPause)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoWait(sName,OBJECT_INVALID,sActor,fPause));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltActionWait(float fDelay, object oActor, float fPause)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoWait(sName,oActor,"",fPause));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoClear(string sName, object oActor, string sActor)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sActor != "")   { oActor = GetObjectByTag(sActor); }

    AssignCommand(oActor,ClearAllActions(TRUE));
}



void GestaltClearActions(float fDelay, object oActor, string sActor="")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoClear(sName,oActor,sActor));
}



void GestaltDoFloatingText(string sName, object oActor, string sMessage, int bFaction = TRUE)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    FloatingTextStringOnCreature(sMessage,oActor,bFaction);
}



void GestaltFloatingText(float fDelay, object oActor, string sMessage, int bFaction = TRUE)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoFloatingText(sName,oActor,sMessage,bFaction));
}



void GestaltDoExecute(string sName, object oTarget, string sScript, string sTarget)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sTarget != "")  { oTarget = GetObjectByTag(sTarget); }

    ExecuteScript(sScript,oTarget);
}



void GestaltExecuteScript(float fDelay, object oTarget, string sScript, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoExecute(sName,oTarget,sScript,sTarget));
}



void GestaltActionExecute(float fDelay, object oActor, object oTarget, string sScript, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,AssignCommand(oActor,ActionDoCommand(GestaltDoExecute(sName,oTarget,sScript,sTarget))));
    GestaltRegisterActor(sName,oActor);
}



void GestaltDoDestroy(string sName, object oActor, string sActor, object oTarget, string sTarget, int bAction)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (sTarget != "")  { oTarget = GetObjectByTag(sTarget); }

    if (bAction)
        {
        AssignCommand(oActor,ActionDoCommand(AssignCommand(oTarget,SetIsDestroyable(TRUE))));
        AssignCommand(oActor,ActionDoCommand(DestroyObject(oTarget)));
        }

    else
        {
        AssignCommand(oTarget,SetIsDestroyable(TRUE));
        DestroyObject(oTarget);
        }
}



void GestaltActionDestroy(float fDelay, object oActor, object oTarget, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoDestroy(sName,oActor,"",oTarget,sTarget,TRUE));
    GestaltRegisterActor(sName,oActor);
}



void GestaltTagActionDestroy(float fDelay, string sActor, object oTarget, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoDestroy(sName,OBJECT_INVALID,sActor,oTarget,sTarget,TRUE));
    GestaltRegisterActor(sName,OBJECT_INVALID,sActor);
}



void GestaltDestroy(float fDelay, object oTarget, string sTarget = "")
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoDestroy(sName,OBJECT_INVALID,"",oTarget,sTarget,FALSE));
}










// Camera functions

void GestaltStopCameraMoves(object oPC, int iParty = 0, int bAuto = TRUE, int iCamID = 0)
{
    object oParty;
    string sCam;
    int iCount;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        if (bAuto)
            { iCamID = GetLocalInt(oParty,"iCamCount"); }

        iCount = iCamID;

        while (iCount > 0)
            {
            // Find the camera movement
            sCam = "iCamStop" + IntToString(iCount);
            SetLocalInt(oParty,sCam,1);
            iCount--;

            // Uncomment the line below to get a message in the game confirming each id which is cancelled
            // AssignCommand(oParty,SpeakString("Camera movement id " + IntToString(iCount) + "has been stopped"));
            }

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



vector GetVectorAB(object oA, object oB)
{
    vector vA = GetPosition(oA);
    vector vB = GetPosition(oB);
    vector vDelta = (vA - vB);
    return vDelta;
}



float GetHorizontalDistanceBetween(object oA, object oB)
{
    vector vHorizontal = GetVectorAB(oA,oB);
    float fDistance = sqrt(pow(vHorizontal.x,2.0) + pow(vHorizontal.y,2.0));
    return fDistance;
}



float GestaltGetDirection(object oTarget, object oPC)
{
    vector vdTarget = GetVectorAB(oTarget,oPC);
    float fDirection = VectorToAngle(vdTarget);
    return fDirection;
}



void GestaltDoCameraMode(string sName, object oPC, int iMode)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    SetCameraMode(oPC,iMode);
}



void GestaltCameraMode(float fDelay, object oPC, int iMode)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoCameraMode(sName,oPC,iMode));
}



void GestaltDoCameraFacing(string sName, float fDirection, float fRange, float fPitch, object oPC, int iTransition)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    AssignCommand(oPC,SetCameraFacing(fDirection,fRange,fPitch,iTransition));
}



void GestaltCameraFacing(float fDelay, float fDirection, float fRange, float fPitch, object oPC, int iTransition = CAMERA_TRANSITION_TYPE_SNAP)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoCameraFacing(sName,fDirection,fRange,fPitch,oPC,iTransition));
}



void GestaltCameraPoint(float fDirection, float fRange, float fPitch, float fdDirection, float fdRange, float fdPitch, float fd2Direction, float fd2Range, float fd2Pitch, float fCount, object oPC, int iCamID, int iFace = 0)
{
    // Check whether this camera movement has been stopped or ended
    string sCam = "iCamStop" + IntToString(iCamID);
    if (GetLocalInt(oPC,sCam) == 1)
        { return; }

    // Work out where to point the camera
    fDirection = fDirection + ((fd2Direction * pow(fCount,2.0)) / 2) + (fdDirection * fCount);
    fRange = fRange + ((fd2Range * pow(fCount,2.0)) / 2) + (fdRange * fCount);
    fPitch = fPitch + ((fd2Pitch * pow(fCount,2.0)) / 2) + (fdPitch * fCount);

    // Reset fDirectionNew if it's gone past 0 or 360 degrees
    while (fDirection < 0.0)    { fDirection = (fDirection + 360.0); }
    while (fDirection > 360.0)  { fDirection = (fDirection - 360.0); }

    // Set the camera and/or player facing, according to iFace
    if (iFace < 2)        { AssignCommand(oPC,SetCameraFacing(fDirection,fRange,fPitch)); }
    if (iFace > 0)        { AssignCommand(oPC,SetFacing(fDirection)); }

    // Store the current position of the camera
    SetLocalFloat(oPC,"fCameraDirection",fDirection);
    SetLocalFloat(oPC,"fCameraRange",fRange);
    SetLocalFloat(oPC,"fCameraPitch",fPitch);
}



void GestaltCameraPosition(float fDirection, float fRange, float fPitch, float fHeight, float fdDirection, float fdRange, float fdPitch, float fdHeight, float fd2Direction, float fd2Range, float fd2Pitch, float fd2Height, float fCount, object oPC, int iCamID, int iFace = 0)
{
    // Check whether this camera movement has been stopped or ended
    string sCam = "iCamStop" + IntToString(iCamID);
    if (GetLocalInt(oPC,sCam) == 1)
        { return; }

    // Work out where to point the camera
    fDirection = fDirection + ((fd2Direction * pow(fCount,2.0)) / 2) + (fdDirection * fCount);
    fRange = fRange + ((fd2Range * pow(fCount,2.0)) / 2) + (fdRange * fCount);
    fPitch = fPitch + ((fd2Pitch * pow(fCount,2.0)) / 2) + (fdPitch * fCount);
    fHeight = fHeight + ((fd2Height * pow(fCount,2.0)) / 2) + (fdHeight * fCount);

    // Reset fDirectionNew if it's gone past 0 or 360 degrees
    while (fDirection < 0.0)    { fDirection = (fDirection + 360.0); }
    while (fDirection > 360.0)  { fDirection = (fDirection - 360.0); }

    // Set the camera and/or player facing, according to iFace
    if (iFace < 2)        { AssignCommand(oPC,SetCameraFacing(fDirection,fRange,fPitch)); }
    if (iFace > 0)        { AssignCommand(oPC,SetFacing(fDirection)); }

    // Adjust camera height
    SetCameraHeight(oPC,fHeight);

    // Store the current position of the camera
    SetLocalFloat(oPC,"fCameraDirection",fDirection);
    SetLocalFloat(oPC,"fCameraRange",fRange);
    SetLocalFloat(oPC,"fCameraPitch",fPitch);
    SetLocalFloat(oPC,"fCameraHeight",fHeight);
}



void GestaltCameraFaceTarget(object oTarget, float fRange, float fPitch, object oPC, int iFace, int iParty = 0, int iCamID = 0)
{
    // Check whether this camera movement has been stopped
    string sCam = "iCamStop" + IntToString(iCamID);
    if (iCamID > 0 && GetLocalInt(oPC,sCam) == 1)
        { return; }

    float fDirection;
    object oParty;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        fDirection = GestaltGetDirection(oTarget,oParty);

        if (iFace < 2)        { AssignCommand(oParty,SetCameraFacing(fDirection,fRange,fPitch)); }
        if (iFace > 0)        { AssignCommand(oParty,SetFacing(fDirection)); }

        // Store the current position of the camera
        SetLocalFloat(oParty,"fCameraDirection",fDirection);
        SetLocalFloat(oParty,"fCameraRange",fRange);
        SetLocalFloat(oParty,"fCameraPitch",fPitch);

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



float GestaltGetPanRate(float fDirection, float fDirection2, float fTicks, int iClockwise)
{
    // Calculates how far the camera needs to move each to tick to go from fDirection to fDirection2
    // in fTicks steps, correcting as necessary to account for clockwise or anti-clockwise movement

    float fdDirection;

    if (iClockwise == 0)
        {
        if (fDirection > fDirection2)               { fdDirection = ((fDirection2 + 360.0 - fDirection) / fTicks); }
        else                                        { fdDirection = ((fDirection2 - fDirection) / fTicks); }
        }

    if (iClockwise == 1)
        {
        if (fDirection2 > fDirection)               { fdDirection = ((fDirection2 - fDirection - 360.0) / fTicks); }
        else                                        { fdDirection = ((fDirection2 - fDirection) / fTicks); }
        }

    if (iClockwise == 2)
        {
        float fCheck = fDirection2 - fDirection;
        if (fCheck > 180.0)                         { fdDirection = ((fDirection2 - fDirection - 360.0) / fTicks); }
        else if (fCheck < -180.0)                   { fdDirection = ((fDirection2 + 360.0 - fDirection) / fTicks); }
        else                                        { fdDirection = ((fDirection2 - fDirection) / fTicks); }
        }

    return fdDirection;
}



void GestaltCameraMove(float fDelay, float fDirection, float fRange, float fPitch, float fDirection2, float fRange2, float fPitch2, float fTime, float fFrameRate, object oPC, int iClockwise = 0, int iFace = 0, int iParty = 0)
{
    // Get timing information
    float fTicks = (fTime * fFrameRate);
    float fdTime = (fTime / fTicks);
    float fStart = fDelay;
    float fCount;

    float fdDirection = GestaltGetPanRate(fDirection,fDirection2,fTicks,iClockwise);
    float fdRange = ((fRange2 - fRange) / fTicks);
    float fdPitch = ((fPitch2 - fPitch) / fTicks);

    int iCamID;
    object oParty;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        // Set the camera to top down mode
        GestaltCameraMode(fDelay,oParty,CAMERA_MODE_TOP_DOWN);

        // Give the camera movement a unique id code so that it can be stopped
        iCamID = GetLocalInt(oParty,"iCamCount") + 1;
        SetLocalInt(oParty,"iCamCount",iCamID);

        // reset variables
        fCount = 0.0;
        fDelay = fStart;

        // Uncomment the line below to get a message in the game telling you the id of this camera movement
        // AssignCommand(oParty,SpeakString("Camera id - " + IntToString(iCamID)));

        // After delay, stop any older camera movements and start this one
        DelayCommand(fStart,GestaltStopCameraMoves(oParty,0,FALSE,iCamID - 1));

        while (fCount <= fTicks)
            {
            DelayCommand(fDelay,GestaltCameraPoint(fDirection,fRange,fPitch,fdDirection,fdRange,fdPitch,0.0,0.0,0.0,fCount,oParty,iCamID,iFace));
            fCount = (fCount + 1.0);
            fDelay = fStart + (fCount * fdTime);
            }

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltCameraCrane(float fDelay, float fDirection, float fRange, float fPitch, float fHeight, float fDirection2, float fRange2, float fPitch2, float fHeight2, float fTime, float fFrameRate, object oPC, int iClockwise = 0, int iFace = 0, int iParty = 0)
{
    // Get timing information
    float fTicks = (fTime * fFrameRate);
    float fdTime = (fTime / fTicks);
    float fStart = fDelay;
    float fCount;

    float fdDirection = GestaltGetPanRate(fDirection,fDirection2,fTicks,iClockwise);
    float fdRange = ((fRange2 - fRange) / fTicks);
    float fdPitch = ((fPitch2 - fPitch) / fTicks);
    float fdHeight = ((fHeight2 - fHeight) / fTicks);

    int iCamID;
    object oParty;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        // Set the camera to top down mode
        GestaltCameraMode(fDelay,oParty,CAMERA_MODE_TOP_DOWN);

        // Give the camera movement a unique id code so that it can be stopped
        iCamID = GetLocalInt(oParty,"iCamCount") + 1;
        SetLocalInt(oParty,"iCamCount",iCamID);

        // reset variables
        fCount = 0.0;
        fDelay = fStart;

        // Uncomment the line below to get a message in the game telling you the id of this camera movement
        // AssignCommand(oParty,SpeakString("Camera id - " + IntToString(iCamID)));

        // After delay, stop any older camera movements and start this one
        DelayCommand(fStart,GestaltStopCameraMoves(oParty,0,FALSE,iCamID - 1));

        while (fCount <= fTicks)
            {
            DelayCommand(fDelay,GestaltCameraPosition(fDirection,fRange,fPitch,fHeight,fdDirection,fdRange,fdPitch,fdHeight,0.0,0.0,0.0,0.0,fCount,oParty,iCamID,iFace));
            fCount = (fCount + 1.0);
            fDelay = fStart + (fCount * fdTime);
            }

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltCameraSmoothStart(float fdDirection1, float fdRange1, float fdPitch1, float fdDirection2, float fdRange2, float fdPitch2, float fTime, float fFrameRate, object oParty, object oSync, int iCamID)
{
    // Get starting position for camera
    float fDirection = GetLocalFloat(oSync,"fCameraDirection");
    float fRange = GetLocalFloat(oSync,"fCameraRange");
    float fPitch = GetLocalFloat(oSync,"fCameraPitch");

    // Get timing information
    float fTicks = (fTime * fFrameRate);
    float fdTime = (fTime / fTicks);
    float fDelay = 0.0;
    float fCount = 0.0;

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
        DelayCommand(fDelay,GestaltCameraPoint(fDirection,fRange,fPitch,fdDirection,fdRange,fdPitch,fd2Direction,fd2Range,fd2Pitch,fCount,oParty,iCamID));
        fCount = (fCount + 1.0);
        fDelay = (fCount * fdTime);
        }

    // Uncomment the line below to display the starting position of the camera movement
    // GestaltDebugOutput(oSync);

    // Uncomment the line below to display the finishing position of the camera movement
    // DelayCommand(fDelay,GestaltDebugOutput(oSync));
}



void GestaltCameraSmooth(float fDelay, float fdDirection1, float fdRange1, float fdPitch1, float fdDirection2, float fdRange2, float fdPitch2, float fTime, float fFrameRate, object oPC, int iParty = 0, int iSync = 1)
{
    object oParty;
    object oSync;
    int iCamID;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        // Work out whose camera position to use as the starting position
        if (iSync == 1)   { oSync = oPC; }
        else              { oSync = oParty; }

        // Set the camera to top down mode
        GestaltCameraMode(fDelay,oParty,CAMERA_MODE_TOP_DOWN);

        // Give the camera movement a unique id code so that it can be stopped
        iCamID = GetLocalInt(oParty,"iCamCount") + 1;
        SetLocalInt(oParty,"iCamCount",iCamID);

        // Uncomment the line below to get a message in the game telling you the id of this camera movement
        // AssignCommand(oParty,SpeakString("Camera id - " + IntToString(iCamID)));

        // After delay, stop any older camera movements and start this one
        DelayCommand(fDelay,GestaltStopCameraMoves(oParty,0,FALSE,iCamID - 1));
        DelayCommand(fDelay,GestaltCameraSmoothStart(fdDirection1,fdRange1,fdPitch1,fdDirection2,fdRange2,fdPitch2,fTime,fFrameRate,oParty,oSync,iCamID));

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltCameraCraneSmoothStart(float fdDirection1, float fdRange1, float fdPitch1, float fdHeight1, float fdDirection2, float fdRange2, float fdPitch2, float fdHeight2, float fTime, float fFrameRate, object oParty, object oSync, int iCamID)
{
    // Get starting position for camera
    float fDirection = GetLocalFloat(oSync,"fCameraDirection");
    float fRange = GetLocalFloat(oSync,"fCameraRange");
    float fPitch = GetLocalFloat(oSync,"fCameraPitch");
    float fHeight = GetLocalFloat(oSync,"fCameraHeight");

    // Get timing information
    float fTicks = (fTime * fFrameRate);
    float fdTime = (fTime / fTicks);
    float fDelay = 0.0;
    float fCount = 0.0;

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
        DelayCommand(fDelay,GestaltCameraPosition(fDirection,fRange,fPitch,fHeight,fdDirection,fdRange,fdPitch,fdHeight,fd2Direction,fd2Range,fd2Pitch,fd2Height,fCount,oParty,iCamID));
        fCount = (fCount + 1.0);
        fDelay = (fCount * fdTime);
        }

    // Uncomment the line below to display the starting position of the camera movement
    // GestaltDebugOutput(oSync);

    // Uncomment the line below to display the finishing position of the camera movement
    // DelayCommand(fDelay,GestaltDebugOutput(oSync));
}



void GestaltCameraCraneSmooth(float fDelay, float fdDirection1, float fdRange1, float fdPitch1, float fdHeight1, float fdDirection2, float fdRange2, float fdPitch2, float fdHeight2, float fTime, float fFrameRate, object oPC, int iParty = 0, int iSync = 1)
{
    object oParty;
    object oSync;
    int iCamID;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        // Work out whose camera position to use as the starting position
        if (iSync == 1)   { oSync = oPC; }
        else              { oSync = oParty; }

        // Set the camera to top down mode
        GestaltCameraMode(fDelay,oParty,CAMERA_MODE_TOP_DOWN);

        // Give the camera movement a unique id code so that it can be stopped
        iCamID = GetLocalInt(oParty,"iCamCount") + 1;
        SetLocalInt(oParty,"iCamCount",iCamID);

        // Uncomment the line below to get a message in the game telling you the id of this camera movement
        // AssignCommand(oParty,SpeakString("Camera id - " + IntToString(iCamID)));

        // After delay, stop any older camera movements and start this one
        DelayCommand(fDelay,GestaltStopCameraMoves(oParty,0,FALSE,iCamID - 1));
        DelayCommand(fDelay,GestaltCameraCraneSmoothStart(fdDirection1,fdRange1,fdPitch1,fdHeight1,fdDirection2,fdRange2,fdPitch2,fdHeight2,fTime,fFrameRate,oParty,oSync,iCamID));

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltDoCameraSetup(string sName, object oPC, float fDirection, float fRange, float fPitch, float fHeight)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    SetLocalFloat(oPC,"fCameraDirection",fDirection);
    SetLocalFloat(oPC,"fCameraRange",fRange);
    SetLocalFloat(oPC,"fCameraPitch",fPitch);
    SetLocalFloat(oPC,"fCameraHeight",fHeight);
}



void GestaltCameraSetup(float fDelay, object oPC, float fDirection, float fRange, float fPitch, float fHeight = 0.0)
{
    string sName = GetLocalString(GetModule(),"cutscene");

    if (fDelay == 0.0)  { GestaltDoCameraSetup(sName,oPC,fDirection,fRange,fPitch,fHeight); }
    else                { DelayCommand(fDelay,GestaltDoCameraSetup(sName,oPC,fDirection,fRange,fPitch,fHeight)); }
}



void GestaltCameraFace(float fDelay, object oStart, float fRange, float fPitch, object oEnd, float fRange2, float fPitch2, float fTime, float fFrameRate, object oPC, int iClockwise = 0, int iFace = 0, int iParty = 0)
{
    // Get timing information
    float fCount = 0.0;
    float fStart = fDelay;
    float fTicks = (fTime * fFrameRate);
    float fdTime = (fTime / fTicks);

    float fDirection;
    float fDirection2;

    float fdDirection;
    float fdRange = ((fRange2 - fRange) / fTicks);
    float fdPitch = ((fPitch2 - fPitch) / fTicks);

    object oParty;
    int iCamID;

    // Get first player
    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        // Set the camera to top down mode
        GestaltCameraMode(fDelay,oParty,CAMERA_MODE_TOP_DOWN);

        // Give the camera movement a unique id code so that it can be stopped
        iCamID = GetLocalInt(oParty,"iCamCount") + 1;
        SetLocalInt(oParty,"iCamCount",iCamID);

        // reset variables
        fCount = 0.0;
        fDelay = fStart;

        // Work out rotation rate for this player
        fDirection = GestaltGetDirection(oStart,oParty);
        fDirection2 = GestaltGetDirection(oEnd,oParty);
        fdDirection = GestaltGetPanRate(fDirection,fDirection2,fTicks,iClockwise);

        // After delay, stop any older camera movements and start this one
        DelayCommand(fStart,GestaltStopCameraMoves(oParty,0,FALSE,iCamID - 1));

        while (fCount <= fTicks)
            {
            DelayCommand(fDelay,GestaltCameraPoint(fDirection,fRange,fPitch,fdDirection,fdRange,fdPitch,0.0,0.0,0.0,fCount,oParty,iCamID,iFace));
            fCount = (fCount + 1.0);
            fDelay = fStart + (fCount * fdTime);
            }

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltCameraTrack(float fDelay, object oTrack, float fRange, float fPitch, float fRange2, float fPitch2, float fTime, float fFrameRate, object oPC, int iFace = 0, int iParty = 0)
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

    object oParty;
    int iCamID;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        // Set the camera to top down mode
        GestaltCameraMode(fDelay,oParty,CAMERA_MODE_TOP_DOWN);

        // Give the camera movement a unique id code so that it can be stopped
        iCamID = GetLocalInt(oParty,"iCamCount") + 1;
        SetLocalInt(oParty,"iCamCount",iCamID);

        // reset variables
        fCount = 0.0;
        fDelay = fStart;
        fRange = fSRange;
        fPitch = fSPitch;

        // After delay, stop any older camera movements and start this one
        DelayCommand(fStart,GestaltStopCameraMoves(oParty,0,FALSE,iCamID - 1));

        while (fCount <= fTicks)
            {
            DelayCommand(fDelay,GestaltCameraFaceTarget(oTrack,fRange,fPitch,oParty,iFace,0,iCamID));
            fPitch = (fPitch + fdPitch);
            fRange = (fRange + fdRange);
            fCount = (fCount + 1.0);
            fDelay = fStart + (fCount * fdTime);
            }

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltDoFadeOut(string sName, object oPC, float fSpeed, int iParty)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    object oParty;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        FadeToBlack(oParty,fSpeed);

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltDoFadeIn(string sName, object oPC, float fSpeed, int iParty)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    object oParty;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        FadeFromBlack(oParty,fSpeed);

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltDoBlack(string sName, object oPC, int iParty)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    object oParty;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        BlackScreen(oParty);

        if (iParty == 1)                       { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                  { oParty = GetNextPC(); }
        else                                   { return; }
        }
}



void GestaltDoStopFade(string sName, object oPC, int iParty)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    object oParty;

    if (iParty == 1)      { oParty = GetFirstFactionMember(oPC); }
    else if (iParty == 2) { oParty = GetFirstPC(); }
    else                  { oParty = oPC; }

    while (GetIsObjectValid(oParty))
        {
        StopFade(oParty);

        if (iParty == 1)                        { oParty = GetNextFactionMember(oParty,TRUE); }
        else if (iParty == 2)                   { oParty = GetNextPC(); }
        else                                    { return; }
        }
}



void GestaltDoFade(string sName, object oPC, int iFade, float fSpeed, float fDuration, int iParty)
{
    if (GetLocalInt(GetModule(),sName))
        { return; }

    if (iFade == FADE_IN)
        {
        if (fDuration > 0.0)                    { GestaltDoBlack(sName,oPC,iParty); }
        DelayCommand(fDuration,GestaltDoFadeIn(sName,oPC,fSpeed,iParty));
        }

    else if (iFade == FADE_OUT)
        {
        GestaltDoFadeOut(sName,oPC,fSpeed,iParty);
        if (fDuration > 0.0)                    { DelayCommand(fDuration,GestaltDoStopFade(sName,oPC,iParty)); }
        }

    else
        {
        GestaltDoFadeOut(sName,oPC,fSpeed,iParty);
        DelayCommand(fDuration,GestaltDoFadeIn(sName,oPC,fSpeed,iParty));
        }
}



void GestaltActionCameraFade(float fDelay, object oActor, object oPC, int iFade, float fSpeed = FADE_SPEED_MEDIUM, float fDuration = 0.0, int iParty = 0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,AssignCommand(oActor,ActionDoCommand(GestaltDoFade(sName,oPC,iFade,fSpeed,fDuration,iParty))));
    GestaltRegisterActor(sName,oActor);
}



void GestaltCameraFade(float fDelay, object oPC, int iFade, float fSpeed = FADE_SPEED_MEDIUM, float fDuration = 0.0, int iParty = 0)
{
    string sName = GetLocalString(GetModule(),"cutscene");
    DelayCommand(fDelay,GestaltDoFade(sName,oPC,iFade,fSpeed,fDuration,iParty));
}



void GestaltFixedCamera(object oPC, float fFrameRate = 50.0)
{
    // Thanks to Tenchi Masaki for the idea for this function
    string sCamera = GetLocalString(oPC,"sGestaltFixedCamera");     // Gets the camera position to use
    if (sCamera == "STOP")                                          // Camera tracking is turned off, stop script and don't recheck
        { return; }
    if (sCamera == "")                                              // Camera tracking is inactive, stop script but recheck in a second
        {
        DelayCommand(1.0,GestaltFixedCamera(oPC,fFrameRate));
        return;
        }

    float fHeight = GetLocalFloat(oPC,"fGestaltFixedCamera");       // Gets the camera height to use
    if (fHeight == 0.0)         { fHeight = 10.0; }                 // Defaults camera height to 10.0 if none has been set yet

    object oCamera = GetObjectByTag(sCamera);
    float fDelay = 1.0 / fFrameRate;
    float fRange = GetHorizontalDistanceBetween(oPC,oCamera);

    float fAngle = GestaltGetDirection(oPC,oCamera);                // Works out angle between camera and player
    float fPitch = atan(fRange/fHeight);                            // Works out vertical tilt
    float fDistance = sqrt(pow(fHeight,2.0) + pow(fRange,2.0));     // Works out camera distance from player
    if (fDistance > 30.0)       { fDistance = 30.0; }               // Sets distance to 30.0 if player is too far away
    if (fDistance < 5.0)        { fDistance = 5.0; }                // Sets distance to 5.0 if player is too close

    AssignCommand(oPC,SetCameraFacing(fAngle,fDistance,fPitch));
    DelayCommand(fDelay,GestaltFixedCamera(oPC,fFrameRate));
}

