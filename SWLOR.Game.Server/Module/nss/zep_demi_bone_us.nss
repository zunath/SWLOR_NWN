//::///////////////////////////////////////////////
//:: zep_demi_bone_us
//:: OnUsed event handler for a CEP demilich Pile
//:: of Bones placeable (found under the custom
//:: placeables: "Dungeons->Tombs, Grave Markers ->
//:: Pile of Bones").
//:://////////////////////////////////////////////
/*
    Transfroms a Pile of Bones to a demilich.

    Assumes this is executed by a resting demilich's
    Pile of Bones placeable.

    A different blueprint will be used for regenerating
    demiliches.
*/
//:://////////////////////////////////////////////
//:: Created by: The Krit
//:: Created on: May 10, 2007
//:://////////////////////////////////////////////


#include "zep_inc_demi"


void main()
{
    // Spawn the demilich.
    ZEPDemilichFromBones(OBJECT_SELF, GetLocalString(OBJECT_SELF, ZEP_DEMI_LOCAL_RESREF), TRUE);
    // Destroy the detector.
    DestroyObject(GetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_SOURCE));
    // Destroy the placeables.
    DestroyObject(GetLocalObject(OBJECT_SELF, ZEP_DEMI_LOCAL_AMBIENT));
    DestroyObject(OBJECT_SELF);
}

