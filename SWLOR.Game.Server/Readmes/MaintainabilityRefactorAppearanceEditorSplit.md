# Maintainability Refactor: AppearanceEditorViewModel Split (P5-16e)

Part map, approved by the owner with the decisions in section 6; PR 1 is built (section 7). Base: `origin/feature/maintainability-refactor` at
`c153dd169`. Limits come from [MaintainabilityRefactorPhase5.md](MaintainabilityRefactorPhase5.md): 1,000 lines and 10 services
per type, 150 lines per method, no partial-class or chunked-method tricks. Line counts below are from a syntax outline of the
six partial files (ranges rounded to about 10); the build report gives exact numbers.

## 1. What the view model does today

`Feature/GuiDefinition/ViewModel/AppearanceEditorViewModel` is 6,183 lines over six partial files, with no constructor
(it uses static helpers, `GameServices.Gui` and NWScript directly). Methods over 150 lines: `InitializeRegions` 167,
`RegisterColorMappings` 188.

| # | Responsibility | Where | Lines |
|---|---|---|---|
| 1 | Per-channel color bindings: 120 `*Region` properties (5 lines each), 120 `*Selected`, 240 `*Tint` and `*Custom` | ColorBindings, ColorSelection, ColorSwatches | ~960 |
| 2 | The color mapping table: `ColorRegion`, `RegisterColorMappings` (188), `InitializeRegions` (167), `ColorIdToCoordinates`, `BuildPaletteRegion` | ColorBindings | ~400 |
| 3 | Other bindings and UI state: 74 + 15 + 97 properties, fields, tab groups, toggles | main, Outfits, PartBrowser | ~860 |
| 4 | Event handlers (35 `Action` members) with their logic inline | all | ~440 |
| 5 | Boot handlers and the static weapon catalog (`LoadAppearances`, `CloseAppearanceWindowOnPossessionBefore`, `LoadWeaponAppearances`) | main | ~70 |
| 6 | Item eligibility rules (`IsValidItem`, `AllowsArmorParts`, `AllowsItemFlags`, weapon part id and option lookups, `EditedArmorParts`) | main | ~130 |
| 7 | Tint map editor: load, custom color apply, RGB components, picker gesture state machine, client watches | main | ~520 |
| 8 | Option and part lists: color and part categories, `LoadBodyParts`, `LoadItemParts`, `PopulateItemParts`, `GetPartLists`, item type editor, tab selection | main | ~560 |
| 9 | Color and part application: `ModifyItemPart`, palette apply and reset, clear to inheritance, copy sides, swatch color resolution, `AdjustArmorPart`, `GetArmorModelType`, per-part override markers | main | ~1,100 |
| 10 | Part browser and body map: browser source and publish, `RefreshPartBrowser`, `RefreshBodyMap`, swatch sync, window fit and placement | PartBrowser | ~650 |
| 11 | Swatch and selection updates (`UpdateColorSwatch`, `UpdateColorSelection`) | ColorSwatches, ColorSelection | ~90 |
| 12 | Outfits: load, describe, wear with one undo | Outfits | ~190 |
| 13 | Initialize, settings load and save, close, refresh events, DM armor display | main | ~300 |

Why the view model cannot stay under 1,000 as it is: items 1, 3 and 4 alone are about 2,300 lines. Item 1 is the real
obstacle (see decision 1).

## 2. The part map

Rules: the view model keeps only bindings, event-handler forwarders, `Initialize` and the refresh handlers. Each extracted type
injects only what it uses and never uses `GameServices` (architecture rule for container-built types). The view model is
built through the factory with constructor injection, like `ConversationViewModel` (`MaintainabilityRefactorContentInjection.md`).
The static helpers it calls today (`TintMap*`, `EquippedItemAppearance`, `WeaponOriginalParts`, `ArmorAppearanceCatalog`) stay
as they are until P5-12; extracted types call them unchanged.

