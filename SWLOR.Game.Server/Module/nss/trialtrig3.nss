#include "in_g_cutscene"


void main()
{
    // Get character
    object oPC = GetEnteringObject();
    location nLac = GetLocation(oPC); //Get creature location

    // checking local flag so they can't spam clones.
    int jediF3 = GetLocalInt(oPC, "jediFlag3");
    if (jediF3 == 3)
        return;

    if (!GetIsPC(oPC))
        return;
    object oArea = GetArea(OBJECT_SELF);

    // Find actors
    SetLocalObject(GetModule(),"cutsceneviewer",oPC);
    // Find the direction the player's facing in at the start of the scene
    float fFace = GetFacing(oPC);

    // Start cutscene, fade in
    GestaltStartCutscene(oPC,"mycutscene", TRUE, TRUE, TRUE, TRUE, 2);

    GestaltSpeak(2.0, oPC,"I wonder what you will do when face with adversity with no option to run. Will you fight? Or will your surrender yourself to your own mind?");
    GestaltSpeak(6.0, oPC,"Choose wisely.");
    GestaltSpeak(12.0, oPC,"*You feel the sensation of a hilt in your hand followed by the ignition of the blade itself. Then your eyes fall upon yourself in the opposite side of the room eyeing you with sheer malice. You have a mere few seconds before the image of yourself charges at you with its own lightsaber held high.");

    // Spawn thief
    object nObj10 = GetNearestObjectByTag("InvisibleObject24");
    location nLoc10 = GetLocation(nObj10); //Get invisible object location


    object oFan24 = CopyObject(oPC,nLoc10,OBJECT_INVALID,"rivalClone"); //Clone creature in his location
    effect nEff24 = EffectVisualEffect(560, FALSE); //Make a visual effect
    ApplyEffectToObject(DURATION_TYPE_PERMANENT, nEff24,oFan24); //Apply effect in invisible object - this effect NOT VISIBLE, but the sounds and move in screen continue
    AssignCommand(oFan24,SetFacing(90.0));
    int nSlot;
    object oItem = GetFirstItemInInventory(oFan24);
    while ( oItem != OBJECT_INVALID ) {
        DestroyObject(oItem);
        oItem = GetNextItemInInventory(oFan24);
    }
    // Destroy equipped items.
    for ( nSlot = 0; nSlot < NUM_INVENTORY_SLOTS; ++nSlot )
        DestroyObject(GetItemInSlot(nSlot, oFan24));

    // Remove all gold.
    TakeGoldFromCreature(GetGold(oFan24), oFan24, TRUE);

    CreateItemOnObject("Robe_Dark",oFan24, 1, "darkRobe");
    object darkRobe = GetItemPossessedBy(oFan24,"darkRobe");
    AssignCommand(oFan24,ActionEquipItem(darkRobe, INVENTORY_SLOT_CHEST));



    CreateItemOnObject("lightsaber_npc_r",oFan24, 1, "lightsaber_b3");
    object lightsaber2 = GetItemPossessedBy(oFan24,"lightsaber_b3");
    AssignCommand(oFan24,ActionEquipItem(lightsaber2, INVENTORY_SLOT_RIGHTHAND));

    ChangeToStandardFaction(oFan24,0);

    DelayCommand(15.0,AssignCommand(oFan24,ActionAttack(oPC)));

    GestaltStopCutscene(15.0, oPC);

    CreateItemOnObject("lightsaber_b",oPC, 1, "lightsaber_b2");
    object lightsaber = GetItemPossessedBy(oPC,"lightsaber_b2");
    AssignCommand(oPC,ActionEquipItem(lightsaber, INVENTORY_SLOT_RIGHTHAND));`

    MusicBackgroundChangeDay(oArea, 462);
    MusicBackgroundChangeNight(oArea, 462);

    SetLocalInt(oPC, "jediFlag3", 3);
}
