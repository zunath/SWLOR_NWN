using Nwn.Toolset.Avalonia.Areas.Generation;

namespace SWLOR.Toolset.AreaGeneration;

/// <summary>Connects the shared Area Generator to the shell's editors, module write guard and explorer.</summary>
public sealed class ShellAreaGeneratorSession : IAreaGeneratorSession
{
    private readonly Func<Task<bool>> _saveAllEditors;
    private readonly Func<IDisposable> _allowModuleWrites;
    private readonly Action<string> _openCreatedArea;

    public ShellAreaGeneratorSession(
        Func<Task<bool>> saveAllEditors,
        Func<IDisposable> allowModuleWrites,
        Action<string> openCreatedArea)
    {
        _saveAllEditors = saveAllEditors ?? throw new ArgumentNullException(nameof(saveAllEditors));
        _allowModuleWrites = allowModuleWrites ?? throw new ArgumentNullException(nameof(allowModuleWrites));
        _openCreatedArea = openCreatedArea ?? throw new ArgumentNullException(nameof(openCreatedArea));
    }

    public Task<bool> SaveOpenEditorsAsync() => _saveAllEditors();

    public IDisposable AllowModuleWrites() => _allowModuleWrites();

    public Task OpenCreatedAreaAsync(string resRef)
    {
        _openCreatedArea(resRef);
        return Task.CompletedTask;
    }
}
