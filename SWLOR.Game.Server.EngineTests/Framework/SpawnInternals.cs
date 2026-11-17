using System;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the private members of the server's spawn instance (the per-area spawn caches and
    /// <c>AdjustScripts</c>). The engine tests drive them directly, as they did on the static class before the spawn
    /// service was converted.
    /// <para>
    /// The Spawn split (<c>Readmes/MaintainabilityRefactorCreaturesSplit.md</c>) moves members into part services. Each lookup
    /// picks the part instance whose class declares the member (<see cref="Declaring"/>), so a pull request that adds a part adds
    /// its interface to <see cref="Parts"/> and no test changes.
    /// </para>
    /// </summary>
    public static class SpawnInternals
    {
        /// <summary>The interfaces of the Spawn parts, the service itself first; a part that moves adds its interface here.</summary>
        private static readonly Type[] Parts = { typeof(ISpawnService), typeof(ISpawnRegistryService) };

        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>The value of a private field of the spawn part that declares it (<c>_activeSpawnsByArea</c>, <c>_queuedSpawns</c>, ...).</summary>
        public static object Field(string name)
        {
            var (instance, type) = Declaring(candidate => candidate.GetField(name, Private) != null, name);
            return type.GetField(name, Private).GetValue(instance);
        }

        /// <summary>Calls a private instance method of the spawn part that declares it with the given arguments.</summary>
        public static object Invoke(string methodName, params object[] arguments)
        {
            var (instance, type) = Declaring(candidate => candidate.GetMethod(methodName, Private) != null, methodName);
            return type.GetMethod(methodName, Private).Invoke(instance, arguments);
        }

        /// <summary>The part instance, and the class in its hierarchy, that declares the member <paramref name="declares"/> accepts.</summary>
        private static (object Instance, Type Type) Declaring(Func<Type, bool> declares, string member)
        {
            foreach (var part in Parts)
            {
                var instance = CompositionRoot.ServerHost.Provider.GetService(part);
                for (var type = instance.GetType(); type != null; type = type.BaseType)
                {
                    if (declares(type))
                        return (instance, type);
                }
            }

            throw new MissingMemberException($"No spawn part declares {member}.");
        }
    }
}
