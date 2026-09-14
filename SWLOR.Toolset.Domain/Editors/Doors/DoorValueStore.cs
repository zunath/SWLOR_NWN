using System.Globalization;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Domain.Editors.Doors
{
    /// <summary>Door-specific storage layered over the shared blueprint/placement value store.</summary>
    public sealed class DoorValueStore : BehaviorValueStore
    {
        public const string RequiredKeyItemPrefix = "REQUIRED_KEY_ITEM_ID_";
        public const string DefaultCloser = "dt_refermeporte";

        private static readonly HashSet<string> KnownClosers = new(StringComparer.OrdinalIgnoreCase)
        {
            DefaultCloser,
            "pug_closedoor8s",
            "gy_2minlockclose",
            "gy_2minclosedoor",
            "relock"
        };

        public DoorValueStore(JsonGffStruct door)
            : base(door)
        {
        }

        public JsonGffStruct Door => Owner;

        public bool HasRequiredKeyItemLocals =>
            Locals.Any(entry => entry.Name.StartsWith(RequiredKeyItemPrefix, StringComparison.Ordinal));

        public IReadOnlyList<int> GetRequiredKeyItemIds()
        {
            return Locals
                .Select(entry => new
                {
                    Entry = entry,
                    Index = ParseRequiredKeyItemIndex(entry.Name)
                })
                .Where(item => item.Index.HasValue && item.Entry.IntValue.HasValue)
                .OrderBy(item => item.Index)
                .Select(item => item.Entry.IntValue!.Value)
                .ToList();
        }

        public void SetRequiredKeyItemIds(IEnumerable<int> ids)
        {
            ArgumentNullException.ThrowIfNull(ids);
            ClearRequiredKeyItemLocals();

            var index = 1;
            foreach (var id in ids)
                Locals.SetInt(RequiredKeyItemPrefix + (index++).ToString(CultureInfo.InvariantCulture), id);
        }

        public void ClearRequiredKeyItemLocals()
        {
            var names = Locals
                .Where(entry => entry.Name.StartsWith(RequiredKeyItemPrefix, StringComparison.Ordinal))
                .Select(entry => entry.Name)
                .ToList();

            foreach (var name in names)
                Locals.Remove(name);
        }

        public bool IsSelfClosing =>
            IsKnownCloser(GetString(BehaviorFieldStorage.Field, "OnOpen"));

        public void SetSelfClosing(bool enabled)
        {
            var current = GetString(BehaviorFieldStorage.Field, "OnOpen");
            if (enabled)
            {
                if (!IsKnownCloser(current))
                    SetString(BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, DefaultCloser);
            }
            else if (IsKnownCloser(current))
            {
                SetString(BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, string.Empty);
            }
        }

        public static bool IsKnownCloser(string? script) =>
            !string.IsNullOrWhiteSpace(script) && KnownClosers.Contains(script);

        public DoorAppearanceChoice? GetAppearance(IReadOnlyList<DoorAppearanceChoice> choices)
        {
            ArgumentNullException.ThrowIfNull(choices);

            var specific = GetInteger(BehaviorFieldStorage.Field, "Appearance") ?? 0;
            if (specific > 0)
                return choices.FirstOrDefault(choice =>
                    choice.Kind == DoorAppearanceKind.Specific && choice.Id == specific);

            var generic = GetInteger(BehaviorFieldStorage.Field, "GenericType_New")
                          ?? GetInteger(BehaviorFieldStorage.Field, "GenericType")
                          ?? 0;
            return choices.FirstOrDefault(choice =>
                choice.Kind == DoorAppearanceKind.Generic && choice.Id == generic);
        }

        public void SetAppearance(DoorAppearanceChoice choice)
        {
            ArgumentNullException.ThrowIfNull(choice);

            if (choice.Kind == DoorAppearanceKind.Generic)
            {
                SetInteger(BehaviorFieldStorage.Field, "Appearance", GffFieldType.Dword, 0);
                SetInteger(BehaviorFieldStorage.Field, "GenericType_New", GffFieldType.Dword, choice.Id);
            }
            else
            {
                SetInteger(BehaviorFieldStorage.Field, "Appearance", GffFieldType.Dword, choice.Id);
                SetInteger(BehaviorFieldStorage.Field, "GenericType_New", GffFieldType.Dword, 0);
            }
        }

        public void Apply(DoorBehavior behavior, bool isInstance)
        {
            ArgumentNullException.ThrowIfNull(behavior);

            foreach (var value in behavior.Manages)
                Apply(value, isInstance);

            if (behavior.Id == DoorBehaviorCatalog.LockedDoorId)
                UpdateKeyRequired();
            else if (behavior.Id == DoorBehaviorCatalog.AreaTransitionId)
            {
                if (GetInteger(BehaviorFieldStorage.Field, "Locked") == 1)
                    UpdateKeyRequired();
                else
                    SetInteger(BehaviorFieldStorage.Field, "KeyRequired", GffFieldType.Byte, 0);
            }
        }

        /// <summary>
        /// Locals <see cref="Clear(DoorBehavior)"/> would remove beyond the behavior's named fields.
        /// </summary>
        /// <remarks>
        /// Custom sweeps the entire table, and any behavior with owned prefixes sweeps everything
        /// under them. Neither set is derivable from the behavior's field list, so a caller that
        /// wants to warn the builder before the sweep has to ask for it here rather than reconstruct
        /// the rule and get it subtly wrong.
        /// </remarks>
        public static IReadOnlyList<string> LocalsClearedBySwitchingFrom(
            DoorValueStore store, DoorBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(store);
            ArgumentNullException.ThrowIfNull(behavior);

            var names = store.Locals.Select(entry => entry.Name);

            if (behavior.Id != DoorBehaviorCatalog.CustomId)
            {
                names = names.Where(name => behavior.OwnedLocalPrefixes.Any(prefix =>
                    name.StartsWith(prefix, StringComparison.Ordinal)));
            }

            return names.ToList();
        }

        public void Clear(DoorBehavior behavior)
        {
            ArgumentNullException.ThrowIfNull(behavior);
            var closer = behavior.Fields.Any(field => field.Name == "OnOpen") &&
                         IsKnownCloser(GetString(BehaviorFieldStorage.Field, "OnOpen"))
                ? GetString(BehaviorFieldStorage.Field, "OnOpen")
                : null;
            var defaultDeath = behavior.Fields.Any(field => field.Name == "OnDeath") &&
                               string.Equals(
                                   GetString(BehaviorFieldStorage.Field, "OnDeath"),
                                   DoorBehaviorCatalog.DefaultDeathScript,
                                   StringComparison.OrdinalIgnoreCase)
                ? GetString(BehaviorFieldStorage.Field, "OnDeath")
                : null;

            Clear(behavior.Manages, behavior.Fields);

            if (behavior.Id == DoorBehaviorCatalog.CustomId)
            {
                foreach (var name in Locals.Select(entry => entry.Name).ToList())
                    Locals.Remove(name);
            }

            foreach (var prefix in behavior.OwnedLocalPrefixes)
            {
                var names = Locals
                    .Where(entry => entry.Name.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(entry => entry.Name)
                    .ToList();
                foreach (var name in names)
                    Locals.Remove(name);
            }

            if (closer != null)
                SetString(BehaviorFieldStorage.Field, "OnOpen", GffFieldType.ResRef, closer);
            if (defaultDeath != null)
                SetString(BehaviorFieldStorage.Field, "OnDeath", GffFieldType.ResRef, defaultDeath);
        }

        public void UpdateKeyRequired()
        {
            var key = GetString(BehaviorFieldStorage.Field, "KeyName");
            SetInteger(
                BehaviorFieldStorage.Field,
                "KeyRequired",
                GffFieldType.Byte,
                string.IsNullOrWhiteSpace(key) ? 0 : 1);
        }

        public void ClearConditionalLockFields(IEnumerable<DoorFieldDefinition> fields)
        {
            foreach (var field in fields.Where(field =>
                         string.Equals(field.VisibleWhenField, "Locked", StringComparison.Ordinal)))
            {
                ClearOne(field.Storage, field.Name, field.FieldType);
            }

            SetInteger(BehaviorFieldStorage.Field, "KeyRequired", GffFieldType.Byte, 0);
        }

        private static int? ParseRequiredKeyItemIndex(string name)
        {
            if (!name.StartsWith(RequiredKeyItemPrefix, StringComparison.Ordinal))
                return null;

            return int.TryParse(
                name[RequiredKeyItemPrefix.Length..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var index)
                ? index
                : null;
        }
    }
}
