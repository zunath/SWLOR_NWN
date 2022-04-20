void main()
{
    object oPC = GetPCSpeaker();
    object oTarget = GetLocalObject(oPC, "dmfi_univ_target");
    location lLocation = GetLocalLocation(oPC, "dmfi_univ_location");
    string sConv = GetLocalString(oPC, "dmfi_univ_conv");

    if (GetLocalInt(oPC, "Tens"))
    {
        SetLocalInt(oPC, "dmfi_univ_int", 10*GetLocalInt(oPC, "Tens") + 7);
        ExecuteScript("dmfi_execute", oPC);
        DeleteLocalInt(oPC, "Tens");
        return;
    }
    else
    {
        if(sConv == "pc_emote" || sConv == "emote" || sConv == "server" || sConv == "onering" || sConv == "rest")
        {
            SetLocalInt(oPC, "dmfi_univ_int", 7);
            ExecuteScript("dmfi_execute", oPC);
            return;
        }
        else
            SetLocalInt(oPC, "Tens", 7);
        return;
    }
}
