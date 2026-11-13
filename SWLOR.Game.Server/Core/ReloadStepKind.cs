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
        /// <summary>A <c>mod_exit</c> handler's step, run for every online player while a reload quiesces the old build (<see cref="PlayerReload.Quiesce"/>).</summary>
        Quiesce = 3,
        /// <summary>A <c>mod_enter</c> handler's step, run for every online player when the new build activates (<see cref="PlayerReload.Restore"/>).</summary>
        Restore = 4,
    }
}