**Per-window state** moves into one plain class, `AppearanceEditorSession` (target, selected color target and channel, tab id,
item type index, tint selections and edit generation, picker gesture fields, outfit ids, browser state, binding-watch
generations). The view model creates it in `Initialize`, and services take it as a parameter. Services are stateless.

**Writing bindings.** Services never see the view model. They return values or call `IAppearanceBindingSink`
(`Publish<T>(string name, T value)`, `Read<T>(string name)`), which the view model implements with its existing
`Set(value, name)` and `Get<T>(name)`. Unit tests use a fake sink.

| Type | Kind and domain | Members (from) | Lines | Injected | Reached by |
|---|---|---|---|---|---|
| `AppearanceEditorViewModel` (stays) | view model, Feature | bindings, `Initialize`, handler forwarders, `Refresh` x4, `OnClientPropertyUpdated`, boot handlers | ~900 (target; ~800 with one-line properties) | `IGuiService` via base + 8 below = 9 | factory |
| `ArmorColorRegionCatalog` | data catalog, `Feature/AppearanceDefinition/ItemAppearance`, exempt (pure table) | `ColorRegion`, the target x channel table (`RegisterColorMappings`), `ColorIdToCoordinates`, `BuildPaletteRegion`, `InitializeRegions` reduced to a loop over the catalog | ~230 | none (static) | static call, as `ArmorAppearanceCatalog` |
| `WeaponAppearanceCatalog` + `IWeaponAppearanceCatalog` | catalog service, `Feature/AppearanceDefinition/ItemAppearance` | `_weaponAppearances`, `LoadWeaponAppearances` | ~50 | none | constructor of eligibility |
| `AppearanceItemEligibility` + interface | rules, `Domain/Character` | `IsValidItem`, `AllowsArmorParts`, `AllowsItemFlags`, `GetWeaponPartId`, `GetWeaponPartOptions`, `EditedArmorParts` | ~150 | `IWeaponAppearanceCatalog` | constructor |
| `AppearanceTintEditorService` | service, `Domain/Character` | `LoadTintMapEditor`, `RefreshTintMapAvailability`, `SetSelectedTintColor`, `ApplyCustomTintColor`, `CaptureTintColorEdit`, `SynchronizeCustomTintComponents`, `CommitCustomTintComponents`, `TryGetEditableTintSelections`, `TryGetSelectedTintLayer` | ~330 | `ITintMapStateService`, others found at build | constructor |
| `AppearanceTintPickerService` | service, `Domain/Character` | `BeginTintPickerGesture`, `StageTintPickerColor`, `QueueTintPickerColor`, `ReleaseTintPickerGesture`, `CancelTintPickerGesture`, `FlushPendingPickerColor`, `StartArmorClientWatches`, `SuspendArmorClientWatches`, `ResumeArmorClientWatches` | ~230 | scheduler seam, `IGuiService` | constructor |
| `AppearanceColorService` | service, `Domain/Character` | `ApplySelectedPaletteColor`, `ApplyArmorPaletteColor`, `ResetCurrentCustomTintOverrides`, `ResetCustomTintOverrides` x2, `ResetArmorColorToInheritance`, `CopyColors`, `CopyColor`, `CopyArmorSide`, `GetArmorSwatchColor`, `ResolveArmorSwatchColorId`, `Mark/ClearPerPartColorOverride`, `ModifyHelmetCloakColor`, the data part of `UpdateColorSwatch` | ~620 | `ITintMapStateService` and what the bodies call | constructor |
| `AppearancePartEditService` | service, `Domain/Character` | `ModifyItemPart`, `AdjustArmorPart`, `ResolveAdjustedArmorValue`, `GetArmorModelType`, `GetArmorPartBinding`, `GetInventorySlot`, `GetModelType`, the scale handlers' logic | ~420 | none known (NWScript and statics) | constructor |
| `AppearancePartListService` | service, `Domain/Character` | `LoadColorCategoryOptions`, `LoadPartCategoryOptions`, `GetPartLists`, `LoadBodyParts`, `LoadItemParts`, `PopulateItemParts`, `LoadBodyPart`, `LoadArmorPart`, `LoadPart` | ~480 | `IWeaponAppearanceCatalog`, `AppearanceItemEligibility` | constructor |
| `AppearancePartBrowserService` | service, `Domain/Character` | `RefreshPartBrowser`, `RefreshBodyMap`, `RefreshBodyMapSelection`, `SetBrowserSource`, `PublishBrowser`, `FitWindowToScreen`, `PlaceBeside`, swatch sync helpers | ~480 | `IGuiService` | constructor |
| `AppearanceOutfitService` | service, `Domain/Items` (check for an existing outfit service first) | `LoadOutfits`, `DescribeOutfit`, `UpdateWardrobeCapacity`, `WearOutfit` and the store, delete and rename steps | ~200 | what the bodies call (outfit entity access) | constructor |

