
// dmfi_univ_listen

// template: dmfi_getln_cbtpl
// triggered from OnPlayerChat callback

#include "dmfi_db_inc"

void main()
{
    int nVolume = GetPCChatVolume();
    object oShouter = GetPCChatSpeaker();
    string sSaid = GetPCChatMessage();

// SendMessageToPC(GetFirstPC(), "ENTER dmfi_univ_listen: speaker=" + GetName(oShouter) + ", channel=" + IntToString(nVolume) + ", said=" + sSaid);
    // first, lets deal with a getln event
    string getln_mode = GetLocalString(OBJECT_SELF, "dmfi_getln_mode");
    if (getln_mode == "name")
    {
        if (sSaid != ".")
        {
            object oTarget = GetLocalObject(oShouter, "dmfi_univ_target");
            SetName(oTarget, sSaid);
        }
        DeleteLocalString(OBJECT_SELF, "dmfi_getln_mode");
    }
    else if (getln_mode == "desc")
    {
        if (sSaid != ".")
        {
            object oTarget = GetLocalObject(oShouter, "dmfi_univ_target");
            SetDescription(oTarget, sSaid);
        }
        DeleteLocalString(OBJECT_SELF, "dmfi_getln_mode");
    }
    else
    {
        // you may wish to define an "abort" input message, such as a line
        // containing a single period:
        if (sSaid != ".")
        {
            // put your code here to process the input line (in sSaid)

            if (GetIsDM(oShouter)) SetLocalInt(GetModule(), "dmfi_Admin" + GetPCPublicCDKey(oShouter), 1);
            if (GetIsDMPossessed(oShouter)) SetLocalObject(GetMaster(oShouter), "dmfi_familiar", oShouter);

            object oTarget = GetLocalObject(oShouter, "dmfi_VoiceTarget");
            object oMaster = OBJECT_INVALID;
            if (GetIsObjectValid(oTarget)) oMaster = oShouter;

            int iPhrase = GetLocalInt(oShouter, "hls_EditPhrase");

            object oSummon;

            if (GetIsObjectValid(oShouter) && GetIsDM(oShouter))
            {
                if (GetTag(OBJECT_SELF) == "dmfi_setting" && GetLocalString(oShouter, "EffectSetting") != "")
                {
                    string sPhrase = GetLocalString(oShouter, "EffectSetting");
                    SetLocalFloat(oShouter, sPhrase, StringToFloat(sSaid));
                    SetDMFIPersistentFloat("dmfi", sPhrase, StringToFloat(sSaid), oShouter);
                    DeleteLocalString(oShouter, "EffectSetting");
                    DelayCommand(0.5, ActionSpeakString("The setting " + sPhrase + " has been changed to " + FloatToString(GetLocalFloat(oShouter, sPhrase))));
                    DelayCommand(1.5, DestroyObject(OBJECT_SELF));
                }
            }

            if (GetIsObjectValid(oShouter) && GetIsPC(oShouter))
            {
                if (sSaid != GetLocalString(GetModule(), "hls_voicebuffer"))
                {
                    SetLocalString(GetModule(), "hls_voicebuffer", sSaid);

                    // PrintString("<Conv>"+GetName(GetArea(oShouter))+ " " + GetName(oShouter) + ": " + sSaid + " </Conv>");

                    // if the phrase begins with .MyName, reparse the string as a voice throw
                    if (GetStringLeft(sSaid, GetStringLength("." + GetName(OBJECT_SELF))) == "." + GetName(OBJECT_SELF) &&
                        (GetLocalInt(GetModule(), "dmfi_Admin" + GetPCPublicCDKey(oShouter)) ||
                        GetIsDM(oShouter) || GetIsDMPossessed(oShouter)))
                    {
                        oTarget = OBJECT_SELF;
                        sSaid = GetStringRight(sSaid, GetStringLength(sSaid) - GetStringLength("." + GetName(OBJECT_SELF)));
                        if (GetStringLeft(sSaid, 1) == " ") sSaid = GetStringRight(sSaid, GetStringLength(sSaid) - 1);
                        sSaid = ":" + sSaid;
                        SetPCChatMessage(sSaid);
// SendMessageToPC(GetFirstPC(), "LEAVE(1) dmfi_univ_listen: speaker=" + GetName(oShouter) + ", channel=" + IntToString(nVolume) + ", said=" + sSaid);
                        return; // must bail out here to prevent clearing of message at end
                    }

                    if (iPhrase)
                    {
                        if (iPhrase > 0)
                        {
                            SetCustomToken(iPhrase, sSaid);
                            SetDMFIPersistentString("dmfi", "hls" + IntToString(iPhrase), sSaid);
                            FloatingTextStringOnCreature("Phrase " + IntToString(iPhrase) + " has been recorded", oShouter, FALSE);
                        }
                        else if (iPhrase < 0)
                        {

                        }
                        DeleteLocalInt(oShouter, "hls_EditPhrase");
                    }
                }
            }
        }
    }

    // after processing, you will likely want to "eat" the text line, so it is
    // not spoken or available for further processing
    SetPCChatMessage("");

// SendMessageToPC(GetFirstPC(), "LEAVE(2) dmfi_univ_listen: speaker=" + GetName(oShouter) + ", channel=" + IntToString(nVolume) + ", said=" + sSaid);

}
