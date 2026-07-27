using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Placeables
{
    /// <summary>
    /// Works out which behavior a stored placeable already has, from the script slots and local
    /// variables it carries. Nothing records the answer in the file, so this runs on open.
    /// </summary>
    /// <remarks>
    /// Deliberately liberal. A placeable that carries a behavior's scripts plus an unrelated extra
    /// slot is still that behavior - the extra script stays on the Advanced tab untouched, because
    /// a quarter of the module's scavenge points also run <c>plc_death</c> and calling them Custom
    /// would hide their loot table behind a raw variable grid for no gain.
    /// </remarks>
    public static class PlaceableBehaviorDetector
    {
        /// <summary>Every script slot a placeable can carry, in GFF field-name form.</summary>
        public static readonly IReadOnlyList<string> ScriptSlots = new[]
        {
            "OnClick", "OnClosed", "OnDamaged", "OnDeath", "OnDisarm", "OnHeartbeat",
            "OnInvDisturbed", "OnLock", "OnMeleeAttacked", "OnOpen", "OnSpellCastAt",
            "OnTrapTriggered", "OnUnlock", "OnUsed", "OnUserDefined"
        };

        /// <summary>
        /// The behavior that best explains this placeable: a named one when its scripts or
        /// variables match, Custom when it is wired in a way no declaration covers, and None when
        /// it is plain decor.
        /// </summary>
        public static PlaceableBehavior Detect(JsonGffStruct root)
        {
            ArgumentNullException.ThrowIfNull(root);

            var scripts = ReadScripts(root);
            var variables = ReadVariableNames(root);

            PlaceableBehavior? best = null;
            var bestScore = 0;

            foreach (var behavior in PlaceableBehaviorCatalog.Behaviors)
            {
                if (behavior.IsSentinel)
                    continue;

                var score = Score(behavior, scripts, variables);
                if (score > bestScore)
                {
                    best = behavior;
                    bestScore = score;
                }
            }

            if (best != null)
                return best;

            return scripts.Count > 0 || variables.Count > 0
                ? PlaceableBehaviorCatalog.Custom
                : PlaceableBehaviorCatalog.None;
        }

        /// <summary>
        /// Variable names on this placeable that the given behavior does not own. These are what
        /// keep the Variables tab present for a named behavior: hiding them would hide stored data.
        /// </summary>
        public static IReadOnlyList<string> UnmanagedVariables(JsonGffStruct root, PlaceableBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(behavior);

            var owned = new HashSet<string>(behavior.VariableNames, StringComparer.Ordinal);
            return ReadVariableNames(root).Where(name => !owned.Contains(name)).ToList();
        }

        /// <summary>
        /// Whether the document still carries evidence for a specifically chosen named behavior.
        /// Script-backed choices must still carry every script that behavior manages; variable-only
        /// choices need any one of their fields. This lets the editor retain an explicit choice when
        /// multiple behaviors share a script while still allowing undo or changed wiring to
        /// reclassify the placeable.
        /// </summary>
        public static bool MatchesStoredSignature(JsonGffStruct root, PlaceableBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(behavior);

            if (behavior.IsSentinel)
                return false;

            if (behavior.Scripts.Count == 0)
            {
                var variables = new HashSet<string>(ReadVariableNames(root), StringComparer.Ordinal);
                return behavior.Fields.Any(field => variables.Contains(field.VariableName));
            }

            var scripts = ReadScripts(root);
            var alternates = new HashSet<string>(
                behavior.AlternateScripts,
                StringComparer.OrdinalIgnoreCase);

            return behavior.Scripts.All(slot =>
                scripts.TryGetValue(slot.Key, out var value) &&
                (string.Equals(value, slot.Value, StringComparison.OrdinalIgnoreCase) ||
                 alternates.Contains(value)));
        }

        /// <summary>Non-empty script slots on this placeable, keyed by slot field name.</summary>
        public static IReadOnlyDictionary<string, string> ReadScripts(JsonGffStruct root)
        {
            ArgumentNullException.ThrowIfNull(root);

            var scripts = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var slot in ScriptSlots)
            {
                var value = root.GetStringOrNull(slot);
                if (!string.IsNullOrWhiteSpace(value))
                    scripts[slot] = value;
            }

            return scripts;
        }

        private static List<string> ReadVariableNames(JsonGffStruct root)
        {
            return new VarTable(root)
                .Select(entry => entry.Name)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }

        /// <summary>
        /// How well a behavior explains what is stored. Script matches dominate because they are
        /// what the server actually dispatches on; variables break ties and carry the behaviors
        /// that have no scripts of their own (harvest nodes, visibility gates).
        /// </summary>
        private static int Score(
            PlaceableBehavior behavior,
            IReadOnlyDictionary<string, string> scripts,
            IReadOnlyCollection<string> variables)
        {
            var alternates = new HashSet<string>(behavior.AlternateScripts, StringComparer.OrdinalIgnoreCase);

            var scriptHits = behavior.Scripts.Count(slot =>
                scripts.TryGetValue(slot.Key, out var value) &&
                (string.Equals(value, slot.Value, StringComparison.OrdinalIgnoreCase) || alternates.Contains(value)));

            // An alternate can land in a slot the behavior does not itself declare, which is how
            // the base-game sit scripts are recognised.
            if (scriptHits == 0 && alternates.Count > 0 && scripts.Values.Any(alternates.Contains))
                scriptHits = 1;

            var fieldHits = behavior.Fields.Count(field => variables.Contains(field.VariableName));
            var requiredHits = behavior.Fields.Count(field =>
                field.IsRequired && variables.Contains(field.VariableName));

            // A behavior defined by its scripts must match one; a behavior defined only by its
            // variables must match one of those. Otherwise a shared variable such as KEY_ITEM_ID
            // would drag in every behavior that happens to mention it.
            //
            // One rather than all, deliberately. A partially wired placeable - a scavenge point with
            // scav_opened but no scav_closed - is still that behavior with a slot missing, and
            // requiring the full signature would drop it to Custom and take the preset away from the
            // very object that needs it. The gap does not survive: PlaceableBehaviorApplier's
            // EnsureExpectedValues runs on save and writes every script the selected behavior
            // declares, so saving completes the wiring rather than persisting the hole.
            if (behavior.Scripts.Count > 0 && scriptHits == 0)
                return 0;

            if (behavior.Scripts.Count == 0 && requiredHits == 0)
                return 0;

            return scriptHits * 10 + requiredHits * 3 + fieldHits;
        }
    }
}