Totals: 6,183 lines become about 900 (view model) and about 3,200 across extracted types, each under 650. The rest is the
removed per-channel properties and table rows (decision 1). Constructor counts: view model 9 of 10 (`IGuiService` plus 8),
every other type 1 to 3. If the build shows more than 10, merge the picker service back into the tint service, not a new type.

Methods over 150 lines: `InitializeRegions` becomes a loop of under 10 lines; `RegisterColorMappings` becomes
`ArmorColorRegionCatalog.Build()` over a literal data table (a data catalog member that only lists data, the exemption in
Phase 5), not chunked. Other large bodies (`AdjustArmorPart` ~110, `UpdateTargetedColor` ~100, `LoadBodyParts` ~100) are under
the limit and move whole.

## 3. State and hot reload

| State | Today | After |
|---|---|---|
| `_target`, color target and channel, tab id, item type index, tint selections, edit generation, picker gesture, outfit ids, browser state | fields on the per-window, per-player view model | fields on `AppearanceEditorSession`, owned by the view model, created in `Initialize` |
| Binding values (`Get`/`Set`) | `GuiViewModelBase` property dictionary | unchanged; reached through `IAppearanceBindingSink` |
| `_weaponAppearances` (static dictionary, filled by a `Rerun` boot handler) | static on the view model | instance of `WeaponAppearanceCatalog`, built once by the container |
| `EditorTabs`, `EditorToggles`, `SettingsToggles`, `OutfitsToggles`, `EditedArmorParts`, `SwatchChannels` | static readonly, immutable | stay on the view model, or in the catalog where the rules use them |
| `LoadAppearances` boot handler | calls `RacialAppearanceRegistry.EnsureLoaded` and loads weapons | stays on the view model with the same name and `BootReload.Rerun`, but only calls `EnsureLoaded` (the catalog needs no boot step). The `BootHandlerClassification.txt` line is unchanged. |

Hot reload (`HotReload.md`): the editor takes a payload (`AppearanceEditorPayload.Target`), so an open editor is closed on
reload and reopens fresh, as today. No window state survives a reload, so the session object needs no carrier. The new services
are stateless singletons that a reload rebuilds. Unchanged behavior: the possession handlers still close the editor and part
browser, and the part browser (a separate window, `AppearancePartBrowserViewModel`) keeps its current link to `ApplyBrowserPart`
and `SelectPreviewView`, which stay `internal` on the view model.

## 4. PR order, tests and proof

Three PRs, each built on the one before (one PR would move about 4,500 lines and is not reviewable).

| PR | Content | View model after | Estimate |
|---|---|---|---|
| 1 | Data and rules: `ArmorColorRegionCatalog`, `WeaponAppearanceCatalog`, `AppearanceItemEligibility`; per-channel properties replaced by name-driven bindings (decision 1); `AppearanceEditorSession` introduced with the existing fields moved in | ~4,500 | Sonnet build, 1 day, review heavy on the mapping proof |
| 2 | Tint and color: `AppearanceTintEditorService`, `AppearanceTintPickerService`, `AppearanceColorService`, `AppearancePartEditService`; the sink | ~2,700 | Sonnet build, 1.5 days |
| 3 | Lists, browser, outfits: `AppearancePartListService`, `AppearancePartBrowserService`, `AppearanceOutfitService`; handlers become forwarders; one-line property style; the line-limit guard | ~900 | Sonnet build, 1 day |

