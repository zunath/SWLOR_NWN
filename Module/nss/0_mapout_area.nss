//::///////////////////////////////////////////////
//:: Reveal Map on Area Load
//:: reveal_map.nss
//:: Copyright (c) 2001 Bioware Corp.
//:: Created By: Phillip Alex Haddox
//:: Created On: August 20, 2002
//:://////////////////////////////////////////////
//
// Put this script on the OnEnter Event script in
// the area properties. It will completely reveal
// the mini-map to all players as they zone in.

void main() {
    object oPC = GetEnteringObject();

    ExploreAreaForPlayer(OBJECT_SELF, oPC);

    ExecuteScript("tlj_area_enter", OBJECT_SELF);
}
