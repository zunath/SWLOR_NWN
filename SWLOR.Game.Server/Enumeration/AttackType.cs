namespace SWLOR.Game.Server.Enumeration
{
// This array is used to signal the preferred attack type of an ability to the native code that manages attack roll overrides.
// Set the local variable ATTACK_TYPE_OVERRIDE on the attacking creature to one of the valid values below.  This is a one-shot:
// the value of the variable will be wiped when it is read. 
//
// If this override is used, the equipped weapon and any related feats etc. will be ignored.  To make a special weapon that uses
// (say) Force to attack, build the weapon in the toolset and give it the ATTACK_TYPE_OVERRIDE int variable with the same 
// values as here.  That weapon will then always make attacks of the specified type, and weapon focus feats etc. will be applied.
    public enum AttackType
    {
        Invalid = 0,
        Melee = 1,
        Ranged = 2,
        Spirit = 3
    }
}