Tests that guard the editor today (all must keep their meaning; mechanical edits only where they name a moved member):

- `SWLOR.Game.Server.Tests`: `AppearanceEditorColorStateTests`, `AppearanceEditorItemValidityTests`, `AppearanceEditorSettingsTests`, `AppearanceEditorWeaponEligibilityTests`, `AppearancePreviewTests`, `AppearanceArmorAdjustmentTests`, `AppearanceEditorLayoutTests`, `AppearanceEditorPossessionFeedbackTests`, `AppearanceTintGestureTests`, `ItemAppearanceCatalogTests`, `TintMapReviewTests`, `WeaponOriginalPartsTests`.
- Some read the view model by reflection (`ResolveArmorSwatchColorId`, `BuildPaletteRegion`, `SelectedPartColorsTitle`) and by parsing the source text of `AppearanceEditorViewModel.cs` and `.PartBrowser.cs` (`AppearanceEditorLayoutTests`, around lines 97, 155, 173). A moved member needs its test pointed at the new type or file; this is expected and checked in each PR.
- `SWLOR.Game.Server.EngineTests`: `AppearanceEditorEngineTests`, `AppearanceEditorDmEngineTests`, `EquippedItemAppearanceEngineTests`, `PowerGloveAppearanceEngineTests`, `TwinPistolAppearanceEngineTests`. They use reflection on `ResetArmorColorToInheritance`, `CommitCustomTintComponents`, `ModifyItemPart`, the 120 `*Region` and `*Selected` properties (a test asserts exactly 120), `Map{slot}Selected`, `Body{name}Selected`, `editor.GlobalLeather2Tint`, and `nameof(editor.GlobalCloth1Tint)` publications.

New tests: a bindings snapshot (every binding name the view model publishes in `Initialize`, in order, with its initial value) generated from the
base commit and compared after each PR; fake-sink unit tests per extracted service for the moved rules; a guard that fails over 1,000 lines or 10
services on the view model.

Proving no behavior change, per PR:

1. Before PR 1, a throwaway generator run on the base commit writes `AppearanceEditorColorMappings.txt`: every (target, channel) with its
   property name and initial region, the matching `Selected`, `Tint` and `Custom` names, and the order `Initialize` publishes them. It is committed
   as a test fixture. After the split a test rebuilds the same text from `ArmorColorRegionCatalog` and compares byte for byte. This includes the
   fact that every target maps to the regions of its own name (there is no cross-mapping).
2. Dye and tint code is not touched: the diff of `Domain/Companions/TintMap*`, `Module/` and `SWLOR_Haks` is empty (checked with `git diff --stat`
   against the base), and moved method bodies are moved verbatim (a normalized-body comparison of old and new text per moved method, run as a script and attached to the PR).
3. Engine tests above run before and after with identical pass sets. `AppearanceEditorEngineTests` already covers palette, RGB, copy and inheritance reset.
4. Client evidence (`InEngineVisualVerification.md`): the same eight states captured before and after on one character and one DM possession: Appearance tab, armor
   colors with a part selected, custom RGB picker, preset palette, Equipment weapon tab, part browser, Outfits tab, Settings tab.
   The captures are compared and shown in the PR.
5. `dotnet test` with filters: the classes named above, `BootHandlerClassification`, the P5 baseline guards and the architecture tests
   (`ConvertedServicesTests`).

## 5. Risks and open decisions

Risks:

