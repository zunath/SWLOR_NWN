namespace SWLOR.Game.Server.Core.NWScript.Enum.Associate
{
    public enum Command
    {
        // These must match the values in nwscreature.h and nwccreaturemenu.cpp
        // Cannot use the value -1 because that is used to start a conversation
        StandGround = -2,
        AttackNearest = -3,
        HealMaster = -4,
        FollowMaster = -5,
        MasterFailedLockpick = -6,
        GuardMaster = -7,
        UnsummonFamiliar = -8,
        UnsummonAnimalCompanion = -9,
        UnsummonSummoned = -10,
        MasterUnderAttack = -11,
        ReleaseDomination = -12,
        UnpossessFamiliar = -13,
        MasterSawTrap = -14,
        MasterAttackedOther = -15,
        MasterGoingtobeAttacked = -16,
        LeaveParty = -17,
        PickLock = -18,
        Inventory = -19,
        DisarmTrap = -20,
        ToggleCasting = -21,
        ToggleStealth = -22,
        ToggleSearch = -23
    }
}