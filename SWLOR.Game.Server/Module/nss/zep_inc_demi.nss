//::///////////////////////////////////////////////
//:: zep_inc_demi
//:://////////////////////////////////////////////
/*
    Constants and functions for use with The Krit's
    revisions of the CEP adaptation of Demigog's
    demilich.
*/
//:://////////////////////////////////////////////
//:: Created by: The Krit
//:: Created on: May 10, 2007
//:://////////////////////////////////////////////
//:: Based on scripts by: Loki Hakanin and Demigog
//:://////////////////////////////////////////////


#include "colors_inc"
#include "zep_inc_scrptdlg"


//------------------------------------------------------------------------------
// CONSTANTS
//------------------------------------------------------------------------------


//------------------------------------------------------------------------------
// MODULE SETTINGS

// The name of a module local integer that holds a number indicating what
// should be done with soul gem victims when a demilich is truly destroyed.
const string ZEP_DEMI_RESS_VICTIMS = "ZEP_DEMILICH_Raise_Victims";
// There are three possibilities for the content of this variable and
// what happens to the victims:
//  0 means victims become raisable, but are left dead.
//  1 means victims are raised.
//  2 means victims are resurrected to full hit points.

// The name of a module local string that holds the name of a script to run
// when a demilich is killed.
const string ZEP_DEMI_DEAD_SCRIPT = "ZEP_DEMILICH_OnDeath_Script";
// Since killing a demilich is not permanent, the standard OnDeath script
// is not called when a demilich dies. Instead, the script whose name is held
// in this local string is run before the demilich retreats into the Pile of
// Bones placeable.
// The functions ZEPDemilichGetVictim() and ZEPDemilichGetVictimParty() might
// be useful in this script.

// The name of a module local string that holds the name of a script to run
// when a demilich is truly destroyed.
const string ZEP_DEMI_DEST_SCRIPT = "ZEP_DEMILICH_Destroyed_Script";
// The object running this script will be the bone pile placeable.
// The PC who destroyed the demilich can be retrieved in this script with
//    GetLocalObject(OBJECT_SELF, "MyDestroyer");
// The ResRef of the destroyed demilich can be retrieved in this script with
//    GetLocalString(OBJECT_SELF, "ZEP_DEMI_ResRef");
// Inventory that will be dropped is held by the object retrieved with
//    GetLocalObject(OBJECT_SELF, "ZEP_DEMI_Holder");
// This script will be run before consuming the holy water and running the
// destruction effects.
// The functions ZEPDemilichGetVictim() and ZEPDemilichGetVictimParty() might
// be useful in this script.

// There are two very fast pseudo-heartbeats used by the demilich routines.
// The delay on these is the following:
const float ZEP_DEMILICH_PSEUDO_DELAY = 6.0;
// This can be overridden for the module (even if this script is in a hak) by
// setting a local variable on the module. If a module variable's name is
// ZEP_DEMILICH_Pseudo_Delay and its type is float, then the contents and that
// variable will override the above.
// NOTES: A smaller delay means less lag when dealing with the soulgem victims,
//        but it also means more strain on the server.

// The number of seconds required for a demilich to regenerate from battle
// can be overridden for a module by storing the custom time as a local
// float named ZEP_DEMILICH_Regen_Time on the module.


//------------------------------------------------------------------------------
// BLUEPRINT SETTINGS

// The minimum level a caster must be before a demilich will consider the
// caster worthy of being a soulgem victim can be overridden for an
// individual blueprint by storing the custom threshold in a local integer
// named ZEP_DEMI_Power_Threshold on the blueprint.

// The save DC of the demilich's attempt to trap the souls of casters can be
// overridden for an individual blueprint by storing the custom DC in a local
// integer named ZEP_DEMI_TrapSoul_SaveDC on the blueprint.


//------------------------------------------------------------------------------
// LEGACY SUPPORT

// If you want to use the older heartbeat-based demilich, add "zep_demi_bone_hb"
// as the OnHeartbeat event of the placeable blueprint zep_demi_skull (Pile of
// Bones), and set the following flag to TRUE.
const int ZEP_DEMI_USE_LEGACY = FALSE;
// IMPORTANT: Do not add a heartbeat event to the blueprint zep_demi_skull0;
//            just add it to the blueprint without the '0'.

// One advantage of using the heartbeat-based demilich is the ability to set
// the placeables' "perception" range. Set that here.
// The demilich's perception range while resting or regenerating, in meters.
// (Setting this higher than 20.0 can be bad.)
const float ZEP_DEMI_PERC_RANGE = 5.0;


//------------------------------------------------------------------------------
// RESTING/REGENERATION


