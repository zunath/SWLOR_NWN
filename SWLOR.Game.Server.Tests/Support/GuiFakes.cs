using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>A refresh event only the fake view models subscribe to.</summary>
public sealed class FakeRefreshEvent : IGuiRefreshEvent
{
}

/// <summary>
/// A view model that records what the GUI service does to it, so the window lifecycle can be tested without building
/// real NUI layouts. <see cref="OnClick"/> is the method a registered element event names.
/// </summary>
public class FakeViewModel : IGuiViewModel, IGuiRefreshable<FakeRefreshEvent>
{
    public uint TetherObject { get; set; }
    public GuiRectangle Geometry { get; set; } = new(0, 0, 0, 0);
    public string ModalPromptText => string.Empty;
    public string ModalConfirmButtonText => string.Empty;
    public string ModalCancelButtonText => string.Empty;
    public string ModalInputText => string.Empty;

    public uint BoundTo { get; private set; }
    public int BindCount { get; private set; }
    public int Token { get; private set; }
    public GuiWindowType BoundType { get; private set; }
    public GuiPayloadBase BoundPayload { get; private set; } = null!;
    public uint BoundTether { get; private set; }
    public int Clicks { get; set; }
    public int Closes { get; private set; }
    public int Refreshes { get; private set; }
    public List<string> Updated { get; } = new();

    public void Bind(uint player, int windowToken, GuiRectangle initialGeometry, GuiWindowType type, GuiPayloadBase payload, uint tetherObject)
    {
        BoundTo = player;
        Token = windowToken;
        BoundType = type;
        BoundPayload = payload;
        BoundTether = tetherObject;
        TetherObject = tetherObject;
        BindCount++;
    }

    public void UpdatePropertyFromClient(string propertyName) => Updated.Add(propertyName);

    public void ChangePartialView(string elementId, string partialName)
    {
    }

    public void OnPointerPressed()
    {
    }

    public void OnPointerReleased()
    {
    }

    public Action OnModalClose() => () => { };
    public Action OnModalConfirmClick() => () => { };
    public Action OnModalCancelClick() => () => { };
    public Action OnInputModalConfirmClick() => () => { };
    public Action OnInputModalCancelClick() => () => { };

    public virtual Action OnWindowClosed() => () => Closes++;

    public Action OnClick(int amount) => () => Clicks += amount;

    public void Refresh(FakeRefreshEvent payload) => Refreshes++;
}

/// <summary>A character sheet that is viewing a chosen target and counts the fake refreshes.</summary>
public sealed class FakeCharacterSheet : CharacterSheetViewModel, IGuiRefreshable<FakeRefreshEvent>
{
    public int Refreshes { get; private set; }

    public void Refresh(FakeRefreshEvent payload) => Refreshes++;

    /// <summary>Makes the sheet show <paramref name="target"/>, as binding it with a payload does.</summary>
    public void ViewTarget(uint target) =>
        typeof(CharacterSheetViewModel).GetField("_target", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(this, target);
}

/// <summary>
/// The GUI service with a chosen set of window definitions instead of the reflection search over every loaded assembly.
/// </summary>
public sealed class TestGuiService : GuiService
{
    private readonly Type[] _definitions;

    public TestGuiService(IDBService db, IDMPresenceService dmPresence, params Type[] definitions)
        : base(db, dmPresence)
    {
        _definitions = definitions;
    }

    protected override IEnumerable<Type> GetWindowDefinitionTypes() => _definitions;
}

/// <summary>The Emotes window built from a fake view model.</summary>
public sealed class FakeEmotesDefinition : IGuiWindowDefinition
{
    public GuiConstructedWindow BuildWindow() => FakeWindows.Build(GuiWindowType.Emotes);
}

/// <summary>The Quests window built from a fake view model that stays open when the area changes.</summary>
public sealed class FakeQuestsDefinition : IGuiWindowDefinition
{
    public GuiConstructedWindow BuildWindow()
    {
        var window = FakeWindows.Build(GuiWindowType.Quests);
        window.KeepsOpenOnAreaChange = true;
        return window;
    }
}

/// <summary>The Character Sheet window built from a real character sheet subclass.</summary>
public sealed class FakeCharacterSheetDefinition : IGuiWindowDefinition
{
    public GuiConstructedWindow BuildWindow() => FakeWindows.Build(GuiWindowType.CharacterSheet, () => new FakeCharacterSheet());
}

/// <summary>A window built from the same fake view model, but not resizable and with a bound size.</summary>
public static class FakeWindows
{
    public static GuiConstructedWindow Build(GuiWindowType type, Func<IGuiViewModel>? viewModel = null)
    {
        return new GuiConstructedWindow(
            type,
            GuiWindowKey.BuildWindowId(type),
            default,
            new GuiRectangle(10, 20, 150, 300),
            new Dictionary<string, Json>(),
            new List<string>(),
            new Dictionary<string, string>(),
            () => new GuiPlayerWindow(viewModel == null ? new FakeViewModel() : viewModel()));
    }
}
