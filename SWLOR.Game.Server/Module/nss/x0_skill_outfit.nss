// Initiates the conversation file for saving/loading outfits

void main()
{
    object oPC = GetLastSpeaker();
    SetLocalString(oPC, "CONVERSATION", "Outfit");

    ExecuteScript("dialog_start", oPC);
}
