void main()
{
  object oPC = GetPCSpeaker();
  DeleteLocalInt(oPC, "WAGER");
  DeleteLocalInt(oPC, "WAGER_ANTE");
  DeleteLocalInt(oPC, "WAGER_GOLD");
}

