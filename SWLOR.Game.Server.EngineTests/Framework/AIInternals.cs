using System;
using System.Collections.Generic;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Reaches the profiles and the private target resolution of the server's AI instance. The engine tests drive
    /// them directly, as they did on the static <c>NPCAI</c> class before the AI service was converted.
    /// <para>
    /// The AI split (<c>Readmes/MaintainabilityRefactorCreaturesSplit.md</c>) moves members into part services. Each lookup picks
    /// the part instance whose class declares the member (<see cref="Declaring"/>), so a pull request that adds a part adds its
    /// interface to <see cref="Parts"/> and no test changes.
    /// </para>
    /// </summary>
    public static class AIInternals
    {
        /// <summary>The interfaces of the AI parts, the service itself first; a part that moves adds its interface here.</summary>
        private static readonly Type[] Parts = { typeof(IAIService), typeof(IAIDecisionService), typeof(IAIActionService) };

        private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>The AI profiles the module cache loaded.</summary>
        public static IReadOnlyDictionary<AIProfileType, AIProfile> Profiles
        {
            get
            {
                var (instance, type) = Declaring(candidate => candidate.GetProperty(nameof(AIService.Profiles), Instance) != null, nameof(AIService.Profiles));
                return (IReadOnlyDictionary<AIProfileType, AIProfile>)type.GetProperty(nameof(AIService.Profiles), Instance).GetValue(instance);
            }
        }

        /// <summary>Calls a private instance method of the AI part that declares it with the given arguments.</summary>
        public static object Invoke(string methodName, params object[] arguments)
        {
            var (instance, type) = Declaring(candidate => candidate.GetMethod(methodName, Instance) != null, methodName);
            return type.GetMethod(methodName, Instance).Invoke(instance, arguments);
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

            throw new MissingMemberException($"No AI part declares {member}.");
        }
    }
}