// The ResRefs of the skull and dust placeables to be created when the demilich
// goes into resting mode.
const string ZEP_DEMI_SKULL_RESREF = "zep_demi_skull";  // Using this draws out the demilich.
const string ZEP_DEMI_INERT_RESREF = "zep_demi_skull0"; // Using this starts a conversation.
const string ZEP_DEMI_DUST_RESREF = "zep_demi_dust";

// The message spoken by a freshly-regenerated demilich.
string ZEP_DEMI_REGEN_MSG = GetStringByStrRef(nZEPDemiRestored); //"At last, I am restored..."
// The message spoken by a demilich responding to intruders.
string ZEP_DEMI_DIST_MSG =  GetStringByStrRef(nZEPDemiDisturbed); //"You disturb my work!"

// The number of seconds required for a demilich to regenerate from battle
// injuries after seemingly being killed.
// Should be at least long enough for a PC to apply Holy Water to the
// demilich's bones.
const float ZEP_DEMILICH_REGEN_TIME = 300.0;
// This can be overridden for a module by storing the custom time as a local
// float named ZEP_DEMILICH_Regen_Time on the module.

// The tag of the item that is needed to destroy a regenerating demilich.
// (By default, this is the tag of Holy Water, whose blueprint is "zep_holy_water".)
const string ZEP_DEMI_DEST_TAG = "zep_holy_water";

// The floating text displayed when a demilich is truly destroyed.
string ZEP_DEMI_FINAL_DEST = GetStringByStrRef(nZEPDemiVictFree); //"With the demilich destroyed, the souls of its victims are released to their bodies."


//------------------------------------------------------------------------------
// SOUL TRAPPING


// The maximum number of soulgem victims the demilich will take on at a time
// (a.k.a. the number of soulgems embedded in a demilich).
const int ZEP_DEMI_NUM_SOULGEMS = 8;
// This is not settable via local variables because changing this value in the
// middle of a game could be bad.

// The minimum level a caster must be before a demilich will consider the
// caster worthy of being a soulgem victim.
const int ZEP_DEMI_POWER_THRESHOLD = 15;
// This can be overridden for an individual blueprint by storing the custom
// threshold in a local integer named ZEP_DEMI_Power_Threshold on the blueprint.

// The save DC of the demilich's attempt to trap the souls of casters.
const int ZEP_DEMI_TRAPSOUL_SAVEDC = 15;
// This can be overridden for an individual blueprint by storing the custom
// DC in a local integer named ZEP_DEMI_TrapSoul_SaveDC on the blueprint.

// The message spoken by a demilich who has been hit by a spell from a high
// level caster, just before attempting to capture the caster's soul.
string ZEP_DEMI_ONSPELL_MSG = GetStringByStrRef(nZEPDemiHavePower); //"Yes, I sense you have power...your potential shall be mine!"

// The combat message sent to a player, as a demilich attempts to capture the
// PC's soul.
// Will be prefixed by the demilich's name.
string ZEP_DEMI_TRAPSOUL_MESSAGE = " attempts Trap the Soul.";

// The message that floats over a PC as the PC's soul is stolen.
string ZEP_DEMI_TRAPSOUL_FLOATINGTEXT = " has trapped the soul of ";


//------------------------------------------------------------------------------
// LOCAL VARIABLES

const string ZEP_DEMI_LOCAL_AMBIENT  = "ZEP_DEMI_Ambient";  // Eye-candy placeable (dust plume).
const string ZEP_DEMI_LOCAL_RESREF   = "ZEP_DEMI_ResRef";   // Blueprint of demilich creature.
const string ZEP_DEMI_LOCAL_SOURCE   = "ZEP_DEMI_Source";   // Placeable storing creature info.
const string ZEP_DEMI_LOCAL_HOLDER   = "ZEP_DEMI_Holder";   // Inventory storage.
const string ZEP_DEMI_LOCAL_SGCORPSE = "ZEP_DEMI_SG_Corpse_";// The corpse of a soul gem victim.
                                                             // Also: the victim stored on the corpse.
const string ZEP_DEMI_LOCAL_HITDICE  = "ZEP_DEMI_HitDice";  // The hit dice of the demilich.
const string ZEP_DEMI_LOCAL_PARTY    = "ZEP_DEMI_Party";    // A party member of a soul gem victim.


//------------------------------------------------------------------------------
// PROTOTYPES
//------------------------------------------------------------------------------

// Moves all items with the droppable flag set from the inventory of oFrom to
// the inventory of oTo. Also moves equipped items.
// (The order of the parameters is comparable to an assignment: oTo = oFrom.)
void MoveDroppableInventory(object oTo, object oFrom);

