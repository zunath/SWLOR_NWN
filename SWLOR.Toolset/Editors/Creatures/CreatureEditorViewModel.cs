using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Render;
using SWLOR.Toolset.Editors.Appearance;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Toolset.Viewport;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>The creature editor shared by all creature blueprints.</summary>
    public sealed partial class CreatureEditorViewModel : ObservableObject, IModelPreviewSource, IDisposable
    {
        private readonly CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly IGameCodeIndex? _gameCodeIndex;
        private readonly Func<string, IReadOnlyList<BehaviorChoice>>? _resolveChoices;
        private readonly Func<JsonGffStruct, RenderModel?>? _resolveModel;
        private readonly ChoicePreviewService? _choicePreviews;
        private readonly Func<BehaviorChoice, string?>? _previewAudio;
        private readonly Dictionary<string, IReadOnlyList<BehaviorRowViewModel>> _roleRowCache =
            new(StringComparer.Ordinal);
        private bool _previewSceneUpdateQueued;
        private int _previewModelGeneration;
        private readonly SemaphoreSlim _previewModelGate = new(1);
        private ModelPreviewControl? _previewView;
        private bool _disposed;

        public ObservableCollection<BehaviorRowViewModel> BasicRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> AppearanceRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> FlagRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> AiRows { get; } = new();
        public ObservableCollection<BehaviorRowViewModel> RoleRows { get; } = new();
        public ObservableCollection<BehaviorListItemViewModel> RoleList { get; } = new();
        public ObservableCollection<CreatureAnimationOption> PreviewAnimations { get; } = new();
        public ObservableCollection<CreatureDeprecatedVariableViewModel> DeprecatedVariables { get; } = new();

        public CreatureEquipmentSet Equipment { get; }
        public CreatureStatsViewModel Stats { get; }
        public CreatureAbilitiesViewModel Abilities { get; }
        public CreatureAiViewModel Ai { get; }
        public CreatureLootViewModel Loot { get; }
        public CreatureBodyPartsViewModel BodyParts { get; }
        public CreatureEquipmentSlotsViewModel EquipmentSlots { get; }
        public VarTableSectionViewModel Variables { get; }
        public AppearanceGallerySectionViewModel? AppearanceGallery { get; }
        public bool HasAppearanceGallery => AppearanceGallery != null;
        public bool ShowsVariablesTab => SelectedRole.AllowsVariables;

        [ObservableProperty]
        private CreatureRole _selectedRole = CreatureRoleCatalog.Default;

        [ObservableProperty]
        private CreatureAnimationOption? _selectedPreviewAnimation;

        [ObservableProperty]
        private int _selectedAppearanceSectionIndex;

        [ObservableProperty]
        private bool _isDirty;

        [ObservableProperty]
        private bool _isModelPreviewLoading;

        public string HeaderName => "Creature";
        public string HeaderKind => "blueprint";
        public string HeaderOwner { get; }
        public string CreatureName
        {
            get
            {
                var first = _store.GetLocalizedText("FirstName");
                var last = _store.GetLocalizedText("LastName");
                return string.Join(" ", new[] { first, last }.Where(part => !string.IsNullOrWhiteSpace(part)));
            }
        }
        public string TemplateResRef => _store.GetString(BehaviorFieldStorage.Field, "TemplateResRef");
        public string QuestUsage { get; private set; } = string.Empty;
        public bool HasQuestUsage => QuestUsage.Length > 0;
        public string DeprecatedWarning { get; private set; } = string.Empty;
        public bool HasDeprecatedWarning => DeprecatedWarning.Length > 0;
        public string ReferenceWarning { get; private set; } = string.Empty;
        public bool HasReferenceWarning => ReferenceWarning.Length > 0;
        public string StatWarning =>
            (_store.GetInteger(BehaviorFieldStorage.Field, "Plot") == 1 ||
             _store.GetInteger(BehaviorFieldStorage.Field, "IsImmortal") == 1) && Stats.HasStatSkin
                ? "Plot or immortal creatures ignore some authored combat outcomes. Check that these stats are intentional."
                : string.Empty;
        public bool HasStatWarning => StatWarning.Length > 0;

        public ResourceIndex? ResourceIndex { get; }
        public AreaScene? PreviewScene { get; private set; }
        public string? PreviewAnimationName => SelectedPreviewAnimation?.AnimationName;
        public bool IsAnimationPlaying => true;

        public Avalonia.Controls.Control PreviewView
        {
            get
            {
                if (_previewView != null)
                    return _previewView;
                _previewView = new ModelPreviewControl { DataContext = this };
                _previewView.SetHostVisible(true);
                return _previewView;
            }
        }

        public CreatureEditorViewModel(
            JsonGffStruct creature,
            string filePath,
            string headerOwner,
            Func<string, Action, bool> runEdit,
            IGameCodeIndex? gameCodeIndex,
            Func<string, IReadOnlyList<BehaviorChoice>>? resolveChoices,
            ResourceIndex? resourceIndex,
            Func<JsonGffStruct, RenderModel?>? resolveModel,
            Func<int, AppearanceRow?> appearance,
            ArmorPartCatalog? armorParts,
            Func<IReadOnlyList<CreatureEquipmentChoice>>? equipmentChoices = null,
            ChoicePreviewService? choicePreviews = null,
            Func<BehaviorChoice, string?>? previewAudio = null,
            Action<string>? openLootDefinition = null,
            IReadOnlyList<AppearanceOption>? appearanceOptions = null,
            ThumbnailService? appearanceThumbnails = null,
            ArmorDyeSwatchService? colorPalettes = null,
            Func<string, string>? resolveItemName = null)
        {
            _store = new CreatureValueStore(creature);
            _runEdit = runEdit;
            _gameCodeIndex = gameCodeIndex;
            _resolveChoices = resolveChoices;
            _resolveModel = resolveModel;
            _choicePreviews = choicePreviews;
            _previewAudio = previewAudio;
            HeaderOwner = headerOwner;
            ResourceIndex = resourceIndex;

            Equipment = new CreatureEquipmentSet(_store, filePath);
            Stats = new CreatureStatsViewModel(_store, Equipment, RunEdit, gameCodeIndex?.Skills);
            Abilities = new CreatureAbilitiesViewModel(_store, RunEdit);
            Ai = new CreatureAiViewModel(_store, RunEdit);
            Loot = new CreatureLootViewModel(
                _store, RunEdit, openDefinition: openLootDefinition, resolveItemName: resolveItemName);
            BodyParts = new CreatureBodyPartsViewModel(
                _store, RunEdit, appearance, armorParts, colorPalettes, OnBodyPartChanged);
            EquipmentSlots = new CreatureEquipmentSlotsViewModel(
                _store,
                RunEdit,
                equipmentChoices ?? (() => Array.Empty<CreatureEquipmentChoice>()),
                OnEquipmentChanged);
            Variables = new VarTableSectionViewModel(RunEdit, _store.Locals, gameCodeIndex, IsCustomVariable);
            if (appearanceOptions != null)
            {
                AppearanceGallery = new AppearanceGallerySectionViewModel(
                    appearanceOptions,
                    appearanceThumbnails,
                    CurrentAppearanceKey,
                    ApplyAppearance,
                    noun: "appearance");
            }

            BuildRows(CreatureEditorLayout.Basic, BasicRows);
            BuildRows(CreatureEditorLayout.Appearance, AppearanceRows);
            BuildRows(CreatureEditorLayout.Flags, FlagRows);
            BuildRows(CreatureEditorLayout.Ai, AiRows);
            BehaviorListItemViewModel.Build(RoleList, CreatureRoleCatalog.All);
            SelectRole(CreatureRoleCatalog.Default);
            UpdateWarnings();
            UpdatePreviewScene();
        }

        [RelayCommand]
        private void ChooseRole(IBehaviorDescriptor? descriptor)
        {
            if (descriptor is CreatureRole role)
                SelectRole(role);
        }

        public void ReloadFromDocument()
        {
            foreach (var row in AllRows())
                row.Reload();
            Stats.Reload();
            Abilities.Reload();
            Ai.Reload();
            Loot.Reload();
            BodyParts.Reload();
            EnsureSelectedAppearanceSectionLoaded();
            EquipmentSlots.Reload();
            AppearanceGallery?.ReloadFromDocument();
            Variables.RefreshFromDocument();
            UpdateWarnings();
            UpdateQuestUsage();
            UpdatePreviewScene();
            NotifySummary();
        }

        public void RefreshPaletteChoices()
        {
            var index = -1;
            for (var current = 0; current < BasicRows.Count; current++)
            {
                if (BasicRows[current].Definition.Name != "PaletteID")
                    continue;
                index = current;
                break;
            }
            if (index < 0)
                return;
            var definition = BasicRows[index].Definition;
            BasicRows[index].Dispose();
            BasicRows[index] = CreateRow(definition);
        }

        public void NormalizeForSave() => Loot.Normalize();

        public void SetDirty(bool value) => IsDirty = value;

        private bool RunEdit(string description, Action mutation)
        {
            var applied = _runEdit(description, mutation);
            if (applied)
                IsDirty = true;
            return applied;
        }

        private void BuildRows(
            IEnumerable<BehaviorFieldDefinition> definitions,
            ObservableCollection<BehaviorRowViewModel> target)
        {
            foreach (var definition in definitions)
                target.Add(CreateRow(definition));
        }

        private IEnumerable<BehaviorRowViewModel> DirectRows() =>
            BasicRows.Concat(AppearanceRows).Concat(FlagRows).Concat(AiRows);

        private IEnumerable<BehaviorRowViewModel> AllRows() =>
            DirectRows().Concat(_roleRowCache.Values.SelectMany(rows => rows));

        private BehaviorRowViewModel CreateRow(
            BehaviorFieldDefinition definition,
            bool deferSearchableChoices = false)
        {
            var defersChoices = deferSearchableChoices &&
                                definition.IsSearchable &&
                                definition.ChoicesKey != null;
            var choices = defersChoices
                ? null
                : definition.ChoicesKey == null
                    ? definition.Choices
                    : _resolveChoices?.Invoke(definition.ChoicesKey) ?? Array.Empty<BehaviorChoice>();
            Func<IReadOnlyList<BehaviorChoice>>? choiceLoader = defersChoices
                ? () => _resolveChoices?.Invoke(definition.ChoicesKey!) ?? Array.Empty<BehaviorChoice>()
                : null;
            var row = new BehaviorRowViewModel(
                definition,
                _store,
                RunEdit,
                choices,
                OnDirectValueChanged,
                _choicePreviews,
                _previewAudio,
                choiceLoader);
            row.Reload();
            return row;
        }

        private void SelectRole(CreatureRole role)
        {
            RoleRows.Clear();
            SelectedRole = role;
            if (!_roleRowCache.TryGetValue(role.Id, out var rows))
            {
                rows = role.Fields.Select(definition => CreateRow(definition, true)).ToList();
                _roleRowCache[role.Id] = rows;
            }
            foreach (var row in rows)
                RoleRows.Add(row);
            BehaviorListItemViewModel.Select(RoleList, role.Id);
            OnPropertyChanged(nameof(ShowsVariablesTab));
            UpdateQuestUsage();
        }

        private void OnDirectValueChanged()
        {
            BodyParts.Reload();
            EnsureSelectedAppearanceSectionLoaded();
            UpdateWarnings();
            UpdateQuestUsage();
            QueuePreviewSceneUpdate();
            NotifySummary();
        }

        private void OnEquipmentChanged()
        {
            QueuePreviewSceneUpdate();
        }

        private void OnBodyPartChanged()
        {
            QueuePreviewSceneUpdate();
        }

        private string CurrentAppearanceKey() =>
            (_store.GetInteger(BehaviorFieldStorage.Field, "Appearance_Type") ?? 0)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

        private bool ApplyAppearance(AppearanceOption option)
        {
            if (option.CreatureAppearanceId is not { } id)
                return false;

            if (!RunEdit(
                    $"Change appearance to {option.Caption}",
                    () => _store.SetInteger(
                        BehaviorFieldStorage.Field,
                        "Appearance_Type",
                        GffFieldType.Word,
                        id)))
            {
                return false;
            }

            OnDirectValueChanged();
            return true;
        }

        private void UpdateQuestUsage()
        {
            var group = _store.Locals.GetInt("QUEST_NPC_GROUP_ID");
            if (group.HasValue && _gameCodeIndex?.NpcGroupQuestIds.TryGetValue(group.Value, out var ids) == true)
            {
                QuestUsage = ids.Count == 0
                    ? string.Empty
                    : "Used by " + string.Join(", ", ids.Select(id =>
                        _gameCodeIndex.FindQuest(id) is { } quest ? quest.Name : id));
            }
            else
            {
                QuestUsage = string.Empty;
            }
            OnPropertyChanged(nameof(QuestUsage));
            OnPropertyChanged(nameof(HasQuestUsage));
        }

        private void UpdateWarnings()
        {
            var deprecated = _store.Locals.Select(entry => entry.Name)
                .Where(IsDeprecatedVariable)
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToList();
            DeprecatedVariables.Clear();
            foreach (var name in deprecated)
                DeprecatedVariables.Add(new CreatureDeprecatedVariableViewModel(name, DeprecatedDisplayName(name)));
            DeprecatedWarning = deprecated.Count == 0
                ? string.Empty
                : deprecated.Count == 1
                    ? "This creature has one obsolete behavior setting. Remove it if it is no longer needed."
                    : $"This creature has {deprecated.Count} obsolete behavior settings. Remove those no longer needed.";
            var referenceWarnings = new List<string>();
            if (MissingFieldChoice("Conversation", CreatureChoiceKeys.Dialogs))
                referenceWarnings.Add("The selected conversation is not available.");
            if (MissingLocalChoice("CONVERSATION", CreatureChoiceKeys.DialogDefinitions))
                referenceWarnings.Add("The selected scripted dialog is not registered.");
            var missingStores = Enumerable.Range(1, 5).Count(rank =>
                MissingLocalChoice($"STORE_TAG_RANK_{rank}", CreatureChoiceKeys.GuildStores));
            if (missingStores > 0)
                referenceWarnings.Add(missingStores == 1
                    ? "One guild store does not resolve to a placed merchant."
                    : $"{missingStores} guild stores do not resolve to placed merchants.");
            ReferenceWarning = string.Join(" ", referenceWarnings);
            OnPropertyChanged(nameof(DeprecatedWarning));
            OnPropertyChanged(nameof(HasDeprecatedWarning));
            OnPropertyChanged(nameof(ReferenceWarning));
            OnPropertyChanged(nameof(HasReferenceWarning));
            OnPropertyChanged(nameof(StatWarning));
            OnPropertyChanged(nameof(HasStatWarning));
        }

        private bool MissingFieldChoice(string fieldName, string choiceKey)
        {
            var stored = _store.GetString(BehaviorFieldStorage.Field, fieldName);
            return MissingChoice(stored, choiceKey);
        }

        private bool MissingLocalChoice(string variableName, string choiceKey)
        {
            var stored = _store.Locals.GetString(variableName);
            return MissingChoice(stored, choiceKey);
        }

        private bool MissingChoice(string? stored, string choiceKey)
        {
            if (string.IsNullOrWhiteSpace(stored) || _resolveChoices == null)
                return false;
            var choices = _resolveChoices(choiceKey);
            return choices.Count > 0 && choices.All(choice =>
                !string.Equals(choice.StringValue, stored, StringComparison.OrdinalIgnoreCase));
        }

        [RelayCommand]
        private void RemoveDeprecated(CreatureDeprecatedVariableViewModel? variable)
        {
            if (variable == null || !DeprecatedVariables.Contains(variable))
                return;
            if (!RunEdit($"Remove obsolete {variable.DisplayName}", () => _store.Locals.Remove(variable.Name)))
                return;
            Variables.RefreshFromDocument();
            UpdateWarnings();
        }

        private static string DeprecatedDisplayName(string name)
        {
            if (name.StartsWith("QUEST_ID_", StringComparison.Ordinal))
                return "legacy quest requirement";
            if (name.StartsWith("KEY_ITEM_", StringComparison.Ordinal))
                return "legacy key-item requirement";
            if (name.StartsWith("SKILL_", StringComparison.Ordinal))
                return "legacy skill requirement";
            if (name.StartsWith("STEAL_LOOT_TABLE_", StringComparison.Ordinal))
                return "legacy steal loot";
            if (name.StartsWith("LOOT_TABLE_", StringComparison.Ordinal) ||
                name.StartsWith("LOOT_TABLE_ID", StringComparison.Ordinal))
                return "legacy loot setting";
            return name switch
            {
                "NPC_GROUP" => "legacy quest group",
                "BEHAVIOUR" or "BEHAVIOR" => "legacy behavior",
                "STORE_TAG" => "legacy merchant store",
                "barActivity" => "legacy ambient activity",
                "IS_SHIP" => "legacy ship marker",
                _ => "obsolete setting"
            };
        }

        private static bool IsDeprecatedVariable(string name)
        {
            var malformedLootRow = name.StartsWith("LOOT_TABLE_", StringComparison.Ordinal) &&
                                   !System.Text.RegularExpressions.Regex.IsMatch(
                                       name,
                                       "^LOOT_TABLE_[0-9]+$");
            return malformedLootRow ||
                   name.StartsWith("QUEST_ID_", StringComparison.Ordinal) ||
                   name.StartsWith("KEY_ITEM_", StringComparison.Ordinal) ||
                   name.StartsWith("SKILL_", StringComparison.Ordinal) ||
                   name.StartsWith("STEAL_LOOT_TABLE_", StringComparison.Ordinal) ||
                   name.StartsWith("LOOT_TABLE_ID", StringComparison.Ordinal) ||
                   name.StartsWith("LOOT_TABLE_ATTEMPTS_", StringComparison.Ordinal) ||
                   name.StartsWith("LOOT_TABLE_CHANCE_", StringComparison.Ordinal) ||
                   name is "NPC_GROUP" or "BEHAVIOUR" or "BEHAVIOR" or "STORE_TAG" or "barActivity" or "IS_SHIP";
        }

        private static bool IsCustomVariable(string name)
        {
            if (IsDeprecatedVariable(name))
                return true;
            if (name.StartsWith("PERK_LEVEL_", StringComparison.Ordinal))
                return false;
            if (System.Text.RegularExpressions.Regex.IsMatch(name, "^LOOT_TABLE_[0-9]+$"))
                return false;
            return name is not (
                "QUEST_NPC_GROUP_ID" or "CONVERSATION" or "GUILD_ID" or
                "STORE_TAG_RANK_1" or "STORE_TAG_RANK_2" or "STORE_TAG_RANK_3" or
                "STORE_TAG_RANK_4" or "STORE_TAG_RANK_5" or "BEAST_TYPE" or
                "PERMANENT_VFX_ID" or "PARALYZE" or "DAZE" or "AI_PROFILE" or "AI_FLAGS");
        }

        /// <summary>
        /// Lets the picker close and publish its selected item before any preview work starts.
        /// Multiple equipment or appearance changes in one UI turn collapse into one model rebuild.
        /// </summary>
        private void QueuePreviewSceneUpdate()
        {
            if (_disposed || _previewSceneUpdateQueued)
                return;

            _previewSceneUpdateQueued = true;
            Dispatcher.UIThread.Post(() =>
            {
                _previewSceneUpdateQueued = false;
                UpdatePreviewScene();
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Snapshots the UTC on the UI thread, then parses and composes its model on a worker.
        /// A serialized gate discards superseded queued requests before they start, matching the
        /// progressive preview strategy used by the item editor.
        /// </summary>
        private void UpdatePreviewScene()
        {
            if (_disposed)
                return;

            if (_resolveModel == null)
            {
                _previewModelGeneration++;
                IsModelPreviewLoading = false;
                ApplyPreviewScene(null);
                return;
            }

            var generation = ++_previewModelGeneration;
            IsModelPreviewLoading = true;
            ApplyPreviewScene(null);

            // The resolver never observes a partially applied edit, and no worker touches the
            // live document while undo/redo or another field mutation is in progress.
            var snapshot = new JsonGffDocument("UTC ", _store.Creature).ToBytes();
            _ = ResolvePreviewModelAsync(snapshot, generation);
        }

        private async Task ResolvePreviewModelAsync(byte[] snapshot, int generation)
        {
            RenderModel? model;
            await _previewModelGate.WaitAsync().ConfigureAwait(false);
            try
            {
                // Rapid item picks cost at most the active composition plus the newest state.
                if (generation != Volatile.Read(ref _previewModelGeneration))
                    return;

                model = await Task.Run(() =>
                {
                    var creature = JsonGffDocument.Parse(snapshot).Root;
                    return _resolveModel!(creature);
                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                model = null;
            }
            finally
            {
                _previewModelGate.Release();
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_disposed || generation != _previewModelGeneration)
                    return;

                IsModelPreviewLoading = false;
                ApplyPreviewScene(model);
            });
        }

        private void ApplyPreviewScene(RenderModel? model)
        {
            PublishAnimations(model);
            PreviewScene = model == null
                ? null
                : new AreaScene
                {
                    Tileset = string.Empty,
                    Width = 1,
                    Height = 1,
                    Tiles = Array.Empty<TilePlacement>(),
                    Instances = new[]
                    {
                        new InstanceMarker
                        {
                            Kind = InstanceMarkerKind.Creature,
                            TemplateResRef = TemplateResRef,
                            Tag = _store.GetString(BehaviorFieldStorage.Field, "Tag"),
                            Position = new Vector3(AreaSceneBuilder.TileSize / 2f, AreaSceneBuilder.TileSize / 2f, 0f),
                            // The default model-preview camera sits on the -Y side while creature
                            // fronts are authored toward +Y, so turn the model around to greet the user.
                            Orientation = new Vector2(-1f, 0f),
                            Model = model
                        }
                    },
                    Diagnostics = new AreaSceneDiagnostics()
                };
            OnPropertyChanged(nameof(PreviewScene));
        }

        private void PublishAnimations(RenderModel? model)
        {
            var previous = SelectedPreviewAnimation?.Display;
            PreviewAnimations.Clear();
            if (model != null)
            {
                var names = model.Animations.Select(animation => animation.Name).ToList();
                var idle = !string.IsNullOrWhiteSpace(model.DefaultAnimationName)
                    ? model.DefaultAnimationName
                    : names.FirstOrDefault(name =>
                        name.Contains("pause", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("idle", StringComparison.OrdinalIgnoreCase));
                var walk = names.FirstOrDefault(name =>
                    name.Contains("walk", StringComparison.OrdinalIgnoreCase));
                var attack = names.FirstOrDefault(name =>
                    name.Contains("attack", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("slash", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("stab", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("kick", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("shot", StringComparison.OrdinalIgnoreCase));

                // The three fixed segments are part of the editor contract. A model without one of
                // the optional clips keeps that segment visible and simply falls back to its rest pose.
                PreviewAnimations.Add(new CreatureAnimationOption("Idle", idle));
                PreviewAnimations.Add(new CreatureAnimationOption("Walk", walk));
                PreviewAnimations.Add(new CreatureAnimationOption("Attack", attack));
            }
            SelectedPreviewAnimation = PreviewAnimations.FirstOrDefault(option => option.Display == previous)
                                       ?? PreviewAnimations.FirstOrDefault();
        }

        partial void OnSelectedPreviewAnimationChanged(CreatureAnimationOption? value) =>
            OnPropertyChanged(nameof(PreviewAnimationName));

        partial void OnSelectedAppearanceSectionIndexChanged(int value) =>
            EnsureSelectedAppearanceSectionLoaded();

        private void EnsureSelectedAppearanceSectionLoaded()
        {
            if (SelectedAppearanceSectionIndex != 2)
                return;

            if (!BodyParts.IsDynamic)
            {
                SelectedAppearanceSectionIndex = 0;
                return;
            }

            _ = BodyParts.EnsureLoadedAsync();
        }

        public void ReloadGameResources()
        {
            if (_disposed)
                return;

            _previewModelGeneration++;
            UpdatePreviewScene();
        }

        private void NotifySummary()
        {
            OnPropertyChanged(nameof(CreatureName));
            OnPropertyChanged(nameof(TemplateResRef));
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _previewModelGeneration++;
            _previewSceneUpdateQueued = false;
            IsModelPreviewLoading = false;
            foreach (var row in AllRows())
                row.Dispose();
            Loot.Dispose();
            AppearanceGallery?.Dispose();
            Equipment.Dispose();
            _previewView?.Dispose();
            _previewView = null;
            PreviewScene = null;
        }
    }
}
