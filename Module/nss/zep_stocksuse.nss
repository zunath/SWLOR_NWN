//:://////////////////////////////////////////////
//:: Created By:  John Hawkins
//:: Created On:  January 2004
//:://////////////////////////////////////////////
//::
//:: this script will make my placeable prisoners
//:: react to you and talk to you if desired.
//::
//:: to converse with the placeable, you need to
//:: put the name of the conversation you wish to
//:: have inside the quotation marks ("") down in
//:: line 57.
//::
//:://////////////////////////////////////////////

void main()
{
    object oUser = GetLastUsedBy();
    int iRnd = d6();
    string sSound = IntToString(iRnd);
    if (GetLocalInt(OBJECT_SELF,"disturbed") == 0)
    {
        object oSelf = OBJECT_SELF;
        PlayAnimation(ANIMATION_PLACEABLE_ACTIVATE);
        SetLocalInt(OBJECT_SELF,"disturbed",1);
    }
    else
    {
        object oSelf = OBJECT_SELF;
        PlayAnimation(ANIMATION_PLACEABLE_DEACTIVATE);
        SetLocalInt(OBJECT_SELF,"disturbed",0);
    }
    if(iRnd==1)
    {
        AssignCommand(oUser,PlaySound("as_pl_ailingm1"));
    }
    if(iRnd==2)
    {
        AssignCommand(oUser,PlaySound("as_pl_despairm1"));
    }
    if(iRnd==3)
    {
        AssignCommand(oUser,PlaySound("as_pl_despairm2"));
    }
    if(iRnd==4)
    {
        AssignCommand(oUser,PlaySound("as_pl_cryingm1"));
    }
    if(iRnd==5)
    {
        AssignCommand(oUser,PlaySound("as_pl_despairm1"));
    }
    if(iRnd==6)
    {
        AssignCommand(oUser,PlaySound("as_pl_despairm2"));
    }
    //AssignCommand(OBJECT_SELF,ActionStartConversation(oUser,"",FALSE,FALSE));
}