// Creates and initializes the objects used to represent a resting or
// regenerating demilich.
// oDemilich is the demilich about to rest or regenerate.
// bWasKilled is TRUE if oDemilich was killed (and needs to regenerate).
void ZEPDemilichSpawnBones(object oDemilich, int bWasKilled);

// Creates a demilich from its resting or regenerating state.
// oBones is the bone placeable storing demilich data.
// sResRef is the blueprint to use (supports custom demiliches).
// bIntrusion is TRUE if the demilich is responding to an intruder.
object ZEPDemilichFromBones(object oBones, string sResRef, int bIntrusion);

// Creates an area of effect that will serve to detect any nearby intruders.
// lTarget is where the effect will be centered.
object ZEPDemilichCreateDetector(location lTarget);

// Restores a regenerating demilich.
// To be run by the bone pile placeable.
// sResRef is the blueprint of the demilich.
// oDust is the associated dust placeable.
// oHolder is the associated inventory holder placeable.
void ZEPDemilichRestore(string sResRef, object oDust, object oHolder);

// Sees if we want to trap oPC in a soul gem.
// If so, returns the gem number to trap oPC within.
// If not, returns -1.
// To be run by the demilich.
int ZEPDemilichChooseSoulGem(object oPC);

// Traps the soul of oPC, which kills oPC and prevents resurreaction.
// To be run by the demilich.
// nGem is the number of the gem in which to trap the soul.
void ZEPDemilichTrapSoul(object oPC, int nGem);

// Frees the soul trapped in a soulgem, allowing the character ro be raised.
// Does nothing if the indicated soulgem does not contain a soul.
// To be run by the demilich or the bone pile placeable.
// nGem is the number of the soulgem.
void ZEPDemilichFreeSoul(int nGem);

// Restores oPC's raisable status.
// Also raises oPC if ZEP_DEMI_RESS_VICTIMS is set.
// To be run by the cloned corpse.
// fDelay is the delay that will be used when recursing pseudo-heartbeat style.
void ZEPDemilichRaiseVictim(object oPC, float fDelay);

// Cleans up the result of oPC's soul being stolen.
// Any cutscene-ghost effects are removed.
// If the death effect worked, oPC will be made invisible and untargettable, and
// a pseudo-heartbeat will be started to track if oPC respawned.
// If the death effect failed, the caller is destroyed.
// To be called by the cloned corpse.
void ZEPDemilichCorpseInit(object oPC);

// Pseudo-heartbeat function that will clean-up if a soul gem victim respawns.
// To be run by the cloned corpse.
// oPC is the real victim.
// fDelay is the delay that will be used when recursing pseudo-heartbeat style.
void ZEPDemilichCorpseCheck(object oPC, float fDelay);

// Retrieves soul gem victim number nGem.
// Valid values for nGem are 0 through ZEP_DEMI_NUM_SOULGEMS - 1.
// Returns OBJECT_INVALID on error.
// To be called by a demilich or the bone pile placeable (as would be the case
// if called from an OnDeath or Destruction script).
object ZEPDemilichGetVictim(int nGem);

// Retrieves a party member of soul gem victim number nGem.
// For PC victims, this is a member of the PC's party when the PC was trapped.
// For NPC victims, this is the NPC's master.
// Valid values for nGem are 0 through ZEP_DEMI_NUM_SOULGEMS - 1.
// Returns OBJECT_INVALID on error.
// To be called by a demilich or the bone pile placeable (as would be the case
// if called from an OnDeath or Destruction script).
object ZEPDemilichGetVictimParty(int nGem);


//------------------------------------------------------------------------------
// FUNCTIONS
//------------------------------------------------------------------------------


//------------------------------------------------------------------------------
// MoveDroppableInventory()
//
// Moves all items with the droppable flag set from the inventory of oFrom to
// the inventory of oTo. Also moves equipped items.
// (The order of the parameters is comparable to an assignment: oTo = oFrom.)
//
void MoveDroppableInventory(object oTo, object oFrom)
{
    // This hangs if oTo is invalid.
    if ( !GetIsObjectValid(oTo) )
        return;

    // Loop through oFrom's inventory.
    object oItem = GetFirstItemInInventory(oFrom);
    while ( oItem != OBJECT_INVALID )
    {
        // Check the droppable flag.
        if ( GetDroppableFlag(oItem) )
        {
            // Move the item.
            CopyItem(oItem, oTo, TRUE);
            DestroyObject(oItem);
        }
        // Advance the loop.
        oItem = GetNextItemInInventory(oFrom);
    }

    // Loop through oFrom's equipment slots.
    int nSlot = NUM_INVENTORY_SLOTS;
    while ( nSlot-- > 0 )
    {
        oItem = GetItemInSlot(nSlot, oFrom);
        // Check the droppable flag.
        if ( GetDroppableFlag(oItem) )
        {
            // Move the item.
            CopyItem(oItem, oTo, TRUE);
            DestroyObject(oItem);
        }
    }
}


