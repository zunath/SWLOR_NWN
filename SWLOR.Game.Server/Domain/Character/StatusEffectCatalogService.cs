using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The definition catalog of the status effects (part 2 of the status effect split): the search over the loaded assemblies that
    /// finds every status effect type when the module caches, and what the catalog answers about a type. Holds no tracker and calls
    /// no other service, so every other part may read it. The search sits behind one-line <c>protected virtual</c> method whose
    /// body is the original call.
    /// </summary>
    public class StatusEffectCatalogService : IStatusEffectCatalogService
    {
        private readonly Dictionary<Type, StatusEffectMetadata> _statusEffects = new();

        /// <summary>
        /// When the module caches, status effects will be discovered and cached.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void CacheData()
        {
            CacheStatusEffects();
        }

        private void CacheStatusEffects()
        {
            _statusEffects.Clear();

            foreach (var type in GetStatusEffectTypes())
            {
                var effect = (StatusEffectBase)Activator.CreateInstance(type);

                _statusEffects[type] = new StatusEffectMetadata(
                    () => (IStatusEffect)Activator.CreateInstance(type),
                    effect.Name,
                    effect.Frequency,
                    effect.SourceType,
                    effect.Categories);
            }

            Console.WriteLine($"Loaded {_statusEffects.Count} status effects.");
        }

        /// <summary>
        /// Creates a fresh instance of a registered status effect so a caller can set per-application options before applying it.
        /// </summary>
        public IStatusEffect CreateStatusEffect(Type statusEffectClass)
        {
            if (!TryCreateStatusEffect(statusEffectClass, out var statusEffect))
                throw new KeyNotFoundException($"Status effect '{statusEffectClass?.Name ?? "null"}' is not registered.");

            return statusEffect;
        }

        public bool TryCreateStatusEffect(Type statusEffectClass, out IStatusEffect statusEffect)
        {
            if (statusEffectClass != null &&
                _statusEffects.TryGetValue(statusEffectClass, out var metadata))
            {
                statusEffect = metadata.Create();
                return true;
            }

            statusEffect = null;
            return false;
        }

        public bool HasStatusEffectDefinition(Type statusEffectClass)
        {
            return statusEffectClass != null && _statusEffects.ContainsKey(statusEffectClass);
        }

        public Type FindDefinitionByName(string name)
        {
            return _statusEffects.Keys.FirstOrDefault(key => key.Name == name);
        }

        public bool TryGetCategories(Type statusEffectClass, out StatusEffectCategory categories)
        {
            if (_statusEffects.TryGetValue(statusEffectClass, out var statusEffect))
            {
                categories = statusEffect.Categories;
                return true;
            }

            categories = StatusEffectCategory.None;
            return false;
        }

        public string GetStatusEffectName(Type statusEffectClass)
        {
            return statusEffectClass != null && _statusEffects.TryGetValue(statusEffectClass, out var statusEffect)
                ? statusEffect.Name
                : statusEffectClass?.Name ?? "Unknown";
        }

        public StatusEffectSourceType GetStatusEffectSourceType(Type statusEffectClass)
        {
            return statusEffectClass != null && _statusEffects.TryGetValue(statusEffectClass, out var statusEffect)
                ? statusEffect.SourceType
                : StatusEffectSourceType.Invalid;
        }

        public List<Type> GetStatusEffectsFromIcon(EffectIconType effectIcon)
        {
            return _statusEffects
                .Where(x => x.Value.Create().Icon == effectIcon)
                .Select(x => x.Key)
                .ToList();
        }

        // The search over every loaded assembly finds the definitions the module cache registers.
        protected virtual IEnumerable<Type> GetStatusEffectTypes()
        {
            return GameAssemblies.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(StatusEffectBase).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);
        }

        private readonly struct StatusEffectMetadata
        {
            public Func<IStatusEffect> Create { get; }
            public string Name { get; }
            public float Frequency { get; }
            public StatusEffectSourceType SourceType { get; }
            public StatusEffectCategory Categories { get; }

            public StatusEffectMetadata(
                Func<IStatusEffect> create,
                string name,
                float frequency,
                StatusEffectSourceType sourceType,
                StatusEffectCategory categories)
            {
                Create = create;
                Name = name;
                Frequency = frequency;
                SourceType = sourceType;
                Categories = categories;
            }
        }
    }
}
