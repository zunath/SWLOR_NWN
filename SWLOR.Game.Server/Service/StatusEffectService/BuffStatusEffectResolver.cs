using System;
using System.Collections.Generic;
using System.Reflection;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Resolves a <see cref="BuffStatusEffectType"/> into a factory that builds the visible status
    /// effect for a stat-driven buff. The mapping comes from <see cref="BuffStatusEffectAttribute"/>
    /// on the enum entry and is cached on first use.
    /// </summary>
    public static class BuffStatusEffectResolver
    {
        private static readonly Dictionary<BuffStatusEffectType, Func<int, IStatusEffect>> _factories = Build();

        /// <summary>
        /// Returns a factory for the given id, or null when the id is Invalid or unmapped.
        /// The factory argument is the buff magnitude, which effects may show in their name.
        /// </summary>
        public static Func<int, IStatusEffect> GetFactory(BuffStatusEffectType type)
        {
            return _factories.TryGetValue(type, out var factory)
                ? factory
                : null;
        }

        public static Func<int, IStatusEffect> GetFactory(int statValue)
        {
            return Enum.IsDefined(typeof(BuffStatusEffectType), statValue)
                ? GetFactory((BuffStatusEffectType)statValue)
                : null;
        }

        private static Dictionary<BuffStatusEffectType, Func<int, IStatusEffect>> Build()
        {
            var factories = new Dictionary<BuffStatusEffectType, Func<int, IStatusEffect>>();

            foreach (var field in typeof(BuffStatusEffectType).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var attribute = field.GetCustomAttribute<BuffStatusEffectAttribute>();
                if (attribute == null)
                    continue;

                var type = (BuffStatusEffectType)field.GetValue(null);
                factories[type] = BuildFactory(attribute.StatusEffectType);
            }

            return factories;
        }

        private static Func<int, IStatusEffect> BuildFactory(Type statusEffectType)
        {
            if (statusEffectType == null || !typeof(IStatusEffect).IsAssignableFrom(statusEffectType))
                throw new InvalidOperationException(
                    $"{statusEffectType?.Name ?? "null"} is not a valid status effect type.");

            // Effects that show their magnitude take it through an int constructor; the rest are
            // fixed-strength and only need the parameterless one.
            var magnitudeConstructor = statusEffectType.GetConstructor(new[] { typeof(int) });
            if (magnitudeConstructor != null)
                return magnitude => (IStatusEffect)magnitudeConstructor.Invoke(new object[] { magnitude });

            return _ => (IStatusEffect)Activator.CreateInstance(statusEffectType);
        }
    }
}
