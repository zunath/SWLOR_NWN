using Nwn.Toolset.Avalonia.Palettes.Workflow;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Shell.Panels.PaletteHost
{
    /// <summary>SWLOR's modal prompts, shared with every editor.</summary>
    internal sealed class SwlorPalettePrompts : IPalettePrompts
    {
        private readonly IEditorPromptService _prompts;

        public SwlorPalettePrompts(IEditorPromptService prompts)
        {
            _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
        }

        public Task<string?> PromptForTextAsync(
            string headline, string message, string initialValue, string confirmLabel) =>
            _prompts.PromptForTextAsync(headline, message, initialValue, confirmLabel);

        public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
            _prompts.ConfirmDestructiveAsync(headline, message, confirmLabel);
    }
}
