using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>The shell's module mutation lock, which packing, validation and Build All raise.</summary>
    internal sealed class SwlorPaletteWriteGate : IPaletteWriteGate
    {
        private readonly ModuleMutationLock _mutationLock;

        public SwlorPaletteWriteGate(ModuleMutationLock mutationLock)
        {
            _mutationLock = mutationLock ?? throw new ArgumentNullException(nameof(mutationLock));
        }

        public event Action? Changed
        {
            add => _mutationLock.Changed += value;
            remove => _mutationLock.Changed -= value;
        }

        public bool IsLocked => _mutationLock.IsLocked;
    }
}
