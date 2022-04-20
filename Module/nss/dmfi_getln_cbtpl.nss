//::///////////////////////////////////////////////
//:: DMFI - DMFI_get_line callback template
//:: dmfi_getln_cbtpl
//:://////////////////////////////////////////////
/*
  A template (skeleton) function for DMFI_get_line callback processing.

  Use this template to create your script to be invoked when your scripted call
  to DMFI_get_line receives input.
*/
//:://////////////////////////////////////////////
//:: Created By: tsunami282
//:: Created On: 2008.05.21
//:://////////////////////////////////////////////

void main()
{
    int nVolume = GetPCChatVolume();
    object oShouter = GetPCChatSpeaker();
    string sSaid = GetPCChatMessage();

    // you may wish to define an "abort" input message, such as a line
    // containing a single period:
    if (sSaid != ".")
    {
        // put your code here to process the input line (in sSaid)

    }

    // after processing, you will likely want to "eat" the text line, so it is
    // not spoken or available for further processing. If you want the line to
    // appear, either comment out the following line, or set it to:
    //     SetPCChatMessage(sSaid);
    SetPCChatMessage("");
}