//------------------------------------------------------------------------------
// ZEPDemilichSpawnBones()
//
// Creates and initializes the objects used to represent a resting or
// regenerating demilich.
// oDemilich is the demilich about to rest or regenerate.
// bWasKilled is TRUE if oDemilich was killed (and needs to regenerate).
//
void ZEPDemilichSpawnBones(object oDemilich, int bWasKilled)
{
    // Get the location where the objects will appear.
    location lDemilich = GetLocation(oDemilich);
    // Get the blueprint of this demilich.
    // (Has to be stored in a variable for when this is an OnDeath event.)
    string sDemilich = GetResRef(oDemilich);

    // Create a skull pile and dust plume.
    object oDust = CreateObject(OBJECT_TYPE_PLACEABLE, ZEP_DEMI_DUST_RESREF, lDemilich);
    object oBones;
    if ( bWasKilled )
        oBones = CreateObject(OBJECT_TYPE_PLACEABLE, ZEP_DEMI_INERT_RESREF, lDemilich);
    else
        oBones = CreateObject(OBJECT_TYPE_PLACEABLE, ZEP_DEMI_SKULL_RESREF, lDemilich);
    // Link the dust to the bones.
    SetLocalObject(oBones, ZEP_DEMI_LOCAL_AMBIENT, oDust);
    // Record the blueprint for this demilich.
    SetLocalString(oBones, ZEP_DEMI_LOCAL_RESREF, sDemilich);
    // Record the hit dice of this demilich.
    SetLocalInt(oBones, ZEP_DEMI_LOCAL_HITDICE, GetHitDice(oDemilich));

    // Copy the variables recording soulgem victims.
    int nGem = ZEP_DEMI_NUM_SOULGEMS;
    while ( nGem-- > 0 )
        SetLocalObject(oBones, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem),
            GetLocalObject(oDemilich, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem)));

    // See if the demilich is regenerating or merely resting.
    if ( bWasKilled )
    {
        // Move the droppable inventory into an invisible placeable.
        object oHolder = CreateObject(OBJECT_TYPE_PLACEABLE, "x0_plc_corpse", lDemilich);
        MoveDroppableInventory(oHolder, oDemilich);
        SetUseableFlag(oHolder, FALSE);
        SetLocalObject(oBones, ZEP_DEMI_LOCAL_HOLDER, oHolder);

        // Find a suitable delay.
        float fDelay = GetLocalFloat(GetModule(), "ZEP_DEMILICH_Regen_Time");
        if ( fDelay == 0.0 )
            fDelay = ZEP_DEMILICH_REGEN_TIME;
        // Delay-restore the demilich.
        AssignCommand(oBones, DelayCommand(fDelay, ZEPDemilichRestore(sDemilich, oDust, oHolder)));
    }
    // A resting legacy Demilich needs no additional work at this point.
    else if ( !ZEP_DEMI_USE_LEGACY )
    {
        // Create an object to detect intruders.
        object oDetector = ZEPDemilichCreateDetector(lDemilich);

        // Initialize the detector.
        SetLocalObject(oDetector, ZEP_DEMI_LOCAL_SOURCE, oBones);
        SetLocalObject(oDetector, ZEP_DEMI_LOCAL_AMBIENT, oDust);
        SetLocalString(oDetector, ZEP_DEMI_LOCAL_RESREF, sDemilich);
        // Link the detector to the bone pile.
        SetLocalObject(oBones, ZEP_DEMI_LOCAL_SOURCE, oDetector);
    }
}


//------------------------------------------------------------------------------
// ZEPDemilichFromBones()
//
// Creates a demilich from its resting or regenerating state.
// oBones is the bone placeable storing demilich data.
// sResRef is the blueprint to use.
// bIntrusion is TRUE if the demilich is responding to an intruder.
//
object ZEPDemilichFromBones(object oBones, string sResRef, int bIntrusion)
{
    // Double-check the blueprint.
    if ( sResRef == "" )
        // Use the CEP default.
        sResRef = "zep_demi_lich";

