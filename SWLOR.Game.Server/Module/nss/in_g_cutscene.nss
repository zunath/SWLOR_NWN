
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
