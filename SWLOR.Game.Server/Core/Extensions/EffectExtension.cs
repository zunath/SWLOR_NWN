using System.Linq;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Removes all effects with the specified tag(s) from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove effects from.</param>
        /// <param name="tags">The tags to look for.</param>
        public static void RemoveEffectByTag(uint creature, params string[] tags)
        {
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var effectTag = GetEffectTag(effect);
                if (tags.Contains(effectTag))
                {
                    RemoveEffect(creature, effect);
                }
            }
        }
    }
}
