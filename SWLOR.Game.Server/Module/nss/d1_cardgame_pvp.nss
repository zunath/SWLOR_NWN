/**********************************/
/*          d1_cardgame_pvp
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 25th February 2003
/**********************************/
/*  Looks to see if a card game is
/*  to be played between two
/*  players.
/**********************************/

void main()
{
    object oPC = GetLastUsedBy();
    object oStool = OBJECT_SELF;
    AssignCommand(oPC, ClearAllActions());
    AssignCommand(oPC, ActionMoveToObject(oStool));
    AssignCommand(oPC, ActionStartConversation(oStool, "d1_cardpvp", TRUE, FALSE));
}

