
#include "dmfi_db_inc"

const int DMFI_DEFAULT_EMOTES_MUTED = FALSE;

int dmfiInitialize(object oUser)
{
//*************************************INITIALIZATION CODE***************************************
//***************************************RUNS ONE TIME ***************************************

//voice stuff is module wide

    if (GetLocalInt(GetModule(), "dmfi_initialized") != 1)
    {
        SendMessageToPC(oUser,":: DMFI Wands & Widgets System ::");
        int iLoop = 20610;
        string sText;
        while (iLoop < 20680)
        {
            sText = GetDMFIPersistentString("dmfi", "hls" + IntToString(iLoop));
            SetCustomToken(iLoop, sText);
            iLoop++;
        }
        SendMessageToAllDMs("DMFI voice custom tokens initialized.");

        SetLocalInt(GetModule(), "dmfi_initialized", 1);
    }


//remainder of settings are user based

    if ((GetLocalInt(oUser, "dmfi_initialized")!=1) && (GetIsDM(oUser) || GetIsDMPossessed(oUser)))
    {
    //if you have campaign variables set - use those settings
        if (GetDMFIPersistentInt("dmfi", "Settings", oUser)==1)
        {
            FloatingTextStringOnCreature("DMFI Settings Restored", oUser, FALSE);
            // SendMessageToPC(oUser, "DMFI Settings Restored");

            int n = GetDMFIPersistentInt("dmfi", "dmfi_alignshift", oUser);
            SetCustomToken(20781, IntToString(n));
            SetLocalInt(oUser, "dmfi_alignshift", n);
            SendMessageToPC(oUser, "Settings: Alignment shift: "+IntToString(n));

            n = GetDMFIPersistentInt("dmfi", "dmfi_safe_factions", oUser);
            SetLocalInt(oUser, "dmfi_safe_factions", n);
            SendMessageToPC(oUser, "Settings: Factions (1 is DMFI Safe Faction): "+IntToString(n));

            n = GetDMFIPersistentInt("dmfi", "dmfi_damagemodifier", oUser);
            SetLocalInt(oUser, "dmfi_damagemodifier",n);
            SendMessageToPC(oUser, "Settings: Damage Modifier: "+IntToString(n));

            n = GetDMFIPersistentInt("dmfi","dmfi_buff_party",oUser);
            SetLocalInt(oUser, "dmfi_buff_party", n);
            if (n==1)
                SetCustomToken(20783, "Party");
            else
                SetCustomToken(20783, "Single Target");

            SendMessageToPC(oUser, "Settings: Buff Party (1 is Party): "+IntToString(n));

            string sLevel = GetDMFIPersistentString("dmfi", "dmfi_buff_level", oUser);
            SetCustomToken(20782, sLevel);
            SetLocalString(oUser, "dmfi_buff_level", sLevel);
            SendMessageToPC(oUser, "Settings: Buff Level: "+ sLevel);

            n = GetDMFIPersistentInt("dmfi", "dmfi_dicebag", oUser);
            SetLocalInt(oUser, "dmfi_dicebag", n);

            string sText;
            if (n==0)
            {
                SetCustomToken(20681, "Private");
                sText = "Private";
            }
            else  if (n==1)
            {
                SetCustomToken(20681, "Global");
                sText = "Global";
            }
            else if (n==2)
            {
                SetCustomToken(20681, "Local");
                sText = "Local";
            }
            else if (n==3)
            {
                SetCustomToken(20681, "DM Only");
                sText = "DM Only";
            }
            SendMessageToPC(oUser, "Settings: Dicebag Reporting: "+sText);

            n = GetDMFIPersistentInt("dmfi", "dmfi_dice_no_animate", oUser);
            SetLocalInt(oUser, "dmfi_dice_no_animate", n);
            SendMessageToPC(oUser, "Settings: Roll Animations (1 is OFF): "+IntToString(n));

            float f = GetDMFIPersistentFloat("dmfi", "dmfi_reputation", oUser);
            SetLocalFloat(oUser, "dmfi_reputation", f);
            SendMessageToPC(oUser, "Settings: Reputation Adjustment: "+FloatToString(f));

            f = GetDMFIPersistentFloat("dmfi", "dmfi_effectduration", oUser);
            SetLocalFloat(oUser, "dmfi_effectduration", f);
            SendMessageToPC(oUser, "Settings: Effect Duration: "+FloatToString(f));

            f = GetDMFIPersistentFloat("dmfi", "dmfi_sound_delay", oUser);
            SetLocalFloat(oUser, "dmfi_sound_delay", f);
            SendMessageToPC(oUser, "Settings: Sound Delay: "+FloatToString(f));

            f = GetDMFIPersistentFloat("dmfi", "dmfi_beamduration", oUser);
            SetLocalFloat(oUser, "dmfi_beamduration", f);
            SendMessageToPC(oUser, "Settings: Beam Duration: "+FloatToString(f));

            f = GetDMFIPersistentFloat("dmfi", "dmfi_stunduration", oUser);
            SetLocalFloat(oUser, "dmfi_stunduration", f);
            SendMessageToPC(oUser, "Settings: Stun Duration: "+FloatToString(f));

            f = GetDMFIPersistentFloat("dmfi", "dmfi_saveamount", oUser);
            SetLocalFloat(oUser, "dmfi_saveamount", f);
            SendMessageToPC(oUser, "Settings: Save Adjustment: "+FloatToString(f));

            f = GetDMFIPersistentFloat("dmfi", "dmfi_effectdelay", oUser);
            SetLocalFloat(oUser, "dmfi_effectdelay", f);
            SendMessageToPC(oUser, "Settings: Effect Delay: "+FloatToString(f));


        }
        else
        {
            FloatingTextStringOnCreature("DMFI Default Settings Initialized", oUser, FALSE);
            // SendMessageToPC(oUser, "DMFI Default Settings Initialized");

            //Setting FOUR campaign variables so 1st use will be slow.
            //Recommend initializing your preferences with no players or
            //while there is NO fighting.
            // SetLocalInt(oUser, "dmfi_initialized", 1);
            SetDMFIPersistentInt("dmfi", "Settings", 1, oUser);

            SetCustomToken(20781, "5");
            SetLocalInt(oUser, "dmfi_alignshift", 5);
            SetDMFIPersistentInt("dmfi", "dmfi_alignshift", 5, oUser);
            SendMessageToPC(oUser, "Settings: Alignment shift: 5");

            SetCustomToken(20783, "Single Target");
            SetLocalInt(oUser, "dmfi_buff_party", 0);
            SetDMFIPersistentInt("dmfi", "dmfi_buff_party", 0, oUser);
            SendMessageToPC(oUser, "Settings: Buff set to Single Target: ");

            SetCustomToken(20782, "Low");
            SetLocalString(oUser, "dmfi_buff_level", "LOW");
            SetDMFIPersistentString("dmfi", "dmfi_buff_level", "LOW", oUser);
            SendMessageToPC(oUser, "Settings: Buff Level set to LOW: ");

            SetLocalInt(oUser, "dmfi_dicebag", 0);
            SetCustomToken(20681, "Private");
            SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 0, oUser);
            SendMessageToPC(oUser, "Settings: Dicebag Rolls set to PRIVATE");

            SetLocalInt(oUser, "", 0);
            SetDMFIPersistentInt("dmfi", "dmfi_safe_factions", 0, oUser);
            SendMessageToPC(oUser, "Settings: Factions set to BW base behavior");

            SetLocalFloat(oUser, "dmfi_reputation", 5.0);
            SetCustomToken(20784, "5");
            SetDMFIPersistentFloat("dmfi", "dmfi_reputation", 5.0, oUser);
            SendMessageToPC(oUser, "Settings: Reputation adjustment: 5");

            SetDMFIPersistentFloat("dmfi", "dmfi_effectduration", 60.0, oUser);
            SetLocalFloat(oUser, "dmfi_effectduration", 60.0);
            SetDMFIPersistentFloat("dmfi", "dmfi_sound_delay", 0.2, oUser);
            SetLocalFloat(oUser, "dmfi_sound_delay", 0.2);
            SetDMFIPersistentFloat("dmfi", "dmfi_beamduration", 5.0, oUser);
            SetLocalFloat(oUser, "dmfi_beamduration", 5.0);
            SetDMFIPersistentFloat("dmfi", "dmfi_stunduration", 1000.0,  oUser);
            SetLocalFloat(oUser, "dmfi_stunduration", 1000.0);
            SetDMFIPersistentFloat("dmfi", "dmfi_saveamount", 5.0, oUser);
            SetLocalFloat(oUser, "dmfi_saveamount", 5.0);
            SetDMFIPersistentFloat("dmfi", "dmfi_effectdelay", 1.0, oUser);
            SetLocalFloat(oUser, "dmfi_effectdelay", 1.0);

            SendMessageToPC(oUser, "Settings: Effect Duration: 60.0");
            SendMessageToPC(oUser, "Settings: Effect Delay: 1.0");
            SendMessageToPC(oUser, "Settings: Beam Duration: 5.0");
            SendMessageToPC(oUser, "Settings: Stun Duration: 1000.0");
            SendMessageToPC(oUser, "Settings: Sound Delay: 0.2");
            SendMessageToPC(oUser, "Settings: Save Adjustment: 5.0");
        }
    }
//********************************END INITIALIZATION***************************

    // inits for all users (DM & player)
    if (GetLocalInt(oUser, "dmfi_initialized")!=1)
    {
        int bEmotesMuted;
        if (GetDMFIPersistentInt("dmfi", "Settings", oUser)==1)
        {
            bEmotesMuted = GetDMFIPersistentInt("dmfi", "dmfi_emotemute", oUser);
        }
        else
        {
            bEmotesMuted = DMFI_DEFAULT_EMOTES_MUTED;
            SetDMFIPersistentInt("dmfi", "dmfi_emotemute", bEmotesMuted, oUser);
        }
        SetLocalInt(oUser, "hls_emotemute", bEmotesMuted);
        SendMessageToPC(oUser, "Settings: Emotes "+(bEmotesMuted ? "muted" : "unmuted"));

        SetLocalObject(oUser, "dmfi_VoiceTarget", OBJECT_INVALID);
        SendMessageToPC(oUser, "Settings: Voice throw target cleared");

        SetLocalObject(oUser, "dmfi_univ_target", oUser);
        SendMessageToPC(oUser, "Settings: Command target set to self");

        SetLocalInt(oUser, "dmfi_initialized", 1);
    }

    return TRUE; // no errors detected
}

