void main()
{
    object oPlayer = GetPCSpeaker();
    object oOpponent = GetLocalObject(oPlayer, "CARD_OPPONENT");

    SendMessageToPC(oOpponent, GetName(oPlayer) + " ignores your request.");
}
