using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

public class AnimationDebugViewModel : GuiViewModelBase<AnimationDebugViewModel, GuiPayloadBase>
{
    public const string ContentElement = "animation_debug_content";
    public const string MainContentPartial = "ANIMATION_DEBUG_MAIN";
    public const int PageSize = 20;
    private int _page;
    private string _previewToken;
    private AnimationPreviewCatalog.Entry[] _visible = Array.Empty<AnimationPreviewCatalog.Entry>();

    public string SearchText { get => Get<string>(); set { Set(value); _page = 0; RefreshResults(); } }
    public string ResultText { get => Get<string>(); set => Set(value); }
    public string PageText { get => Get<string>(); set => Set(value); }
    public string StatusText { get => Get<string>(); set => Set(value); }
    public bool HasPrevious { get => Get<bool>(); set => Set(value); }
    public bool HasNext { get => Get<bool>(); set => Set(value); }
    public GuiBindingList<string> Names { get => Get<GuiBindingList<string>>(); set => Set(value); }
    public GuiBindingList<string> Durations { get => Get<GuiBindingList<string>>(); set => Set(value); }

    protected override void Initialize(GuiPayloadBase initialPayload)
    {
        SearchText = "";
        StatusText = "Stand outside combat, then choose Play. Stop releases the preview pose.";
        if (!AnimationPreviewCatalog.CanUse(Player))
        {
            Gui.CloseWindow(Player, GuiWindowType.AnimationDebug, Player);
            return;
        }
        WatchOnClient(m => m.SearchText);
        ChangePartialView(ContentElement, MainContentPartial);
    }

    private void RefreshResults()
    {
        var matches = AnimationPreviewCatalog.Search(SearchText);
        var pages = Math.Max(1, (matches.Length + PageSize - 1) / PageSize);
        _page = Math.Clamp(_page, 0, pages - 1);
        _visible = matches.Skip(_page * PageSize).Take(PageSize).ToArray();
        var names = new GuiBindingList<string>();
        var durations = new GuiBindingList<string>();
        foreach (var entry in _visible)
        {
            names.Add(entry.DisplayName);
            durations.Add($"{entry.Clip.Duration:0.##}s");
        }
        Names = names;
        Durations = durations;
        ResultText = matches.Length == 0 ? "No animations match your search." : $"{matches.Length} animations";
        PageText = $"Page {_page + 1} / {pages}";
        HasPrevious = _page > 0;
        HasNext = _page + 1 < pages;
    }

    public Action OnPrevious() => () => { _page--; RefreshResults(); };
    public Action OnNext() => () => { _page++; RefreshResults(); };
    public Action OnPlayRow() => () =>
    {
        if (!AnimationPreviewCatalog.CanUse(Player)) return;
        var row = NuiGetEventArrayIndex();
        if (row < 0 || row >= _visible.Length) return;
        if (GetIsInCombat(Player) || GetCurrentHitPoints(Player) <= 0)
        {
            StatusText = "Stand outside combat and recover before previewing an animation.";
            return;
        }
        var entry = _visible[row];
        _previewToken = NamedAnimation.Play(Player, entry.Clip);
        StatusText = $"Last played: {entry.DisplayName} ({entry.Clip.Duration:0.##}s). Use Play to repeat.";
    };
    public Action OnStop() => () =>
    {
        if (!AnimationPreviewCatalog.CanUse(Player)) return;
        StatusText = NamedAnimation.StopIfCurrent(Player, _previewToken) ? "Preview stopped." : "No preview is playing.";
        _previewToken = null;
    };
    public override Action OnWindowClosed() => () =>
    {
        NamedAnimation.StopIfCurrent(Player, _previewToken);
        _previewToken = null;
    };
}
