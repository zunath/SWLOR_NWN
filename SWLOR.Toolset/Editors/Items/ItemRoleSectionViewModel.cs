using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The Behavior tab's role rail and the card for whichever role is currently selected. A role is
    /// never stored on the uti directly - it is classified from itemproperty entries - so switching
    /// TO a role writes nothing here; only switching AWAY from one that owns properties the item
    /// still carries clears anything, and <see cref="RoleChanged"/> lets the shell keep its own
    /// classification (<c>ItemEditorViewModel.Role</c>) in step afterward.
    /// </summary>
    public sealed partial class ItemRoleSectionViewModel : ObservableObject
    {
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly IEditorPromptService? _prompts;
        private readonly Action<ItemRole>? _roleChanged;

        private ItemFamily _family;
        private string _familyDisplay = string.Empty;

        public ObservableCollection<BehaviorListItemViewModel> RoleList { get; } = new();

        [ObservableProperty]
        private ItemRole _role = ItemRoleCatalog.Custom;

        [ObservableProperty]
        private bool _hasRoles;

        [ObservableProperty]
        private ItemRoleCardViewModel? _card;

        private readonly Workspace.OutputLogService? _log;


        public ItemRoleSectionViewModel(
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            IEditorPromptService? prompts,
            Action<ItemRole>? roleChanged,
            Workspace.OutputLogService? log = null)
        {
            _log = log;
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _resolveChoices = resolveChoices;
            _prompts = prompts;
            _roleChanged = roleChanged;
        }

        /// <summary>
        /// Rebuilds the rail and card for a family/role combination - called on load and whenever the
        /// owning editor reclassifies (base item change, external reload).
        /// </summary>
        public void Rebuild(ItemFamily family, ItemRole current, string familyDisplay)
        {
            ArgumentNullException.ThrowIfNull(current);

            _family = family;
            _familyDisplay = familyDisplay;
            Role = current;

            var roles = ItemRoleCatalog.RolesFor(family);
            HasRoles = roles.Count > 0;

            BuildRoleList(roles);
            Card = BuildCard(current);
        }

        [RelayCommand]
        private void ChooseRole(IBehaviorDescriptor? descriptor)
        {
            if (descriptor is not ItemRole role || role.Id == Role.Id)
                return;

            _ = ChooseRoleAsync(role);
        }

        /// <summary>
        /// The switch itself, with a confirmation in front of it when the outgoing role owns
        /// properties the item still actually carries.
        /// </summary>
        public async Task ChooseRoleAsync(ItemRole role)
        {
            ArgumentNullException.ThrowIfNull(role);

            var previous = Role;
            if (role.Id == previous.Id)
                return;

            try
            {
                // A property both roles own (Meal and Enhancement share 108) survives the switch: the
                // prompt promises to clear only what is "not part of" the target role, and deleting a
                // shared property would empty the incoming card of the very value it exists to show.
                var owned = ItemRoleOwnership.OwnedProperties(previous.Id);
                var kept = ItemRoleOwnership.OwnedProperties(role.Id);
                var losing = _store.Properties
                    .Where(property => owned.Contains(property.PropertyId) &&
                                       !kept.Contains(property.PropertyId))
                    .ToList();

                if (losing.Count > 0 && _prompts != null)
                {
                    var labels = losing
                        .Select(property => ItemRoleOwnership.LabelFor(property.PropertyId))
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                    var confirmed = await _prompts.ConfirmDestructiveAsync(
                        $"Change behavior to {role.DisplayName}?",
                        $"This clears {string.Join(", ", labels)}, which " +
                        $"{(labels.Count == 1 ? "is" : "are")} not part of {role.DisplayName}. " +
                        "Undo will put it back until the item is saved.",
                        "Change behavior").ConfigureAwait(true);

                    if (!confirmed)
                    {
                        SelectRail(previous.Id);
                        return;
                    }
                }

                if (losing.Count > 0)
                {
                    var applied = _runEdit($"Change behavior to {role.DisplayName}", () =>
                    {
                        foreach (var property in losing)
                            _store.SetPropertyValue(property.PropertyId, property.SubtypeId, 0, null);
                    });

                    if (!applied)
                    {
                        SelectRail(previous.Id);
                        return;
                    }
                }

                Role = role;
                SelectRail(role.Id);
                Card = BuildCard(role);
                _roleChanged?.Invoke(role);
            }
            catch (Exception ex)
            {
                // A failed prompt or apply must not leave the rail highlighting a role the card
                // never switched to - put the highlight back where the item's Role actually is,
                // the same recovery the declined-confirmation path above already performs.
                _log?.AppendLine($"Role switch to '{role.DisplayName}' failed: {ex.Message}");
                SelectRail(previous.Id);
            }
        }

        private void BuildRoleList(IReadOnlyList<ItemRole> roles)
        {
            RoleList.Clear();
            if (roles.Count == 0)
                return;

            RoleList.Add(BehaviorListItemViewModel.Header($"{_familyDisplay} behaviors"));
            foreach (var role in roles.Where(role => role.Id != ItemRoleCatalog.CustomId))
                RoleList.Add(BehaviorListItemViewModel.For(role));

            RoleList.Add(BehaviorListItemViewModel.Rule());
            RoleList.Add(BehaviorListItemViewModel.For(ItemRoleCatalog.Custom));

            SelectRail(Role.Id);
        }

        private void SelectRail(string roleId) => BehaviorListItemViewModel.Select(RoleList, roleId);

        private ItemRoleCardViewModel BuildCard(ItemRole role) =>
            new(role, _store, _runEdit, _resolveChoices);
    }
}
