
int StartingConditional()
{
   int nMyNum = GetLocalInt(OBJECT_SELF, "dmfi_dmwOffset");
   SetLocalInt(OBJECT_SELF, "dmfi_dmwOffset", nMyNum+1);

   object oMySpeaker = GetPCSpeaker();
   object oMyTarget = GetLocalObject(oMySpeaker, "dmfi_univ_target");
   location lMyLoc = GetLocalLocation(oMySpeaker, "dmfi_univ_location");

   string sMyString = GetLocalString(oMySpeaker, "dmw_dialog" + IntToString(nMyNum));

   if(sMyString == "")
   {
      return FALSE;
   }
   else
   {
      SetCustomToken(8000 + nMyNum, sMyString);
      return TRUE;
   }
}
