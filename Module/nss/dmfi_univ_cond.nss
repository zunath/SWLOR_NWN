//DMFI Universal Wand scripts by hahnsoo

////////////////////////////////////////////////////////////////////////
//This initializes the rest dialog.
//If limited by Time, report how long it will take before the PC can rest again
//If DM, tell the DM the interval of time between rests.
//If limited by placeable, report that the PC needs a restable object nearby
//If DM, tell the DM that the rest is limited by placeables.
//If limited by Armor, report that the PC is wearing armor that prevents resting
//If DM, tell the DM that the rest is limited by armor weight.
//If in an area that doesn't allow spell memorization, report this to the PC
//If DM, tell the DM that spell memorization is restricted in this area.
//If hit point restrictions are "up", tell the PC how many hitpoints they would gain by resting
//If DM, tell the DM what level of Hitpoint restrictions are in effect.
void SetRestTokens(object oPC)
{
    object oArea = GetArea(oPC);
    int iSettings = GetLocalInt(oPC, "dmfi_r_settings");
    int iMinutesPerHour = FloatToInt(HoursToSeconds(1))/60;
    SetCustomToken(20792, IntToString(iMinutesPerHour));
    SetCustomToken(20793, IntToString(iMinutesPerHour * 2));
    SetCustomToken(20794, IntToString(iMinutesPerHour * 4));
    SetCustomToken(20795, IntToString(iMinutesPerHour * 6));
    SetCustomToken(20796, IntToString(iMinutesPerHour * 8));
    SetCustomToken(20797, IntToString(iMinutesPerHour * 12));
    SetCustomToken(20798, IntToString(iMinutesPerHour * 24));

    if (GetIsDM(oPC))
    {
        string sRest = "";
        if (iSettings & 0x00000080)
        {
            SetCustomToken(20789, "[LOCAL]");
            sRest = sRest + "[LOCAL] settings in effect";
            if (iSettings & 0x00000002)
                sRest = sRest + "\nThis is a No Rest area";
            else if (!(iSettings & 0x00000001))
                sRest = sRest + "\nThis is an Unlimited Rest area";
        }
        else
        {
            SetCustomToken(20789, "[GLOBAL]");
            sRest = sRest + "[GLOBAL] settings in effect";
            if (iSettings & 0x00000002)
                sRest = sRest + "\nNo Rest is set globally";
            else if (!(iSettings & 0x00000001))
                sRest = sRest + "\nUnlimited Rest is set globally";
        }
        if (iSettings & 0x00000004)
        {
            sRest = sRest + "\nRest is limited by Time: ";
            switch (iSettings & 0x00000f00)
            {
                default:
                case 0x00000100: sRest = sRest + "1 hour"; break;
                case 0x00000200: sRest = sRest + "2 hours"; break;
                case 0x00000300: sRest = sRest + "4 hours"; break;
                case 0x00000400: sRest = sRest + "6 hours"; break;
                case 0x00000500: sRest = sRest + "8 hours"; break;
                case 0x00000600: sRest = sRest + "12 hours"; break;
                case 0x00000700: sRest = sRest + "24 hours"; break;
            }
        }
        if (iSettings & 0x00000008) //Placeables
        {
            sRest = sRest + "\nRest is limited by Placeables: ";
            if (!(iSettings & 0x00001000)) sRest = sRest + "DMFI_placeables ";
            if (iSettings & 0x00002000) sRest = sRest + "Campfires ";
            if (iSettings & 0x00004000) sRest = sRest + "Bed_Rolls ";
            if (iSettings & 0x00008000) sRest = sRest + "Beds ";
            if (iSettings & 0x00010000) sRest = sRest + "Tents ";
            if ((iSettings & 0x00020000) || (iSettings & 0x00040000) || (iSettings & 0x00080000))
            {
                sRest = sRest + "\nClasses that ignore restrictions: ";
                if (iSettings & 0x00020000) sRest = sRest + "Druids ";
                if (iSettings & 0x00040000) sRest = sRest + "Rangers ";
                if (iSettings & 0x00080000) sRest = sRest + "Barbarians ";
            }
        }
        if (iSettings & 0x00000010) //Armor
        {
            sRest = sRest + "\nRest is limited by Armor: ";
            switch (iSettings & 0x00f00000)
            {
                default:
                case 0x00100000: sRest = sRest + "2 pounds"; break;
                case 0x00200000: sRest = sRest + "6 pounds"; break;
                case 0x00300000: sRest = sRest + "11 pounds"; break;
                case 0x00400000: sRest = sRest + "16 pounds"; break;
                case 0x00500000: sRest = sRest + "31 pounds"; break;
                case 0x00600000: sRest = sRest + "41 pounds"; break;
                case 0x00700000: sRest = sRest + "46 pounds"; break;
            }
        }
        if (iSettings & 0x00000020) //Hit point limits
        {
            sRest = sRest + "\nHit points are limited to: ";
            switch (iSettings & 0x0f000000)
            {
                case 0x01000000: sRest = sRest + "0 HP"; break;
                case 0x02000000: sRest = sRest + "1 HP/level"; break;
                case 0x03000000: sRest = sRest + "(CON) HP"; break;
                case 0x04000000: sRest = sRest + "10 percent of max"; break;
                case 0x05000000: sRest = sRest + "25 percent of max"; break;
                case 0x06000000: sRest = sRest + "50 percent of max"; break;
                default:
                case 0x07000000: sRest = sRest + "100 percent"; break;
            }
        }
        if (iSettings & 0x00000040) //Spell memorization
        {
            sRest = sRest + "\nSpell memorization is OFF";
        }
        SetCustomToken(20791, sRest);
    }
    else //For PCs
    { //Setting rest tokens
        string sRest = "";
        if (iSettings & 0x00000080)
        {
            if (iSettings & 0x00000002)
                sRest = sRest + "\nThis is a No Rest area";
            else if (!(iSettings & 0x00000001))
                sRest = sRest + "\nThis is an Unlimited Rest area";
        }
        else
        {
            if (iSettings & 0x00000002)
                sRest = sRest + "\nNo Rest is set globally";
            else if (!(iSettings & 0x00000001))
                sRest = sRest + "\nUnlimited Rest is set globally";
        }
        if (iSettings & 0x00000004 && iSettings & 0x00000001)
        {
            int iTime = GetTimeHour() + GetCalendarDay() * 24 + GetCalendarMonth() * 24 * 28 + GetCalendarYear() * 24 * 28 * 12;
            int iNext = GetLocalInt(oPC, "dmfi_r_nextrest");
            if (iNext > iTime)
                sRest = sRest + "\nYou may rest again in " + IntToString(iNext - iTime) + " hours";
        }
        if (iSettings & 0x00000008 && iSettings & 0x00000001) //Placeables
        {
            if (!(GetLevelByClass(CLASS_TYPE_DRUID, oPC) && (iSettings & 0x00020000)) ||
                !(GetLevelByClass(CLASS_TYPE_RANGER, oPC) && (iSettings & 0x00040000)) ||
                !(GetLevelByClass(CLASS_TYPE_BARBARIAN, oPC) && (iSettings & 0x00080000)))
            {
                object oPlaceable = GetFirstObjectInShape(SHAPE_SPHERE, 6.0f, GetLocation(oPC), TRUE, OBJECT_TYPE_PLACEABLE);
                int iBreak = 0;
                while (GetIsObjectValid(oPlaceable) && !iBreak)
                {
                    if (!(iSettings & 0x00001000) && GetTag(oPlaceable) == "dmfi_rest") //DMFI Placeables: by default, ON
                        iBreak = 1;
                    if ((iSettings & 0x00002000) && GetStringLowerCase(GetName(oPlaceable)) == "campfire") //Campfires
                        iBreak = 1;
                    if ((iSettings & 0x00004000) && (GetStringLowerCase(GetName(oPlaceable)) == "bed roll" || GetStringLowerCase(GetName(oPlaceable)) == "bedroll")) //Bed rolls
                        iBreak = 1;
                    if ((iSettings & 0x00008000) && GetStringLowerCase(GetName(oPlaceable)) == "bed") //beds
                        iBreak = 1;
                    if ((iSettings & 0x00010000) && GetStringLowerCase(GetName(oPlaceable)) == "tent") //tents
                        iBreak = 1;
                    oPlaceable = GetNextObjectInShape(SHAPE_SPHERE, 6.0f, GetLocation(oPC), TRUE, OBJECT_TYPE_PLACEABLE);
                }
                if (!iBreak)
                {
                    sRest = sRest + "\nYou are not near a rest placeable";
                }
            }
        }
        if ((iSettings & 0x00000010) && iSettings & 0x00000001)//Armor
        {
            int iArmor = (iSettings & 0x00f00000);
            object oArmor = GetItemInSlot(INVENTORY_SLOT_CHEST, oPC);
            int iWeight = GetWeight(oArmor);
            switch(iArmor)
            {
                default:
                case 0x00100000: if (iWeight > 20) sRest = sRest + "\nYou cannot rest in armor heavier than Clothing"; break;
                case 0x00200000: if (iWeight > 60) sRest = sRest + "\nYou cannot rest in armor heavier than Padded"; break;
                case 0x00300000: if (iWeight > 110) sRest = sRest + "\nYou cannot rest in armor heavier than Leather"; break;
                case 0x00400000: if (iWeight > 160) sRest = sRest + "\nYou cannot rest in armor heavier than Studded Leather"; break;
                case 0x00500000: if (iWeight > 310) sRest = sRest + "\nYou cannot rest in armor heavier than Chain Shirt"; break;
                case 0x00600000: if (iWeight > 410) sRest = sRest + "\nYou cannot rest in armor heavier than Chain Mail"; break;
                case 0x00700000: if (iWeight > 460) sRest = sRest + "\nYou cannot rest in armor heavier than Banded Mail"; break;
            }
        }
        if (iSettings & 0x00000020 && iSettings & 0x00000001) //Hit point limits
        {
            sRest = sRest + "\nOn Rest, you will regain ";
            switch (iSettings & 0x0f000000)
            {
                case 0x01000000: sRest = sRest + "0 HP"; break;
                case 0x02000000: sRest = sRest + IntToString(GetHitDice(oPC)) + " HP"; break;
                case 0x03000000: sRest = sRest + IntToString(GetAbilityScore(oPC, ABILITY_CONSTITUTION)) + " HP"; break;
                case 0x04000000: sRest = sRest + IntToString(GetMaxHitPoints(oPC)/10) + " HP"; break;
                case 0x05000000: sRest = sRest + IntToString(GetMaxHitPoints(oPC)/4) + " HP"; break;
                case 0x06000000: sRest = sRest + IntToString(GetMaxHitPoints(oPC)/2) + " HP"; break;
                default:
                case 0x07000000: sRest = sRest + "full HP"; break;
            }
            sRest = sRest + "\nResting will drop you from the party";
        }
        if (iSettings & 0x00000040 && iSettings & 0x00000001) //Spell memorization
        {
            sRest = sRest + "\nYou cannot memorize spells here";
        }
        SetCustomToken(20790, sRest);
    }
}

