namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The attribute mapping a space weapon uses (plan section 5). Energy weapons copy the ground ranged mapping
    /// (AGI accuracy, PER damage); launchers copy the melee mapping (PER accuracy, MGT damage).
    /// </summary>
    public enum SpaceWeaponFamily
    {
        Energy = 1,
        Launcher = 2,
    }
}
