//::///////////////////////////////////////////////
//:: zep_demi_dest
//:://////////////////////////////////////////////
/*
    Permanently destroys a demilich who has already
    been forced into a regenerative state.

    Must be called by the bone pile representing a
    demilich in a regenerative state. This is done,
    for example, for "Actions Taken" scripts in
    a conversation with the bone pile.

    Used by conversation zep_demi_regen_c.
*/
//:://////////////////////////////////////////////
//:: Created by: Loki Hakanin
//:: Created on: April 25th, 2004
//:: Updated: April 27th, 2004
//:://////////////////////////////////////////////
//:: Modified by: The Krit
//:: Modified on: May 10, 2007
//:://////////////////////////////////////////////

//:://////////////////////////////////////////////
//:: Summary of effects:
//::    1.) PC crouches over the bones.
//::    2.) Floating text on our demilich's bones explains what's going on.
//::    3.) VFX representing the demilich's departing soul -- and the
//::        returning souls of its victims -- is played.
//::    4.) Victims are notified of their restored status.
//::    5.) Victims are resurrected, if a flag to do so is set.
//::    6.) Nearby creatures are possibly dazed or stunned by the death knell.
//::    7.) The demilich's bone and dust cloud placeables are destroyed.
//:://////////////////////////////////////////////


#include "zep_inc_demi"


// The time it takes for the Wail of the Banshee visual to scream.
const float WAIL_TIME = 3.0f;


// Handles the actual destruction and consequences.
// Executed by the PC.
void DestroyDemilich(object oBones);

// Attempts to daze or stun those nearby the demilich when it is destroyed.
// Executed by the Pile of Bones.
void DeathKnell();


// Main entry point.
void main()
{
    object oPC = GetPCSpeaker();
    object oBones = OBJECT_SELF;

    // Animate the PC to crouch down and interact with the demilich's bones.
    AssignCommand(oPC, ActionMoveToObject(oBones));
    AssignCommand(oPC, ActionDoCommand(SetFacingPoint(GetPosition(oBones))));
    AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_GET_LOW, 1.0, 3.0));

    // Destroy the demilich once the animation is complete.
    AssignCommand(oPC, ActionDoCommand(DestroyDemilich(oBones)));
}


// Handles the actual destruction and consequences.
// Executed by the PC.
void DestroyDemilich(object oBones)
{
    // See if the demilich regenerated before we got to this point.
    if ( !GetIsObjectValid(oBones) )
        return;

    // Prevent the demilich from regenerating.
    SetLocalInt(oBones, "DESTROYED", TRUE);

    // Allow a module-specific script to be called here.
    SetLocalObject(oBones, "MyDestroyer", OBJECT_SELF);
    ExecuteScript(GetLocalString(GetModule(), ZEP_DEMI_DEST_SCRIPT), oBones);

    // Consume the demilich-destroying item (holy water, by default).
    object oHolyWater = GetItemPossessedBy(OBJECT_SELF, ZEP_DEMI_DEST_TAG);
    int nSize = GetItemStackSize(oHolyWater);
    if ( nSize > 1 )
        // Only consume one item.
        SetItemStackSize(oHolyWater, nSize - 1);
    else
        DestroyObject(oHolyWater);

    // Display the demilich's soul departing. (Impressive, no?)
    ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_WAIL_O_BANSHEES), oBones);

    // Display some floating text over the PC who did the demilich in.
    FloatingTextStringOnCreature(ZEP_DEMI_FINAL_DEST, OBJECT_SELF);

    // Calculate a partial delay.
    float fInterval = WAIL_TIME / IntToFloat(ZEP_DEMI_NUM_SOULGEMS+1);
    // Loop through the soul gems.
    int nGem = ZEP_DEMI_NUM_SOULGEMS;
    while ( nGem-- > 0 )
        // Release this soul.
        AssignCommand(oBones, DelayCommand(IntToFloat(nGem) * fInterval,
                                            ZEPDemilichFreeSoul(nGem)));

    // Destroy the demilich bone, dust, and inventory placeables once the above effects finish.
    DestroyObject(GetLocalObject(oBones, ZEP_DEMI_LOCAL_AMBIENT), WAIL_TIME);
    DestroyObject(GetLocalObject(oBones, ZEP_DEMI_LOCAL_HOLDER), WAIL_TIME);
    DestroyObject(oBones, WAIL_TIME + 0.1); // Slightly longer delay so DeathKnell() can finish.
    // A little extra visual effect as the bones disappear.
    DelayCommand(WAIL_TIME, ApplyEffectToObject(DURATION_TYPE_INSTANT,
                                EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_NATURE),
                                oBones));
    // Effects of the death knell.
    AssignCommand(oBones, DelayCommand(WAIL_TIME, DeathKnell()));
}


// Attempts to daze or stun those nearby the demilich when it is destroyed.
// Executed by the Pile of Bones.
void DeathKnell()
{
    // Create the effects.
    // (Extraordinary because they represent being overwhelmed, not a magical effect.)
    effect eStun = ExtraordinaryEffect(EffectStunned());
    effect eDaze = ExtraordinaryEffect(EffectDazed());
    // Get the save DC.
    int nDC = GetLocalInt(OBJECT_SELF, ZEP_DEMI_LOCAL_HITDICE);

    // Prepare to loop through all nearby creatures.
    int nCount = 1;
    object oCreature = GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, OBJECT_SELF, nCount);
    float fDistance = GetDistanceToObject(oCreature);

    // Start with those relatively close. These will be stunned or dazed.
    while ( 0.0 <= fDistance  &&  fDistance <= RADIUS_SIZE_LARGE )
    {
        // Allow a Will save.
        if ( !WillSave(oCreature, nDC) )
            // Stun them for 1-4 rounds.
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eStun, oCreature, RoundsToSeconds(d4()));
        else
            // Daze them for 1-2 rounds.
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eDaze, oCreature, RoundsToSeconds(d2()));

        // Update the loop.
        oCreature = GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, OBJECT_SELF, ++nCount);
        fDistance = GetDistanceToObject(oCreature);
    }

    // Continue with those a bit further away. These might be dazed.
    while ( 0.0 <= fDistance  &&  fDistance <= RADIUS_SIZE_COLOSSAL )
    {
        // Allow a Will save.
        if ( !WillSave(oCreature, nDC) )
            // Daze them for 1-4 rounds.
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eDaze, oCreature, RoundsToSeconds(d4()));

        // Update the loop.
        oCreature = GetNearestCreature(CREATURE_TYPE_IS_ALIVE, TRUE, OBJECT_SELF, ++nCount);
        fDistance = GetDistanceToObject(oCreature);
    }
}