    // Create the demilich.
    object oDemilich = CreateObject(OBJECT_TYPE_CREATURE, sResRef, GetLocation(oBones));
    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_MIND), oBones);

    // Copy the variables recording soulgem victims.
    int nGem = ZEP_DEMI_NUM_SOULGEMS;
    while ( nGem-- > 0  )
        SetLocalObject(oDemilich, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem),
            GetLocalObject(oBones, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem)));

    // Have the demilich say something appropriate.
    string sSayThis = ColorTokenShout();
    if ( bIntrusion )
        sSayThis += ZEP_DEMI_DIST_MSG;
    else
        sSayThis += ZEP_DEMI_REGEN_MSG;
    sSayThis += ColorTokenEnd();
    // A creature apparently will not be heard if told to speak immediately upon
    // spawning. On my machine, a quarter-second delay worked well, so a full
    // second should be safe, yet still not noticeable by a player.
    AssignCommand(oDemilich, DelayCommand(1.0, SpeakString(sSayThis)));
    // Add a little audial panache.
    AssignCommand(oDemilich, DelayCommand(0.5, PlaySound("c_demilich_bat2")));

    // Return the newly created demilich.
    return oDemilich;
}


//------------------------------------------------------------------------------
// ZEPDemilichCreateDetector()
//
// Creates an area of effect that will serve to detect any nearby intruders.
// lTarget is where the effect will be centered.
//
object ZEPDemilichCreateDetector(location lTarget)
{
    // Create an invisible area of effect to detect intruders.
    effect eDetector = EffectAreaOfEffect(AOE_PER_CUSTOM_AOE, "zep_demi_aoe_ent");
    ApplyEffectAtLocation(DURATION_TYPE_PERMANENT, eDetector, lTarget);

    // Look for the area of effect object we just created.
    object oDetector = GetFirstObjectInShape(SHAPE_CUBE, 0.0, lTarget, FALSE,
                                             OBJECT_TYPE_AREA_OF_EFFECT);
    while( GetIsObjectValid(oDetector) )
    {
        // Match creator, tag, and not initialized yet.
        if( GetAreaOfEffectCreator(oDetector) == OBJECT_SELF  &&
            GetTag(oDetector) == "VFX_CUSTOM"  &&
            GetLocalObject(oDetector, ZEP_DEMI_LOCAL_SOURCE) == OBJECT_INVALID )
        {
            // Return this object.
            return oDetector;
        }

        // Get the next candidate AOE object.
        oDetector = GetNextObjectInShape(SHAPE_CUBE, 0.0, lTarget, FALSE,
                                         OBJECT_TYPE_AREA_OF_EFFECT);
    }

    // This should never happen, but there still needs to be a default return value.
    return OBJECT_INVALID;
}


//------------------------------------------------------------------------------
// ZEPDemilichRestore()
//
// Restores a regenerating demilich.
// To be run by the bone pile placeable.
// sResRef is the blueprint of the demilich.
// oDust is the associated dust placeable.
// oHolder is the associated inventory holder placeable.
//
void ZEPDemilichRestore(string sResRef, object oDust, object oHolder)
{
    // See if the demilich is in the process of being destroyed.
    if ( GetLocalInt(OBJECT_SELF, "DESTROYED") )
        // Abort.
        return;

    // Spawn the demilich.
    ZEPDemilichFromBones(OBJECT_SELF, sResRef, FALSE);

    // Destroy oHolder's inventory.
    object oItem = GetFirstItemInInventory(oHolder);
    while ( oItem != OBJECT_INVALID )
    {
        DestroyObject(oItem);
        oItem = GetNextItemInInventory(oHolder);
    }

    // Destroy the placeables.
    DestroyObject(oHolder);
    DestroyObject(oDust);
    DestroyObject(OBJECT_SELF);
}


//------------------------------------------------------------------------------
// ZEPDemilichChooseSoulGem()
//
// Sees if we want to trap oPC in a soul gem.
// If so, returns the gem number to trap oPC within.
// If not, returns -1.
// To be run by the demilich.
//
int ZEPDemilichChooseSoulGem(object oPC)
{
    // Do not trap myself.
    if ( oPC == OBJECT_SELF )
        return -1;

    // Make sure oPC is the right type of creature.
    if ( !GetIsPC(oPC)  &&  !GetLocalInt(oPC, "ZEP_DEMILICH_AllowSoulGem") )
        // Do not trap.
        return -1;

    // Get the threshold for our attention from the demilich.
    int nThreshold = GetLocalInt(OBJECT_SELF, "ZEP_DEMI_Power_Threshold");
    if ( nThreshold == 0 )
        // Use the module default.
        nThreshold = ZEP_DEMI_POWER_THRESHOLD;
    // See if oPC is not worthy of attention.
    if ( GetCasterLevel(oPC) < nThreshold )
        // Do not trap.
        return -1;

    // Find the weakest entrapped soul.
    int nWeakestGem = -1;
    int nWeakestLevel = 99;
    int nGem = ZEP_DEMI_NUM_SOULGEMS;
    while ( nGem-- > 0 )
    {
        // Get the level of the prisoner of this soul gem.
        int nLevel = GetHitDice(GetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem)));
        // Check for an empty gem.
        if ( nLevel == 0 )
            // Use this gem.
            return nGem;
        // Check for a new lowest level.
        else if ( nLevel < nWeakestLevel )
        {
            // Remember this gem.
            nWeakestLevel = nLevel;
            nWeakestGem = nGem;
        }
    }

    // See if we found a prisoner we would give up for oPC.
    if ( nWeakestLevel < GetHitDice(oPC) )
        // Use the weakest gem.
        return nWeakestGem;

    // At this point, it's not worth the effort. Do not trap.
    return -1;
}


