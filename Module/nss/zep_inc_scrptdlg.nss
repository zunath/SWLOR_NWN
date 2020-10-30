//File name: zep_inc_scrptdlg
//Author: Loki Hakanin
//Usage/Explanation:
//      This include file is intended for use with the NWN CEP.
//      This is an index of the cep.tlk file dialogue TLK file
//      offsets for the various strings of text that are
//      sent to the player, DMs, etc, during the execution
//      of various CEP scripts.

//const int nZEPScriptTLKOffset = 16818216;
//Traps
const int nZEPPitJump =  (16818226);        //df_t0_camopita.nss:        SendMessageToPC(oPC,"You jump to the side and =avoid falling into the pit");
const int nZEPPitFall =  (16818227);        //df_t0_camopita.nss:        SendMessageToPC(oPC,"You fall into the =pit!");
const int nZEPPitClimb = (16818228);        //df_t0_camopita.nss:        DelayCommand(30.0,SendMessageToPC(oPC,"You finally climb out of the =pit"));
const int nZEPDrowning = (16818231);        //trap_fire.nss:            AssignCommand(oPC,SpeakString("Blub!  =Glug!"));
const int nZEPTrapFind = (16818234);        //trap_on_acquire.nss:        SendMessageToPC(oPC,"Nearest Trap: ="+GetName(GetNearestTrapToObject(oPC)));
const int nZEPTrapSearch1 = (16818235);     //trap_on_load.nss:            PrintString("In "+sTag+" Checking for: ="+IntToString(nID)+", found: "+GetTag(oTrap));
const int nZEPTrapSearch2= (16818236);      //trap_on_load.nss:            PrintString("In "+sTag+" Checking for: ="+IntToString(nID)+", found: "+GetTag(oTrap));
const int nZEPTrapSearch3 = (16818237);     //trap_on_load.nss:            PrintString("In "+sTag+" Checking for: ="+IntToString(nID)+", found: "+GetTag(oTrap));
const int nZEPTrapReset = (16818238);       //trap_reset.nss:    SendMessageToPC(oPC,"The lever clicks into its new =postion");

//Item Properties
const int nZEPGenderRestTXT = (16818246);   //zep_inc_main.nss:            SendMessageToPC(oPC,"You cannot equip that =item due to gender differences.");

//Doors
const int nZEPDoorLocked = (16818256);      //zep_openclose.nss:        SpeakString("Locked");

//Demilich Scripts
const int nZEPReturnToLife = (16818266);    //zep_demi_dest.nss:        SendMessageToPC(oVictimCounter,"You feel =disoriented momentarily as your soul returns to its mortal coil.");
const int nZEPCacklingLaugh = (16818267);   //zep_demi_onspell.nss:            =DelayCommand(6.0,SpeakString("Hahhahaha....",TALKVOLUME_SHOUT));
const int nZEPCantBeRaised = (16818268);    //zep_inc_demi.nss:const string ZEP_DEMI_RESLAY_MSG  =3D " jerks upright =and spasms for a few moments before collapsing again.";
const int nZEPNoRaiseExplan = (16818269);   //zep_inc_demi.nss:const string ZEP_DEMI_RESLAY_MSG2 =3D "Until the =demilich's captive souls are freed, its victims cannot be raised.";
const int nZEPDemiRestored = (16818270);    //zep_inc_demi.nss:const string ZEP_DEMI_REGEN_MSG =3D "At last, I am =restored...";
const int nZEPDemiDisturbed = (16818271);   //zep_inc_demi.nss:const string ZEP_DEMI_DIST_MSG =3D "You disturb my =work!";
const int nZEPDemiHavePower = (16818272);   //zep_inc_demi.nss:const string ZEP_DEMI_ONSPELL_MSG =3D "Yes, I sense you =have power...your potential shall be mine!";
const int nZEPDemiVictFree = (16818273);    //zep_inc_demi.nss:const string ZEP_DEMI_FINAL_DEST =3D "With the demilich =destroyed, the souls of its victims are released to their bodies.";

//Marilith Scripts
const int nZEPMarilithDMG = (16818286);     //zep_marilith_end.nss:        SendMessageToPC(oTarget, "You were hit for ="+IntToString(nDamage));

//Rust monster scripts
const int nZEPRustMonBrush = (16818296);    //zep_rust_cmb_end.nss:        SendMessageToPC(oPC,"The Rust Monster's =antennae brush against your "+sItem );
const int nZEPRustButResist1 = (16818297);  //zep_rust_cmb_end.nss:          SendMessageToPC(oPC,"But the "+sItem+" =resists the rust effects!");
const int nZEPRustButResist2 = (16818298);  //zep_rust_cmb_end.nss:          SendMessageToPC(oPC,"But the "+sItem+" =resists the rust effects!");
const int nZEPRustAndDest = (16818299);     //zep_rust_cmb_end.nss:          SendMessageToPC(oPC,"And destroys your ="+sItem+"!");
const int nZEPRustYourTXT = (16818300);     //zep_rust_dmg.nss: "Your "
const int nZEPRustWeapRes = (16818301);     //zep_rust_dmg.nss:                SendMessageToPC(oPC,"Your "+sWeapon+" =resists the rust effects.");
const int nZEPRustWeapDest = (16818302);    //zep_rust_dmg.nss:                SendMessageToPC(oPC,"Your "+sWeapon+" =damages the monster, but is destroyed in the process!");

const int nZEPFoundSecret = (16818316);     //zep_sarcof1use.nss: "You have discovered a secret passage!"

