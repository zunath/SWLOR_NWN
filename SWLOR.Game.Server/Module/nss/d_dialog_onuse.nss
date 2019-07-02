/**********************************/
/*           d_dialog_onuse
/**********************************/
/*  OnUsed to initiate
/*  conversations with placeable
/*  objects.
/**********************************/

void main()
{
    object oPC = GetLastUsedBy();

    AssignCommand (oPC, ClearAllActions (TRUE));
    ActionStartConversation (oPC);
}
