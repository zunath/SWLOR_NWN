#include "d1_cards_jinc"
void main()
{
    object oPC = GetPCSpeaker();
    int nSelection = GetMenuSelection();

    // select deck based on menu choice
    object oPlayer = GetMenuObjectValue (nSelection - 1);

    object oArea = GetArea(oPlayer);
    object oCentre = GetGameCentre(oArea);
    vector vStart = GetPosition (oCentre);
    vector vDestination = Vector (vStart.x - 25.0, vStart.y, vStart.z);
    location lDestination = Location (oArea, vDestination, 0.0);

    AssignCommand(oPC, ClearAllActions());
    AssignCommand(oPC, JumpToLocation(lDestination));
}
