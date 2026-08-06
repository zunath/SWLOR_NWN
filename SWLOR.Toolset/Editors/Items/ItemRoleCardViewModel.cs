using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Editors.Behaviors;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The right-hand card for the role currently selected on the Behavior tab. What it shows is
    /// entirely decided by the role: a spell picker for Consumable/Grenade, an "unlocks" line for the
    /// roles that add Stats-tab groups, a fixed pair of statements for CreatureItem, and otherwise
    /// just the role's own summary (plus a note that Custom exposes every field, for Custom).
    /// </summary>
    public sealed partial class ItemRoleCardViewModel : ObservableObject
    {
        /// <summary>
        /// itempropdef.2da row 15's CostTableResRef - verified against
        /// SWLOR_Haks/sw_2da/itempropdef.2da, row "CastSpell". Not a general item-property fact, so
        /// it stays local to the one thing on this card that writes property 15.
        /// </summary>
        private const int CastSpellPropertyId = 15;

        private const int CastSpellCostTableId = 3;

        private readonly ItemRole _role;
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IReadOnlyList<BehaviorChoiceViewModel> _allSpells;

        [ObservableProperty]
        private string _spellSearchText = string.Empty;

        [ObservableProperty]
        private IReadOnlyList<string> _statements = Array.Empty<string>();

        public string Title => _role.DisplayName;

        public string? Summary => _role.Summary;

        public bool ShowsSpellPicker =>
            _role.Id is ItemRoleCatalog.ConsumableId;

        public ObservableCollection<BehaviorChoiceViewModel> SpellChoices { get; } = new();

        public ItemRoleCardViewModel(
            ItemRole role,
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices)
        {
            _role = role ?? throw new ArgumentNullException(nameof(role));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));

            _allSpells = ShowsSpellPicker
                ? BehaviorChoiceViewModel.From(resolveChoices?.Invoke(ItemChoiceKeys.Spells) ?? Array.Empty<BehaviorChoice>())
                : Array.Empty<BehaviorChoiceViewModel>();

            if (ShowsSpellPicker)
                RefreshSpellSelection();

            BuildStatements();
        }

        partial void OnSpellSearchTextChanged(string value) => RefreshSpellChoices();

        [RelayCommand]
        private void SelectSpell(BehaviorChoiceViewModel? choice)
        {
            if (choice == null || !ShowsSpellPicker)
                return;

            var spellId = (int)choice.Value;
            var current = _store.Properties
                .FirstOrDefault(property => property.PropertyId == CastSpellPropertyId);
            var hasCurrent = current.PropertyId == CastSpellPropertyId;
            var existingCostValue = hasCurrent ? current.CostValue : 1;

            if (!_runEdit($"Set spell to {choice.Display}", () =>
                {
                    // Property 15's Subtype IS the spell id, so switching spells switches the
                    // Subtype the entry is keyed by - the old entry has to be removed explicitly
                    // rather than overwritten, or it survives alongside the new one.
                    if (hasCurrent && current.SubtypeId != spellId)
                        _store.SetPropertyValue(CastSpellPropertyId, current.SubtypeId, 0, null);

                    _store.SetPropertyValue(CastSpellPropertyId, spellId, CastSpellCostTableId, existingCostValue);
                }))
            {
                return;
            }

            RefreshSpellSelection();
            BuildStatements();
        }

        /// <summary>The stored Subtype (spell row id), or null when property 15 is not set.</summary>
        private int? StoredSpellId
        {
            get
            {
                var current = _store.Properties
                    .FirstOrDefault(property => property.PropertyId == CastSpellPropertyId);
                return current.PropertyId == CastSpellPropertyId ? current.SubtypeId : null;
            }
        }

        private void RefreshSpellSelection()
        {
            var current = StoredSpellId;
            foreach (var spell in _allSpells)
                spell.IsSelected = current.HasValue && spell.Value == current.Value;

            RefreshSpellChoices();
        }

        /// <summary>
        /// Every match, not a first page. The list is virtualized in the template, and a cap here
        /// was invisible to the builder: scrolling simply stopped partway with nothing to say why
        /// or any way to reach the rest.
        /// </summary>
        private void RefreshSpellChoices()
        {
            SpellChoices.Clear();

            var filtered = string.IsNullOrWhiteSpace(SpellSearchText)
                ? _allSpells
                : _allSpells.Where(spell =>
                    spell.Display.Contains(SpellSearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var spell in filtered)
                SpellChoices.Add(spell);
        }

        private void BuildStatements()
        {
            var statements = new List<string>();

            if (ShowsSpellPicker)
            {
                var current = StoredSpellId;
                if (current.HasValue)
                {
                    var display = _allSpells.FirstOrDefault(spell => spell.Value == current.Value)?.Display
                        ?? $"Spell {current.Value}";
                    statements.Add($"Cast Spell — {display}");
                }
            }
            else
            {
                var unlocked = ItemRoleCatalog.GroupsUnlockedBy(_role.Id);
                if (unlocked.Count > 0)
                {
                    statements.Add(
                        $"Unlocks: {string.Join(", ", unlocked.Select(ItemStatGroupViewModel.TitleFor))}");
                }
                else if (_role.Id == ItemRoleCatalog.CreatureItemId)
                {
                    statements.Add("Stats — NPC group on the Stats tab");
                    statements.Add("Economy — restricted automatically");
                }
            }

            if (_role.AllowsVariables)
                statements.Add("Variables — tab shown");

            Statements = statements;
        }
    }
}
