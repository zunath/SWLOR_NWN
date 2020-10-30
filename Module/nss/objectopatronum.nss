#include "in_g_cutscene"

 void main()
 {

 // Retrieving invisible object.
  object nObj = GetNearestObjectByTag("InvisibleObject1");
  location nLoc = GetLocation(nObj); //Get invisible object location

  // Get character
  object oPC = GetEnteringObject();
  location nLac = GetLocation(oPC); //Get creature location

  // Copying the character.
     if (!GetIsPC(oPC))
     { return; }


    //SetLocalInt(oPC,"CLONED",1);  //Mark creature
    // object oFan = CopyObject(oPC,nLoc,OBJECT_INVALID,""); //Clone creature in his location
    //effect nEff = EffectVisualEffect(VFX_FNF_SUMMON_CELESTIAL,FALSE); //Make a visual effect
    //ApplyEffectToObject(DURATION_TYPE_INSTANT,nEff,nObj); //Apply effect in invisible object - this effect NOT VISIBLE, but the sounds and move in screen continue
   // ExecuteScript("dead2",oFan); //Execute special script in new clone creature
    //AssignCommand(oFan,ActionAttack(oPC)); //Make this new creature attack the last creature in the sphere - need, because the new creature is a PC too
    //SetLocalInt(oFan,"HATE",1); //Mark a special variable in new creature
    //SetLocalInt(oFan,"CLONED",1); //Mark new creature avoid invisible object clone her too
    //object oAr = GetArea(OBJECT_SELF); //Object my area



    // Find actors
    SetLocalObject(GetModule(),"cutsceneviewer",oPC);




    // Find the direction the player's facing in at the start of the scene
    float fFace = GetFacing(oPC);


    // Start cutscene, fade in
    GestaltStartCutscene    (oPC,"mycutscene",TRUE,TRUE,TRUE,TRUE,2);
    GestaltCameraFade       (0.0,  oPC,   FADE_IN,FADE_SPEED_MEDIUM);


    // Player delivers lines, then turns to face NPC
    GestaltActionSpeak      (2.0,  oPC,
                            "The familiar void of space...",
                            ANIMATION_NONE,0.0);

    GestaltActionSpeak      (8.0,  oPC,
                            "*A entire galaxy surrounds you, stars eternally shimmering in the black abyss beyond. All around you, a thick unmoving purple nebulous mass.*",
                            ANIMATION_NONE,0.0);

    GestaltActionSpeak      (12.0,  oPC,
                            "*While you are capable of breathing, you find that it is hauntingly cold here.*",
                            ANIMATION_NONE,0.0);

    GestaltActionSpeak      (16.0,  oPC,
                            "*A voice presses into your mind.* Have you come seeking knowledge? Power? Or perhaps you are simply trying to find yourself in all the chaos. There will be choices, traveler. All answers lead to a truth, but is it your truth? We will find out together. Good luck.",
                            ANIMATION_NONE,0.0);






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
    GestaltStopCutscene     (20.0, oPC);
}
