namespace SWLOR.Game.Server.Core
{
    /// <summary>What a step of the reload bootstrap is (<see cref="ReloadBootstrap"/>).</summary>
    public enum ReloadStepKind
    {
        /// <summary>A <see cref="BootReload.Rerun"/> boot handler, run again by the new build.</summary>
        Rerun = 0,

        /// <summary>The adoption step that stands in for a boot-only handler (<see cref="BootPhaseAttribute.AdoptedBy"/>).</summary>
        Adopt = 1,

        /// <summary>What the old build leaves on the module for an adoption step when a reload quiesces it (<see cref="BootPhaseAttribute.CarriedBy"/>).</summary>
        Carry = 2,
    }
}
