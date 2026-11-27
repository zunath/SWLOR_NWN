using SWLOR.Game.Contracts;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// How the hook files of <c>Native/</c> (static code) reach the native hook control: the container's
    /// <see cref="IHookControl"/>, which is the process's own <c>NativeHookTable</c> on the native entry and the host's on the
    /// host entry. It only resolves, through the same per-type slot a <see cref="GameServices"/> property reads, so it holds
    /// no state and serves the container a test installed. It is not a <see cref="GameServices"/> property because the hook
    /// control is not a game service.
    /// </summary>
    public static class NativeHookAccess
    {
        /// <summary>The container's native hook control.</summary>
        public static IHookControl Hooks => FacadeAccessor.Resolve<IHookControl>();
    }
}
