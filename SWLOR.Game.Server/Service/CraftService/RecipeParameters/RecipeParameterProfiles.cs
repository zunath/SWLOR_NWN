using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Service.CraftService.RecipeParameters
{
    /// <summary>
    /// Finds the minigame profiles compiled into an assembly.
    /// </summary>
    public static class RecipeParameterProfiles
    {
        /// <summary>The profiles compiled into the game server.</summary>
        public static IReadOnlyList<IRecipeParameterProfile> Game => Discover(typeof(IRecipeParameterProfile).Assembly);

        public static IReadOnlyList<IRecipeParameterProfile> Discover(Assembly assembly)
        {
            var profiles = assembly.GetTypes()
                .Where(type => typeof(IRecipeParameterProfile).IsAssignableFrom(type) &&
                               !type.IsInterface &&
                               !type.IsAbstract)
                .Select(type => (IRecipeParameterProfile)Activator.CreateInstance(type))
                .OrderBy(profile => profile.Key, StringComparer.Ordinal)
                .ToList();

            EnsureUniqueKeys(profiles);
            return profiles;
        }

        /// <summary>Keys are matched without regard to case on the command line, so two keys that differ only by case collide.</summary>
        public static void EnsureUniqueKeys(IEnumerable<IRecipeParameterProfile> profiles)
        {
            var duplicate = profiles.GroupBy(profile => profile.Key, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidOperationException(
                    $"Recipe parameter profiles use the key '{duplicate.Key}' more than once (keys are compared without regard to case).");
            }
        }
    }
}
