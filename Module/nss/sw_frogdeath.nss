// NWN starts a standing death with the same animation it uses for a live knockdown.
void main()
{
    object frog = OBJECT_SELF;
    if (Get2DAString("appearance", "RACE", GetAppearanceType(frog)) != "sw_frogboss")
        return;

    ReplaceObjectAnimation(frog, "ckdbck", "sw_fdrop");
    AssignCommand(GetModule(), DelayCommand(1.5, ReplaceObjectAnimation(frog, "ckdbck", "")));
}
