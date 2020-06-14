#include "in_g_cutscene"

 void main()
 {




 // Retrieving invisible object.
  object nObj = GetNearestObjectByTag("InvisibleObject1");
  location nLoc = GetLocation(nObj); //Get invisible object location
  object nObj2 = GetNearestObjectByTag("InvisibleObject2");
  location nLoc2 = GetLocation(nObj2); //Get invisible object location
  object nObj3 = GetNearestObjectByTag("InvisibleObject3");
  location nLoc3 = GetLocation(nObj3); //Get invisible object location
  object nObj4 = GetNearestObjectByTag("InvisibleObject4");
  location nLoc4 = GetLocation(nObj4); //Get invisible object location

  object nObj5 = GetNearestObjectByTag("InvisibleObject5");
  location nLoc5 = GetLocation(nObj5); //Get invisible object location
  object nObj6 = GetNearestObjectByTag("InvisibleObject6");
  location nLoc6 = GetLocation(nObj6); //Get invisible object location
  object nObj7 = GetNearestObjectByTag("InvisibleObject7");
  location nLoc7 = GetLocation(nObj7); //Get invisible object location
  object nObj8 = GetNearestObjectByTag("InvisibleObject8");
  location nLoc8 = GetLocation(nObj8); //Get invisible object location
  object nObj9 = GetNearestObjectByTag("InvisibleObject9");
  location nLoc9 = GetLocation(nObj9); //Get invisible object location


  // Get character
  object oPC = GetEnteringObject();
  location nLac = GetLocation(oPC); //Get creature location

 // checking local flag so they can't spam clones.
  int jediF = GetLocalInt(oPC, "jediFlag");
  if (jediF == 1)
  return;

     if (!GetIsPC(oPC))
     { return; }

    AssignCommand(oPC, SpeakString("*The voice again surges through your mind.* A choice is set before you, traveler. What will you do with your power? Defend those who cannot defend themselves? Or bring your own order to the galaxy?"));


     object oFan = CopyObject(oPC,nLoc,OBJECT_INVALID,"oFan1"); //Clone creature in his location
     effect nEff = EffectVisualEffect(529 ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff,oFan); //Apply effect in invisible object - this effect NOT VISIBLE, but the sounds and move in screen continue
     CreateItemOnObject("lightsaber_b",oFan, 1, "lightsaber_b2");
     object lightsaber = GetItemPossessedBy(oFan,"lightsaber_b2");
     CreateItemOnObject("HasteJedi2",oFan, 1, "HasteJedi2");
     object haste = GetItemPossessedBy(oFan,"HasteJedi2");
    AssignCommand(oFan,ActionEquipItem(haste,INVENTORY_SLOT_NECK));
    AssignCommand(oFan,ActionEquipItem(lightsaber,INVENTORY_SLOT_RIGHTHAND));
     AssignCommand(oFan,SetFacing(0.0));
     SetPlotFlag(oFan,1);


     object oFan9 = CopyObject(oPC,nLoc9,OBJECT_INVALID,"oFan2"); //Clone creature in his location
     effect nEff9 = EffectVisualEffect(530 ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff9,oFan9);
     CreateItemOnObject("lightsaber_npc_r",oFan9, 1, "lightsaber_b2");
     object lightsaber2 = GetItemPossessedBy(oFan9,"lightsaber_b2");
     CreateItemOnObject("HasteJedi2",oFan9, 1, "HasteJedi2");
     object haste2 = GetItemPossessedBy(oFan9,"HasteJedi2");
     AssignCommand(oFan9,ActionEquipItem(haste2,INVENTORY_SLOT_NECK));
     AssignCommand(oFan9,ActionEquipItem(lightsaber2,INVENTORY_SLOT_RIGHTHAND));
       AssignCommand(oFan9,SetFacing(180.0));
       SetPlotFlag(oFan9,1);
     AssignCommand(oFan9,ActionAttack(oFan, 0));

     AssignCommand(oFan,ActionAttack(oFan9, 0));





     object oFan2 = CopyObject(oPC,nLoc2,OBJECT_INVALID,"oFan3"); //Clone creature in his location
     effect nEff2 = EffectVisualEffect(529 ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff2,oFan2);
       AssignCommand(oFan2,ActionPlayAnimation(9,1.0,10000.0));
       AssignCommand(oFan2,SetFacing(0.0));
            SetPlotFlag(oFan2,1);

     object oFan3 = CopyObject(oPC,nLoc3,OBJECT_INVALID,"oFan4"); //Clone creature in his location
     effect nEff3 = EffectVisualEffect(529 ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff3,oFan3);
       AssignCommand(oFan3,ActionPlayAnimation(9,1.0,10000.0));
       AssignCommand(oFan3,SetFacing(0.0));
            SetPlotFlag(oFan3,1);

     object oFan4 = CopyObject(oPC,nLoc4,OBJECT_INVALID,"oFan5"); //Clone creature in his location
     effect nEff4 = EffectVisualEffect(529 ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff4,oFan4);
       AssignCommand(oFan4,ActionPlayAnimation(9,1.0,10000.0));
       AssignCommand(oFan4,SetFacing(0.0));
            SetPlotFlag(oFan4,1);

     // Bad guy spawn
     object oFan5 = CopyObject(oPC,nLoc5,OBJECT_INVALID,"oFan6"); //Clone creature in his location
     effect nEff5 = EffectVisualEffect(530,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff5,oFan5); //Apply effect in invisible object - this effect NOT VISIBLE, but the sounds and move in screen continue
     AssignCommand(oFan5,ActionPlayAnimation(18,1.0,10000.0));
     AssignCommand(oFan5,SetFacing(0.0));
          SetPlotFlag(oFan5,1);


     object oFan6 = CopyObject(oPC,nLoc6,OBJECT_INVALID,"oFan7"); //Clone creature in his location
     effect nEff6 = EffectVisualEffect(530,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff6,oFan6);
       AssignCommand(oFan6,ActionPlayAnimation(9,1.0,10000.0));
       AssignCommand(oFan6,SetFacing(180.0));
       SetPlotFlag(oFan6,1);

     object oFan7 = CopyObject(oPC,nLoc7,OBJECT_INVALID,"oFan8"); //Clone creature in his location
     effect nEff7 = EffectVisualEffect(530 ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff7,oFan7);
       AssignCommand(oFan7,ActionPlayAnimation(4,1.0,10000.0));
       AssignCommand(oFan7,SetFacing(180.0));
       SetPlotFlag(oFan7,1);

     object oFan8 = CopyObject(oPC,nLoc8,OBJECT_INVALID,"oFan9"); //Clone creature in his location
     effect nEff8 = EffectVisualEffect(530,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff8,oFan8);
       AssignCommand(oFan8,ActionPlayAnimation(20,1.0,10000.0));
       AssignCommand(oFan8,SetFacing(180.0));
        SetPlotFlag(oFan8,1);

     effect eBeam = EffectBeam(73, oFan5, BODY_NODE_HAND);
     ApplyEffectToObject(DURATION_TYPE_PERMANENT, eBeam, oFan8);


      SetPlotFlag(oFan9,1);


   // ExecuteScript("dead2",oFan); //Execute special script in new clone creature
    //AssignCommand(oFan,ActionAttack(oPC)); //Make this new creature attack the last creature in the sphere - need, because the new creature is a PC too
    //SetLocalInt(oFan,"HATE",1); //Mark a special variable in new creature
    //SetLocalInt(oFan,"CLONED",1); //Mark new creature avoid invisible object clone her too
    //object oAr = GetArea(OBJECT_SELF); //Object my area



    // Find actors
  /*  SetLocalObject(GetModule(),"cutsceneviewer",oPC);




    // Find the direction the player's facing in at the start of the scene
    float fFace = GetFacing(oPC);


    // Start cutscene, fade in
    GestaltStartCutscene    (oPC,"mycutscene",TRUE,TRUE,TRUE,TRUE,2);
    GestaltCameraFade       (0.0,  oPC,   FADE_IN,FADE_SPEED_MEDIUM);


    // Player delivers lines, then turns to face NPC
    GestaltActionSpeak      (1.0,  oPC,
                            "The familiar void of space...",
                            ANIMATION_NONE,4.0);

    GestaltActionSpeak      (5.0,  oPC,
                            "A entire galaxy surrounds you, stars eternally shimmering in the black abyss beyond. All around you, a thick unmoving purple nebulous mass.",
                            ANIMATION_NONE,4.0);

    GestaltActionSpeak      (5.0,  oPC,
                            "While you are capable of breathing, you find that it is hauntingly cold here.",
                            ANIMATION_NONE,4.0);


   // GestaltActionAnimate    (5.0,  oPC,   NORMAL,2.0);
   // GestaltFace             (9.2,  oPC,   0.0,2,oFan);


    // Move the NPC towards the player and congratulate him
    //GestaltActionMove       (5.0,  oFan,  oPC,FALSE,1.0,4.0);

    //GestaltActionSpeak      (9.0,  oFan,
                            //"That was quite a performance!",
                            //Animation.Get_Mid,2.0);


    // Fade to black and remove the NPC
    //GestaltCameraFade       (12.0, oPC,   FADE_CROSS,FADE_SPEED_MEDIUM,2.0);
   //GestaltDestroy          (13.0, oFan);


    // Camera movements
    GestaltCameraMove       (0.0,
                            0.0,5.0,180.0,
                            180.0,5.0,180.0,
                            10.0,30.0,oPC);

    GestaltCameraMove       (8.0,
                            180.0,5.0,180.0,
                            0.0,5.0,180.0,
                            10.0,30.0,oPC,1);


    // End cutscene
    GestaltStopCutscene     (20.0, oPC); */


SetLocalInt(oPC, "jediFlag", 1);
}