- **Dye and color mappings.** The split must change no dye or color mapping. Mitigation is the byte-identical mapping fixture, the empty tint-code diff and the client captures above. No `Module/`, 2DA or hak data is touched.
- **Removing the 480 declared properties** (decision 1) changes how tests read them, not how the client sees them: the window definition binds by string
  (`Bind<GuiRectangle>(name + "Region")`) and `GuiViewModelBase.Set(value, name)` already works without a declared property. The client-update path
  (`UpdatePropertyFromClient`) looks properties up with `GetProperty` but only runs for client-watched bindings, none of which are among the 480.
  The bindings snapshot proves it.
- **Publish order.** `Initialize` publishes bindings in a fixed order, and some client behavior may depend on it. The loop over the catalog keeps the table's order.
- **Source-text tests** pin file and member names (section 4); they are updated in the same PR as the move.
- **Services at the limit.** The view model uses 9 of 10 services. Adding one later needs a merge of two.

Open decisions (recommendation first):

1. Replace the 120 `Region`, 120 `Selected`, 120 `Tint` and 120 `Custom` declared properties with name-driven bindings driven by `ArmorColorRegionCatalog`. **Recommended: yes.** Without it the view model stays near 1,250 lines even with one-line properties. The alternative is an owner exemption for "pure binding declarations", which Phase 5 does not list.
2. Rewrite the remaining multi-line properties in the one-line style the Outfits file already uses (`{ get => Get<bool>(); set => Set(value); }`). **Recommended: yes.** It cuts about 250 lines and changes no behavior.
3. Keep the `LoadAppearances` boot handler on the view model (only `EnsureLoaded`) so the boot classification snapshot does not change. **Recommended: yes.**
4. Three PRs, not one. **Recommended: three.**
5. Outfit service in `Domain/Items` unless an outfit service already exists there at build time. **Recommended: check first, reuse if present.**
6. Static helpers (`TintMap*`, `EquippedItemAppearance`) stay static until P5-12; no conversion in this item. **Recommended: yes.**

## 6. Owner decisions (2026-11-17)

1. The 480 per-channel properties go, once the framework's name-based `Get`/`Set` was confirmed to be the same code path and a
   binding-identity test proves the bindings and values unchanged. 2. No one-line property reformatting: if the view model is
   still over 1,000 lines after the real moves, another responsibility moves out, or the numbers go to the owner. 3. The
   `LoadAppearances` boot handler stays on the view model. 4. Three PRs, built and gated one at a time. 5. `TintMap*` and
   `EquippedItemAppearance` stay static until P5-12. `AppearanceOutfitService`: look for an existing outfit owner first.
   `IAppearanceBindingSink` goes in the domain of the services (below Feature). The view model's constructor follows
   `MaintainabilityRefactorContentInjection.md`. Client captures of the eight editor states before, after PR 1 and after PR 3.

Decision 6 (2026-11-18), on the view model's size after the real moves: "We need to be pragmatic. Sometimes files will be longer than
desired but it's ok to keep in this case." `AppearanceEditorViewModel` stays at its measured size (about 1,337 lines) with its declared
properties; no more logic moves and no name-driven bindings. It is recorded in `Exemptions.json` (type size only, category "owner
exception"); its services (10) and methods (150 lines) are still measured.

## 7. PR 1 as built

- `ArmorColorRegionCatalog` and `ArmorColorRegion` (`Feature/AppearanceDefinition/ItemAppearance`): the swatches are generated
  from the 20 targets and 6 channels in publishing order, with the four binding names each. The map's "literal table" was not
  needed: the old table was regular (name = target + channel), and the fixture proves it.
- `AppearanceColorTarget` (`Domain/Character`): the editor's `ColorTarget` enum moved out of the view model so the catalog and
  the later services can name it.
- `WeaponAppearanceCatalog` and `AppearanceItemEligibility` are static types in `Feature/AppearanceDefinition/ItemAppearance`,
  not services: they use only NWScript and the internal `IWeaponAppearanceDefinition`, `ArmorAppearanceCatalog` and
  `TrainingSaberCatalog` of the Feature layer, which a Domain type may not reference. The view model keeps its constructor
  until PR 2, where the first injected service arrives.
- `AppearanceEditorSession` (`Feature/GuiDefinition/ViewModel`): the 41 per-window fields. It stays in Feature for the same
  reason (it holds `AppearancePreviewView`, `AppearanceEditorSlot` and `PlayerOutfit`).
- The window definition binds swatches by expression. `GuiHelper<T>.GetPropertyName` now also accepts a call to the view
  model's `GetBindingValue<T>("Name")`, which names a binding that has no declared property (an additive change to shared GUI
  code; declared properties take the old path).
