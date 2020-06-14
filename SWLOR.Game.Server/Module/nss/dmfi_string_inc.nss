//::///////////////////////////////////////////////
//:: DMFI - string functions and constants
//:: dmfi_string_inc
//:://////////////////////////////////////////////
/*
    Library of functions relating to strings for DMFI.
*/
//:://////////////////////////////////////////////
//:: Created By: tsunami282
//:: Created On: 2008.08.11
//:://////////////////////////////////////////////

#include "x3_inc_string"

const string DMFI_MESSAGE_COLOR_ALERT = "733"; // default 733 - brite red
const string DMFI_MESSAGE_COLOR_STATUS = "773"; // default 773 - yellow
const string DMFI_MESSAGE_COLOR_EAVESDROP = "777"; // default 777 - white
const string DMFI_MESSAGE_COLOR_TRANSLATION = "555"; // default 733 - lite gray
const string DMFI_MESSAGE_COLOR_OTHER = ""; // default blank

////////////////////////////////////////////////////////////////////////
string LTrim(string sTrimMe, string sDelim = " ")
{
    int l;

    if (sDelim != "")
    {
        l = GetStringLength(sTrimMe);
        while (GetStringLeft(sTrimMe, 1) == sDelim)
        {
            l--;
            if (l < 1)
            {
                sTrimMe = "";
                break;
            }
            sTrimMe = GetStringRight(sTrimMe, l);
        }
    }

    return sTrimMe;
}

////////////////////////////////////////////////////////////////////////
void DMFISendMessageToPC(object oPC, string sMsg, int bAllDMs=FALSE,
                         string sRGB="")
{
    string sColMsg;
    object oTarget = oPC;
    if (bAllDMs) oTarget = GetFirstPC();
    while (GetIsObjectValid(oTarget))
    {
        if ((!bAllDMs) || (GetIsDM(oTarget) || GetIsDMPossessed(oTarget)))
        {
            if (sRGB != "")
            {
                sColMsg = StringToRGBString(sMsg, sRGB);
            }
            else
            {
                sColMsg = sMsg;
            }
            SendMessageToPC(oTarget, sColMsg);
        }

        if (!bAllDMs) break;
        oTarget = GetNextPC();
    }

}

