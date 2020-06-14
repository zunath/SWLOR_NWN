#include "in_g_cutscene"

 void main()
 {

 // Retrieving invisible object.
  object nObj = GetNearestObjectByTag("InvisibleObject10");
  location nLoc = GetLocation(nObj); //Get invisible object location
  object nObj2 = GetNearestObjectByTag("InvisibleObject11");
  location nLoc2 = GetLocation(nObj2); //Get invisible object location
  object nObj3 = GetNearestObjectByTag("InvisibleObject12");
  location nLoc3 = GetLocation(nObj3); //Get invisible object location
  object nObj4 = GetNearestObjectByTag("InvisibleObject13");
  location nLoc4 = GetLocation(nObj4); //Get invisible object location




  object nObj5 = GetNearestObjectByTag("InvisibleObject14");
  location nLoc5 = GetLocation(nObj5); //Get invisible object location
  object nObj6 = GetNearestObjectByTag("InvisibleObject15");
  location nLoc6 = GetLocation(nObj6); //Get invisible object location
  object nObj7 = GetNearestObjectByTag("InvisibleObject16");
  location nLoc7 = GetLocation(nObj7); //Get invisible object location
  object nObj8 = GetNearestObjectByTag("InvisibleObject17");
  location nLoc8 = GetLocation(nObj8); //Get invisible object location
  object nObj9 = GetNearestObjectByTag("InvisibleObject18");
  location nLoc9 = GetLocation(nObj9); //Get invisible object location
  object nObj11 = GetNearestObjectByTag("InvisibleObject20");
  location nLoc11 = GetLocation(nObj11); //Get invisible object location
  object nObj12 = GetNearestObjectByTag("InvisibleObject21");
  location nLoc12 = GetLocation(nObj12); //Get invisible object location
  object nObj13 = GetNearestObjectByTag("InvisibleObject22");
  location nLoc13 = GetLocation(nObj13); //Get invisible object location
    object nObj14 = GetNearestObjectByTag("InvisibleObject23");
  location nLoc14 = GetLocation(nObj14); //Get invisible object location



  // Get character
  object oPC = GetEnteringObject();
  location nLac = GetLocation(oPC); //Get creature location

 // checking local flag so they can't spam clones.
  int jediF2 = GetLocalInt(oPC, "jediFlag2");
  if (jediF2 == 2)
  return;

     if (!GetIsPC(oPC))
     { return; }


     object oFan = CopyObject(oPC,nLoc,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff = EffectVisualEffect(552  ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff,oFan); //Apply effect in invisible object - this effect NOT VISIBLE, but the sounds and move in screen continue
     AssignCommand(oFan,SetFacing(0.0));



     object oFan9 = CopyObject(oPC,nLoc9,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff9 = EffectVisualEffect(552 ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff9,oFan9);
     AssignCommand(oFan9,SetFacing(270.0));






     object oFan2 = CopyObject(oPC,nLoc2,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff2 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff2,oFan2);
     AssignCommand(oFan2,SetFacing(270.0));

     object oFan3 = CopyObject(oPC,nLoc3,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff3 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff3,oFan3);
     AssignCommand(oFan3,SetFacing(270.0));


     object oFan4 = CopyObject(oPC,nLoc4,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff4 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff4,oFan4);
       AssignCommand(oFan4,SetFacing(270.0));

     // Bad guy spawn
     object oFan5 = CopyObject(oPC,nLoc5,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff5 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff5,oFan5); //Apply effect in invisible object - this effect NOT VISIBLE, but the sounds and move in screen continue
     AssignCommand(oFan5,SetFacing(270.0));


     object oFan6 = CopyObject(oPC,nLoc6,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff6 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff6,oFan6);
       AssignCommand(oFan6,SetFacing(270.0));

     object oFan7 = CopyObject(oPC,nLoc7,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff7 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff7,oFan7);
       AssignCommand(oFan7,SetFacing(270.0));

     object oFan8 = CopyObject(oPC,nLoc8,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff8 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff8,oFan8);
       AssignCommand(oFan8,SetFacing(270.0));




   object nObj19 = GetNearestObjectByTag("InvisibleObject19");
   location nLoc19 = GetLocation(nObj19); //Get invisible object location

    // Find actors
    SetLocalObject(GetModule(),"cutsceneviewer",oPC);
    // Find the direction the player's facing in at the start of the scene
    float fFace = GetFacing(oPC);


    // Start cutscene, fade in
    GestaltStartCutscene    (oPC,"mycutscene",TRUE,TRUE,TRUE,TRUE,2);


     // Spawn thief
     object nObj10 = GetNearestObjectByTag("InvisibleObject10");
     location nLoc10 = GetLocation(nObj10); //Get invisible object location


     object oFan10 = CopyObject(oPC,nLac,OBJECT_INVALID,""); //Clone creature in his location
     effect nEff10 = EffectVisualEffect(552  ,FALSE); //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff,oFan10); //Apply effect in invisible object - this effect NOT VISIBLE, but the sounds and move in screen continue
     AssignCommand(oFan10,SetFacing(0.0));
     AssignCommand(oFan10,ActionPlayAnimation(106 ,1.0));



     GestaltSpeak(2.0, oFan10,"*A man steals from you, then sprints into the crowd.*");
     GestaltActionMove(2.0,oFan10,nObj19, 1, 2.0, 0.0, "InvisibleObject19", TRUE);


    // End cutscene
    GestaltStopCutscene(5.0, oPC);

    DestroyObject(oFan,5.0);
    DestroyObject(oFan2,5.0);
    DestroyObject(oFan3,5.0);
    DestroyObject(oFan4,5.0);
    DestroyObject(oFan5,5.0);
    DestroyObject(oFan6,5.0);
    DestroyObject(oFan7,5.0);
    DestroyObject(oFan8,5.0);
    DestroyObject(oFan9,5.0);
    DestroyObject(oFan10,5.0);

    AssignCommand(oPC, SpeakString("*The voice again surges through your mind.* Your power over the galaxy will allow you to decide the fate of others. What would you do if the life of another was in your hands?"));




     object oFan11 = CopyObject(oPC,nLoc11,OBJECT_INVALID,"oFan11"); //Clone creature in his location
     effect nEff11 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff11,oFan11);
       AssignCommand(oFan11,SetFacing(20.0));
       DelayCommand(6.0,AssignCommand(oFan11,ActionSpeakString("I'm sorry, I'm just trying to feed my family!",0 )));
       AssignCommand(oFan11,ActionPlayAnimation(9 ,1.0,4000.0));
       SetPlotFlag(oFan11,1);

      object oFan12 = CopyObject(oPC,nLoc12,OBJECT_INVALID,"oFan12"); //Clone creature in his location
     effect nEff12 = EffectVisualEffect(529  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff12,oFan12);
       AssignCommand(oFan12,SetFacing(180.0));
      CreateItemOnObject("lightsaber_b",oFan12, 1, "lightsaber_b2");
     object lightsaber = GetItemPossessedBy(oFan12,"lightsaber_b2");
     AssignCommand(oFan12,ActionEquipItem(lightsaber,INVENTORY_SLOT_RIGHTHAND));
     DelayCommand(6.0,AssignCommand(oFan12,ActionSpeakString("You are under arrest for stealing.",0 )));
     SetPlotFlag(oFan12,1);



     object oFan13 = CopyObject(oPC,nLoc13,OBJECT_INVALID,"oFan13"); //Clone creature in his location
     effect nEff13 = EffectVisualEffect(552  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff13,oFan13);
       AssignCommand(oFan13,SetFacing(20.0));
       DelayCommand(3.0,AssignCommand(oFan13,ActionPlayAnimation(17 ,1.0,9999.0)));
       SetPlotFlag(oFan13,1);

      object oFan14 = CopyObject(oPC,nLoc14,OBJECT_INVALID,"oFan14"); //Clone creature in his location
     effect nEff14 = EffectVisualEffect(529  ,FALSE);  //Make a visual effect
     ApplyEffectToObject(DURATION_TYPE_PERMANENT,nEff14,oFan14);
       AssignCommand(oFan14,SetFacing(180.0));
     CreateItemOnObject("JediTrialRed",oFan14, 1, "lightsaber_b2");
     object lightsaber2 = GetItemPossessedBy(oFan14,"lightsaber_b2");
    AssignCommand(oFan14,ActionEquipItem(lightsaber2,INVENTORY_SLOT_RIGHTHAND));
    SetPlotFlag(oFan14,1);

    DelayCommand(6.0,AssignCommand(oFan14,ActionSpeakString("You were a fool for stealing from me.",0 )));











SetLocalInt(oPC, "jediFlag2", 2);
}