- Proof: `ArmorColorRegionCatalogTests` (mapping and the 480 declared names against a fixture captured on the base commit) and
  `AppearanceEditorBindingIdentityEngineTests` (every binding name, order and JSON value through the opening and 16 colour steps,
  against a transcript captured on the base commit).

## 8. PR 2 as built

- `Feature/AppearanceEditing` (new folder, `DomainMap.json` UI): three content singletons the container builds (`ContentInjection.json`,
  `ContentRegistrations.cs`): `AppearanceTintEditorService` (tint editor and picker gestures, one type: the two call each other, and the
  container cannot build singletons that need each other), `AppearanceColorService`, `AppearancePartEditService`; plus
  `AppearanceEditorWindowHandlers` (the DM possession handler, moved off the view model because made content may not hold a static
  member that reaches a service; one `HandlerRenames.txt` line and the two `ScriptHandlerOrder.txt` lines), `IAppearanceEditorHost` and
  `AppearanceEditorBindings`.
- A service method takes the view model as `IAppearanceEditorHost`, which the view model implements explicitly by delegating to its own
  members, so every property setter keeps its logic. The moved bodies are the old ones with receivers changed (`host.`, `host.Session`,
  a `host` argument). Differences beyond receivers: the 19 armor-selection watches (`WatchArmorSelections`) and the 19-case part switch
  (`GetArmorPartBinding` now returns the selection too) stay in the view model as host methods, `nameof(Binding)` became
  `AppearanceEditorBindings.Binding`, and the window-open check is `host.IsWindowOpen()`.
- The view model is made content: only `AppearanceEditorViewModel.cs` is in `ContentInjection.json`, because the tool refuses a
  partial class whose parts are all listed. The other partials call the services through small private forwarders
  (`LoadTintMapEditor`, `AdjustArmorPart`) in the main part. Tests build it with `Content.Create<AppearanceEditorViewModel>()` and
  reach a service through the property the container injected.

## 9. PR 3 as built

- The view model is one file, `AppearanceEditorViewModel.cs` (1,337 lines, 9 services: color, outfit, part browser, part edit, part list,
  settings, swatch, tint editor, `IGuiService`; it was 3,469 lines in six partial files and 9 services). The five partial files are
  gone. It is over the 1,000-line limit and exempt from the type size measure by owner decision 6 (section 6); the numbers behind that are below.
- New content singletons in `Feature/AppearanceEditing` (the services take the view model as `IAppearanceEditorHost`, as in PR 2, and
  never call each other upward; a call back into the lists goes through the host):
  `AppearancePartListService` (659 lines, 10 services), `AppearancePartBrowserService` (525, 5), `AppearanceSwatchService` (497, 3),
  `AppearanceOutfitService` (294, 2: `IDBService`, `IWardrobeService`, which already owns the wardrobe rules) and
  `AppearanceSettingsService` (237, 3). The map listed three; the swatch indicators and the settings and scale code are their own
  types because the part browser service would have passed 700 lines and the view model would otherwise have stayed over 1,000.
  `GetItem`, `GetOffHandAppearanceOwner` and `IsValidItem` live with the part list service, so the view model no longer injects the
  power glove, twin pistol, creature color and database services.