//------------------------------------------------------------------------------
// ZEPDemilichTrapSoul()
//
// Traps the soul of oPC, which kills oPC and prevents resurreaction.
// To be run by the demilich.
// nGem is the number of the gem in which to trap the soul.
//
void ZEPDemilichTrapSoul(object oPC, int nGem)
{
    float fDelay = 1.5;

    // If there is an existing prisoner, free it.
    ZEPDemilichFreeSoul(nGem);

    // Stop the PC for this effect. (Makes the visual effects look better.)
    AssignCommand(oPC, ClearAllActions());
    DelayCommand(0.1, SetCommandable(FALSE, oPC));
    DelayCommand(fDelay, SetCommandable(TRUE, oPC));

    // Clone oPC in place.
    ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectCutsceneGhost(), oPC);
    object oClone = CopyObject(oPC, GetLocation(oPC), OBJECT_INVALID, "ZEP_DEMILICH_VICTIM");
    // The clone will become a selectable, but not raisable, corpse.
    AssignCommand(oClone, SetIsDestroyable(FALSE, FALSE, TRUE));
    // Record the soon-to-be corpse.
    SetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem), oClone);
    SetLocalObject(oClone, ZEP_DEMI_LOCAL_SGCORPSE, oPC);

    // Apply a visual effect.
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY,
        EffectBeam(VFX_BEAM_HOLY, OBJECT_SELF, BODY_NODE_HAND),
        oClone, fDelay);

    // Give some feedback.
    DelayCommand(fDelay, FloatingTextStringOnCreature(
                            GetName(OBJECT_SELF) + ZEP_DEMI_TRAPSOUL_FLOATINGTEXT +
                            GetName(oPC) + "!", oPC));

    // Kill PC and clone.
    effect oDeath = SupernaturalEffect(EffectDeath());
    DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_INSTANT, oDeath, oClone));
    DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_INSTANT, oDeath, oPC));

    // Turn processing over to the clone.
    // This will either hide oPC so it cannot be targetted by Raise Dead, or
    // destroy the clone so there does not appear to be a copy involved.
    AssignCommand(oClone, DelayCommand(fDelay + 0.1, ZEPDemilichCorpseInit(oPC)));
}


//------------------------------------------------------------------------------
// ZEPDemilichFreeSoul()
//
// Frees the soul trapped in a soulgem, allowing the character ro be raised.
// Does nothing if the indicated soulgem does not contain a soul.
// To be run by the demilich or the bone pile placeable.
// nGem is the number of the soulgem.
//
void ZEPDemilichFreeSoul(int nGem)
{
    // Retrieve and delete the relevant local variable.
    object oCorpse = GetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem));
    DeleteLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem));

    // See if there is a soul currently trapped in the soulgem.
    if ( !GetIsObjectValid(oCorpse) )
        // No soul to free, nothing to do.
        return;

    // Get the PC whose soul is being released.
    object oPC = GetLocalObject(oCorpse, ZEP_DEMI_LOCAL_SGCORPSE);

    // Generate visual effects.
    float fVFXDuration = 0.4 + 0.07 * GetDistanceToObject(oCorpse);
    // Use an auxiliary placeable so that the visuals can overlap, and because
    // faked spells don't fire reliably.
    object oVFXMaker = CreateObject(OBJECT_TYPE_PLACEABLE, "x0_plc_bomb", GetLocation(OBJECT_SELF));
    AssignCommand(oVFXMaker, ActionCastSpellAtObject(SPELL_PHANTASMAL_KILLER, oCorpse));
    AssignCommand(oVFXMaker, ActionDoCommand(DestroyObject(oVFXMaker)));
    // Visual on the corpse.
    AssignCommand(oCorpse, DelayCommand(fVFXDuration,
        ApplyEffectToObject(DURATION_TYPE_INSTANT,
                            EffectVisualEffect(VFX_IMP_RESTORATION_GREATER),
                            oCorpse)));
    // Make the corpse disappear (in case the victim logged out).
    AssignCommand(oCorpse, DelayCommand(fVFXDuration + 1.0,
        ApplyEffectToObject(DURATION_TYPE_PERMANENT,
                            EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY),
                            oCorpse)));

    // Get the pseudo-heartbeat delay.
    float fDelay = GetLocalFloat(GetModule(), "ZEP_DEMILICH_Pseudo_Delay");
    if ( fDelay == 0.0 )
        // Use the default.
        fDelay = ZEP_DEMILICH_PSEUDO_DELAY;
    // Restore the PC.
    // Delayed to give the visual effect time to execute.
    AssignCommand(oCorpse, DelayCommand(fVFXDuration + 0.1, ZEPDemilichRaiseVictim(oPC, fDelay)));
}


