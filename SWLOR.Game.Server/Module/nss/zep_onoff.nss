//::///////////////////////////////////////////////
//:: ZEP_ONOFF.nss
//:: Copyright (c) 2001 Bioware Corp.
//:: Modified by Dan Heidel 1/14/04 for CEP
//:://////////////////////////////////////////////
/*
    Turns the placeable object's animation on/off

    Since the activation state for a placeable cannot
    be querried, this state must be stored in the
    local int CEP_L_AMION.  If the placeable is
    activated by default, CEP_L_AMION must be 1.  If the
    placeable is deactivated by default, CEP_L_AMION must
    be set to 0 or else incorrect behavior will result.
    All CEP placeables have local variables set properly.

    Also, for musical instruments, etc, sounds will be
    played if specified.
    CEP_L_SOUND1 is the name of the WAV file to play
    when the placeable is activated.
    CEP_L_SOUND2 is the name of the WAV file to play
    when the placeable is deactivated.
    If either of these is not defined, no sound will
    be played for that anim.
    By default, all CEP placeables that have sounds
    attached to them already have local variables
    defined for them.

*/
//:://////////////////////////////////////////////
//:: Created By:  Brent
//:: Created On:  January 2002
//:://////////////////////////////////////////////


void main()
{
    string sSound1 = GetLocalString(OBJECT_SELF, "CEP_L_SOUND1");
    string sSound2 = GetLocalString(OBJECT_SELF, "CEP_L_SOUND2");
    if (GetLocalInt(OBJECT_SELF,"CEP_L_AMION") == 0)
    {
        object oSelf = OBJECT_SELF;
        PlaySound(sSound1);
        DelayCommand(0.1, PlayAnimation(ANIMATION_PLACEABLE_ACTIVATE));
        SetLocalInt(OBJECT_SELF,"CEP_L_AMION",1);
    }
    else
    {
        object oSelf = OBJECT_SELF;
        PlaySound(sSound2);
        DelayCommand(0.1, PlayAnimation(ANIMATION_PLACEABLE_DEACTIVATE));
        SetLocalInt(OBJECT_SELF,"CEP_L_AMION",0);
    }
}