////////////////////////////////////////////////////////////////////////
int StartingConditional()
{
    object oPC = GetPCSpeaker();
    DeleteLocalInt(oPC, "Tens");
    int iOffset = GetLocalInt(oPC, "dmfi_univ_offset")+1;
    string sOffset = GetLocalString(oPC, "dmfi_univ_conv");
    SetLocalInt(oPC, "dmfi_univ_offset", iOffset);

    if (sOffset == "afflict" && iOffset==1)
        return TRUE;
    else if (sOffset == "pc_emote" && iOffset==2)
        return TRUE;
    else if (sOffset == "emote" && iOffset==2)
        return TRUE;
    else if (sOffset == "encounter" && iOffset==3)
        return TRUE;
    else if (sOffset == "fx" && iOffset==4)
        return TRUE;
    else if (sOffset == "music" && iOffset==5)
        return TRUE;
    else if (sOffset == "sound" && iOffset==6)
        return TRUE;
    else if (sOffset == "xp" && iOffset==7)
        return TRUE;
    else if (sOffset == "onering" && iOffset==8)
        return TRUE;
    else if (sOffset == "pc_dicebag" && iOffset==9)
    {
        SetLocalInt(oPC, "dmfi_univ_offset", 8);

        if (GetLocalInt(oPC, "dmfi_dicebag")==0)
                SetCustomToken(20681, "Private");
        else  if (GetLocalInt(oPC, "dmfi_dicebag")==1)
                SetCustomToken(20681, "Global");
        else if (GetLocalInt(oPC, "dmfi_dicebag")==2)
                SetCustomToken(20681, "Local");
        else if (GetLocalInt(oPC, "dmfi_dicebag")==3)
                SetCustomToken(20681, "DM Only");

        return TRUE;
    }
    else if (sOffset == "dicebag" && iOffset==10)
    {
        SetLocalInt(oPC, "dmfi_univ_offset", 9);

        if (GetLocalInt(oPC, "dmfi_dicebag")==0)
                SetCustomToken(20681, "Private");
        else  if (GetLocalInt(oPC, "dmfi_dicebag")==1)
                SetCustomToken(20681, "Global");
        else if (GetLocalInt(oPC, "dmfi_dicebag")==2)
                SetCustomToken(20681, "Local");
        else if (GetLocalInt(oPC, "dmfi_dicebag")==3)
                SetCustomToken(20681, "DM Only");

        string sName = GetName(GetLocalObject(oPC, "dmfi_univ_target"));
        SetCustomToken(20680, sName);

        return TRUE;
    }
    else if (sOffset == "voice" &&
        GetIsObjectValid(GetLocalObject(oPC, "dmfi_univ_target")) &&
        oPC != GetLocalObject(oPC, "dmfi_univ_target") &&
        iOffset==11)
    {
        string sName = GetName(GetLocalObject(oPC, "dmfi_univ_target"));
        SetCustomToken(20680, sName);
        // pc range single/party
        int hookparty = GetLocalInt(oPC, "dmfi_MyListenerPartyMode");
        if (hookparty == 0) SetCustomToken(20681, "*Single* / Party");
        else SetCustomToken(20681, "Single / *Party*");
        return TRUE;
    }
    else if (sOffset == "voice" &&
        !GetIsObjectValid(GetLocalObject(oPC, "dmfi_univ_target")) &&
        iOffset==12)
    {
        string sName = GetName(GetLocalObject(oPC, "dmfi_univ_target"));
        SetCustomToken(20680, sName);
        // loc range earshot/area/module
        int hookparty = GetLocalInt(oPC, "dmfi_MyListenerPartyMode");
        if (hookparty == 0) SetCustomToken(20681, "*Earshot* / Area / Module");
        else if (hookparty == 1) SetCustomToken(20681, "Earshot / *Area* / Module");
        else SetCustomToken(20681, "Earshot / Area / *Module*");
        return TRUE;
    }
    else if (sOffset == "voice" &&
        GetIsObjectValid(GetLocalObject(oPC, "dmfi_univ_target")) &&
        oPC == GetLocalObject(oPC, "dmfi_univ_target") &&
        iOffset==13)
    {
        string sName = GetName(GetLocalObject(oPC, "dmfi_univ_target"));
        SetCustomToken(20680, sName);
        // self bcast one dm/all dm
        int hookbcast = GetLocalInt(oPC, "dmfi_MyListenerBcastMode");
        if (hookbcast == 0) SetCustomToken(20681, "*Self* / All DMs");
        else SetCustomToken(20681, "Self / *All DMs*");
        return TRUE;
    }
    else if (sOffset == "faction" && iOffset==14)
    {
        int iLoop = 1;
        string sName;
        object sFaction;
        while (iLoop < 10)
        {
            sFaction = GetLocalObject(oPC, "dmfi_customfaction" + IntToString(iLoop));
            sName = GetName(sFaction);
            SetCustomToken(20690 + iLoop, sName + "'s Faction ");
            iLoop++;
        }

        SetCustomToken(20690, GetName(GetLocalObject(oPC, "dmfi_henchman")));
        SetCustomToken(20784, FloatToString(GetLocalFloat(oPC, "dmfi_reputation")));
        sName = GetName(GetLocalObject(oPC, "dmfi_univ_target"));
        SetCustomToken(20680, sName);
        return TRUE;
    }
    else if (sOffset == "dmw" && iOffset ==15)
    {
        SetCustomToken(20781, IntToString(GetLocalInt(oPC, "dmfi_alignshift")));
        return TRUE;
    }
    else if (sOffset == "buff" && iOffset ==16)
    {
        if (GetLocalInt(oPC, "dmfi_buff_party")==0)
            SetCustomToken(20783, "Single Target");
        else
            SetCustomToken(20783, "Party");
        SetCustomToken(20782, GetLocalString(oPC, "dmfi_buff_level"));
        return TRUE;
    }
    else if (sOffset == "rest" && iOffset == 17 && !GetIsDM(oPC) && GetLocalInt(oPC, "dmfi_norest")) //This is the case of a No-Rest situation
    {
        SetRestTokens(oPC);
        return TRUE;
    }
    else if (sOffset == "rest" && iOffset == 18 && !GetIsDM(oPC) && !GetLocalInt(oPC, "dmfi_norest")) //This is the case of a Rest situation
    {
        SetRestTokens(oPC);
        return TRUE;
    }
    else if (sOffset == "rest" && iOffset == 19 && GetIsDM(oPC)) //This is the case of a DM activating the rest menu
    {
        SetRestTokens(oPC);
        return TRUE;
    }
    else if (sOffset == "naming" && iOffset==20)
    {
        string sName = GetName(GetLocalObject(oPC, "dmfi_univ_target"));
        SetCustomToken(20680, sName);
        return TRUE;
    }
    return FALSE;
}
