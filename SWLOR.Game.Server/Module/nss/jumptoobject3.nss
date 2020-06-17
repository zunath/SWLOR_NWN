#include "in_g_cutscene"

void main()
{

    object oArea = GetArea(OBJECT_SELF);
    object oPC = GetLastUsedBy();
    object oFan24 = GetNearestObjectByTag("rivalClone");
    int cloneDeathFlag = FALSE;

    if (GetIsDead(oFan24) == TRUE)
    {
        string itemTag;
        int nSlot;
        object oItem = GetFirstItemInInventory(oPC);
        while ( oItem != OBJECT_INVALID )
        {
            itemTag = GetTag(oItem);

            if (itemTag == "lightsaber_b2" || itemTag == "darkRobe" || itemTag == "lightsaber_b3" )
            {
                DestroyObject(oItem);}
                oItem = GetNextItemInInventory(oPC);
            }

            // Destroy equipped items.
            oItem = GetItemInSlot(5, oPC);
            itemTag = GetTag(oItem);
            if (itemTag == "lightsaber_b2" || itemTag == "darkRobe" || itemTag == "lightsaber_b3")
            {
                DestroyObject(oItem);
            }
            oItem = GetItemInSlot(4, oPC);
            itemTag = GetTag(oItem);
            if (itemTag == "lightsaber_b2" || itemTag == "darkRobe" || itemTag == "lightsaber_b3")
            {
                DestroyObject(oItem);
            }

            // Find actors
            SetLocalObject(GetModule(),"cutsceneviewer",oPC);
            // Find the direction the player's facing in at the start of the scene
            float fFace = GetFacing(oPC);

            // Start cutscene, fade in
            GestaltStartCutscene    (oPC,"mycutscene",TRUE,TRUE,TRUE,TRUE,2);

            GestaltPlayMusic(0.0,oArea, TRUE,391, 34.5);
            GestaltSpeak(2.0, oPC,"*A voice once again resonates within your mind.* You are more resilient than I originally anticipated. How interesting.");
            GestaltSpeak(8.0, oPC,"Rarely will we find ourselves in situations where running is an option. Especially in these dire times. You have a fighting spirit, traveler. You'll need it.");

            GestaltStopCutscene(17.0, oPC);

            oItem = GetNextItemInInventory(oPC);
        }


        location locSpellTarget = GetLocation(OBJECT_SELF);

        object portal = GetNearestObjectByTag("WP_jumpTarget3");
        DelayCommand(18.0,ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_UNSUMMON), locSpellTarget));

        DelayCommand(19.0,AssignCommand(oPC,ActionJumpToObject(portal)));
        DelayCommand(34.5,MusicBackgroundChangeDay(oArea,469));
        MusicBackgroundChangeNight(oArea,469);
}