//------------------------------------------------------------------------------
// void ZEPDemilichRaiseVictim()
//
// Restores oPC's raisable status.
// Also raises oPC if ZEP_DEMI_RESS_VICTIMS is set.
// To be run by the cloned corpse.
// fDelay is the delay that will be used when recursing pseudo-heartbeat style.
//
void ZEPDemilichRaiseVictim(object oPC, float fDelay)
{
    // See if target is invalid.
    if ( !GetIsObjectValid(oPC) )
    {
        // Player must have logged out.
        // Search again next round.
        DelayCommand(fDelay, ZEPDemilichRaiseVictim(oPC, fDelay));
        return;
    }

    // Make oPC visible again.
    // Loop through active effects.
    effect eInvis = GetFirstEffect(oPC);
    while ( GetIsEffectValid(eInvis) )
    {
        // Check for the cutscene invisibility.
        if ( GetEffectCreator(eInvis) == OBJECT_SELF )
            // Remove this effect.
            RemoveEffect(oPC, eInvis);
        // Update the loop.
        eInvis = GetNextEffect(oPC);
    }

    // Send a message to oPC to explain the situation.
    SendMessageToPC(oPC, GetStringByStrRef(nZEPReturnToLife)); // "You feel disoriented momentarily as your soul returns to its mortal shell."

    // Check the ZEP_DEMI_RESS_VICTIMS flag.
    int nResVictims = GetLocalInt(GetModule(), ZEP_DEMI_RESS_VICTIMS);
    if ( nResVictims > 0 )
    {
        // Raise the vicitm.
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectResurrection(), oPC);
        // Check for full resurrection.
        if ( nResVictims > 1 )
            // Heal the victim to full hit points.
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectHeal(GetMaxHitPoints(oPC)), oPC);
    }
    // This corpse is no longer needed.
    SetIsDestroyable(TRUE, FALSE, FALSE);
    DestroyObject(OBJECT_SELF);
}