- `WeaponAppearanceCatalog` is an instance behind `IWeaponAppearanceCatalog`, registered in `ServiceRegistrations` (the content
  registration rule wants a constructor that takes a registered type, and the catalog needs none). `AppearanceItemEligibility` is a
  content singleton that takes the catalog; its `IsValid` is an instance method, its flag and part lookups stay static. Injected
  members carry the interface's name (`IWeaponAppearanceCatalog.Definitions`), which `ContentInjectionRules` requires for an
  interface that does not end in `Service`. The `RunClassConstructor` boot step is gone: `LoadAppearances` only calls
  `RacialAppearanceRegistry.EnsureLoaded`. No handler moved, so `HandlerRenames.txt`, `ScriptHandlerOrder.txt` and
  `BootHandlerClassification.txt` are unchanged.
- Moved bodies are the old ones with receivers changed (`host.`, `host.Session`, a `host` argument, static helpers qualified by their
  new owner). A normalized comparison of 109 moved bodies finds 97 identical after those edits; the rest differ only by the same
  receiver edits (static arrays qualified, a handler body split into its service call), the removed `EnsureLoaded` line, and the
  two methods split between the view model and a service (`OnCloseWindow`, `InitializeSettings`).
- Two logic moves beyond the map, and no reformatting (owner decision 2): the 19 armor selection setters called one inline body that
  differed only in the part; it is now `AppearancePartEditService.ArmorSelectionChanged(host, part, value)`, and each setter is the
  old setter with that one call, `AppearancePartEditService.ArmorSelectionChanged(this, AppearanceArmor.<its part>, value)`, in place of
  the inline `if (_session.ArmorClientBindingsWatched && !_session.SkipAdjustArmorPart && value != GetItemAppearance(GetItem(),
  ItemAppearanceType.ArmorModel, (int)AppearanceArmor.<its part>)) AdjustArmorPart(<its part>, 0)`; the same condition and call now sit
  once in the service (`AppearanceEditorColorStateTests` checks the 19 call sites and the condition). The palette sheet choice of a
  color category is `AppearanceSwatchService.UpdateColorSheet`, moved whole. All properties keep their original multi-line form.
- Numbers for the owner (view model, 1,337 lines): 745 lines are declared bindings (20 part option lists at 5 lines each, 19 armor
  selection setters at 11, about 90 plain properties at 5, and the rest); 117 are handler forwarders; 27 are the explicit host members;
  362 are logic, of which these still sit in the view model: `Initialize` 75, `SelectEditorTab` 45, the item type page change in the
  `SelectedItemTypeIndex` setter 39, `SelectedTintColor` setter 32, `GetArmorPartBinding` 26 and `WatchArmorSelections` 24 (both need
  the view model's own properties), `OnEditorPartialApplied` 19, `RefreshTintMapEditorAfterAppearanceChange` 15,
  `EquipmentTypeValue` 14, `LoadItemTypeEditor` 13, `OnClientPropertyUpdated` 13, `Refresh(ArmorDisplayChanged)` 10,
  `ToggleItemEquippedFlags` 10, `InitializeSettings` 10, `OnModalClosedRestore` 7. Moving every one of those that can move (about 250
  lines: `Initialize`, `SelectEditorTab`, the item type change, the picker setter and the refresh bodies; the tab groups and the
  `WatchOnClient` calls cannot leave) would leave about 1,090 lines, so no cohesive move reaches 1,000 without declaring fewer
  bindings. The owner decided (decision 6) to keep the view model at its measured size, 1,337 lines, with no further moves.
- Guard: `AppearanceEditorSizeTests` fails when the view model is not a single file or grows past its measured 1,337 lines (the exemption
  above), any other editor type is over 1,000 lines, any method is over 150 lines, or any editor type takes more than 10 services.
- Proof: `AppearanceEditorBindingIdentityEngineTests` has two more transcripts, one through the part lists, browser, helmet, cloak,
  weapon and off hand pages, and one through the wardrobe with the headless player fixture. Both are masked for the armor part option
  lists, which depend on which models the server's hak set holds (the other two transcripts, captured earlier, include them and fail
  on a server home whose haks lack those models, on the base as well). The test also writes an unmasked transcript beside a mismatch,
  and all four, taken on the base commit and on this branch on one server home, are byte for byte identical.
