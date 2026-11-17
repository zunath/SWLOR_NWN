namespace SWLOR.Game.Server.Tests.Support;

/// <summary>A view model with one property of each shape a NUI element binds, so a binding's name can be checked.</summary>
public sealed class GuiJsonModel : FakeViewModel
{
    public string Text { get; set; } = string.Empty;
    public bool Flag { get; set; }
    public float Amount { get; set; }
    public int Count { get; set; }
}
