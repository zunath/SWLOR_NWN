void main()
{
    object oPC = GetPCSpeaker();
    object oTarget = GetLocalObject(oPC, "dmfi_univ_target");
    location lLocation = GetLocalLocation(oPC, "dmfi_univ_location");
    string sConv = GetLocalString(oPC, "dmfi_univ_conv");

    if (GetLocalInt(oPC, "Tens"))
    {
        SetLocalInt(oPC, "dmfi_univ_int", 10*GetLocalInt(oPC, "Tens"));
        ExecuteScript("dmfi_execute", oPC);
        DeleteLocalInt(oPC, "Tens");
        return;
    }
    else
    {
        if(sConv == "voice" || sConv == "pc_emote" || sConv == "faction")
        {
            SetLocalInt(oPC, "dmfi_univ_int", 10);
            ExecuteScript("dmfi_execute", oPC);
            return;
        }
        else
            SetLocalInt(oPC, "Tens", 10);
        return;
    }
}
