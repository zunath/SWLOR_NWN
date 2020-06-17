void main()
{
    location lLoc = GetLocation(OBJECT_SELF);
    string sSecretSwap = GetLocalString(OBJECT_SELF, "CEP_L_SECRETSWAP");
    //SendMessageToPC(GetFirstPC(), GetResRef(OBJECT_SELF));
    //SendMessageToPC(GetFirstPC(), sSecretSwap);
    object oNew = CreateObject(OBJECT_TYPE_PLACEABLE, sSecretSwap, lLoc);
    SetLocalString(oNew, "CEP_L_SECRETSWAP", GetResRef(OBJECT_SELF));
    SetLocalString(oNew, "CEP_L_GATEBLOCK", GetLocalString(OBJECT_SELF, "CEP_L_GATEBLOCK"));
    //SendMessageToPC(GetFirstPC(), "New ResRef "+GetResRef(oNew));
    //SendMessageToPC(GetFirstPC(), "New Tag "+GetTag(oNew));
    //SendMessageToPC(GetFirstPC(), "New Swap "+GetLocalString(oNew, "CEP_L_SECRETSWAP"));
    ExecuteScript("zep_doorkill", OBJECT_SELF);
    ExecuteScript("zep_doorspawn", oNew);
    DestroyObject(OBJECT_SELF);
}
