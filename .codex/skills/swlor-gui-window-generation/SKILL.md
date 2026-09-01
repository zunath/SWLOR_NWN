---
name: swlor-gui-window-generation
description: Build a new SWLOR NUI GUI window from a wireframe image or written spec through a GuiWindowType enum entry, IGuiWindowDefinition, GuiViewModelBase view model, debug chat command, and warning-free boot validation. Use when creating, modifying, or repairing NUI windows, tabs, tables, modals, or bindings in this repository.
---

# SWLOR GUI Window Generation

## Core Rule

Build with the framework's proven components — never hand-roll their equivalents:

- `GuiStandardLayout.AddStandardLayout` for the window root — **even when the
  wireframe has no tabs** (then: zero `AddTabRow` calls, no `SetTabPanelHeight`, and
  ONE partial holding the entire body, applied in `Initialize`). NEVER hand-roll the
  root layout — not even as stacked rows in a root column: deviating from the
  standard shape freezes the content region at a constant width regardless of window
  resizing (layout rule R5).
- `GuiTabGroup` + `GuiToggleGroupSync` for tabs (rule R4).
- `OnModalClosedRestore` override for any tabbed window that shows modals (rule R6).
- Copy widget patterns from `SWLOR.Game.Server/Feature/GuiDefinition/DebugNuiGalleryDefinition.cs`
  (the living example of every widget and binding path) before inventing anything.
  Every builder call you write must already exist under
  `SWLOR.Game.Server/Service/GuiService/Component/` — grep before writing.

**Zero `[NUI layout warning]` lines for your window at server boot is a hard gate.**
The validator only reports confirmed defects; a warning means your layout will fail.

Leave every data access as a `// TODO: data plumbing` stub returning a plausible
placeholder value — window structure first, data wiring is a separate task.

## Important Files

- `SWLOR.Game.Server/Service/GuiService/GuiWindowType.cs` — window id enum (debug windows: 900 range)
- `SWLOR.Game.Server/Feature/GuiDefinition/` — window definitions (reflection-registered)
- `SWLOR.Game.Server/Feature/GuiDefinition/ViewModel/` — view models
- `SWLOR.Game.Server/Service/GuiService/Component/GuiStandardLayout.cs` — the root-layout helper
- `SWLOR.Game.Server/Service/GuiService/Component/GuiTabGroup.cs` — tab orchestration helpers
- `SWLOR.Game.Server/Feature/ChatCommandDefinition/DebuggingChatCommand.cs` — debug open commands
- `SWLOR.Game.Server/Readmes/GuiWindowAuthoring.md` — full authoring guide (lifecycle, skeleton, binding table)
- `SWLOR.Game.Server/Readmes/NuiLayoutRules.md` — layout rules R1-R7 and verified-working shapes
- `SWLOR.Game.Server/Feature/GuiDefinition/DebugNuiGalleryDefinition.cs` — living widget reference (`/nuigallery`)

## Workflow

1. Read the wireframe image (or written spec). Inventory every visual element into a
   widget list using `references/wireframe-to-widget-mapping.md`: element → builder
   calls → binding type → ViewModel property. Note for each datum whether it is
   static text or bound (bound = anything that changes at runtime).
2. Read `references/layout-rules-digest.md`. Decide the layout split: which regions
   are tab partials, which are side rails, and the fixed panel width (250-560f).
   **If the wireframe has no tab strip:** the standard layout is still mandatory —
   plan ONE partial containing the entire body (stacked sections all live inside that
   single partial), zero tab rows, and follow the `TEMPLATE(no-tabs)` markers in the
   asset templates.
3. Copy `assets/WindowDefinitionTemplate.cs` and `assets/WindowViewModelTemplate.cs`
   into `SWLOR.Game.Server/Feature/GuiDefinition/` and `.../ViewModel/`. Rename the
   files and classes for your window. Add the `GuiWindowType` enum entry. Fill every
   `// TEMPLATE:` marker per the widget inventory; keep every `// TODO: data plumbing`
   stub returning placeholder values.
4. Add a debug chat command in `DebuggingChatCommand.cs` following the `NuiGallery()`
   method (`.Permissions(AuthorizationLevel.Admin)` + `.AvailableToAllOnTestEnvironment()`,
   action = `Gui.TogglePlayerWindow(user, GuiWindowType.YourWindow)`).
5. Work through `references/authoring-checklist.md` — mechanical yes/no sweeps for
   the rules that cause runtime failures (assign-before-watch, margined widgets in
   fixed rows, modal restore, invented APIs).
6. Build once with deployment disabled, then run the GUI corpus tests:
   `dotnet build SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj -p:RunPostBuildEvent=Never`
   followed by `dotnet test SWLOR.Game.Server.Tests/SWLOR.Game.Server.Tests.csproj
   --no-build --filter "FullyQualifiedName~GuiLayoutValidationTests"`.
7. Boot the dev server and check the Server log: there must be ZERO
   `[NUI layout warning]` entries mentioning your window. (The six warnings from `GUI_WINDOW_DebugNuiGallery`
   are intentional regression canaries — ignore those, and only those.) Optionally
   pull your window's `[NUI JSON]` lines from `debugserver/app_logs/Server/` to
   sanity-check the emitted structure.
8. Open the window in-game via your chat command: every tab renders and switches, the
   window resizes without client errors, and every modal path leaves the tab content
   intact.

## References

- `references/wireframe-to-widget-mapping.md` — read at step 1 (element-to-builder table).
- `references/layout-rules-digest.md` — read at step 2 (rules digest + verified-working list).
- `references/authoring-checklist.md` — run at step 5 (pre-build sweeps).
- `assets/WindowDefinitionTemplate.cs`, `assets/WindowViewModelTemplate.cs` — copy at step 3.
