using System.Threading;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The time source for the combat, status effect, perk and crafting code. It reads the system clock unless a test
    /// holds an <see cref="OverrideForTests"/> scope; production code never calls the override.
    /// </summary>
    public static class Clock
    {
        private static ClockOverride _override;

        /// <summary>The current UTC time. Same value as <see cref="DateTime.UtcNow"/> unless overridden.</summary>
        public static DateTime UtcNow
        {
            get
            {
                var active = _override;
                return active?.ReadUtcNow != null ? active.ReadUtcNow() : DateTime.UtcNow;
            }
        }

        /// <summary>Milliseconds since the machine started. Same value as <see cref="Environment.TickCount64"/> unless overridden.</summary>
        public static long TickCount64
        {
            get
            {
                var active = _override;
                return active?.ReadTickCount64 != null ? active.ReadTickCount64() : Environment.TickCount64;
            }
        }

        /// <summary>
        /// Test-only: replaces the readings until the returned scope is disposed, which restores the system clock.
        /// A reading left null keeps reading the system clock. Throws when a scope is already active.
        /// </summary>
        public static IDisposable OverrideForTests(Func<DateTime> utcNow = null, Func<long> tickCount64 = null)
        {
            var scope = new ClockOverride(utcNow, tickCount64);
            if (Interlocked.CompareExchange(ref _override, scope, null) != null)
                throw new InvalidOperationException("A Clock override is already active; dispose it before overriding again.");

            return scope;
        }

        private sealed class ClockOverride : IDisposable
        {
            public readonly Func<DateTime> ReadUtcNow;
            public readonly Func<long> ReadTickCount64;

            public ClockOverride(Func<DateTime> utcNow, Func<long> tickCount64)
            {
                ReadUtcNow = utcNow;
                ReadTickCount64 = tickCount64;
            }

            public void Dispose()
            {
                // Only the active scope can clear the override, so disposing twice or a stale scope changes nothing.
                Interlocked.CompareExchange(ref _override, null, this);
            }
        }
    }
}