//------------------------------------------------------------------------------
// ZEPDemilichCorpseInit()
//
// Cleans up the result of oPC's soul being stolen.
// Any cutscene-ghost effects are removed.
// If the death effect worked, oPC will be made invisible and untargettable, and
// a pseudo-heartbeat will be started to track if oPC respawned.
// If the death effect failed, the caller is destroyed.
// To be called by the cloned corpse.
//
void ZEPDemilichCorpseInit(object oPC)
{
    // Make the corpse cutscene-ghosted. (Not sure if this helps, but it might.)
    ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectCutsceneGhost(), OBJECT_SELF);

    // Loop through active effects on oPC.
    effect eGhost = GetFirstEffect(oPC);
    while ( GetIsEffectValid(eGhost) )
    {
        // Look for cutscene-ghost effects.
        if ( GetEffectType(eGhost) == EFFECT_TYPE_CUTSCENEGHOST )
            // Remove cutscene-ghost.
            RemoveEffect(oPC, eGhost);
        eGhost = GetNextEffect(oPC);
    }

    if ( GetIsDead(oPC) )
    {
        // Hide oPC with cutscene invisibility.
        // Effect is extraordinary so that it cannot be dispelled, but can be gotten
        // rid of by the PC (by resting) if something goes wrong with the removal scripts.
        ApplyEffectToObject(DURATION_TYPE_PERMANENT,
            ExtraordinaryEffect(EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY)),
            oPC);

        // Prevent party members from targetting the character's portrait.
        if ( GetIsPC(oPC) )
        {
            // First, find a party member who is not oPC, if any.
            object oParty = GetFirstFactionMember(oPC);
            if ( oParty == oPC )
                oParty = GetNextFactionMember(oPC);
            // Keep a record of oPC's party in case the module wants it later.
            SetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_PARTY, oParty);
            // Now remove oPC from the party so others can't target the portrait.
            RemoveFromParty(oPC);
        }
        else
        {
            // See if oPC has a master.
            object oMaster = GetMaster(oPC);
            if ( oMaster != OBJECT_INVALID )
            {
                // Keep a record of oPC's master in case the module wants it later.
                SetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_PARTY, oMaster);
                // Now remove oPC from the party so others can't target the portrait.
                // NOTE: This function is called a split second after the death
                // effect is applied, so any module-specific code will have a chance
                // to run before this line fires the henchman.
                RemoveHenchman(GetMaster(oPC), oPC);
            }
        }
    }

    // This clone corpse should not drop any items.
    // Clear out inventory.
    object oItem = GetFirstItemInInventory();
    while ( oItem != OBJECT_INVALID )
    {
        DestroyObject(oItem);
        oItem = GetNextItemInInventory();
    }
    // Flag equipped items as undroppable.
    int nSlot = NUM_INVENTORY_SLOTS;
    while ( nSlot-- > 0 )
        SetDroppableFlag(GetItemInSlot(nSlot), FALSE);
    // Remove gold.
    TakeGoldFromCreature(GetGold(), OBJECT_SELF, TRUE);

    // Get the pseudo-heartbeat delay.
    float fDelay = GetLocalFloat(GetModule(), "ZEP_DEMILICH_Pseudo_Delay");
    if ( fDelay == 0.0 )
        // Use the default.
        fDelay = ZEP_DEMILICH_PSEUDO_DELAY;
    // Start a pseudo-heartbeat that will destroy the caller when oPC is alive.
    ZEPDemilichCorpseCheck(oPC, fDelay);
}


//------------------------------------------------------------------------------
// ZEPDemilichCorpseCheck()
//
// Pseudo-heartbeat function that will clean-up if a soul gem victim respawns.
// To be run by the cloned corpse.
// oPC is the real victim.
// fDelay is the delay that will be used when recursing pseudo-heartbeat style.
//
void ZEPDemilichCorpseCheck(object oPC, float fDelay)
{
    // See if oPC is still dead (or logged off).
    if ( GetIsDead(oPC)  ||  !GetIsObjectValid(oPC) )
        // Recurse the pseudo-heartbeat.
        DelayCommand(fDelay, ZEPDemilichCorpseCheck(oPC, fDelay));
    else
    {
        // oPC is alive! Hooray!
        // Since oPC was made cutscene-invisible, this can only happen via
        // respawning, DM intervention, or release from the demilich.

        // Make oPC visible again.
        // Loop through active effects.
        effect eInvis = GetFirstEffect(oPC);
        while ( GetIsEffectValid(eInvis) )
        {
            // Check for the cutscene invisibility.
            if ( GetEffectCreator(eInvis) == OBJECT_SELF )
                // Remove this effect.
                RemoveEffect(oPC, eInvis);
            // Update the loop.
            eInvis = GetNextEffect(oPC);
        }

        // This corpse is no longer needed.
        SetIsDestroyable(TRUE, FALSE, FALSE);
        DestroyObject(OBJECT_SELF);
    }
}


//------------------------------------------------------------------------------
// ZEPDemilichGetVictim()
//
// Retrieves soul gem victim number nGem.
//
// Valid values for nGem are 0 through ZEP_DEMI_NUM_SOULGEMS - 1.
// Returns OBJECT_INVALID on error.
//
// To be called by a demilich or the bone pile placeable (as would be the case
// if called from an OnDeath or Destruction script).
//
object ZEPDemilichGetVictim(int nGem)
{
    return GetLocalObject(
            GetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem)),
            ZEP_DEMI_LOCAL_PARTY);
}


//------------------------------------------------------------------------------
// ZEPDemilichGetVictimParty()
//
// Retrieves a party member of soul gem victim number nGem.
// For PC victims, this is a member of the PC's party when the PC was trapped.
// For NPC victims, this is the NPC's master.
//
// Valid values for nGem are 0 through ZEP_DEMI_NUM_SOULGEMS - 1.
// Returns OBJECT_INVALID on error.
//
// To be called by a demilich or the bone pile placeable (as would be the case
// if called from an OnDeath or Destruction script).
//
object ZEPDemilichGetVictimParty(int nGem)
{
    return GetLocalObject(
            GetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_SGCORPSE + IntToString(nGem)),
            ZEP_DEMI_LOCAL_SGCORPSE);
}

