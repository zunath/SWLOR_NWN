void main()
{
  object oPC = GetLastUsedBy();
  AssignCommand(oPC, ActionStartConversation(OBJECT_SELF, "d1_cardpvp", TRUE, FALSE));
}
