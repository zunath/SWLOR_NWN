using Dock.Model.Mvvm.Controls;
using Dock.Model.Core;
using Nwn.Toolset.Avalonia.Docking;
using SWLOR.Toolset.Settings;
using SWLOR.Toolset.Shell.Panels;
using SharedDockFactory = Nwn.Toolset.Avalonia.Docking.ToolsetDockFactory;

namespace SWLOR.Toolset.Shell;

/// <summary>Connects the shared builder docking layout to SWLOR's tool services.</summary>
public sealed class ToolsetDockFactory : SharedDockFactory
{
    private readonly PropertiesViewModel _properties;
    private readonly PaletteViewModel _palette;
    private readonly ScriptReferenceViewModel _scriptReference;
    private readonly ProblemsViewModel _problems;
    private readonly AreaContentsViewModel _areaContents;

    public ToolsetDockFactory(ModuleExplorerViewModel explorer, PropertiesViewModel properties,
        SearchViewModel search, OutputViewModel output, ValidationViewModel validation,
        PaletteViewModel palette, ProblemsViewModel problems, ScriptReferenceViewModel scriptReference,
        AreaContentsViewModel areaContents, ToolsetSettings? settings = null)
        : base(new ToolsetDockPanels([explorer, areaContents], [palette, scriptReference],
            [output, validation, problems], [properties, search]), settings?.DockProportions)
    {
        _properties = properties;
        _palette = palette;
        _scriptReference = scriptReference;
        _problems = problems;
        _areaContents = areaContents;
    }

    public IAreaPlacementTarget? ActivePlacementTarget => ActiveDocument as IAreaPlacementTarget;

    protected override void OnActiveDocumentChanged(Document? document)
    {
        base.OnActiveDocumentChanged(document);
        _palette.OnActiveAreaChanged();
    }

    public override void OpenDocument(Document document)
    {
        if (RootDock is null) return;
        base.OpenDocument(document);
        if (document is Editors.AreaEditorViewModel) Focus(_areaContents);
    }

    public void NotifyActiveAreaChanged() => _palette.OnActiveAreaChanged();
    public void RefreshTlkLabels() => _properties.RefreshTlkLabels();
    public void ShowAreaContents() => Focus(_areaContents);
    public void ShowRightTool(bool scriptReference)
    {
        var target = scriptReference ? (IDockable)_scriptReference : _palette;
        if (target.Owner is IDock dock && dock.ActiveDockable != target) SetActiveDockable(target);
    }
    public void ShowProblems() => SetActiveDockable(_problems);
}
