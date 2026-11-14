using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// Which types a container registers as a service or content singleton: what a constructor of content may take, what the content
    /// factory never builds, and what the reload carry and digests leave out. The container's own helpers (the provider, the scope
    /// factory), the enumerables it answers for any type and the <see cref="Lazy{T}"/> it registers for every service do not count.
    /// </summary>
    internal static class RegisteredTypes
    {
        /// <summary>The predicate for <paramref name="provider"/>'s registrations.</summary>
        public static Func<Type, bool> Of(IServiceProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var isService = provider.GetRequiredService<IServiceProviderIsService>();
            return type => IsRegistered(isService, type);
        }

        /// <summary>The predicate for the registrations of <paramref name="services"/>, without building a container.</summary>
        public static Func<Type, bool> Of(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var registered = new HashSet<Type>();
            foreach (var descriptor in services)
            {
                if (!descriptor.ServiceType.IsGenericTypeDefinition)
                    registered.Add(descriptor.ServiceType);
            }

            return type => type != null && !IsContainerHelper(type) && registered.Contains(type);
        }

        private static bool IsRegistered(IServiceProviderIsService isService, Type type)
        {
            return type != null && !IsContainerHelper(type) && isService.IsService(type);
        }

        private static bool IsContainerHelper(Type type)
        {
            if (type.IsValueType || type == typeof(string) || type == typeof(object))
                return true;
            if (type == typeof(IServiceProvider) || type == typeof(IServiceScopeFactory) || type == typeof(IServiceProviderIsService) ||
                type == typeof(IServiceProviderIsKeyedService))
                return true;
            if (!type.IsConstructedGenericType)
                return false;

            var definition = type.GetGenericTypeDefinition();
            return definition == typeof(IEnumerable<>) || definition == typeof(Lazy<>);
        }
    }
}
